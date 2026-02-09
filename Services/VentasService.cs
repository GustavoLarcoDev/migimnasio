using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Servicio de ventas y estadísticas financieras.
/// Todos los ingresos y gastos se calculan SOLO desde la tabla Logs (fuente inmutable).
/// Eliminar un cliente no afecta los registros financieros.
/// La tabla Clientes solo se usa para métricas de membresías y conteo de clientes.
/// </summary>
public class VentasService : IVentasService
{
    private readonly ApplicationDbContext _context;

    public VentasService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene estadísticas de ventas completas para un gimnasio.
    /// Calcula ingresos, gastos y ganancias agrupados por día, semana, mes y año.
    /// También calcula membresías de 30+ días creadas en el mes y clientes diarios de hoy.
    /// </summary>
    /// <param name="gimnasioId">ID único del gimnasio para filtrar datos</param>
    /// <returns>Objeto anónimo con todas las métricas financieras del gimnasio</returns>
    public async Task<object> GetVentasStatsAsync(Guid gimnasioId)
    {
        // Definir los límites de cada periodo de tiempo
        var hoy = DateTime.Now.Date;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var inicioAnio = new DateTime(hoy.Year, 1, 1);

        // Calcular inicio de la semana actual (lunes)
        // DayOfWeek.Sunday = 0, Monday = 1, etc.
        // Si hoy es domingo, retroceder una semana completa para que lunes sea el inicio
        var inicioSemana = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));

        // Cargar todos los clientes y logs del gimnasio a memoria
        // para realizar cálculos en memoria (evita múltiples queries a la DB)
        var clientes = await _context.Clientes
            .Where(c => c.GimnasioId == gimnasioId)
            .ToListAsync();

        var logs = await _context.Logs
            .Where(l => l.GimnasioId == gimnasioId)
            .ToListAsync();

        // --- Métricas especiales ---

        // Membresías de 30+ días: suma de precios de clientes con membresía >= 30 días creados este mes
        var membresias30Dias = clientes
            .Where(c => c.Dias >= 30 && c.FechaDeCreacion >= inicioMes)
            .Sum(c => c.Precio);

        // Clientes diarios: cuenta de clientes marcados como "diario" creados hoy
        var clientesDiariosHoy = clientes
            .Count(c => c.EsDiario && c.FechaDeCreacion.Date == hoy);

        // --- Ingresos por periodo ---
        // Ingresos vienen SOLO de Logs (fuente inmutable de verdad financiera).
        // Cada creación, renovación y registro manual ya genera un log con el monto.
        // Así, eliminar un cliente no afecta los ingresos registrados.

        var ingresosDia = logs.Where(l => l.Fecha.Date == hoy && l.Monto > 0).Sum(l => l.Monto);
        var ingresosSemana = logs.Where(l => l.Fecha.Date >= inicioSemana && l.Monto > 0).Sum(l => l.Monto);
        var ingresosMes = logs.Where(l => l.Fecha >= inicioMes && l.Monto > 0).Sum(l => l.Monto);
        var ingresosAnio = logs.Where(l => l.Fecha >= inicioAnio && l.Monto > 0).Sum(l => l.Monto);

        // --- Gastos por periodo ---
        // Gastos = logs con monto negativo (se usa Math.Abs para mostrar como positivo)

        var gastosDia = logs.Where(l => l.Fecha.Date == hoy && l.Monto < 0).Sum(l => Math.Abs(l.Monto));
        var gastosSemana = logs.Where(l => l.Fecha.Date >= inicioSemana && l.Monto < 0).Sum(l => Math.Abs(l.Monto));
        var gastosMes = logs.Where(l => l.Fecha >= inicioMes && l.Monto < 0).Sum(l => Math.Abs(l.Monto));
        var gastosAnio = logs.Where(l => l.Fecha >= inicioAnio && l.Monto < 0).Sum(l => Math.Abs(l.Monto));

        // --- Ganancias netas = ingresos - gastos ---
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

    /// <summary>
    /// Genera datos formateados para gráficos ApexCharts según el periodo solicitado.
    /// Cada periodo produce diferentes agrupaciones:
    /// - "dia": últimas 24 horas en bloques de 3 horas (8 puntos)
    /// - "semana": últimos 7 días individuales (7 puntos)
    /// - "mes": últimas 4 semanas (4 puntos)
    /// - "anio": últimos 12 meses (12 puntos)
    /// </summary>
    /// <param name="gimnasioId">ID único del gimnasio</param>
    /// <param name="periodo">Periodo de agrupación: "dia", "semana", "mes" o "anio"</param>
    /// <returns>Objeto con labels (eje X), ingresos (serie 1) y gastos (serie 2)</returns>
    public async Task<object> GetChartDataAsync(Guid gimnasioId, string periodo)
    {
        var hoy = DateTime.Now.Date;
        var ahora = DateTime.Now;

        // Ingresos y gastos vienen SOLO de Logs (fuente inmutable).
        // Eliminar un cliente no afecta los registros financieros.
        var logs = await _context.Logs
            .Where(l => l.GimnasioId == gimnasioId)
            .ToListAsync();

        var labels = new List<string>();
        var ingresos = new List<decimal>();
        var gastos = new List<decimal>();

        switch (periodo.ToLower())
        {
            case "dia":
                for (int i = 7; i >= 0; i--)
                {
                    var bloqueInicio = ahora.AddHours(-i * 3);
                    var bloqueFin = bloqueInicio.AddHours(3);
                    labels.Add(bloqueInicio.ToString("hh tt"));

                    ingresos.Add(logs.Where(l => l.Fecha >= bloqueInicio && l.Fecha < bloqueFin && l.Monto > 0).Sum(l => l.Monto));
                    gastos.Add(logs.Where(l => l.Fecha >= bloqueInicio && l.Fecha < bloqueFin && l.Monto < 0).Sum(l => Math.Abs(l.Monto)));
                }
                break;

            case "semana":
                for (int i = 6; i >= 0; i--)
                {
                    var fecha = hoy.AddDays(-i);
                    labels.Add(fecha.ToString("dd/MM"));

                    ingresos.Add(logs.Where(l => l.Fecha.Date == fecha && l.Monto > 0).Sum(l => l.Monto));
                    gastos.Add(logs.Where(l => l.Fecha.Date == fecha && l.Monto < 0).Sum(l => Math.Abs(l.Monto)));
                }
                break;

            case "mes":
                for (int i = 3; i >= 0; i--)
                {
                    var inicioSemana = hoy.AddDays(-7 * i - (int)hoy.DayOfWeek);
                    var finSemana = inicioSemana.AddDays(6);
                    labels.Add($"Sem {4 - i}");

                    ingresos.Add(logs.Where(l => l.Fecha.Date >= inicioSemana && l.Fecha.Date <= finSemana && l.Monto > 0).Sum(l => l.Monto));
                    gastos.Add(logs.Where(l => l.Fecha.Date >= inicioSemana && l.Fecha.Date <= finSemana && l.Monto < 0).Sum(l => Math.Abs(l.Monto)));
                }
                break;

            case "anio":
                for (int i = 11; i >= 0; i--)
                {
                    var fecha = hoy.AddMonths(-i);
                    var inicioMes = new DateTime(fecha.Year, fecha.Month, 1);
                    var finMes = inicioMes.AddMonths(1).AddDays(-1);
                    labels.Add(fecha.ToString("MMM"));

                    ingresos.Add(logs.Where(l => l.Fecha.Date >= inicioMes && l.Fecha.Date <= finMes && l.Monto > 0).Sum(l => l.Monto));
                    gastos.Add(logs.Where(l => l.Fecha.Date >= inicioMes && l.Fecha.Date <= finMes && l.Monto < 0).Sum(l => Math.Abs(l.Monto)));
                }
                break;
        }

        return new { labels, ingresos, gastos };
    }

    public async Task<object> GetClientesChartDataAsync(Guid gimnasioId, string periodo)
    {
        var hoy = DateTime.Now.Date;
        var ahora = DateTime.Now;

        var clientes = await _context.Clientes
            .Where(c => c.GimnasioId == gimnasioId)
            .ToListAsync();

        var labels = new List<string>();
        var nuevosClientes = new List<int>();

        switch (periodo.ToLower())
        {
            case "dia":
                for (int i = 7; i >= 0; i--)
                {
                    var bloqueInicio = ahora.AddHours(-i * 3);
                    var bloqueFin = bloqueInicio.AddHours(3);
                    labels.Add(bloqueInicio.ToString("hh tt"));
                    nuevosClientes.Add(clientes.Count(c => c.FechaDeCreacion >= bloqueInicio && c.FechaDeCreacion < bloqueFin));
                }
                break;

            case "semana":
                for (int i = 6; i >= 0; i--)
                {
                    var fecha = hoy.AddDays(-i);
                    labels.Add(fecha.ToString("dd/MM"));
                    nuevosClientes.Add(clientes.Count(c => c.FechaDeCreacion.Date == fecha));
                }
                break;

            case "mes":
                for (int i = 3; i >= 0; i--)
                {
                    var inicioSemana = hoy.AddDays(-7 * i - (int)hoy.DayOfWeek);
                    var finSemana = inicioSemana.AddDays(6);
                    labels.Add($"Sem {4 - i}");
                    nuevosClientes.Add(clientes.Count(c => c.FechaDeCreacion.Date >= inicioSemana && c.FechaDeCreacion.Date <= finSemana));
                }
                break;

            case "anio":
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
