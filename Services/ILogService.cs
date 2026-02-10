namespace Gimnasio.Services;

public interface ILogService
{
    Task CreateLogAsync(Guid negocioId, string tipo, string message, decimal monto = 0, Guid? clienteId = null, string nombreCliente = null);
    Task<object> GetLogsAsync(Guid negocioId);
    Task<object> GetLogAsync(Guid id, Guid negocioId);
    Task<(bool success, string message)> CrearLogManualAsync(Guid negocioId, string message, decimal monto);
    Task<(bool success, string message)> EditarLogAsync(Guid id, Guid negocioId, string message, decimal monto);
    Task<(bool success, string message)> EliminarLogAsync(Guid id, Guid negocioId);
    Task<(bool success, string message)> EliminarTodosLogsAsync(Guid negocioId);
    Task<byte[]> ExportLogsExcelAsync(Guid negocioId);
    Task<DateTime?> GetOldestLogDateAsync(Guid negocioId);
}
