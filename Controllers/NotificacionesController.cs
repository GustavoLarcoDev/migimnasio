using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

public class NotificacionesController : NegocioBaseController
{
    private readonly INotificationService _service;

    public NotificacionesController(INotificationService service, IAuthService authService)
        : base(authService) => _service = service;

    // ── Consultas ──

    [HttpGet("GetNotificaciones")]
    public Task<IActionResult> GetNotificaciones(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _service.GetNotificacionesAsync(nId)));

    [HttpGet("GetNotificacionesCount")]
    public Task<IActionResult> GetNotificacionesCount(Guid negocioId)
        => Execute(negocioId, async nId => Ok(new { count = await _service.GetNotificacionesCountAsync(nId) }));

    // ── Acciones ──

    [HttpPost("MarcarNotificacionLeida")]
    public Task<IActionResult> MarcarLeida(Guid id, Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var (success, message) = await _service.MarcarLeidaAsync(id, nId);
            return success
                ? Ok(new { success = true, message })
                : NotFound(new { success = false, message });
        });

    [HttpPost("MarcarTodasNotificacionesLeidas")]
    public Task<IActionResult> MarcarTodasLeidas(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            await _service.MarcarTodasLeidasAsync(nId);
            return Ok(new { success = true, message = "Todas las notificaciones marcadas como leidas" });
        });

    [HttpPost("GenerarNotificaciones")]
    public Task<IActionResult> GenerarNotificaciones(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var (success, message, count) = await _service.GenerarNotificacionesAsync(nId);
            return Ok(new { success = true, message, count });
        });
}
