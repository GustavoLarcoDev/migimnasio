using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

public class LogsController : NegocioBaseController
{
    private readonly ILogService _logService;

    public LogsController(ILogService logService, IAuthService authService)
        : base(authService) => _logService = logService;

    // ── Crear ──

    [HttpPost("CrearLog")]
    public Task<IActionResult> CrearLog([FromForm] LogCreateDto model)
        => Execute(model.NegocioId, async nId => ServiceResult(await _logService.CrearLogManualAsync(nId, model.Message, model.Monto)));

    // ── Consultas ──

    [HttpGet("GetLogs")]
    public Task<IActionResult> GetLogs(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _logService.GetLogsAsync(nId)));

    [HttpGet("GetLog")]
    public Task<IActionResult> GetLog(Guid id, Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var log = await _logService.GetLogAsync(id, nId);
            if (log == null)
                return NotFound(new { success = false, message = "Log no encontrado" });
            return Ok(log);
        });

    [HttpGet("GetOldestLogDate")]
    public Task<IActionResult> GetOldestLogDate(Guid negocioId)
        => Execute(negocioId, async nId => Ok(new { fecha = await _logService.GetOldestLogDateAsync(nId) }));

    // ── Eliminar ──

    [HttpPost("EliminarTodosLogs")]
    public Task<IActionResult> EliminarTodosLogs(Guid negocioId)
        => Execute(negocioId, async nId => ServiceResult(await _logService.EliminarTodosLogsAsync(nId)));

    // ── Excel ──

    [HttpGet("ExportLogsExcel")]
    public Task<IActionResult> ExportLogsExcel(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var content = await _logService.ExportLogsExcelAsync(nId);
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Logs_{TimeHelper.Now:yyyyMMdd}.xlsx");
        });
}
