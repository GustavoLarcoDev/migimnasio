namespace Gimnasio.Services;

public interface INotificationService
{
    Task<object> GetNotificacionesAsync(Guid gimnasioId);
    Task<int> GetNotificacionesCountAsync(Guid gimnasioId);
    Task<(bool success, string message)> MarcarLeidaAsync(Guid id, Guid gimnasioId);
    Task<(bool success, string message)> MarcarTodasLeidasAsync(Guid gimnasioId);
    Task<(bool success, string message, int count)> GenerarNotificacionesAsync(Guid gimnasioId);
}
