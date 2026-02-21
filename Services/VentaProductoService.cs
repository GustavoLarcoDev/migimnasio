// ═══════════════════════════════════════════════════════════════════════════════
// VentaProductoService.cs — Servicio del Punto de Venta (POS) para modelo Tienda
//
// RESPONSABILIDADES:
//   - Procesar ventas completas con multiples productos en una sola orden
//   - Descontar stock de cada producto vendido (con validacion de disponibilidad)
//   - Calcular subtotal, IVA y descuentos
//   - Generar recibo HTML profesional automaticamente
//   - Registrar movimientos de inventario y logs financieros
//   - Consultar historial de ordenes de venta
//
// FLUJO DE UNA VENTA POS:
//   1. El cajero agrega productos al carrito en el frontend
//   2. Se envia la orden completa a RegistrarVentaAsync
//   3. Se abre una transaccion para garantizar atomicidad
//   4. Por cada producto: validar stock -> descontar -> crear detalle y movimiento
//   5. Calcular totales con IVA y descuento
//   6. Generar recibo HTML y vincularlo a la orden
//   7. Registrar log financiero
//   8. Commit de la transaccion (todo o nada)
//
// CONCURRENCIA:
//   Si dos cajeros intentan vender el ultimo stock simultaneamente,
//   DbUpdateConcurrencyException se captura y se hace rollback completo.
// ═══════════════════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════════════
    // REGISTRO DE VENTA POS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Procesa una venta completa desde el POS dentro de una transaccion.
    /// Devuelve una tupla con el resultado, IDs de la orden y del recibo generado.
    /// </summary>
    public async Task<(bool success, string message, Guid? ordenId, Guid? reciboId)> RegistrarVentaAsync(
        Guid negocioId, string nombreCliente, string emailCliente,
        List<DetalleOrdenVentaDto> items, decimal descuentoAdicional, decimal porcentajeIva,
        string tipoOrden = "local", Guid? mesaId = null, Guid? empleadoId = null, string direccionEntrega = null)
    {
        if (items == null || !items.Any()) return (false, "La orden no contiene productos", null, null);

        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.NegocioId == negocioId);
        if (negocio == null) return (false, "Negocio no encontrado", null, null);

        // Transaccion: si algo falla, se revierte todo (stock, orden, recibo, movimientos)
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Obtener numero secuencial para el recibo
            var numReciboStr = await _reciboService.ObtenerSiguienteNumeroAsync(negocioId);

            // Crear la orden de venta (cabecera)
            var orden = new OrdenVenta
            {
                OrdenVentaId = Guid.NewGuid(),
                NegocioId = negocioId,
                NombreCliente = nombreCliente,
                FechaCreacion = TimeHelper.Now,
                NumeroOrden = int.Parse(numReciboStr),
                PorcentajeIva = porcentajeIva,
                GastosAdicionales = 0,
                TipoOrden = tipoOrden ?? "local",
                MesaId = mesaId,
                EmpleadoId = empleadoId,
                DireccionEntrega = direccionEntrega,
                Detalles = new List<DetalleOrdenVenta>()
            };

            decimal subtotalGeneral = 0;

            // Precargar todos los productos en una sola query (evita N+1)
            var productoIds = items.Select(i => i.ProductoId).ToList();
            var productos = await _context.Productos
                .Where(p => productoIds.Contains(p.ProductoId) && p.NegocioId == negocioId && p.IsActive)
                .ToListAsync();

            // Procesar cada item del carrito
            foreach (var item in items)
            {
                if (item.Cantidad <= 0) continue;

                var producto = productos.FirstOrDefault(p => p.ProductoId == item.ProductoId);
                if (producto == null) return (false, $"Producto {item.ProductoId} no encontrado", null, null);

                // Validar stock disponible antes de descontar
                if (producto.Stock < item.Cantidad) return (false, $"Stock insuficiente de {producto.Nombre}", null, null);

                // Descontar stock
                var stockAnterior = producto.Stock;
                producto.Stock -= item.Cantidad;
                producto.FechaDeActualizacion = TimeHelper.Now;

                // Calcular subtotal del producto (precio unitario x cantidad)
                var subtotalProd = producto.PrecioVenta * item.Cantidad;
                subtotalGeneral += subtotalProd;

                // Crear linea de detalle de la orden
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

                // Registrar movimiento de inventario con snapshot de precios del momento
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

            // Calcular IVA y total final de la orden
            var montoIva = subtotalGeneral * (porcentajeIva / 100m);
            orden.Subtotal = subtotalGeneral;
            orden.MontoIva = montoIva;
            orden.Total = subtotalGeneral + montoIva - descuentoAdicional;

            _context.OrdenesVenta.Add(orden);
            await _context.SaveChangesAsync();

            // Si la orden tiene mesa asignada (restaurante local), marcarla como ocupada
            if (orden.MesaId.HasValue)
            {
                var mesa = await _context.Mesas.FirstOrDefaultAsync(m => m.MesaId == orden.MesaId.Value && m.NegocioId == negocioId);
                if (mesa != null)
                {
                    mesa.Estado = "ocupada";
                    await _context.SaveChangesAsync();
                }
            }

            // Generar recibo HTML profesional y guardarlo en la BD
            var esRestaurante = negocio.TipoNegocio == "restaurante";
            await _reciboService.CrearReciboAsync(
                negocioId: negocioId,
                numeroRecibo: numReciboStr,
                tipoRecibo: esRestaurante ? "Venta Restaurante" : "Venta Tienda",
                destinatarioEmail: emailCliente ?? "",
                destinatarioNombre: nombreCliente ?? "Cliente de Mostrador",
                negocioNombre: negocio.NegocioNombre ?? (esRestaurante ? "Restaurante" : "Tienda"),
                concepto: $"Venta POS - Orden #{orden.NumeroOrden}",
                monto: orden.Total,
                contenidoHtml: GenerarHtmlReciboTienda(orden, negocio, productos, descuentoAdicional)
            );

            // Vincular el recibo recien creado con la orden de venta
            Guid? reciboId = null;
            var reciboDb = await _context.Recibos.FirstOrDefaultAsync(r => r.NegocioId == negocioId && r.NumeroRecibo == orden.NumeroOrden);
            if (reciboDb != null)
            {
                reciboId = reciboDb.ReciboId;
                orden.ReciboId = reciboDb.ReciboId;
                _context.Update(orden);
                await _context.SaveChangesAsync();
            }

            // Registrar log financiero de la venta (ingreso positivo)
            await _logService.CreateLogAsync(negocioId, "venta_tienda_pos",
                $"Venta POS Orden #{orden.NumeroOrden}, Cliente: {nombreCliente}, Total: ${orden.Total:F2}",
                orden.Total);

            await transaction.CommitAsync();
            return (true, "Venta procesada exitosamente", orden.OrdenVentaId, reciboId);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otro usuario modifico el stock durante la transaccion
            await transaction.RollbackAsync();
            return (false, "El stock de uno de los productos cambió durante la transacción. Por favor, reintente.", null, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Error general: {ex.Message}", null, null);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CONSULTAS DE ORDENES
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene una orden de venta con todos sus detalles, productos y recibo asociado.
    /// </summary>
    public async Task<OrdenVenta> GetOrdenVentaAsync(Guid ordenVentaId, Guid negocioId)
    {
        return await _context.OrdenesVenta
            .Include(o => o.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(o => o.Recibo)
            .FirstOrDefaultAsync(o => o.OrdenVentaId == ordenVentaId && o.NegocioId == negocioId);
    }

    /// <summary>
    /// Obtiene el listado de todas las ordenes del negocio, ordenadas de la mas reciente a la mas antigua.
    /// </summary>
    public async Task<List<OrdenVenta>> GetOrdenesVentaAsync(Guid negocioId)
    {
        return await _context.OrdenesVenta
            .Include(o => o.Detalles)
            .Where(o => o.NegocioId == negocioId)
            .OrderByDescending(o => o.FechaCreacion)
            .ToListAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GENERACION DE RECIBO HTML
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Genera el HTML del recibo de venta con diseno profesional.
    /// Incluye: encabezado con nombre del negocio, tabla de productos,
    /// subtotal, IVA, descuento y total. Los valores se escapan con HtmlEncode
    /// para prevenir XSS.
    /// </summary>
    private string GenerarHtmlReciboTienda(OrdenVenta orden, Gym negocio, List<Producto> productos, decimal descuento)
    {
        // Funcion auxiliar para escapar HTML y prevenir XSS
        Func<string, string> enc = System.Net.WebUtility.HtmlEncode;
        var nombreNeg = enc(negocio.NegocioNombre ?? "Tienda");
        var nombreCli = enc(orden.NombreCliente ?? "Mostrador");

        // Construir filas de la tabla de productos con colores alternados e imágenes
        var itemsHtml = "";
        var altRow = false;
        foreach (var det in orden.Detalles)
        {
            var prod = productos.FirstOrDefault(p => p.ProductoId == det.ProductoId);
            var nombre = enc(prod?.Nombre ?? "Producto");
            var bg = altRow ? "background: #f8f9fa;" : "";

            // Imagen del producto (Base64 data URI) o placeholder
            var imgHtml = "";
            if (!string.IsNullOrEmpty(prod?.ImagenUrl))
                imgHtml = $"<img src='{prod.ImagenUrl}' style='width:40px;height:40px;object-fit:cover;border-radius:6px;margin-right:10px;vertical-align:middle;' alt=''>";
            else
                imgHtml = "<div style='display:inline-block;width:40px;height:40px;border-radius:6px;background:#e9ecef;vertical-align:middle;margin-right:10px;text-align:center;line-height:40px;color:#adb5bd;font-size:18px;'>&#128722;</div>";

            itemsHtml += $@"<tr style='{bg}'>
                <td style='padding: 10px 12px; color: #333;'>{imgHtml}<span style='vertical-align:middle;'>{nombre}</span></td>
                <td style='padding: 10px 12px; color: #333; text-align: center;'>{det.Cantidad}</td>
                <td style='padding: 10px 12px; color: #333; text-align: right;'>${det.PrecioUnitario:F2}</td>
                <td style='padding: 10px 12px; color: #333; text-align: right;'>${det.Subtotal:F2}</td>
            </tr>";
            altRow = !altRow;
        }

        // Fila de descuento (solo si aplica)
        var descuentoHtml = descuento > 0
            ? $"<tr><td style='padding: 10px 12px; color: #F1416C; font-weight: bold;' colspan='3'>Descuento</td><td style='padding: 10px 12px; color: #F1416C; font-weight: bold; text-align: right;'>-${descuento:F2}</td></tr>"
            : "";

        // Fila de IVA (solo si el porcentaje es mayor a 0)
        var ivaHtml = orden.PorcentajeIva > 0
            ? $"<tr style='background: #f8f9fa;'><td style='padding: 10px 12px; color: #666;' colspan='3'>IVA ({orden.PorcentajeIva}%)</td><td style='padding: 10px 12px; color: #333; text-align: right;'>${orden.MontoIva:F2}</td></tr>"
            : "";

        // Plantilla HTML completa del recibo con estilos inline (compatible con email)
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
