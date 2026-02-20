// ═══════════════════════════════════════════════════════════════════════════
// IInventarioService.cs — Contrato del servicio de inventario
//
// Define todas las operaciones posibles sobre el inventario de un negocio:
//   - CRUD de productos (crear, leer, editar, eliminar lógico)
//   - Cuatro tipos de movimiento de stock: venta, devolución, restock, ajuste
//   - Consulta del historial de movimientos y estadísticas del día
//   - Exportación a Excel con dos hojas (productos + movimientos)
//
// PATRÓN GENERAL:
//   Cada operación que modifica stock recibe un (negocioId) para garantizar
//   que un negocio nunca pueda tocar los datos de otro (aislamiento multi-tenant).
//   Las operaciones de escritura devuelven (bool success, string message) para
//   que el controlador pueda responder al frontend con un mensaje claro.
// ═══════════════════════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

/// <summary>
/// Contrato del servicio de gestión de inventario.
/// Implementado por <see cref="InventarioService"/>.
/// </summary>
public interface IInventarioService
{
    // ═══════════════════════════════════════════════════════════════════════
    // CRUD DE PRODUCTOS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los productos activos del negocio.
    /// Incluye campos calculados: <c>MargenUnitario</c> (precio − costo)
    /// y <c>StockBajo</c> (true si el stock actual es menor o igual al mínimo).
    /// Devuelve <c>object</c> porque se proyecta a un tipo anónimo optimizado
    /// para serializar como JSON al frontend.
    /// </summary>
    Task<object> GetProductosAsync(Guid negocioId);

    /// <summary>
    /// Obtiene un producto específico por su ID.
    /// Siempre filtra por <paramref name="negocioId"/> para evitar que un negocio
    /// acceda a los productos de otro (seguridad multi-tenant).
    /// Devuelve <c>null</c> si el producto no existe o fue eliminado lógicamente.
    /// </summary>
    Task<Producto> GetProductoAsync(Guid id, Guid negocioId);

    /// <summary>
    /// Crea un nuevo producto con validación previa de todos sus campos.
    /// Reglas de negocio que se validan:
    ///   - El nombre no puede estar vacío
    ///   - El precio de venta debe ser mayor a 0
    ///   - El costo de compra debe ser mayor a 0
    ///   - El stock inicial no puede ser negativo
    /// Registra un log de tipo "producto_creado" al finalizar.
    /// </summary>
    Task<(bool success, string message, Guid? dataId)> CrearProductoAsync(ProductoCreateDto model);

    /// <summary>
    /// Edita un producto existente.
    /// Solo guarda si realmente hubo cambios; si todos los valores son iguales
    /// devuelve (false, "No se detectaron cambios") sin tocar la base de datos.
    /// Nota: el Stock nunca se modifica aquí — para eso existen los métodos
    /// de movimiento (Vender, Devolver, Restock, Ajustar).
    /// Registra un log detallado con los campos que cambiaron (ej. "precio: $10 → $12").
    /// </summary>
    Task<(bool success, string message)> EditarProductoAsync(ProductoCreateDto model);

    /// <summary>
    /// Elimina un producto de forma LÓGICA: pone <c>IsActive = false</c>.
    /// El producto NO se borra físicamente de la base de datos porque sus movimientos
    /// históricos deben seguir siendo consultables para reportes y auditoría.
    /// Registra un log con el stock restante al momento de la eliminación.
    /// </summary>
    Task<(bool success, string message)> EliminarProductoAsync(Guid id, Guid negocioId);

    // ═══════════════════════════════════════════════════════════════════════
    // GESTIÓN DE CATEGORÍAS (MODELO TIENDA)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las categorías de un negocio ordenadas según el campo Orden.
    /// Incluye los productos activos asociados a cada categoría para pintar las Pestañas (Tabs).
    /// </summary>
    Task<List<CategoriaProducto>> GetCategoriasAsync(Guid negocioId);

    /// <summary>
    /// Crea una nueva categoría.
    /// </summary>
    Task<(bool success, string message)> CrearCategoriaAsync(Guid negocioId, string nombre);

    /// <summary>
    /// Elimina una categoría. Sus productos quedan huérfanos (CategoriaProductoId = null) pero no se eliminan.
    /// </summary>
    Task<(bool success, string message)> EliminarCategoriaAsync(Guid categoriaId, Guid negocioId);

    /// <summary>
    /// Reordena de forma masiva las categorías basándose en un array de IDs en un nuevo orden.
    /// Utilizado en Drag And Drop de "Tabs".
    /// </summary>
    Task<(bool success, string message)> ReordenarCategoriasAsync(Guid negocioId, List<Guid> categoriasOrdenadasIds);

    /// <summary>
    /// Mueve un producto específico a una categoría nueva o a los productos "Sin Categorizar" (null).
    /// </summary>
    Task<(bool success, string message)> MoverProductoDeCategoriaAsync(Guid productoId, Guid negocioId, Guid? nuevaCategoriaId);

    /// <summary>
    /// Actualiza la ImagenUrl de un Producto.
    /// </summary>
    Task<(bool success, string message)> CambiarImagenProductoAsync(Guid productoId, Guid negocioId, string urlImagen);

