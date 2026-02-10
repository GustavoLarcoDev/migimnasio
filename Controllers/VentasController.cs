using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class VentasController : Controller
{
    private readonly IVentasService _ventasService;

    public VentasController(IVentasService ventasService)
    {
        _ventasService = ventasService;
    }

    [HttpGet("GetVentasStats")]
    public async Task<IActionResult> GetVentasStats(Guid negocioId)
    {
        try
        {
            var stats = await _ventasService.GetVentasStatsAsync(negocioId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetChartData")]
    public async Task<IActionResult> GetChartData(Guid negocioId, string periodo = "semana")
    {
        try
        {
            var data = await _ventasService.GetChartDataAsync(negocioId, periodo);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetClientesChartData")]
    public async Task<IActionResult> GetClientesChartData(Guid negocioId, string periodo = "semana")
    {
        try
        {
            var data = await _ventasService.GetClientesChartDataAsync(negocioId, periodo);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
