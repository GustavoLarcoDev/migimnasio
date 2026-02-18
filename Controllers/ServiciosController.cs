// ═══════════════════════════════════════════════════════════
// ServiciosController.cs — Controlador de servicios del modelo artesanal
// CRUD de servicios que ofrece el negocio
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class ServiciosController : Controller
{
    private readonly IServicioNegocioService _servicioService;
    private readonly IAuthService _authService;

    public ServiciosController(IServicioNegocioService servicioService, IAuthService authService)
    {
        _servicioService = servicioService;
        _authService = authService;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS DE SERVICIOS
    // ═══════════════════════════════════════════════════════════

    [HttpGet("GetServicios")]
    public async Task<IActionResult> GetServicios(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var servicios = await _servicioService.GetServiciosAsync(negocioId);
            return Ok(servicios);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetServicio")]
    public async Task<IActionResult> GetServicio(Guid servicioId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var servicio = await _servicioService.GetServicioAsync(servicioId, negocioId);
            if (servicio == null)
                return NotFound(new { success = false, message = "Servicio no encontrado" });

            return Ok(servicio);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE SERVICIOS
    // ═══════════════════════════════════════════════════════════

    [HttpPost("CrearServicio")]
    public async Task<IActionResult> CrearServicio([FromForm] ServicioCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _servicioService.CrearServicioAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("EditarServicio")]
    public async Task<IActionResult> EditarServicio([FromForm] ServicioCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _servicioService.EditarServicioAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("EliminarServicio")]
    public async Task<IActionResult> EliminarServicio(Guid servicioId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _servicioService.EliminarServicioAsync(servicioId, negocioId);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
