namespace Gimnasio.Services;

/// <summary>
/// Interfaz para el servicio de ventas y estadísticas financieras.
/// Proporciona métricas de ingresos, gastos y ganancias agrupadas por periodo,
/// así como datos formateados para gráficos del dashboard.
/// </summary>
public interface IVentasService
{
    /// <summary>
    /// Obtiene todas las estadísticas de ventas para un negocio:
    /// ingresos, gastos y ganancias por día, semana, mes y año.
    /// También incluye membresías de 30+ días del mes y conteo de clientes diarios.
    /// </summary>
    /// <param name="negocioId">ID único del negocio</param>
    /// <returns>Objeto con todas las métricas financieras</returns>
    Task<object> GetVentasStatsAsync(Guid negocioId);

    /// <summary>
    /// Genera datos para gráficos de ventas agrupados por el periodo indicado.
    /// Retorna arrays de labels, ingresos y gastos listos para ApexCharts.
    /// </summary>
    /// <param name="negocioId">ID único del negocio</param>
    /// <param name="periodo">Periodo: "dia" (24h), "semana" (7 días), "mes" (4 semanas), "anio" (12 meses)</param>
    /// <returns>Objeto con arrays: labels, ingresos, gastos</returns>
    Task<object> GetChartDataAsync(Guid negocioId, string periodo);

    /// <summary>
    /// Genera datos para gráfico de clientes nuevos agrupados por periodo.
    /// Retorna arrays de labels y nuevosClientes listos para ApexCharts.
    /// </summary>
    Task<object> GetClientesChartDataAsync(Guid negocioId, string periodo);
}
