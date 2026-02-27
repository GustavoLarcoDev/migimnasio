// ═══════════════════════════════════════════════════════════════════════════
// InventarioService.cs — Implementación del servicio de gestión de inventario
//
// RESPONSABILIDADES:
//   - CRUD de productos (alta, consulta, edición, baja lógica)
//   - Cuatro tipos de movimiento de stock:
//       "venta"      → reduce stock, registra ingreso financiero
//       "devolucion" → incrementa stock, registra gasto (devolución de dinero al cliente)
//       "restock"    → incrementa stock, registra gasto (compra a proveedor)
//       "ajuste"     → corrige stock sin impacto financiero (conteo físico)
//   - Estadísticas del dashboard (totales del día)
//   - Exportación a Excel de dos hojas (ClosedXML)
//
// CONCURRENCIA:
//   Todos los métodos que modifican stock capturan DbUpdateConcurrencyException.
//   Esto protege contra el escenario donde dos empleados venden el mismo
//   producto al mismo tiempo y el stock quedaría negativo sin esta protección.
//
// TRAZABILIDAD:
//   Cada operación de stock genera dos registros:
//     1. Un MovimientoInventario (tabla permanente para consulta de historial)
//     2. Un Log (tabla de auditoría general del negocio con el monto financiero)
//   Esto crea un rastro inmutable: nunca se borra, solo se agrega.
// ═══════════════════════════════════════════════════════════════════════════

