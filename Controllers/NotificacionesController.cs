using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Gimnasios")]
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
    public async Task<IActionResult> GetNotificaciones(Guid gimnasioId)
    {
        try
        {
            var gimnasioIdClaim = _authService.GetGimnasioId(User);
            if (!gimnasioIdClaim.HasValue || gimnasioId != gimnasioIdClaim.Value)
                return Forbid();

            var notificaciones = await _notificationService.GetNotificacionesAsync(gimnasioId);
            return Ok(notificaciones);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetNotificacionesCount")]
    public async Task<IActionResult> GetNotificacionesCount(Guid gimnasioId)
    {
        try
        {
            var count = await _notificationService.GetNotificacionesCountAsync(gimnasioId);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("MarcarNotificacionLeida")]
    public async Task<IActionResult> MarcarLeida(Guid id, Guid gimnasioId)
    {
        try
        {
            var (success, message) = await _notificationService.MarcarLeidaAsync(id, gimnasioId);
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
    public async Task<IActionResult> MarcarTodasLeidas(Guid gimnasioId)
    {
        try
        {
            var (success, message) = await _notificationService.MarcarTodasLeidasAsync(gimnasioId);
            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("GenerarNotificaciones")]
    public async Task<IActionResult> GenerarNotificaciones(Guid gimnasioId)
    {
        try
        {
            var gimnasioIdClaim = _authService.GetGimnasioId(User);
            if (!gimnasioIdClaim.HasValue || gimnasioId != gimnasioIdClaim.Value)
                return Forbid();

            var (success, message, count) = await _notificationService.GenerarNotificacionesAsync(gimnasioId);
            return Ok(new { success = true, message, count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
