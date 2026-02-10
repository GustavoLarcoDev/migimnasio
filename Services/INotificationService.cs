namespace Gimnasio.Services;

public interface INotificationService
{
    Task<object> GetNotificacionesAsync(Guid negocioId);
    Task<int> GetNotificacionesCountAsync(Guid negocioId);
    Task<(bool success, string message)> MarcarLeidaAsync(Guid id, Guid negocioId);
    Task<(bool success, string message)> MarcarTodasLeidasAsync(Guid negocioId);
    Task<(bool success, string message, int count)> GenerarNotificacionesAsync(Guid negocioId);
}
