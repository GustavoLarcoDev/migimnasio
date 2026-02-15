// ═══════════════════════════════════════════════════════════
// NotificacionesController.cs — Controlador de notificaciones
// Maneja la generación automática de alertas de vencimiento,
// consulta, conteo y marcado de notificaciones como leídas
// ═══════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las últimas 50 notificaciones del negocio
    /// </summary>
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

    /// <summary>
    /// Obtiene el conteo de notificaciones no leídas (para el badge del sidebar)
    /// </summary>
    [HttpGet("GetNotificacionesCount")]
    public async Task<IActionResult> GetNotificacionesCount(Guid negocioId)
    {
        try
        {
            var negocioIdClaim = _authService.GetNegocioId(User);
            if (!negocioIdClaim.HasValue || negocioId != negocioIdClaim.Value)
                return Forbid();

            var count = await _notificationService.GetNotificacionesCountAsync(negocioId);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ACCIONES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Marca una notificación individual como leída
    /// </summary>
    [HttpPost("MarcarNotificacionLeida")]
    public async Task<IActionResult> MarcarLeida(Guid id, Guid negocioId)
    {
        try
        {
            var negocioIdClaim = _authService.GetNegocioId(User);
            if (!negocioIdClaim.HasValue || negocioId != negocioIdClaim.Value)
                return Forbid();

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

    /// <summary>
    /// Marca todas las notificaciones del negocio como leídas
    /// </summary>
    [HttpPost("MarcarTodasNotificacionesLeidas")]
    public async Task<IActionResult> MarcarTodasLeidas(Guid negocioId)
    {
        try
        {
            var negocioIdClaim = _authService.GetNegocioId(User);
            if (!negocioIdClaim.HasValue || negocioId != negocioIdClaim.Value)
                return Forbid();

            var (success, message) = await _notificationService.MarcarTodasLeidasAsync(negocioId);
            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Genera notificaciones automáticas para clientes cuya membresía
    /// vence dentro de los próximos 3 días. Evita duplicados por día.
    /// Se ejecuta al cargar el dashboard.
    /// </summary>
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
