// ═══════════════════════════════════════════════════════════
// IInventarioService.cs — Contrato del servicio de inventario
// Define las operaciones para gestión de productos:
// CRUD, ventas, devoluciones, restock, ajustes y exportación
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

public interface IInventarioService
{
    // ═══════════════════════════════════════════════════════════
    // CRUD DE PRODUCTOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los productos activos con margen y alerta de stock bajo
    /// </summary>
    Task<object> GetProductosAsync(Guid negocioId);

    /// <summary>
    /// Obtiene un producto específico por su ID dentro de un negocio
    /// </summary>
    Task<Producto> GetProductoAsync(Guid id, Guid negocioId);

    /// <summary>
    /// Crea un nuevo producto con validación de nombre, precios y stock
    /// </summary>
    Task<(bool success, string message)> CrearProductoAsync(ProductoCreateDto model);

    /// <summary>
    /// Edita un producto existente y registra los cambios en logs
    /// </summary>
    Task<(bool success, string message)> EditarProductoAsync(ProductoCreateDto model);

    /// <summary>
    /// Elimina un producto de forma lógica (IsActive = false)
    /// </summary>
    Task<(bool success, string message)> EliminarProductoAsync(Guid id, Guid negocioId);

    // ═══════════════════════════════════════════════════════════
    // MOVIMIENTOS DE INVENTARIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Registra la venta de unidades de un producto y genera log de ingreso
    /// </summary>
    Task<(bool success, string message)> VenderProductoAsync(Guid productoId, Guid negocioId, int cantidad);

    /// <summary>
    /// Registra la devolución de unidades y genera log de gasto
    /// </summary>
    Task<(bool success, string message)> DevolverProductoAsync(Guid productoId, Guid negocioId, int cantidad, string nota);

    /// <summary>
    /// Registra un reabastecimiento de stock y genera log de gasto
    /// </summary>
    Task<(bool success, string message)> RestockAsync(Guid productoId, Guid negocioId, int cantidad, decimal costoTotal);

    /// <summary>
    /// Ajusta el stock real tras conteo físico y genera log de ajuste
    /// </summary>
    Task<(bool success, string message)> AjustarStockAsync(Guid productoId, Guid negocioId, int stockReal, string nota);

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS Y EXPORTACIÓN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene el historial de movimientos de inventario ordenados por fecha
    /// </summary>
    Task<object> GetMovimientosAsync(Guid negocioId);

    /// <summary>
    /// Calcula estadísticas de inventario: productos, stock bajo, ventas del día
    /// </summary>
    Task<object> GetInventarioStatsAsync(Guid negocioId);

    /// <summary>
    /// Exporta productos y movimientos a un archivo Excel de dos hojas
    /// </summary>
    Task<byte[]> ExportInventarioExcelAsync(Guid negocioId);
}
