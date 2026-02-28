using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

public class ServiciosController : NegocioBaseController
{
    private readonly IServicioNegocioService _service;

    public ServiciosController(IServicioNegocioService service, IAuthService authService)
        : base(authService) => _service = service;

    // ── Consultas ──

    [HttpGet("GetServicios")]
    public Task<IActionResult> GetServicios(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _service.GetServiciosAsync(nId)));

    [HttpGet("GetServicio")]
    public Task<IActionResult> GetServicio(Guid servicioId, Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var servicio = await _service.GetServicioAsync(servicioId, nId);
            if (servicio == null)
                return NotFound(new { success = false, message = "Servicio no encontrado" });
            return Ok(servicio);
        });

    // ── CRUD ──

    [HttpPost("CrearServicio")]
    public Task<IActionResult> CrearServicio([FromForm] ServicioCreateDto dto)
        => Execute(dto.NegocioId, async _ => ServiceResult(await _service.CrearServicioAsync(dto)));

    [HttpPost("EditarServicio")]
    public Task<IActionResult> EditarServicio([FromForm] ServicioCreateDto dto)
        => Execute(dto.NegocioId, async _ => ServiceResult(await _service.EditarServicioAsync(dto)));

    [HttpPost("EliminarServicio")]
    public Task<IActionResult> EliminarServicio(Guid servicioId, Guid negocioId)
        => Execute(negocioId, async nId => ServiceResult(await _service.EliminarServicioAsync(servicioId, nId)));
}
