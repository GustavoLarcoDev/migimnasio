using ClosedXML.Excel;
using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class InventarioService : IInventarioService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogService _logService;

    public InventarioService(ApplicationDbContext context, ILogService logService)
    {
        _context = context;
        _logService = logService;
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE PRODUCTOS
    // ═══════════════════════════════════════════════════════════

    public async Task<object> GetProductosAsync(Guid negocioId)
    {
        return await _context.Productos
            .Where(p => p.NegocioId == negocioId && p.IsActive)
            .OrderBy(p => p.Nombre)
            .Select(p => new
            {
                p.ProductoId,
                p.Nombre,
                p.PrecioVenta,
                p.CostoCompra,
                p.Stock,
                p.StockMinimo,
                p.FechaCreacion,
                MargenUnitario = p.PrecioVenta - p.CostoCompra,
                StockBajo = p.Stock <= p.StockMinimo
            })
            .ToListAsync();
    }

    public async Task<Producto> GetProductoAsync(Guid id, Guid negocioId)
    {
        return await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == id && p.NegocioId == negocioId && p.IsActive);
    }

    public async Task<(bool success, string message)> CrearProductoAsync(ProductoCreateDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Nombre))
            return (false, "El nombre del producto es obligatorio");
        if (model.PrecioVenta <= 0)
            return (false, "El precio de venta debe ser mayor a 0");
        if (model.CostoCompra <= 0)
            return (false, "El costo de compra debe ser mayor a 0");
        if (model.Stock < 0)
            return (false, "El stock no puede ser negativo");

        var producto = new Producto
        {
            ProductoId = Guid.NewGuid(),
            NegocioId = model.NegocioId,
            Nombre = model.Nombre,
            PrecioVenta = model.PrecioVenta,
            CostoCompra = model.CostoCompra,
            Stock = model.Stock,
            StockMinimo = model.StockMinimo,
            FechaCreacion = DateTime.Now,
            FechaDeActualizacion = DateTime.Now
        };

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        await _logService.CreateLogAsync(model.NegocioId, "producto_creado",
            $"Producto creado: {model.Nombre}, stock inicial: {model.Stock}, precio: ${model.PrecioVenta:F2}, costo: ${model.CostoCompra:F2}",
            0);

        return (true, "Producto creado exitosamente");
    }

    public async Task<(bool success, string message)> EditarProductoAsync(ProductoCreateDto model)
    {
        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == model.ProductoId && p.NegocioId == model.NegocioId && p.IsActive);

        if (producto == null)
            return (false, "Producto no encontrado");

        var cambios = new List<string>();
        if (producto.Nombre != model.Nombre)
            cambios.Add($"nombre: {producto.Nombre} → {model.Nombre}");
        if (producto.PrecioVenta != model.PrecioVenta)
            cambios.Add($"precio venta: ${producto.PrecioVenta:F2} → ${model.PrecioVenta:F2}");
        if (producto.CostoCompra != model.CostoCompra)
            cambios.Add($"costo: ${producto.CostoCompra:F2} → ${model.CostoCompra:F2}");
        if (producto.StockMinimo != model.StockMinimo)
            cambios.Add($"stock mínimo: {producto.StockMinimo} → {model.StockMinimo}");

        if (cambios.Count == 0)
            return (false, "No se detectaron cambios");

        var nombreAnterior = producto.Nombre;
        producto.Nombre = model.Nombre;
        producto.PrecioVenta = model.PrecioVenta;
        producto.CostoCompra = model.CostoCompra;
        producto.StockMinimo = model.StockMinimo;
        producto.FechaDeActualizacion = DateTime.Now;

        _context.Update(producto);
        await _context.SaveChangesAsync();

        await _logService.CreateLogAsync(model.NegocioId, "producto_editado",
            $"Producto {nombreAnterior} actualizado: {string.Join(", ", cambios)}", 0);

        return (true, "Producto actualizado exitosamente");
    }

    public async Task<(bool success, string message)> EliminarProductoAsync(Guid id, Guid negocioId)
    {
        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == id && p.NegocioId == negocioId && p.IsActive);

        if (producto == null)
            return (false, "Producto no encontrado");

        producto.IsActive = false;
        producto.FechaDeActualizacion = DateTime.Now;

        _context.Update(producto);
        await _context.SaveChangesAsync();

        await _logService.CreateLogAsync(negocioId, "producto_eliminado",
            $"Producto eliminado: {producto.Nombre} (stock restante: {producto.Stock})", 0);

        return (true, "Producto eliminado exitosamente");
    }

    // ═══════════════════════════════════════════════════════════
    // MOVIMIENTOS DE INVENTARIO
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool success, string message)> VenderProductoAsync(Guid productoId, Guid negocioId, int cantidad)
    {
        if (cantidad <= 0)
            return (false, "La cantidad debe ser mayor a 0");

        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == productoId && p.NegocioId == negocioId && p.IsActive);

        if (producto == null)
            return (false, "Producto no encontrado");
        if (producto.Stock < cantidad)
            return (false, $"Stock insuficiente. Disponible: {producto.Stock}");

        var stockAnterior = producto.Stock;
        producto.Stock -= cantidad;
        producto.FechaDeActualizacion = DateTime.Now;
        var total = cantidad * producto.PrecioVenta;

        _context.MovimientosInventario.Add(new MovimientoInventario
        {
            MovimientoId = Guid.NewGuid(),
            NegocioId = negocioId,
            ProductoId = productoId,
            NombreProducto = producto.Nombre,
            Tipo = "venta",
            Cantidad = cantidad,
            PrecioUnitario = producto.PrecioVenta,
            Total = total,
            StockAnterior = stockAnterior,
            StockNuevo = producto.Stock,
            Fecha = DateTime.Now
        });

        _context.Update(producto);
        await _context.SaveChangesAsync();

        await _logService.CreateLogAsync(negocioId, "venta_inventario",
            $"Venta inventario: {cantidad}x {producto.Nombre} @ ${producto.PrecioVenta:F2} = ${total:F2}",
            total);

        return (true, $"Venta registrada: {cantidad}x {producto.Nombre} (${total:F2})");
    }

    public async Task<(bool success, string message)> DevolverProductoAsync(Guid productoId, Guid negocioId, int cantidad, string nota)
    {
        if (cantidad <= 0)
            return (false, "La cantidad debe ser mayor a 0");
        if (string.IsNullOrWhiteSpace(nota))
            return (false, "La razón de la devolución es obligatoria");

        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == productoId && p.NegocioId == negocioId && p.IsActive);

        if (producto == null)
            return (false, "Producto no encontrado");

        var stockAnterior = producto.Stock;
        producto.Stock += cantidad;
        producto.FechaDeActualizacion = DateTime.Now;
        var total = cantidad * producto.PrecioVenta;

        _context.MovimientosInventario.Add(new MovimientoInventario
        {
            MovimientoId = Guid.NewGuid(),
            NegocioId = negocioId,
            ProductoId = productoId,
            NombreProducto = producto.Nombre,
            Tipo = "devolucion",
            Cantidad = cantidad,
            PrecioUnitario = producto.PrecioVenta,
            Total = total,
            StockAnterior = stockAnterior,
            StockNuevo = producto.Stock,
            Nota = nota,
            Fecha = DateTime.Now
        });

        _context.Update(producto);
        await _context.SaveChangesAsync();

        await _logService.CreateLogAsync(negocioId, "devolucion_inventario",
            $"Devolución inventario: {cantidad}x {producto.Nombre}, ${total:F2}. Razón: {nota}",
            -total);

        return (true, $"Devolución registrada: {cantidad}x {producto.Nombre}");
    }

    public async Task<(bool success, string message)> RestockAsync(Guid productoId, Guid negocioId, int cantidad, decimal costoTotal)
    {
        if (cantidad <= 0)
            return (false, "La cantidad debe ser mayor a 0");
        if (costoTotal < 0)
            return (false, "El costo no puede ser negativo");

        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == productoId && p.NegocioId == negocioId && p.IsActive);

        if (producto == null)
            return (false, "Producto no encontrado");

        var stockAnterior = producto.Stock;
        producto.Stock += cantidad;
        producto.FechaDeActualizacion = DateTime.Now;
        var costoUnitario = costoTotal / cantidad;

        _context.MovimientosInventario.Add(new MovimientoInventario
        {
            MovimientoId = Guid.NewGuid(),
            NegocioId = negocioId,
            ProductoId = productoId,
            NombreProducto = producto.Nombre,
            Tipo = "restock",
            Cantidad = cantidad,
            PrecioUnitario = costoUnitario,
            Total = costoTotal,
            StockAnterior = stockAnterior,
            StockNuevo = producto.Stock,
            Fecha = DateTime.Now
        });

        _context.Update(producto);
        await _context.SaveChangesAsync();

        await _logService.CreateLogAsync(negocioId, "restock_inventario",
            $"Restock inventario: +{cantidad} {producto.Nombre}, costo ${costoTotal:F2}",
            -costoTotal);

        return (true, $"Restock registrado: +{cantidad} {producto.Nombre}");
    }

    public async Task<(bool success, string message)> AjustarStockAsync(Guid productoId, Guid negocioId, int stockReal, string nota)
    {
        if (stockReal < 0)
            return (false, "El stock real no puede ser negativo");
        if (string.IsNullOrWhiteSpace(nota))
            return (false, "La razón del ajuste es obligatoria");

        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == productoId && p.NegocioId == negocioId && p.IsActive);

        if (producto == null)
            return (false, "Producto no encontrado");

        var diferencia = stockReal - producto.Stock;
        if (diferencia == 0)
            return (false, "El stock real es igual al stock actual, no hay ajuste necesario");

        var stockAnterior = producto.Stock;
        producto.Stock = stockReal;
        producto.FechaDeActualizacion = DateTime.Now;

        _context.MovimientosInventario.Add(new MovimientoInventario
        {
            MovimientoId = Guid.NewGuid(),
            NegocioId = negocioId,
            ProductoId = productoId,
            NombreProducto = producto.Nombre,
            Tipo = "ajuste",
            Cantidad = Math.Abs(diferencia),
            PrecioUnitario = producto.PrecioVenta,
            Total = Math.Abs(diferencia) * producto.PrecioVenta,
            StockAnterior = stockAnterior,
            StockNuevo = stockReal,
            Nota = nota,
            Fecha = DateTime.Now
        });

        _context.Update(producto);
        await _context.SaveChangesAsync();

        var tipoAjuste = diferencia > 0 ? "incremento" : "reducción";
        await _logService.CreateLogAsync(negocioId, "ajuste_inventario",
            $"Ajuste inventario ({tipoAjuste}): {producto.Nombre}, {stockAnterior} → {stockReal} ({diferencia:+#;-#}). Razón: {nota}",
            0);

        return (true, $"Stock ajustado: {producto.Nombre} ahora tiene {stockReal} unidades ({diferencia:+#;-#})");
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    public async Task<object> GetMovimientosAsync(Guid negocioId)
    {
        return await _context.MovimientosInventario
            .Where(m => m.NegocioId == negocioId)
            .OrderByDescending(m => m.Fecha)
            .Select(m => new
            {
                m.MovimientoId,
                m.ProductoId,
                m.NombreProducto,
                m.Tipo,
                m.Cantidad,
                m.PrecioUnitario,
                m.Total,
                m.StockAnterior,
                m.StockNuevo,
                m.Nota,
                m.Fecha
            })
            .ToListAsync();
    }

    public async Task<object> GetInventarioStatsAsync(Guid negocioId)
    {
        var productos = await _context.Productos
            .Where(p => p.NegocioId == negocioId && p.IsActive)
            .ToListAsync();

        var hoy = DateTime.Now.Date;
        var movimientosHoy = await _context.MovimientosInventario
            .Where(m => m.NegocioId == negocioId && m.Fecha.Date == hoy)
            .ToListAsync();

        return new
        {
            totalProductos = productos.Count,
            stockBajo = productos.Count(p => p.Stock <= p.StockMinimo),
            ventasHoyUnidades = movimientosHoy.Where(m => m.Tipo == "venta").Sum(m => m.Cantidad),
            ingresosHoy = movimientosHoy.Where(m => m.Tipo == "venta").Sum(m => m.Total)
                         - movimientosHoy.Where(m => m.Tipo == "devolucion").Sum(m => m.Total),
            devolucionesHoy = movimientosHoy.Where(m => m.Tipo == "devolucion").Sum(m => m.Total),
            valorInventario = productos.Sum(p => p.Stock * p.CostoCompra)
        };
    }

    // ═══════════════════════════════════════════════════════════
    // EXPORTACION EXCEL
    // ═══════════════════════════════════════════════════════════

    public async Task<byte[]> ExportInventarioExcelAsync(Guid negocioId)
    {
        var productos = await _context.Productos
            .Where(p => p.NegocioId == negocioId && p.IsActive)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var movimientos = await _context.MovimientosInventario
            .Where(m => m.NegocioId == negocioId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();

        using var workbook = new XLWorkbook();

        // Hoja 1: Productos
        var wsProductos = workbook.Worksheets.Add("Productos");
        wsProductos.Cell(1, 1).Value = "Producto";
        wsProductos.Cell(1, 2).Value = "Precio Venta";
        wsProductos.Cell(1, 3).Value = "Costo";
        wsProductos.Cell(1, 4).Value = "Stock";
        wsProductos.Cell(1, 5).Value = "Stock Mínimo";
        wsProductos.Cell(1, 6).Value = "Margen";

        var headerRange1 = wsProductos.Range(1, 1, 1, 6);
        headerRange1.Style.Font.Bold = true;
        headerRange1.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
        headerRange1.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var p in productos)
        {
            wsProductos.Cell(row, 1).Value = p.Nombre;
            wsProductos.Cell(row, 2).Value = p.PrecioVenta;
            wsProductos.Cell(row, 3).Value = p.CostoCompra;
            wsProductos.Cell(row, 4).Value = p.Stock;
            wsProductos.Cell(row, 5).Value = p.StockMinimo;
            wsProductos.Cell(row, 6).Value = p.PrecioVenta - p.CostoCompra;

            if (p.Stock <= p.StockMinimo)
                wsProductos.Cell(row, 4).Style.Font.FontColor = XLColor.Red;

            row++;
        }
        wsProductos.Columns().AdjustToContents();

        // Hoja 2: Movimientos
        var wsMov = workbook.Worksheets.Add("Movimientos");
        wsMov.Cell(1, 1).Value = "Fecha";
        wsMov.Cell(1, 2).Value = "Producto";
        wsMov.Cell(1, 3).Value = "Tipo";
        wsMov.Cell(1, 4).Value = "Cantidad";
        wsMov.Cell(1, 5).Value = "Precio Unit.";
        wsMov.Cell(1, 6).Value = "Total";
        wsMov.Cell(1, 7).Value = "Stock Antes";
        wsMov.Cell(1, 8).Value = "Stock Después";
        wsMov.Cell(1, 9).Value = "Nota";

        var headerRange2 = wsMov.Range(1, 1, 1, 9);
        headerRange2.Style.Font.Bold = true;
        headerRange2.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
        headerRange2.Style.Font.FontColor = XLColor.White;

        row = 2;
        foreach (var m in movimientos)
        {
            wsMov.Cell(row, 1).Value = m.Fecha.ToString("dd/MM/yyyy HH:mm");
            wsMov.Cell(row, 2).Value = m.NombreProducto;
            wsMov.Cell(row, 3).Value = m.Tipo;
            wsMov.Cell(row, 4).Value = m.Cantidad;
            wsMov.Cell(row, 5).Value = m.PrecioUnitario;
            wsMov.Cell(row, 6).Value = m.Total;
            wsMov.Cell(row, 7).Value = m.StockAnterior;
            wsMov.Cell(row, 8).Value = m.StockNuevo;
            wsMov.Cell(row, 9).Value = m.Nota ?? "";
            row++;
        }
        wsMov.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
