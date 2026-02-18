// ═══════════════════════════════════════════════════════════
// EmpleadosController.cs — Controlador de empleados del modelo artesanal
// CRUD de empleados, horarios semanales y excepciones de horario
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class EmpleadosController : Controller
{
    private readonly IEmpleadoService _empleadoService;
    private readonly IAuthService _authService;

    public EmpleadosController(IEmpleadoService empleadoService, IAuthService authService)
    {
        _empleadoService = empleadoService;
        _authService = authService;
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE EMPLEADOS
    // ═══════════════════════════════════════════════════════════

    [HttpGet("GetEmpleados")]
    public async Task<IActionResult> GetEmpleados(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var empleados = await _empleadoService.GetEmpleadosAsync(negocioId);
            return Ok(empleados);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetEmpleado")]
    public async Task<IActionResult> GetEmpleado(Guid empleadoId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var empleado = await _empleadoService.GetEmpleadoAsync(empleadoId, negocioId);
            if (empleado == null)
                return NotFound(new { success = false, message = "Empleado no encontrado" });

            return Ok(empleado);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("CrearEmpleado")]
    public async Task<IActionResult> CrearEmpleado([FromForm] EmpleadoCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _empleadoService.CrearEmpleadoAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("EditarEmpleado")]
    public async Task<IActionResult> EditarEmpleado([FromForm] EmpleadoCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _empleadoService.EditarEmpleadoAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("EliminarEmpleado")]
    public async Task<IActionResult> EliminarEmpleado(Guid empleadoId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _empleadoService.EliminarEmpleadoAsync(empleadoId, negocioId);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetEmpleadosConDisponibilidad")]
    public async Task<IActionResult> GetEmpleadosConDisponibilidad(Guid negocioId, DateTime fecha)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var empleados = await _empleadoService.GetEmpleadosConDisponibilidadAsync(negocioId, fecha);
            return Ok(empleados);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // HORARIOS
    // ═══════════════════════════════════════════════════════════

    [HttpGet("GetHorarios")]
    public async Task<IActionResult> GetHorarios(Guid empleadoId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var horarios = await _empleadoService.GetHorariosAsync(empleadoId, negocioId);
            return Ok(horarios);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("GuardarHorarios")]
    public async Task<IActionResult> GuardarHorarios([FromBody] HorarioEmpleadoDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _empleadoService.GuardarHorariosAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // EXCEPCIONES DE HORARIO
    // ═══════════════════════════════════════════════════════════

    [HttpPost("CrearExcepcionHorario")]
    public async Task<IActionResult> CrearExcepcionHorario(
        Guid empleadoId, Guid negocioId, DateTime fecha, bool esDiaLibre,
        string horaInicio, string horaFin, string motivo)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _empleadoService.CrearExcepcionAsync(
                empleadoId, negocioId, fecha, esDiaLibre, horaInicio, horaFin, motivo);

            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("EliminarExcepcionHorario")]
    public async Task<IActionResult> EliminarExcepcionHorario(Guid excepcionId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _empleadoService.EliminarExcepcionAsync(excepcionId, negocioId);
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
