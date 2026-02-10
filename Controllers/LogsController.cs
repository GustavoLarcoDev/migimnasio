using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Negocios")]
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
            var negocioId = _authService.GetNegocioId(User);
            if (!negocioId.HasValue || model.NegocioId != negocioId.Value)
                return Forbid();

            var (success, message) = await _logService.CrearLogManualAsync(model.NegocioId, model.Message, model.Monto);

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
    public async Task<IActionResult> GetLogs(Guid negocioId)
    {
        try
        {
            var gymId = _authService.GetNegocioId(User);
            if (!gymId.HasValue || negocioId != gymId.Value)
                return Forbid();

            var logs = await _logService.GetLogsAsync(negocioId);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetLog")]
    public async Task<IActionResult> GetLog(Guid id, Guid negocioId)
    {
        try
        {
            var log = await _logService.GetLogAsync(id, negocioId);
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
    public async Task<IActionResult> GetOldestLogDate(Guid negocioId)
    {
        try
        {
            var gymId = _authService.GetNegocioId(User);
            if (!gymId.HasValue || negocioId != gymId.Value)
                return Forbid();

            var oldest = await _logService.GetOldestLogDateAsync(negocioId);
            return Ok(new { fecha = oldest });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("EliminarTodosLogs")]
    public async Task<IActionResult> EliminarTodosLogs(Guid negocioId)
    {
        try
        {
            var gymId = _authService.GetNegocioId(User);
            if (!gymId.HasValue || negocioId != gymId.Value)
                return Forbid();

            var (success, message) = await _logService.EliminarTodosLogsAsync(negocioId);
            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("ExportLogsExcel")]
    public async Task<IActionResult> ExportLogsExcel(Guid negocioId)
    {
        try
        {
            var gymId = _authService.GetNegocioId(User);
            if (!gymId.HasValue || negocioId != gymId.Value)
                return Forbid();

            var content = await _logService.ExportLogsExcelAsync(negocioId);
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
