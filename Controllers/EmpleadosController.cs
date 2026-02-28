// ═══ EmpleadosController.cs — CRUD empleados, horarios y excepciones ═══

using Gimnasio.Data;
using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

public class EmpleadosController : NegocioBaseController
{
    private readonly IEmpleadoService _empleadoService;
    private readonly ApplicationDbContext _context;

    public EmpleadosController(
        IEmpleadoService empleadoService,
        IAuthService authService,
        ApplicationDbContext context) : base(authService)
    {
        _empleadoService = empleadoService;
        _context = context;
    }

    // ═══ CRUD de empleados ═══

    [HttpGet("GetEmpleados")]
    public Task<IActionResult> GetEmpleados(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _empleadoService.GetEmpleadosAsync(nId)));

    [HttpGet("GetEmpleado")]
    public Task<IActionResult> GetEmpleado(Guid empleadoId, Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var empleado = await _empleadoService.GetEmpleadoAsync(empleadoId, nId);
            if (empleado == null)
                return NotFound(new { success = false, message = "Empleado no encontrado" });
            return Ok(empleado);
        });

    [HttpPost("CrearEmpleado")]
    public Task<IActionResult> CrearEmpleado([FromForm] EmpleadoCreateDto dto)
        => Execute(dto.NegocioId, async nId => ServiceResult(await _empleadoService.CrearEmpleadoAsync(dto)));

    [HttpPost("EditarEmpleado")]
    public Task<IActionResult> EditarEmpleado([FromForm] EmpleadoCreateDto dto)
        => Execute(dto.NegocioId, async nId => ServiceResult(await _empleadoService.EditarEmpleadoAsync(dto)));

    [HttpPost("EliminarEmpleado")]
    public Task<IActionResult> EliminarEmpleado(Guid empleadoId, Guid negocioId)
        => Execute(negocioId, async nId => ServiceResult(await _empleadoService.EliminarEmpleadoAsync(empleadoId, nId)));

    [HttpGet("GetEmpleadosConDisponibilidad")]
    public Task<IActionResult> GetEmpleadosConDisponibilidad(Guid negocioId, DateTime fecha)
        => Execute(negocioId, async nId => Ok(await _empleadoService.GetEmpleadosConDisponibilidadAsync(nId, fecha)));

    // ═══ Horarios semanales ═══

    [HttpGet("GetHorarios")]
    public Task<IActionResult> GetHorarios(Guid empleadoId, Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _empleadoService.GetHorariosAsync(empleadoId, nId)));

    [HttpPost("GuardarHorarios")]
    public Task<IActionResult> GuardarHorarios([FromBody] HorarioEmpleadoDto dto)
        => Execute(dto.NegocioId, async nId => ServiceResult(await _empleadoService.GuardarHorariosAsync(dto)));

    // ═══ Excepciones de horario ═══

    [HttpGet("GetExcepciones")]
    public Task<IActionResult> GetExcepciones(Guid empleadoId, Guid negocioId, DateTime? desde, DateTime? hasta)
        => Execute(negocioId, async nId => Ok(await _empleadoService.GetExcepcionesAsync(empleadoId, nId, desde, hasta)));

    [HttpPost("CrearExcepcionHorario")]
    public Task<IActionResult> CrearExcepcionHorario(
        Guid empleadoId, Guid negocioId, DateTime fecha, bool esDiaLibre,
        string horaInicio, string horaFin, string motivo)
        => Execute(negocioId, async nId =>
        {
            var (success, message, excepcionId) = await _empleadoService.CrearExcepcionAsync(
                empleadoId, nId, fecha, esDiaLibre, horaInicio, horaFin, motivo);
            if (!success) return BadRequest(new { success, message });
            return Ok(new { success, message, excepcionId });
        });

    [HttpPost("EliminarExcepcionHorario")]
    public Task<IActionResult> EliminarExcepcionHorario(Guid excepcionId, Guid negocioId)
        => Execute(negocioId, async nId => ServiceResult(await _empleadoService.EliminarExcepcionAsync(excepcionId, nId)));
}
