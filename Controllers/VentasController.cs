// ═══════════════════════════════════════════════════════════
// VentasController.cs — Controlador de ventas y estadísticas financieras
// Provee endpoints AJAX para estadísticas de ingresos/gastos
// y datos formateados para gráficos ApexCharts
// ═══════════════════════════════════════════════════════════

using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class VentasController : Controller
{
    private readonly IVentasService _ventasService;
    private readonly IAuthService _authService;

    public VentasController(IVentasService ventasService, IAuthService authService)
    {
        _ventasService = ventasService;
        _authService = authService;
    }

    /// <summary>
    /// Obtiene estadísticas completas de ventas: ingresos, gastos y ganancias
    /// agrupados por día, semana, mes y año
    /// </summary>
    [HttpGet("GetVentasStats")]
    public async Task<IActionResult> GetVentasStats(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var stats = await _ventasService.GetVentasStatsAsync(negocioId);
            return Ok(stats);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene datos para el gráfico de ingresos vs gastos.
    /// Periodos: "dia" (24h), "semana" (7 días), "mes" (4 semanas), "anio" (12 meses)
    /// </summary>
    [HttpGet("GetChartData")]
    public async Task<IActionResult> GetChartData(Guid negocioId, string periodo = "semana")
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var data = await _ventasService.GetChartDataAsync(negocioId, periodo);
            return Ok(data);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene datos para el gráfico de clientes nuevos por periodo
    /// </summary>
    [HttpGet("GetClientesChartData")]
    public async Task<IActionResult> GetClientesChartData(Guid negocioId, string periodo = "semana")
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var data = await _ventasService.GetClientesChartDataAsync(negocioId, periodo);
            return Ok(data);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }
}
