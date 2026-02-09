using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Gimnasios")]
[Authorize]
public class LogsController : Controller
{
    private readonly ILogService _logService;
    private readonly IAuthService _authService;

    public LogsController(ILogService logService, IAuthService authService)
    {
        _logService = logService;
        _authService = authService;
    }

    [HttpPost("CrearLog")]
    public async Task<IActionResult> CrearLog([FromForm] LogCreateDto model)
    {
        try
        {
            var gimnasioId = _authService.GetGimnasioId(User);
            if (!gimnasioId.HasValue || model.GimnasioId != gimnasioId.Value)
                return Forbid();

            var (success, message) = await _logService.CrearLogManualAsync(model.GimnasioId, model.Message, model.Monto);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetLogs")]
    public async Task<IActionResult> GetLogs(Guid gimnasioId)
    {
        try
        {
            var gymId = _authService.GetGimnasioId(User);
            if (!gymId.HasValue || gimnasioId != gymId.Value)
                return Forbid();

            var logs = await _logService.GetLogsAsync(gimnasioId);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetLog")]
    public async Task<IActionResult> GetLog(Guid id, Guid gimnasioId)
    {
        try
        {
            var log = await _logService.GetLogAsync(id, gimnasioId);
            if (log == null)
                return NotFound(new { success = false, message = "Log no encontrado" });

            return Ok(log);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetOldestLogDate")]
    public async Task<IActionResult> GetOldestLogDate(Guid gimnasioId)
    {
        try
        {
            var gymId = _authService.GetGimnasioId(User);
            if (!gymId.HasValue || gimnasioId != gymId.Value)
                return Forbid();

            var oldest = await _logService.GetOldestLogDateAsync(gimnasioId);
            return Ok(new { fecha = oldest });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("EliminarTodosLogs")]
    public async Task<IActionResult> EliminarTodosLogs(Guid gimnasioId)
    {
        try
        {
            var gymId = _authService.GetGimnasioId(User);
            if (!gymId.HasValue || gimnasioId != gymId.Value)
                return Forbid();

            var (success, message) = await _logService.EliminarTodosLogsAsync(gimnasioId);
            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("ExportLogsExcel")]
    public async Task<IActionResult> ExportLogsExcel(Guid gimnasioId)
    {
        try
        {
            var gymId = _authService.GetGimnasioId(User);
            if (!gymId.HasValue || gimnasioId != gymId.Value)
                return Forbid();

            var content = await _logService.ExportLogsExcelAsync(gimnasioId);
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Logs_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
