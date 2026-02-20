// ═══════════════════════════════════════════════════════════
// VentaProductoService.cs — Implementación POS (Tienda)
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class VentaProductoService : IVentaProductoService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogService _logService;
    private readonly IReciboService _reciboService;

    public VentaProductoService(
        ApplicationDbContext context,
        ILogService logService,
        IReciboService reciboService)
    {
        _context = context;
        _logService = logService;
        _reciboService = reciboService;
    }

    public async Task<(bool success, string message, Guid? ordenId, Guid? reciboId)> RegistrarVentaAsync(
        Guid negocioId, string nombreCliente, string emailCliente,
        List<DetalleOrdenVentaDto> items, decimal descuentoAdicional, decimal porcentajeIva)
    {
        if (items == null || !items.Any()) return (false, "La orden no contiene productos", null, null);

        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.NegocioId == negocioId);
        if (negocio == null) return (false, "Negocio no encontrado", null, null);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var numReciboStr = await _reciboService.ObtenerSiguienteNumeroAsync(negocioId);

            var orden = new OrdenVenta
            {
                OrdenVentaId = Guid.NewGuid(),
                NegocioId = negocioId,
                NombreCliente = nombreCliente,
                FechaCreacion = TimeHelper.Now,
                NumeroOrden = int.Parse(numReciboStr),
                PorcentajeIva = porcentajeIva,
                GastosAdicionales = 0,
                Detalles = new List<DetalleOrdenVenta>()
            };

            decimal subtotalGeneral = 0;

            // Precargar todos los productos de la orden en una sola query
            var productoIds = items.Select(i => i.ProductoId).ToList();
            var productos = await _context.Productos
                .Where(p => productoIds.Contains(p.ProductoId) && p.NegocioId == negocioId && p.IsActive)
                .ToListAsync();

            foreach (var item in items)
            {
                if (item.Cantidad <= 0) continue;

                var producto = productos.FirstOrDefault(p => p.ProductoId == item.ProductoId);
                if (producto == null) return (false, $"Producto {item.ProductoId} no encontrado", null, null);
                if (producto.Stock < item.Cantidad) return (false, $"Stock insuficiente de {producto.Nombre}", null, null);

                var stockAnterior = producto.Stock;
                producto.Stock -= item.Cantidad;
                producto.FechaDeActualizacion = TimeHelper.Now;

                var subtotalProd = producto.PrecioVenta * item.Cantidad;
                subtotalGeneral += subtotalProd;

                var detalle = new DetalleOrdenVenta
                {
                    DetalleId = Guid.NewGuid(),
                    OrdenVentaId = orden.OrdenVentaId,
                    ProductoId = producto.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = producto.PrecioVenta,
                    Subtotal = subtotalProd
                };
                orden.Detalles.Add(detalle);

                _context.MovimientosInventario.Add(new MovimientoInventario
                {
                    MovimientoId = Guid.NewGuid(),
                    NegocioId = negocioId,
                    ProductoId = producto.ProductoId,
                    NombreProducto = producto.Nombre,
                    Tipo = "venta_tienda",
                    Cantidad = item.Cantidad,
                    PrecioUnitario = producto.PrecioVenta,
                    Total = subtotalProd,
                    StockAnterior = stockAnterior,
                    StockNuevo = producto.Stock,
                    Fecha = TimeHelper.Now
                });

                _context.Update(producto);
            }

            // Calcular IVA y total
            var montoIva = subtotalGeneral * (porcentajeIva / 100m);
            orden.Subtotal = subtotalGeneral;
            orden.MontoIva = montoIva;
            orden.Total = subtotalGeneral + montoIva - descuentoAdicional;

            _context.OrdenesVenta.Add(orden);
            await _context.SaveChangesAsync();

            // Crear el Recibo con diseño profesional
            await _reciboService.CrearReciboAsync(
                negocioId: negocioId,
                numeroRecibo: numReciboStr,
                tipoRecibo: "Venta Tienda",
                destinatarioEmail: emailCliente ?? "",
                destinatarioNombre: nombreCliente ?? "Cliente de Mostrador",
                negocioNombre: negocio.NegocioNombre ?? "Tienda",
                concepto: $"Venta POS - Orden #{orden.NumeroOrden}",
                monto: orden.Total,
                contenidoHtml: GenerarHtmlReciboTienda(orden, negocio, productos, descuentoAdicional)
            );

            Guid? reciboId = null;
            var reciboDb = await _context.Recibos.FirstOrDefaultAsync(r => r.NegocioId == negocioId && r.NumeroRecibo == orden.NumeroOrden);
            if (reciboDb != null)
            {
                reciboId = reciboDb.ReciboId;
                orden.ReciboId = reciboDb.ReciboId;
                _context.Update(orden);
                await _context.SaveChangesAsync();
            }

            await _logService.CreateLogAsync(negocioId, "venta_tienda_pos",
                $"Venta POS Orden #{orden.NumeroOrden}, Cliente: {nombreCliente}, Total: ${orden.Total:F2}",
                orden.Total);

            await transaction.CommitAsync();
            return (true, "Venta procesada exitosamente", orden.OrdenVentaId, reciboId);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return (false, "El stock de uno de los productos cambió durante la transacción. Por favor, reintente.", null, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Error general: {ex.Message}", null, null);
        }
    }

    public async Task<OrdenVenta> GetOrdenVentaAsync(Guid ordenVentaId, Guid negocioId)
    {
        return await _context.OrdenesVenta
            .Include(o => o.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(o => o.Recibo)
            .FirstOrDefaultAsync(o => o.OrdenVentaId == ordenVentaId && o.NegocioId == negocioId);
    }

    public async Task<List<OrdenVenta>> GetOrdenesVentaAsync(Guid negocioId)
    {
        return await _context.OrdenesVenta
            .Include(o => o.Detalles)
            .Where(o => o.NegocioId == negocioId)
            .OrderByDescending(o => o.FechaCreacion)
            .ToListAsync();
    }

    private string GenerarHtmlReciboTienda(OrdenVenta orden, Gym negocio, List<Producto> productos, decimal descuento)
    {
        Func<string, string> enc = System.Net.WebUtility.HtmlEncode;
        var nombreNeg = enc(negocio.NegocioNombre ?? "Tienda");
        var nombreCli = enc(orden.NombreCliente ?? "Mostrador");

        var itemsHtml = "";
        var altRow = false;
        foreach (var det in orden.Detalles)
        {
            var nombre = enc(productos.FirstOrDefault(p => p.ProductoId == det.ProductoId)?.Nombre ?? "Producto");
            var bg = altRow ? "background: #f8f9fa;" : "";
            itemsHtml += $@"<tr style='{bg}'>
                <td style='padding: 10px 12px; color: #333;'>{nombre}</td>
                <td style='padding: 10px 12px; color: #333; text-align: center;'>{det.Cantidad}</td>
                <td style='padding: 10px 12px; color: #333; text-align: right;'>${det.PrecioUnitario:F2}</td>
                <td style='padding: 10px 12px; color: #333; text-align: right;'>${det.Subtotal:F2}</td>
            </tr>";
            altRow = !altRow;
        }

        var descuentoHtml = descuento > 0
            ? $"<tr><td style='padding: 10px 12px; color: #F1416C; font-weight: bold;' colspan='3'>Descuento</td><td style='padding: 10px 12px; color: #F1416C; font-weight: bold; text-align: right;'>-${descuento:F2}</td></tr>"
            : "";

        var ivaHtml = orden.PorcentajeIva > 0
            ? $"<tr style='background: #f8f9fa;'><td style='padding: 10px 12px; color: #666;' colspan='3'>IVA ({orden.PorcentajeIva}%)</td><td style='padding: 10px 12px; color: #333; text-align: right;'>${orden.MontoIva:F2}</td></tr>"
            : "";

        return $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<style>body{{margin:0;padding:0;font-family:Arial,sans-serif;background:#f5f5f5;}}</style>
</head><body>
<div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #3E97FF, #1B74E4); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 24px;'>Recibo de Venta</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0; font-size: 15px;'>{nombreNeg}</p>
        <p style='color: rgba(255,255,255,0.75); margin: 6px 0 0; font-size: 13px;'>#{orden.NumeroOrden:D6}</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none;'>
        <table style='width: 100%; border-collapse: collapse; font-size: 14px; margin-bottom: 16px;'>
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Cliente</td>
                <td style='padding: 10px 12px; color: #333;'>{nombreCli}</td>
            </tr>
            <tr>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Fecha</td>
                <td style='padding: 10px 12px; color: #333;'>{orden.FechaCreacion:dd/MM/yyyy HH:mm}</td>
            </tr>
        </table>

        <table style='width: 100%; border-collapse: collapse; font-size: 14px;'>
            <tr style='background: #3E97FF; color: white;'>
                <th style='padding: 10px 12px; text-align: left; border-radius: 6px 0 0 0;'>Producto</th>
                <th style='padding: 10px 12px; text-align: center;'>Cant.</th>
                <th style='padding: 10px 12px; text-align: right;'>Precio</th>
                <th style='padding: 10px 12px; text-align: right; border-radius: 0 6px 0 0;'>Subtotal</th>
            </tr>
            {itemsHtml}
            <tr style='border-top: 1px solid #e0e0e0;'>
                <td style='padding: 10px 12px; color: #666;' colspan='3'>Subtotal</td>
                <td style='padding: 10px 12px; color: #333; text-align: right; font-weight: bold;'>${orden.Subtotal:F2}</td>
            </tr>
            {ivaHtml}
            {descuentoHtml}
            <tr style='border-top: 2px solid #3E97FF;'>
                <td style='padding: 14px 12px; color: #3E97FF; font-weight: bold; font-size: 17px;' colspan='3'>TOTAL</td>
                <td style='padding: 14px 12px; color: #3E97FF; font-weight: bold; font-size: 17px; text-align: right;'>${orden.Total:F2}</td>
            </tr>
        </table>

        <p style='color: #888; font-size: 12px; margin: 24px 0 5px; text-align: center;'>
            Este documento NO es una factura legal. Si necesita una factura, contacte directamente al negocio.
        </p>
        <p style='color: #999; font-size: 11px; margin: 0; text-align: center;'>
            Generado automaticamente
        </p>
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px; border-radius: 0 0 12px 12px;'>
        My-Negocio &mdash; Gestion inteligente para tu negocio
    </div>
</div>
</body></html>";
    }
}
