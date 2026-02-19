// ═══════════════════════════════════════════════════════════
// VentasService.cs — Servicio de estadísticas financieras y gráficos
//
// FUENTE DE VERDAD: Todas las cifras financieras se calculan EXCLUSIVAMENTE
// desde la tabla Logs (no desde la tabla Clientes). Esto es intencional:
// si un dueño elimina un cliente, los ingresos que ese cliente generó
// deben seguir apareciendo en el historial financiero.
//
// La tabla Clientes se usa SOLO para métricas de membresías activas:
//   - Membresías de 30+ días creadas este mes
//   - Clientes diarios de hoy
//
// PERIODOS DISPONIBLES:
//   - "dia": últimas 24 horas en bloques de 3 horas (8 puntos)
//   - "semana": 7 días individuales
//   - "mes": 4 semanas
//   - "anio": 12 meses individuales
//
// DATOS PARA APEXCHARTS: Los métodos GetChartDataAsync y GetClientesChartDataAsync
// retornan arrays de labels y valores listos para pasarle directamente a ApexCharts
// en el frontend sin transformaciones adicionales.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Implementación del servicio de ventas. Calcula estadísticas financieras
/// (ingresos, gastos, ganancias) y genera datos para los gráficos ApexCharts
/// del dashboard del negocio.
///
/// Usa <see cref="LogService"/> como fuente inmutable de datos financieros.
/// </summary>
public class VentasService : IVentasService
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor: recibe el DbContext por inyección de dependencias.
    /// </summary>
    public VentasService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // ESTADÍSTICAS DE VENTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula todas las métricas financieras del negocio para el resumen
    /// de la pestaña Ventas del dashboard.
    ///
    /// Retorna:
    ///   - Ingresos, gastos y ganancias para: día, semana, mes, año
    ///   - membresias30Dias: suma de precios de membresías de 30+ días creadas este mes
    ///   - clientesDiariosHoy: cuántos clientes diarios entraron hoy
    ///
    /// Los datos se cargan todos en memoria con dos queries (clientes y logs)
    /// en lugar de hacer muchos queries pequeños, porque es más eficiente
    /// para cálculos con múltiples periodos de tiempo.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    public async Task<object> GetVentasStatsAsync(Guid negocioId)
    {
        var hoy = TimeHelper.Now.Date;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var inicioAnio = new DateTime(hoy.Year, 1, 1);

        // Calcular inicio de la semana actual (lunes = día 0 en formato europeo).
        // (((int)hoy.DayOfWeek + 6) % 7) convierte el DayOfWeek de C# (domingo=0)
        // al formato donde lunes=0, martes=1, ... domingo=6.
        var inicioSemana = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));

        // Cargar clientes y logs en memoria para hacer todos los cálculos
        // en LINQ to Objects (más flexible para operaciones de fecha).
        var clientes = await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
            .ToListAsync();

        var logs = await _context.Logs
            .Where(l => l.NegocioId == negocioId)
            .ToListAsync();

        // Membresías de 30+ días creadas este mes.
        // "30+ días" identifica membresías mensuales (vs diarias que son de 1 día).
        var membresias30Dias = clientes
            .Where(c => c.Dias >= 30 && c.FechaDeCreacion >= inicioMes)
            .Sum(c => c.Precio);

        // Clientes que ingresaron hoy marcados como "diario" (pase de un día)
        var clientesDiariosHoy = clientes
            .Count(c => c.EsDiario && c.FechaDeCreacion.Date == hoy);

        // Ingresos por periodo: solo logs con monto positivo (pagos recibidos)
        var ingresosDia = logs.Where(l => l.Fecha.Date == hoy && l.Monto > 0).Sum(l => l.Monto);
        var ingresosSemana = logs.Where(l => l.Fecha.Date >= inicioSemana && l.Monto > 0).Sum(l => l.Monto);
        var ingresosMes = logs.Where(l => l.Fecha >= inicioMes && l.Monto > 0).Sum(l => l.Monto);
        var ingresosAnio = logs.Where(l => l.Fecha >= inicioAnio && l.Monto > 0).Sum(l => l.Monto);

        // Gastos por periodo: solo logs con monto negativo (gastos registrados).
        // Se usa Math.Abs para mostrar el valor absoluto (positivo) del gasto.
        var gastosDia = logs.Where(l => l.Fecha.Date == hoy && l.Monto < 0).Sum(l => Math.Abs(l.Monto));
        var gastosSemana = logs.Where(l => l.Fecha.Date >= inicioSemana && l.Monto < 0).Sum(l => Math.Abs(l.Monto));
        var gastosMes = logs.Where(l => l.Fecha >= inicioMes && l.Monto < 0).Sum(l => Math.Abs(l.Monto));
        var gastosAnio = logs.Where(l => l.Fecha >= inicioAnio && l.Monto < 0).Sum(l => Math.Abs(l.Monto));

        // Ganancia neta = ingresos - gastos para cada periodo
        var gananciaDia = ingresosDia - gastosDia;
        var gananciaSemana = ingresosSemana - gastosSemana;
        var gananciaMes = ingresosMes - gastosMes;
        var gananciaAnio = ingresosAnio - gastosAnio;

        return new
        {
            membresias30Dias,
            clientesDiariosHoy,
            ingresosDia,
            ingresosSemana,
            ingresosMes,
            ingresosAnio,
            gastosDia,
            gastosSemana,
            gastosMes,
            gastosAnio,
            gananciaDia,
            gananciaSemana,
            gananciaMes,
            gananciaAnio
        };
    }

    // ═══════════════════════════════════════════════════════════
    // DATOS PARA GRÁFICOS DE VENTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Genera arrays de datos listos para el gráfico de líneas de ApexCharts
    /// que muestra ingresos vs gastos en el tiempo.
    ///
    /// Agrupaciones según el periodo:
    /// <list type="bullet">
    ///   <item>"dia": 8 bloques de 3 horas que cubren las últimas 24 horas</item>
    ///   <item>"semana": 7 puntos, uno por cada día de la semana pasada</item>
    ///   <item>"mes": 4 puntos, uno por cada semana del mes</item>
    ///   <item>"anio": 12 puntos, uno por cada mes del año</item>
    /// </list>
    ///
    /// Retorna { labels, ingresos, gastos } donde cada array tiene la misma longitud.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <param name="periodo">"dia", "semana", "mes" o "anio".</param>
    public async Task<object> GetChartDataAsync(Guid negocioId, string periodo)
    {
        var hoy = TimeHelper.Now.Date;
        var ahora = TimeHelper.Now;

        // Cargar todos los logs en memoria para hacer los cálculos por periodo
        var logs = await _context.Logs
            .Where(l => l.NegocioId == negocioId)
            .ToListAsync();

        var labels = new List<string>();
        var ingresos = new List<decimal>();
        var gastos = new List<decimal>();

        switch (periodo.ToLower())
        {
            case "dia":
                // 8 bloques de 3 horas: desde 24 horas atrás hasta ahora.
                // i=7 → hace 21 horas, i=6 → hace 18 horas, ..., i=0 → ahora.
                for (int i = 7; i >= 0; i--)
                {
                    var bloqueInicio = ahora.AddHours(-i * 3);
                    var bloqueFin = bloqueInicio.AddHours(3);
                    labels.Add(bloqueInicio.ToString("hh tt")); // Ej: "09 AM"

                    ingresos.Add(logs.Where(l => l.Fecha >= bloqueInicio && l.Fecha < bloqueFin && l.Monto > 0).Sum(l => l.Monto));
                    gastos.Add(logs.Where(l => l.Fecha >= bloqueInicio && l.Fecha < bloqueFin && l.Monto < 0).Sum(l => Math.Abs(l.Monto)));
                }
                break;

            case "semana":
                // 7 días individuales, de hace 6 días hasta hoy
                for (int i = 6; i >= 0; i--)
                {
                    var fecha = hoy.AddDays(-i);
                    labels.Add(fecha.ToString("dd/MM")); // Ej: "15/02"

                    ingresos.Add(logs.Where(l => l.Fecha.Date == fecha && l.Monto > 0).Sum(l => l.Monto));
                    gastos.Add(logs.Where(l => l.Fecha.Date == fecha && l.Monto < 0).Sum(l => Math.Abs(l.Monto)));
                }
                break;

            case "mes":
                // 4 semanas del mes actual
                for (int i = 3; i >= 0; i--)
                {
                    var inicioSemana = hoy.AddDays(-7 * i - (int)hoy.DayOfWeek);
                    var finSemana = inicioSemana.AddDays(6);
                    labels.Add($"Sem {4 - i}"); // Ej: "Sem 1", "Sem 2", etc.

                    ingresos.Add(logs.Where(l => l.Fecha.Date >= inicioSemana && l.Fecha.Date <= finSemana && l.Monto > 0).Sum(l => l.Monto));
                    gastos.Add(logs.Where(l => l.Fecha.Date >= inicioSemana && l.Fecha.Date <= finSemana && l.Monto < 0).Sum(l => Math.Abs(l.Monto)));
                }
                break;

            case "anio":
                // 12 meses, de hace 11 meses hasta el mes actual
                for (int i = 11; i >= 0; i--)
                {
                    var fecha = hoy.AddMonths(-i);
                    var inicioMes = new DateTime(fecha.Year, fecha.Month, 1);
                    var finMes = inicioMes.AddMonths(1).AddDays(-1);
                    labels.Add(fecha.ToString("MMM")); // Ej: "Ene", "Feb", etc.

                    ingresos.Add(logs.Where(l => l.Fecha.Date >= inicioMes && l.Fecha.Date <= finMes && l.Monto > 0).Sum(l => l.Monto));
                    gastos.Add(logs.Where(l => l.Fecha.Date >= inicioMes && l.Fecha.Date <= finMes && l.Monto < 0).Sum(l => Math.Abs(l.Monto)));
                }
                break;
        }

        return new { labels, ingresos, gastos };
    }

    // ═══════════════════════════════════════════════════════════
    // DATOS PARA GRÁFICO DE CLIENTES NUEVOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Genera arrays de datos para el gráfico de barras que muestra cuántos
    /// clientes nuevos se registraron en cada periodo de tiempo.
    ///
    /// Usa la tabla Clientes (FechaDeCreacion) en lugar de Logs, porque
    /// el conteo de clientes es independiente de los movimientos de dinero.
    ///
    /// Mismas agrupaciones que <see cref="GetChartDataAsync"/>:
    /// "dia" (bloques de 3h), "semana" (7 días), "mes" (4 semanas), "anio" (12 meses).
    ///
    /// Retorna { labels, nuevosClientes } donde cada array tiene la misma longitud.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <param name="periodo">"dia", "semana", "mes" o "anio".</param>
    public async Task<object> GetClientesChartDataAsync(Guid negocioId, string periodo)
    {
        var hoy = TimeHelper.Now.Date;
        var ahora = TimeHelper.Now;

        // Cargar todos los clientes del negocio para filtrar por fecha en memoria
        var clientes = await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
            .ToListAsync();

        var labels = new List<string>();
        var nuevosClientes = new List<int>();

        switch (periodo.ToLower())
        {
            case "dia":
                // 8 bloques de 3 horas
                for (int i = 7; i >= 0; i--)
                {
                    var bloqueInicio = ahora.AddHours(-i * 3);
                    var bloqueFin = bloqueInicio.AddHours(3);
                    labels.Add(bloqueInicio.ToString("hh tt"));
                    nuevosClientes.Add(clientes.Count(c => c.FechaDeCreacion >= bloqueInicio && c.FechaDeCreacion < bloqueFin));
                }
                break;

            case "semana":
                // 7 días individuales
                for (int i = 6; i >= 0; i--)
                {
                    var fecha = hoy.AddDays(-i);
                    labels.Add(fecha.ToString("dd/MM"));
                    nuevosClientes.Add(clientes.Count(c => c.FechaDeCreacion.Date == fecha));
                }
                break;

            case "mes":
                // 4 semanas del mes
                for (int i = 3; i >= 0; i--)
                {
                    var inicioSemana = hoy.AddDays(-7 * i - (int)hoy.DayOfWeek);
                    var finSemana = inicioSemana.AddDays(6);
                    labels.Add($"Sem {4 - i}");
                    nuevosClientes.Add(clientes.Count(c => c.FechaDeCreacion.Date >= inicioSemana && c.FechaDeCreacion.Date <= finSemana));
                }
                break;

            case "anio":
                // 12 meses
                for (int i = 11; i >= 0; i--)
                {
                    var fecha = hoy.AddMonths(-i);
                    var inicioMes = new DateTime(fecha.Year, fecha.Month, 1);
                    var finMes = inicioMes.AddMonths(1).AddDays(-1);
                    labels.Add(fecha.ToString("MMM"));
                    nuevosClientes.Add(clientes.Count(c => c.FechaDeCreacion.Date >= inicioMes && c.FechaDeCreacion.Date <= finMes));
                }
                break;
        }

        return new { labels, nuevosClientes };
    }
}
