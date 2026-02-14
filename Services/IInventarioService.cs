using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

public interface IInventarioService
{
    // CRUD de productos
    Task<object> GetProductosAsync(Guid negocioId);
    Task<Producto> GetProductoAsync(Guid id, Guid negocioId);
    Task<(bool success, string message)> CrearProductoAsync(ProductoCreateDto model);
    Task<(bool success, string message)> EditarProductoAsync(ProductoCreateDto model);
    Task<(bool success, string message)> EliminarProductoAsync(Guid id, Guid negocioId);

    // Movimientos de inventario
    Task<(bool success, string message)> VenderProductoAsync(Guid productoId, Guid negocioId, int cantidad);
    Task<(bool success, string message)> DevolverProductoAsync(Guid productoId, Guid negocioId, int cantidad, string nota);
    Task<(bool success, string message)> RestockAsync(Guid productoId, Guid negocioId, int cantidad, decimal costoTotal);
    Task<(bool success, string message)> AjustarStockAsync(Guid productoId, Guid negocioId, int stockReal, string nota);

    // Consultas
    Task<object> GetMovimientosAsync(Guid negocioId);
    Task<object> GetInventarioStatsAsync(Guid negocioId);

    // Exportacion
    Task<byte[]> ExportInventarioExcelAsync(Guid negocioId);
}
