using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

public class SugerenciasController : NegocioBaseController
{
    private readonly ISugerenciaService _service;

    public SugerenciasController(ISugerenciaService service, IAuthService authService)
        : base(authService) => _service = service;

    // ── Negocio ──

    [HttpPost("EnviarSugerencia")]
    public Task<IActionResult> EnviarSugerencia(Guid negocioId, string mensaje)
        => Execute(negocioId, async nId =>
        {
            var negocioNombre = User.Identity?.Name ?? "Desconocido";
            return ServiceResult(await _service.CrearSugerenciaAsync(nId, negocioNombre, mensaje));
        });

    // ── Admin ──

    [HttpGet("Sugerencias")]
    public IActionResult Sugerencias()
    {
        if (!AuthService.IsAdmin(User))
            return RedirectToAction("Login", "Auth");
        return View("~/Views/Negocios/Sugerencias.cshtml");
    }

    [HttpGet("GetSugerencias")]
    public async Task<IActionResult> GetSugerencias()
    {
        try
        {
            if (!AuthService.IsAdmin(User))
                return Forbid();
            return Ok(await _service.GetSugerenciasAsync());
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("MarcarSugerenciaLeida")]
    public async Task<IActionResult> MarcarSugerenciaLeida(Guid id)
    {
        try
        {
            if (!AuthService.IsAdmin(User))
                return Forbid();
            return ServiceResult(await _service.MarcarLeidaAsync(id));
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }
}
