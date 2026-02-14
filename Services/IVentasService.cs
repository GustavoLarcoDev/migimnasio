// ═══════════════════════════════════════════════════════════
// IVentasService.cs — Contrato del servicio de ventas
// Define las operaciones para estadísticas financieras
// y generación de datos para gráficos ApexCharts
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Services;

public interface IVentasService
{
    /// <summary>
    /// Obtiene estadísticas completas de ventas: ingresos, gastos y ganancias
    /// por día, semana, mes y año. También incluye membresías de 30+ días
    /// y conteo de clientes diarios de hoy.
    /// </summary>
    Task<object> GetVentasStatsAsync(Guid negocioId);

    /// <summary>
    /// Genera datos para gráficos de ingresos vs gastos (ApexCharts).
    /// Periodos: "dia" (24h en bloques de 3h), "semana" (7 días),
    /// "mes" (4 semanas), "anio" (12 meses)
    /// </summary>
    Task<object> GetChartDataAsync(Guid negocioId, string periodo);

    /// <summary>
    /// Genera datos para gráfico de clientes nuevos agrupados por periodo
    /// </summary>
    Task<object> GetClientesChartDataAsync(Guid negocioId, string periodo);
}
