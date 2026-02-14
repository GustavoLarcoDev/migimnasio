// ═══════════════════════════════════════════════════════════
// ILogService.cs — Contrato del servicio de logs/registros
// Define las operaciones para gestión de logs financieros:
// creación automática y manual, consulta, eliminación y exportación
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Services;

public interface ILogService
{
    /// <summary>
    /// Crea un log automático (llamado desde ClienteService al crear/editar/renovar/eliminar clientes)
    /// </summary>
    Task CreateLogAsync(Guid negocioId, string tipo, string message, decimal monto = 0, Guid? clienteId = null, string nombreCliente = null);

    /// <summary>
    /// Obtiene todos los logs del negocio ordenados por fecha descendente
    /// </summary>
    Task<object> GetLogsAsync(Guid negocioId);

    /// <summary>
    /// Obtiene un log individual por su ID
    /// </summary>
    Task<object> GetLogAsync(Guid id, Guid negocioId);

    /// <summary>
    /// Crea un log manual de ingreso o gasto. El tipo se asigna según el signo del monto.
    /// </summary>
    Task<(bool success, string message)> CrearLogManualAsync(Guid negocioId, string message, decimal monto);

    /// <summary>
    /// Edita la descripción y monto de un log existente
    /// </summary>
    Task<(bool success, string message)> EditarLogAsync(Guid id, Guid negocioId, string message, decimal monto);

    /// <summary>
    /// Elimina un log individual
    /// </summary>
    Task<(bool success, string message)> EliminarLogAsync(Guid id, Guid negocioId);

    /// <summary>
    /// Elimina todos los logs de un negocio (acción irreversible)
    /// </summary>
    Task<(bool success, string message)> EliminarTodosLogsAsync(Guid negocioId);

    /// <summary>
    /// Exporta todos los logs a Excel con resumen de ingresos, gastos y balance
    /// </summary>
    Task<byte[]> ExportLogsExcelAsync(Guid negocioId);

    /// <summary>
    /// Obtiene la fecha del log más antiguo (para filtros de rango de fecha)
    /// </summary>
    Task<DateTime?> GetOldestLogDateAsync(Guid negocioId);
}
