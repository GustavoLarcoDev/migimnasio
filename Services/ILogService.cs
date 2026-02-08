namespace Gimnasio.Services;

public interface ILogService
{
    Task CreateLogAsync(Guid gimnasioId, string tipo, string message, decimal monto = 0, Guid? clienteId = null, string nombreCliente = null);
    Task<object> GetLogsAsync(Guid gimnasioId);
    Task<object> GetLogAsync(Guid id, Guid gimnasioId);
    Task<(bool success, string message)> CrearLogManualAsync(Guid gimnasioId, string message, decimal monto);
    Task<(bool success, string message)> EditarLogAsync(Guid id, Guid gimnasioId, string message, decimal monto);
    Task<(bool success, string message)> EliminarLogAsync(Guid id, Guid gimnasioId);
    Task<(bool success, string message)> EliminarTodosLogsAsync(Guid gimnasioId);
    Task<byte[]> ExportLogsExcelAsync(Guid gimnasioId);
}
