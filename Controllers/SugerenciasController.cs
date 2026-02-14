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

    // Admin endpoints
    [HttpGet("Sugerencias")]
    public IActionResult Sugerencias()
    {
        if (!_authService.IsAdmin(User))
            return RedirectToAction("Login", "Auth");

        return View("~/Views/Negocios/Sugerencias.cshtml");
    }

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
