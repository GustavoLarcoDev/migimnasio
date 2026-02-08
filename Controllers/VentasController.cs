using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Gimnasios")]
[Authorize]
public class VentasController : Controller
{
    private readonly IVentasService _ventasService;

    public VentasController(IVentasService ventasService)
    {
        _ventasService = ventasService;
    }

    [HttpGet("GetVentasStats")]
    public async Task<IActionResult> GetVentasStats(Guid gimnasioId)
    {
        try
        {
            var stats = await _ventasService.GetVentasStatsAsync(gimnasioId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetChartData")]
    public async Task<IActionResult> GetChartData(Guid gimnasioId, string periodo = "semana")
    {
        try
        {
            var data = await _ventasService.GetChartDataAsync(gimnasioId, periodo);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetClientesChartData")]
    public async Task<IActionResult> GetClientesChartData(Guid gimnasioId, string periodo = "semana")
    {
        try
        {
            var data = await _ventasService.GetClientesChartDataAsync(gimnasioId, periodo);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