    // ═══════════════════════════════════════════════════════════════════════
    // MOVIMIENTOS DE INVENTARIO
    // Los cuatro tipos de movimiento que puede tener un producto son:
    //   "venta"      — salida de stock, genera ingreso financiero
    //   "devolucion" — entrada de stock, genera gasto (reversión de venta)
    //   "restock"    — entrada de stock por compra a proveedor, genera gasto
    //   "ajuste"     — corrección de stock tras conteo físico, neutro financieramente
    //
    // Todos los métodos usan DbUpdateConcurrencyException para detectar colisiones
    // si dos usuarios venden el mismo producto al mismo tiempo.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registra la venta de <paramref name="cantidad"/> unidades de un producto.
    /// Flujo:
    ///   1. Valida que haya stock suficiente
    ///   2. Reduce el stock del producto
    ///   3. Crea un registro en MovimientosInventario de tipo "venta"
    ///   4. Registra un log de ingreso con el total cobrado
    /// Si dos usuarios intentan vender simultáneamente y el stock no alcanza para
    /// ambos, el segundo recibirá un error de concurrencia (no un número negativo).
    /// </summary>
    Task<(bool success, string message)> VenderProductoAsync(Guid productoId, Guid negocioId, int cantidad);

    /// <summary>
    /// Registra la devolución de <paramref name="cantidad"/> unidades de un producto.
    /// Flujo:
    ///   1. Valida cantidad y que la nota de razón no esté vacía (requerida para auditoría)
    ///   2. Incrementa el stock del producto
    ///   3. Crea un registro en MovimientosInventario de tipo "devolucion"
    ///   4. Registra un log de GASTO (monto negativo) porque el dinero se devuelve al cliente
    /// </summary>
    Task<(bool success, string message)> DevolverProductoAsync(Guid productoId, Guid negocioId, int cantidad, string nota);

    /// <summary>
    /// Registra el reabastecimiento de stock cuando se compra mercancía al proveedor.
    /// Flujo:
    ///   1. Valida cantidad y que el costo no sea negativo
    ///   2. Incrementa el stock del producto
    ///   3. Crea un registro en MovimientosInventario de tipo "restock"
    ///      (el precio unitario se calcula dividiendo el costo total entre la cantidad)
    ///   4. Registra un log de GASTO (costo total negativo) porque se gastó dinero comprando
    /// </summary>
    Task<(bool success, string message)> RestockAsync(Guid productoId, Guid negocioId, int cantidad, decimal costoTotal);

    /// <summary>
    /// Ajusta el stock cuando el conteo físico difiere del sistema.
    /// Casos de uso típicos: productos dañados, pérdida por vencimiento, error de registro previo.
    /// Flujo:
    ///   1. Valida que <paramref name="stockReal"/> no sea negativo y que haya una nota explicativa
    ///   2. Calcula la diferencia (stockReal − stockActual)
    ///   3. Si la diferencia es 0, devuelve error (no hay nada que ajustar)
    ///   4. Sobreescribe el stock con el valor real contado
    ///   5. Crea un registro en MovimientosInventario de tipo "ajuste"
    ///   6. Registra un log con la diferencia formateada (+5 o -3) y la razón
    /// A diferencia de los otros movimientos, el ajuste es financieramente neutro (total = 0).
    /// </summary>
    Task<(bool success, string message)> AjustarStockAsync(Guid productoId, Guid negocioId, int stockReal, string nota);

    // ═══════════════════════════════════════════════════════════════════════
    // CONSULTAS Y EXPORTACIÓN
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene el historial completo de movimientos del negocio, ordenados del más
    /// reciente al más antiguo. Incluye todos los tipos: venta, devolucion, restock, ajuste.
    /// Devuelve <c>object</c> para serializar directamente a JSON (tipo anónimo optimizado).
    /// </summary>
    Task<object> GetMovimientosAsync(Guid negocioId);

    /// <summary>
    /// Calcula las estadísticas del inventario para el widget del dashboard.
    /// Incluye:
    ///   - totalProductos: cuántos productos activos tiene el negocio
    ///   - stockBajo: cuántos productos están en o por debajo del mínimo (alerta roja)
    ///   - ventasHoyUnidades: unidades vendidas en el día actual
    ///   - ingresosHoy: ventas del día menos devoluciones del día (ingreso neto real)
    ///   - devolucionesHoy: monto total de devoluciones del día
    ///   - valorInventario: suma de (stock × costoCompra) por producto (valor de reposición)
    /// </summary>
    Task<object> GetInventarioStatsAsync(Guid negocioId);

    /// <summary>
    /// Exporta el inventario completo a un archivo Excel (.xlsx) en memoria.
    /// El archivo tiene dos hojas:
    ///   - "Productos": lista de productos con precios, stock y margen calculado.
    ///     Los productos con stock bajo se resaltan en rojo.
    ///   - "Movimientos": historial completo de operaciones del inventario.
    /// Devuelve el archivo como <c>byte[]</c> para enviarlo directamente como
    /// respuesta HTTP (Content-Type: application/vnd.openxmlformats-officedocument...).
    /// </summary>
    Task<byte[]> ExportInventarioExcelAsync(Guid negocioId);
}
