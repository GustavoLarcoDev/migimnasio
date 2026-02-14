// ═══════════════════════════════════════════════════════════
// INotificationService.cs — Contrato del servicio de notificaciones
// Define las operaciones para generación automática de alertas
// de vencimiento y gestión del estado leído/no leído
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Services;

public interface INotificationService
{
    /// <summary>
    /// Obtiene las últimas 50 notificaciones del negocio
    /// </summary>
    Task<object> GetNotificacionesAsync(Guid negocioId);

    /// <summary>
    /// Obtiene el conteo de notificaciones no leídas (para badge del sidebar)
    /// </summary>
    Task<int> GetNotificacionesCountAsync(Guid negocioId);

    /// <summary>
    /// Marca una notificación individual como leída
    /// </summary>
    Task<(bool success, string message)> MarcarLeidaAsync(Guid id, Guid negocioId);

    /// <summary>
    /// Marca todas las notificaciones del negocio como leídas
    /// </summary>
    Task<(bool success, string message)> MarcarTodasLeidasAsync(Guid negocioId);

    /// <summary>
    /// Genera notificaciones para clientes cuya membresía vence en 3 días.
    /// Evita duplicados verificando si ya existe una notificación para ese cliente hoy.
    /// </summary>
    Task<(bool success, string message, int count)> GenerarNotificacionesAsync(Guid negocioId);
}