using ClosedXML.Excel;
using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Servicio de gestión de inventario de productos.
/// Maneja el ciclo de vida completo de un producto: desde su alta hasta
/// cada movimiento de stock que afecta la cantidad disponible.
/// </summary>
public class InventarioService : IInventarioService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogService _logService;

    /// <summary>
    /// El constructor recibe los servicios por inyección de dependencia.
    /// <paramref name="context"/> es el acceso a la base de datos (EF Core).
    /// <paramref name="logService"/> registra eventos de auditoría del negocio.
    /// </summary>
    public InventarioService(ApplicationDbContext context, ILogService logService)
    {
        _context = context;
        _logService = logService;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CRUD DE PRODUCTOS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los productos activos del negocio con campos calculados.
    /// Se proyecta a tipo anónimo en lugar de devolver la entidad completa para:
    ///   - Evitar serializar propiedades innecesarias al JSON
    ///   - Calcular MargenUnitario y StockBajo directamente en la consulta SQL
    /// </summary>
    public async Task<object> GetProductosAsync(Guid negocioId)
    {
        return await _context.Productos.AsNoTracking()
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
                // MargenUnitario: cuánto gana el negocio por cada unidad vendida
                MargenUnitario = p.PrecioVenta - p.CostoCompra,
                // StockBajo: true cuando hay que reordenar mercancía (llega al límite mínimo)
                StockBajo = p.Stock <= p.StockMinimo,
                CategoriaProductoId = p.CategoriaProductoId,
                p.ImagenUrl,
                p.Receta
            })
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un producto específico por su ID.
    /// El filtro doble (ProductoId + NegocioId + IsActive) garantiza que:
    ///   1. El producto pertenezca al negocio del usuario autenticado (seguridad)
    ///   2. No se devuelvan productos eliminados lógicamente
    /// </summary>
    public async Task<Producto> GetProductoAsync(Guid id, Guid negocioId)
    {
        return await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == id && p.NegocioId == negocioId && p.IsActive);
    }

    /// <summary>
    /// Crea un nuevo producto con validación exhaustiva de todos sus campos.
    /// Las validaciones se hacen en el servicio (no solo en el modelo) para
    /// que los mensajes de error lleguen exactamente como están escritos aquí
    /// al usuario final sin necesidad de configurar atributos de anotación de datos.
    /// </summary>
    public async Task<(bool success, string message, Guid? dataId)> CrearProductoAsync(ProductoCreateDto model)
    {
        // Validaciones de negocio — no dependen de la base de datos, son rápidas
        if (string.IsNullOrWhiteSpace(model.Nombre))
            return (false, "El nombre del producto es obligatorio", null);
        if (model.PrecioVenta <= 0)
            return (false, "El precio de venta debe ser mayor a 0", null);
        if (model.CostoCompra <= 0)
            return (false, "El costo de compra debe ser mayor a 0", null);
        if (model.Stock < 0)
            return (false, "El stock no puede ser negativo", null);

        var producto = new Producto
        {
            ProductoId        = Guid.NewGuid(),
            NegocioId         = model.NegocioId,
            Nombre            = model.Nombre,
            PrecioVenta       = model.PrecioVenta,
            CostoCompra       = model.CostoCompra,
            Stock             = model.Stock,
            StockMinimo       = model.StockMinimo,
            FechaCreacion     = TimeHelper.Now,
            FechaDeActualizacion = TimeHelper.Now,
            CategoriaProductoId = model.CategoriaProductoId == Guid.Empty ? null : model.CategoriaProductoId,
            ImagenUrl = null,
            Receta = model.Receta
        };

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        // Log con monto = 0 porque crear un producto no es un ingreso/gasto en sí,
        // el gasto ocurrirá cuando se haga el primer restock
        await _logService.CreateLogAsync(model.NegocioId, "producto_creado",
            $"Producto creado: {model.Nombre}, stock inicial: {model.Stock}, precio: ${model.PrecioVenta:F2}, costo: ${model.CostoCompra:F2}",
            0);

        return (true, "Producto creado exitosamente", producto.ProductoId);
    }

    /// <summary>
    /// Edita los metadatos de un producto existente (nombre, precios, stock mínimo, stock).
    /// Si el stock cambió, se crea un movimiento de ajuste automático para mantener
    /// el historial de movimientos íntegro.
    ///
    /// El sistema de diff detecta exactamente qué cambió para escribir un log
    /// legible: "precio venta: $10.00 → $12.00, stock mínimo: 5 → 10".
    /// Si nada cambió, se retorna error temprano para no hacer una escritura
    /// innecesaria a la base de datos.
    /// </summary>
    public async Task<(bool success, string message)> EditarProductoAsync(ProductoCreateDto model)
    {
        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == model.ProductoId && p.NegocioId == model.NegocioId && p.IsActive);

        if (producto == null)
            return (false, "Producto no encontrado");

        // Detectar qué campos cambiaron para el log de auditoría
        var cambios = new List<string>();
        if (producto.Nombre != model.Nombre)
            cambios.Add($"nombre: {producto.Nombre} → {model.Nombre}");
        if (producto.PrecioVenta != model.PrecioVenta)
            cambios.Add($"precio venta: ${producto.PrecioVenta:F2} → ${model.PrecioVenta:F2}");
        if (producto.CostoCompra != model.CostoCompra)
            cambios.Add($"costo: ${producto.CostoCompra:F2} → ${model.CostoCompra:F2}");
        if (producto.StockMinimo != model.StockMinimo)
            cambios.Add($"stock mínimo: {producto.StockMinimo} → {model.StockMinimo}");

        // Detectar cambio de stock — si cambió, crear movimiento de ajuste
        var stockCambio = model.Stock >= 0 && producto.Stock != model.Stock;
        if (stockCambio)
            cambios.Add($"stock: {producto.Stock} → {model.Stock}");

        var nuevaCat = model.CategoriaProductoId == Guid.Empty ? null : model.CategoriaProductoId;
        if (producto.CategoriaProductoId != nuevaCat)
            cambios.Add("categoría actualizada");
        if (producto.Receta != model.Receta)
            cambios.Add("receta actualizada");

        // Si no hubo cambios reales, no vale la pena escribir en la base de datos
        if (cambios.Count == 0)
            return (false, "No se detectaron cambios");

        // Guardamos el nombre anterior para el log (puede haber cambiado)
        var nombreAnterior = producto.Nombre;

        // Si el stock cambió, registrar movimiento de ajuste para mantener audit trail
        if (stockCambio)
        {
            var stockAnterior = producto.Stock;
            var diferencia = model.Stock - producto.Stock;
            producto.Stock = model.Stock;

            _context.MovimientosInventario.Add(new MovimientoInventario
            {
                MovimientoId   = Guid.NewGuid(),
                NegocioId      = model.NegocioId,
                ProductoId     = producto.ProductoId,
                NombreProducto = producto.Nombre,
                Tipo           = "ajuste",
                Cantidad       = Math.Abs(diferencia),
                PrecioUnitario = producto.PrecioVenta,
                Total          = Math.Abs(diferencia) * producto.PrecioVenta,
                StockAnterior  = stockAnterior,
                StockNuevo     = model.Stock,
                Nota           = "Ajuste desde edición de producto",
                Fecha          = TimeHelper.Now
            });
        }

        producto.Nombre              = model.Nombre;
        producto.PrecioVenta         = model.PrecioVenta;
        producto.CostoCompra         = model.CostoCompra;
        producto.StockMinimo         = model.StockMinimo;
        producto.CategoriaProductoId = nuevaCat;
        producto.Receta              = model.Receta;
        producto.FechaDeActualizacion = TimeHelper.Now;

        _context.Update(producto);
        await _context.SaveChangesAsync();

        // El log usa el nombre anterior para que sea legible incluso si el nombre cambió
        await _logService.CreateLogAsync(model.NegocioId, "producto_editado",
            $"Producto {nombreAnterior} actualizado: {string.Join(", ", cambios)}", 0);

        return (true, "Producto actualizado exitosamente");
    }

    /// <summary>
    /// Elimina un producto de forma LÓGICA estableciendo IsActive = false.
    ///
    /// Por qué no se elimina físicamente (DELETE):
    ///   - Los movimientos históricos (ventas, restocks, etc.) referencian el producto
    ///   - Borrar el producto rompería la integridad referencial de los reportes pasados
    ///   - Se mantiene el nombre del producto en MovimientoInventario.NombreProducto
    ///     como snapshot para que los reportes históricos sigan siendo legibles
    ///
    /// El log incluye el stock restante para documentar cuántas unidades quedaron
    /// en bodega al momento de dar de baja el producto.
    /// </summary>
    public async Task<(bool success, string message)> EliminarProductoAsync(Guid id, Guid negocioId)
    {
        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == id && p.NegocioId == negocioId && p.IsActive);

        if (producto == null)
            return (false, "Producto no encontrado");

        // Baja lógica: el registro queda en la base de datos pero invisible en listados
        producto.IsActive             = false;
        producto.FechaDeActualizacion = TimeHelper.Now;

        _context.Update(producto);
        await _context.SaveChangesAsync();

        await _logService.CreateLogAsync(negocioId, "producto_eliminado",
            $"Producto eliminado: {producto.Nombre} (stock restante: {producto.Stock})", 0);

        return (true, "Producto eliminado exitosamente");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MOVIMIENTOS DE INVENTARIO
    //
    // PATRÓN COMPARTIDO DE LOS CUATRO MÉTODOS:
    //   1. Validar parámetros de entrada
    //   2. Buscar el producto (con filtro de negocioId para seguridad)
    //   3. Calcular el nuevo stock (suma o resta según el tipo)
    //   4. Insertar un MovimientoInventario con snapshot del estado antes/después
    //   5. Guardar cambios dentro de un try/catch de concurrencia
    //   6. Registrar un log financiero (positivo=ingreso, negativo=gasto)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registra la venta de unidades de un producto al público.
    /// El stock se REDUCE. El log registra un ingreso positivo.
    ///
    /// Validación de stock antes de la venta:
    ///   Si Stock &lt; cantidad → error inmediato. No se puede vender lo que no hay.
    ///
    /// Protección de concurrencia:
    ///   Si dos empleados venden el mismo producto simultáneamente y EF Core
    ///   detecta que la fila cambió desde que la leímos, lanza DbUpdateConcurrencyException.
    ///   Capturamos esto y pedimos al usuario que recargue, en lugar de dejar que
    ///   el stock quede en un valor incorrecto.
    /// </summary>
    public async Task<(bool success, string message)> VenderProductoAsync(Guid productoId, Guid negocioId, int cantidad, string metodoPago = "Efectivo", string numeroConfirmacion = null)
    {
        if (cantidad <= 0)
            return (false, "La cantidad debe ser mayor a 0");

        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == productoId && p.NegocioId == negocioId && p.IsActive);

        if (producto == null)
            return (false, "Producto no encontrado");

        // Verificar stock antes de comprometerse con la venta
        if (producto.Stock < cantidad)
            return (false, $"Stock insuficiente. Disponible: {producto.Stock}");

        var stockAnterior = producto.Stock;
        producto.Stock -= cantidad;                   // Reducir stock
        producto.FechaDeActualizacion = TimeHelper.Now;
        var total = cantidad * producto.PrecioVenta;  // Ingreso generado por esta venta

        // Snapshot inmutable del movimiento: guarda los precios del MOMENTO de la venta,
        // no una referencia al producto (cuyos precios podrían cambiar en el futuro)
        _context.MovimientosInventario.Add(new MovimientoInventario
        {
            MovimientoId   = Guid.NewGuid(),
            NegocioId      = negocioId,
            ProductoId     = productoId,
            NombreProducto = producto.Nombre,   // Snapshot del nombre en este momento
            Tipo           = "venta",
            Cantidad       = cantidad,
            PrecioUnitario = producto.PrecioVenta,
            Total          = total,
            StockAnterior  = stockAnterior,
            StockNuevo     = producto.Stock,
            MetodoPago     = metodoPago ?? "Efectivo",
            Fecha          = TimeHelper.Now
        });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Update(producto);
            await _context.SaveChangesAsync();

            // El total es positivo porque es un ingreso para el negocio
            var confirmInfo = !string.IsNullOrEmpty(numeroConfirmacion) ? $" (Ref: {numeroConfirmacion})" : "";
            await _logService.CreateLogAsync(negocioId, "venta_inventario",
                $"Venta inventario: {cantidad}x {producto.Nombre} @ ${producto.PrecioVenta:F2} = ${total:F2} [{metodoPago ?? "Efectivo"}]{confirmInfo}",
                total);

            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return (false, "El stock fue modificado por otro usuario. Recarga e intenta de nuevo.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return (true, $"Venta registrada: {cantidad}x {producto.Nombre} (${total:F2})");
    }

    /// <summary>
    /// Registra la devolución de productos por parte de un cliente.
    /// El stock se INCREMENTA (las unidades regresan al inventario).
    /// El log registra un gasto negativo (se devuelve dinero al cliente).
    ///
    /// La nota es obligatoria porque sin ella el historial de auditoría
    /// queda incompleto: no sabremos si fue un producto dañado, un error
    /// de cobro, o un cliente insatisfecho.
    /// </summary>
    public async Task<(bool success, string message)> DevolverProductoAsync(Guid productoId, Guid negocioId, int cantidad, string nota)
    {
        if (cantidad <= 0)
            return (false, "La cantidad debe ser mayor a 0");

        // La nota es obligatoria para tener trazabilidad completa de por qué se devolvió
        if (string.IsNullOrWhiteSpace(nota))
            return (false, "La razón de la devolución es obligatoria");

        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == productoId && p.NegocioId == negocioId && p.IsActive);

        if (producto == null)
            return (false, "Producto no encontrado");

        var stockAnterior = producto.Stock;
        producto.Stock += cantidad;               // Las unidades regresan al inventario
        producto.FechaDeActualizacion = TimeHelper.Now;
        var total = cantidad * producto.PrecioVenta;

        _context.MovimientosInventario.Add(new MovimientoInventario
        {
            MovimientoId   = Guid.NewGuid(),
            NegocioId      = negocioId,
            ProductoId     = productoId,
            NombreProducto = producto.Nombre,
            Tipo           = "devolucion",
            Cantidad       = cantidad,
            PrecioUnitario = producto.PrecioVenta,
            Total          = total,
            StockAnterior  = stockAnterior,
            StockNuevo     = producto.Stock,
            Nota           = nota,  // Razón de la devolución (obligatoria)
            Fecha          = TimeHelper.Now
        });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Update(producto);
            await _context.SaveChangesAsync();

            // El total es NEGATIVO porque es dinero que sale del negocio (se devuelve al cliente)
            await _logService.CreateLogAsync(negocioId, "devolucion_inventario",
                $"Devolución inventario: {cantidad}x {producto.Nombre}, ${total:F2}. Razón: {nota}",
                -total);

            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return (false, "El stock fue modificado por otro usuario. Recarga e intenta de nuevo.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return (true, $"Devolución registrada: {cantidad}x {producto.Nombre}");
    }

    /// <summary>
    /// Registra el reabastecimiento de stock por compra a proveedor.
    /// El stock se INCREMENTA. El log registra un gasto (dinero que sale del negocio).
    ///
    /// El costoTotal es el precio pagado al proveedor por toda la remesa.
    /// El costoUnitario se calcula dividiendo (costoTotal / cantidad) para almacenarlo
    /// en PrecioUnitario del movimiento, facilitando análisis de costo promedio.
    ///
    /// El log usa -costoTotal (negativo) porque representa un egreso del negocio,
    /// no un ingreso. Esto permite que los reportes financieros muestren el saldo neto.
    /// </summary>
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
        producto.Stock += cantidad;              // Las unidades nuevas entran al inventario
        producto.FechaDeActualizacion = TimeHelper.Now;

        // Costo unitario calculado: si compramos 10 unidades por $50, cada una costó $5
        var costoUnitario = costoTotal / cantidad;

        _context.MovimientosInventario.Add(new MovimientoInventario
        {
            MovimientoId   = Guid.NewGuid(),
            NegocioId      = negocioId,
            ProductoId     = productoId,
            NombreProducto = producto.Nombre,
            Tipo           = "restock",
            Cantidad       = cantidad,
            PrecioUnitario = costoUnitario,  // Costo por unidad comprada al proveedor
            Total          = costoTotal,     // Costo total de la compra
            StockAnterior  = stockAnterior,
            StockNuevo     = producto.Stock,
            Fecha          = TimeHelper.Now
            // Nota: no es obligatoria en restock (el proveedor y factura son la documentación)
        });

        try
        {
            _context.Update(producto);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "El stock fue modificado por otro usuario. Recarga e intenta de nuevo.");
        }

        // Log con monto NEGATIVO: el negocio gastó dinero comprando mercancía
        await _logService.CreateLogAsync(negocioId, "restock_inventario",
            $"Restock inventario: +{cantidad} {producto.Nombre}, costo ${costoTotal:F2}",
            -costoTotal);

        return (true, $"Restock registrado: +{cantidad} {producto.Nombre}");
    }

    /// <summary>
    /// Ajusta el stock del sistema para que coincida con el conteo físico real.
    /// Se usa cuando hay discrepancias entre lo que dice el sistema y lo que hay
    /// físicamente en bodega (por pérdidas, daños, hurto o errores de registro).
    ///
    /// A diferencia de los otros movimientos, el ajuste es financieramente neutro
    /// (el log usa monto = 0) porque no es una venta ni una compra.
    ///
    /// El movimiento guarda Math.Abs(diferencia) como Cantidad porque siempre
    /// es positivo; el signo de la diferencia se puede inferir comparando
    /// StockAnterior vs StockNuevo.
    ///
    /// Formato especial del log: "{diferencia:+#;-#}" imprime "+5" o "-3"
    /// con signo explícito para que sea claro si fue un incremento o reducción.
    /// </summary>
    public async Task<(bool success, string message)> AjustarStockAsync(Guid productoId, Guid negocioId, int stockReal, string nota)
    {
        if (stockReal < 0)
            return (false, "El stock real no puede ser negativo");

        // La nota es obligatoria para explicar la razón del ajuste (ej. "producto caducado")
        if (string.IsNullOrWhiteSpace(nota))
            return (false, "La razón del ajuste es obligatoria");

        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.ProductoId == productoId && p.NegocioId == negocioId && p.IsActive);

        if (producto == null)
            return (false, "Producto no encontrado");

        // diferencia positiva = se encontraron más unidades de las que el sistema tenía
        // diferencia negativa = hay menos unidades físicas que las que registra el sistema
        var diferencia = stockReal - producto.Stock;

        // Si no hay diferencia, el sistema ya está correcto — no crear movimiento innecesario
        if (diferencia == 0)
            return (false, "El stock real es igual al stock actual, no hay ajuste necesario");

        var stockAnterior = producto.Stock;
        producto.Stock                = stockReal;  // Sobreescribir con el valor real contado
        producto.FechaDeActualizacion = TimeHelper.Now;

        _context.MovimientosInventario.Add(new MovimientoInventario
        {
            MovimientoId   = Guid.NewGuid(),
            NegocioId      = negocioId,
            ProductoId     = productoId,
            NombreProducto = producto.Nombre,
            Tipo           = "ajuste",
            Cantidad       = Math.Abs(diferencia),        // Siempre positivo; el signo está en StockAnterior vs StockNuevo
            PrecioUnitario = producto.PrecioVenta,
            Total          = Math.Abs(diferencia) * producto.PrecioVenta,  // Valor del ajuste en dinero (para reportes)
            StockAnterior  = stockAnterior,
            StockNuevo     = stockReal,
            Nota           = nota,
            Fecha          = TimeHelper.Now
        });

        try
        {
            _context.Update(producto);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "El stock fue modificado por otro usuario. Recarga e intenta de nuevo.");
        }

        // Texto descriptivo del tipo de ajuste para el log
        var tipoAjuste = diferencia > 0 ? "incremento" : "reducción";

        // El log financiero usa 0 porque un ajuste no es ni ingreso ni gasto
        await _logService.CreateLogAsync(negocioId, "ajuste_inventario",
            $"Ajuste inventario ({tipoAjuste}): {producto.Nombre}, {stockAnterior} → {stockReal} ({diferencia:+#;-#}). Razón: {nota}",
            0);

        return (true, $"Stock ajustado: {producto.Nombre} ahora tiene {stockReal} unidades ({diferencia:+#;-#})");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene el historial completo de movimientos del negocio.
    /// Ordenados del más reciente al más antiguo para que el usuario vea primero
    /// lo que pasó hoy. Se proyecta a tipo anónimo para serialización eficiente.
    /// </summary>
    public async Task<object> GetMovimientosAsync(Guid negocioId)
    {
        return await _context.MovimientosInventario.AsNoTracking()
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
                m.MetodoPago,
                m.Fecha
            })
            .ToListAsync();
    }

    /// <summary>
    /// Calcula estadísticas de inventario para los widgets del dashboard.
    ///
    /// Por qué cargamos todos los productos y movimientos en memoria:
    ///   - Los productos son pocos (decenas a cientos), no miles
    ///   - Necesitamos múltiples agregaciones sobre el mismo dataset
    ///   - Es más eficiente una sola consulta + LINQ en memoria que
    ///     múltiples consultas SQL con diferentes GROUP BY
    ///
    /// ingresosHoy = ventas − devoluciones (ingreso neto real del día)
    /// valorInventario = suma(stock × costoCompra) — cuánto vale la bodega a precio de costo
    /// </summary>
    public async Task<object> GetInventarioStatsAsync(Guid negocioId)
    {
        // Cargar todos los productos activos para calcular múltiples métricas sobre ellos
        var productos = await _context.Productos.AsNoTracking()
            .Where(p => p.NegocioId == negocioId && p.IsActive)
            .ToListAsync();

        // Solo los movimientos de hoy para las estadísticas diarias
        var hoy = TimeHelper.Now.Date;
        var movimientosHoy = await _context.MovimientosInventario.AsNoTracking()
            .Where(m => m.NegocioId == negocioId && m.Fecha.Date == hoy)
            .ToListAsync();

        return new
        {
            // Cuántos productos distintos tiene el negocio
            totalProductos = productos.Count,

            // Cuántos están en alerta de stock bajo (necesitan reabastecimiento urgente)
            stockBajo = productos.Count(p => p.Stock <= p.StockMinimo),

            // Unidades físicas vendidas hoy (no en dinero, sino en piezas)
            ventasHoyUnidades = movimientosHoy.Where(m => m.Tipo == "venta" || m.Tipo == "venta_tienda").Sum(m => m.Cantidad),

            // Ingreso neto: lo que entró por ventas menos lo que salió por devoluciones
            ingresosHoy = movimientosHoy.Where(m => m.Tipo == "venta" || m.Tipo == "venta_tienda").Sum(m => m.Total)
                         - movimientosHoy.Where(m => m.Tipo == "devolucion").Sum(m => m.Total),

            // Monto total devuelto hoy (ayuda a detectar problemas de calidad o servicio)
            devolucionesHoy = movimientosHoy.Where(m => m.Tipo == "devolucion").Sum(m => m.Total),

            // Valor de reposición del inventario: cuánto costaría reponer todo lo que hay
            valorInventario = productos.Sum(p => p.Stock * p.CostoCompra)
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EXPORTACIÓN EXCEL
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Genera un archivo Excel en memoria con dos hojas de trabajo usando ClosedXML.
    ///
    /// Por qué ClosedXML en lugar de otras opciones:
    ///   - No requiere Microsoft Office instalado en el servidor (a diferencia de Interop)
    ///   - API fluida y legible (a diferencia del bajo nivel de OpenXML SDK)
    ///   - Soporta estilos, colores y ajuste automático de columnas
    ///
    /// Hoja 1 "Productos":
    ///   Lista de productos con precio, costo, stock y margen calculado.
    ///   Los productos con stock bajo se marcan en ROJO para identificación rápida.
    ///
    /// Hoja 2 "Movimientos":
    ///   Historial completo de todas las operaciones (venta, restock, devolución, ajuste).
    ///   Útil para auditoría contable.
    ///
    /// El archivo se genera en un MemoryStream (sin tocar el disco del servidor) y
    /// se devuelve como byte[] para que el controlador lo envíe como descarga HTTP.
    /// </summary>
    public async Task<byte[]> ExportInventarioExcelAsync(Guid negocioId)
    {
        // Cargar datos ordenados para el Excel
        var productos = await _context.Productos.AsNoTracking()
            .Where(p => p.NegocioId == negocioId && p.IsActive)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var movimientos = await _context.MovimientosInventario.AsNoTracking()
            .Where(m => m.NegocioId == negocioId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();

        // XLWorkbook es el libro de Excel; se destruye automáticamente con 'using'
        using var workbook = new XLWorkbook();

        // ── Hoja 1: Productos ──────────────────────────────────────────────
        var wsProductos = workbook.Worksheets.Add("Productos");

        // Encabezados de columna
        wsProductos.Cell(1, 1).Value = "Producto";
        wsProductos.Cell(1, 2).Value = "Precio Venta";
        wsProductos.Cell(1, 3).Value = "Costo";
        wsProductos.Cell(1, 4).Value = "Stock";
        wsProductos.Cell(1, 5).Value = "Stock Mínimo";
        wsProductos.Cell(1, 6).Value = "Margen";

        // Estilo de encabezado: fondo azul con texto blanco en negrita
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

            // Resaltar en rojo los productos que necesitan reabastecimiento urgente
            if (p.Stock <= p.StockMinimo)
                wsProductos.Cell(row, 4).Style.Font.FontColor = XLColor.Red;

            row++;
        }

        // Ajusta el ancho de cada columna al contenido más largo automáticamente
        wsProductos.Columns().AdjustToContents();

        // ── Hoja 2: Movimientos ────────────────────────────────────────────
        var wsMov = workbook.Worksheets.Add("Movimientos");

        // Encabezados de columna
        wsMov.Cell(1, 1).Value = "Fecha";
        wsMov.Cell(1, 2).Value = "Producto";
        wsMov.Cell(1, 3).Value = "Tipo";
        wsMov.Cell(1, 4).Value = "Cantidad";
        wsMov.Cell(1, 5).Value = "Precio Unit.";
        wsMov.Cell(1, 6).Value = "Total";
        wsMov.Cell(1, 7).Value = "Stock Antes";
        wsMov.Cell(1, 8).Value = "Stock Después";
        wsMov.Cell(1, 9).Value = "Nota";

        // Mismo estilo de encabezado que la hoja de productos
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
            wsMov.Cell(row, 9).Value = m.Nota ?? "";  // Nota puede ser null (ej. en ventas normales)
            row++;
        }
        wsMov.Columns().AdjustToContents();

        // Serializar el workbook a un array de bytes en memoria
        // Se usa 'using' para liberar el stream una vez que se copió el contenido
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GESTION DE CATEGORIAS (exclusivo modelo Tienda)
    //
    // Las categorias permiten organizar productos en pestanas (tabs) dentro
    // del catalogo y del POS. El orden se maneja via drag & drop.
    // Al eliminar una categoria, los productos quedan sin categoria (null).
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todas las categorias del negocio con sus productos activos.
    /// Se usa para poblar las pestanas del inventario y del catalogo.
    /// </summary>
    public async Task<List<CategoriaProducto>> GetCategoriasAsync(Guid negocioId)
    {
        return await _context.CategoriasProducto
            .Include(c => c.Productos.Where(p => p.IsActive))
            .Where(c => c.NegocioId == negocioId)
            .OrderBy(c => c.Orden)
            .ToListAsync();
    }

    /// <summary>
    /// Crea una nueva categoria. El orden se asigna automaticamente al final
    /// (maxOrden + 1) para que aparezca como la ultima pestana.
    /// </summary>
    public async Task<(bool success, string message)> CrearCategoriaAsync(Guid negocioId, string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return (false, "El nombre de la categoría es requerido.");

        // Obtener el orden maximo actual para asignar el siguiente
        var currentMaxOrder = await _context.CategoriasProducto
            .Where(c => c.NegocioId == negocioId)
            .MaxAsync(c => (int?)c.Orden) ?? 0;

        var cat = new CategoriaProducto
        {
            CategoriaId = Guid.NewGuid(),
            NegocioId = negocioId,
            Nombre = nombre.Trim(),
            Orden = currentMaxOrder + 1,
            FechaCreacion = TimeHelper.Now
        };

        _context.CategoriasProducto.Add(cat);
        await _context.SaveChangesAsync();
        return (true, "Categoría creada exitosamente");
    }

    /// <summary>
    /// Elimina una categoria y des-asigna sus productos (quedan con CategoriaProductoId = null).
    /// Los productos NO se eliminan, solo pierden su agrupacion.
    /// </summary>
    public async Task<(bool success, string message)> EliminarCategoriaAsync(Guid categoriaId, Guid negocioId)
    {
        var categoria = await _context.CategoriasProducto
            .Include(c => c.Productos)
            .FirstOrDefaultAsync(c => c.CategoriaId == categoriaId && c.NegocioId == negocioId);

        if (categoria == null) return (false, "Categoría no encontrada.");

        // Des-asignar productos antes de eliminar la categoria
        foreach (var prod in categoria.Productos)
        {
            prod.CategoriaProductoId = null;
        }

        _context.CategoriasProducto.Remove(categoria);
        await _context.SaveChangesAsync();
        return (true, "Categoría eliminada. Los productos han sido des-asignados.");
    }

    /// <summary>
    /// Reordena las categorias segun el orden de IDs recibido desde el frontend
    /// (resultado de un drag & drop). La posicion en la lista = nuevo valor de Orden.
    /// </summary>
    public async Task<(bool success, string message)> ReordenarCategoriasAsync(Guid negocioId, List<Guid> categoriasOrdenadasIds)
    {
        var categorias = await _context.CategoriasProducto
            .Where(c => c.NegocioId == negocioId && categoriasOrdenadasIds.Contains(c.CategoriaId))
            .ToListAsync();

        for (int i = 0; i < categoriasOrdenadasIds.Count; i++)
        {
            var cid = categoriasOrdenadasIds[i];
            var cat = categorias.FirstOrDefault(c => c.CategoriaId == cid);
            if (cat != null) cat.Orden = i + 1;
        }
        await _context.SaveChangesAsync();
        return (true, "Categorías re-ordenadas.");
    }

    /// <summary>
    /// Mueve un producto de una categoria a otra (o lo deja sin categoria si nuevaCategoriaId es null).
    /// Valida que la categoria destino exista y pertenezca al mismo negocio.
    /// </summary>
    public async Task<(bool success, string message)> MoverProductoDeCategoriaAsync(Guid productoId, Guid negocioId, Guid? nuevaCategoriaId)
    {
        var prod = await _context.Productos.FirstOrDefaultAsync(p => p.ProductoId == productoId && p.NegocioId == negocioId);
        if (prod == null) return (false, "Producto no encontrado.");

        if (nuevaCategoriaId.HasValue)
        {
            var catExists = await _context.CategoriasProducto.AnyAsync(c => c.CategoriaId == nuevaCategoriaId.Value && c.NegocioId == negocioId);
            if (!catExists) return (false, "La categoría destino no existe.");
        }

        prod.CategoriaProductoId = nuevaCategoriaId;
        prod.FechaDeActualizacion = TimeHelper.Now;
        await _context.SaveChangesAsync();
        return (true, "Producto re-categorizado.");
    }

    /// <summary>
    /// Actualiza la URL de la imagen de un producto.
    /// La imagen se sube al servidor en el controlador y aqui solo se guarda la ruta.
    /// </summary>
    public async Task<(bool success, string message)> CambiarImagenProductoAsync(Guid productoId, Guid negocioId, string urlImagen)
    {
        var prod = await _context.Productos.FirstOrDefaultAsync(p => p.ProductoId == productoId && p.NegocioId == negocioId);
        if (prod == null) return (false, "Producto no encontrado.");

        prod.ImagenUrl = urlImagen;
        prod.FechaDeActualizacion = TimeHelper.Now;
        await _context.SaveChangesAsync();
        return (true, "Imagen del producto actualizada.");
    }
}
