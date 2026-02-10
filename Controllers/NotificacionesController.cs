using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class NotificacionesController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly IAuthService _authService;

    public NotificacionesController(INotificationService notificationService, IAuthService authService)
    {
        _notificationService = notificationService;
        _authService = authService;
    }

    [HttpGet("GetNotificaciones")]
    public async Task<IActionResult> GetNotificaciones(Guid negocioId)
    {
        try
        {
            var negocioIdClaim = _authService.GetNegocioId(User);
            if (!negocioIdClaim.HasValue || negocioId != negocioIdClaim.Value)
                return Forbid();

            var notificaciones = await _notificationService.GetNotificacionesAsync(negocioId);
            return Ok(notificaciones);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetNotificacionesCount")]
    public async Task<IActionResult> GetNotificacionesCount(Guid negocioId)
    {
        try
        {
            var count = await _notificationService.GetNotificacionesCountAsync(negocioId);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("MarcarNotificacionLeida")]
    public async Task<IActionResult> MarcarLeida(Guid id, Guid negocioId)
    {
        try
        {
            var (success, message) = await _notificationService.MarcarLeidaAsync(id, negocioId);
            if (!success)
                return NotFound(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("MarcarTodasNotificacionesLeidas")]
    public async Task<IActionResult> MarcarTodasLeidas(Guid negocioId)
    {
        try
        {
            var (success, message) = await _notificationService.MarcarTodasLeidasAsync(negocioId);
            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("GenerarNotificaciones")]
    public async Task<IActionResult> GenerarNotificaciones(Guid negocioId)
    {
        try
        {
            var negocioIdClaim = _authService.GetNegocioId(User);
            if (!negocioIdClaim.HasValue || negocioId != negocioIdClaim.Value)
                return Forbid();

            var (success, message, count) = await _notificationService.GenerarNotificacionesAsync(negocioId);
            return Ok(new { success = true, message, count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
