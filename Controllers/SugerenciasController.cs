// ═══════════════════════════════════════════════════════════
// SugerenciasController.cs — Controlador de sugerencias/feedback
// Los negocios pueden enviar sugerencias al admin.
// El admin puede ver todas las sugerencias y marcarlas como leídas.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class SugerenciasController : Controller
{
    private readonly ISugerenciaService _sugerenciaService;
    private readonly IAuthService _authService;

    public SugerenciasController(ISugerenciaService sugerenciaService, IAuthService authService)
    {
        _sugerenciaService = sugerenciaService;
        _authService = authService;
    }

    // ═══════════════════════════════════════════════════════════
    // ENDPOINTS DE NEGOCIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Permite a un negocio enviar una sugerencia al administrador.
    /// Máximo 1000 caracteres por mensaje.
    /// </summary>
    [HttpPost("EnviarSugerencia")]
    public async Task<IActionResult> EnviarSugerencia(Guid negocioId, string mensaje)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var negocioNombre = User.Identity?.Name ?? "Desconocido";
            var (success, message) = await _sugerenciaService.CrearSugerenciaAsync(negocioId, negocioNombre, mensaje);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ENDPOINTS DE ADMIN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Muestra la vista de administración de sugerencias (solo admin)
    /// </summary>
    [HttpGet("Sugerencias")]
    public IActionResult Sugerencias()
    {
        if (!_authService.IsAdmin(User))
            return RedirectToAction("Login", "Auth");

        return View("~/Views/Negocios/Sugerencias.cshtml");
    }

    /// <summary>
    /// Obtiene todas las sugerencias ordenadas por fecha (solo admin)
    /// </summary>
    [HttpGet("GetSugerencias")]
    public async Task<IActionResult> GetSugerencias()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var sugerencias = await _sugerenciaService.GetSugerenciasAsync();
            return Ok(sugerencias);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Marca una sugerencia como leída (solo admin)
    /// </summary>
    [HttpPost("MarcarSugerenciaLeida")]
    public async Task<IActionResult> MarcarSugerenciaLeida(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message) = await _sugerenciaService.MarcarLeidaAsync(id);
            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
