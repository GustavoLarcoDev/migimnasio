// ═══════════════════════════════════════════════════════════
// VentasService.cs — Servicio de ventas y estadísticas financieras
// Calcula ingresos, gastos y ganancias SOLO desde la tabla Logs
// (fuente inmutable). Eliminar un cliente no afecta los registros.
// La tabla Clientes solo se usa para métricas de membresías.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class VentasService : IVentasService
{
    private readonly ApplicationDbContext _context;

    public VentasService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // ESTADÍSTICAS DE VENTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula todas las métricas financieras del negocio:
    /// - Ingresos, gastos y ganancias por día, semana, mes y año
    /// - Membresías de 30+ días creadas este mes
    /// - Clientes diarios de hoy
    /// </summary>
    public async Task<object> GetVentasStatsAsync(Guid negocioId)
    {
        // Definir los límites de cada periodo de tiempo
        var hoy = TimeHelper.Now.Date;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var inicioAnio = new DateTime(hoy.Year, 1, 1);

        // Calcular inicio de la semana actual (lunes)
        var inicioSemana = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));

        // Cargar datos a memoria para evitar múltiples queries a la DB
        var clientes = await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
            .ToListAsync();

        var logs = await _context.Logs
            .Where(l => l.NegocioId == negocioId)
            .ToListAsync();

        // Membresías de 30+ días: suma de precios de membresías largas creadas este mes
        var membresias30Dias = clientes
            .Where(c => c.Dias >= 30 && c.FechaDeCreacion >= inicioMes)
            .Sum(c => c.Precio);

        // Clientes diarios de hoy
        var clientesDiariosHoy = clientes
            .Count(c => c.EsDiario && c.FechaDeCreacion.Date == hoy);

        // Ingresos por periodo (monto > 0 en Logs)
        var ingresosDia = logs.Where(l => l.Fecha.Date == hoy && l.Monto > 0).Sum(l => l.Monto);
        var ingresosSemana = logs.Where(l => l.Fecha.Date >= inicioSemana && l.Monto > 0).Sum(l => l.Monto);
        var ingresosMes = logs.Where(l => l.Fecha >= inicioMes && l.Monto > 0).Sum(l => l.Monto);
        var ingresosAnio = logs.Where(l => l.Fecha >= inicioAnio && l.Monto > 0).Sum(l => l.Monto);

        // Gastos por periodo (monto < 0 en Logs, se muestra como positivo)
        var gastosDia = logs.Where(l => l.Fecha.Date == hoy && l.Monto < 0).Sum(l => Math.Abs(l.Monto));
        var gastosSemana = logs.Where(l => l.Fecha.Date >= inicioSemana && l.Monto < 0).Sum(l => Math.Abs(l.Monto));
        var gastosMes = logs.Where(l => l.Fecha >= inicioMes && l.Monto < 0).Sum(l => Math.Abs(l.Monto));
        var gastosAnio = logs.Where(l => l.Fecha >= inicioAnio && l.Monto < 0).Sum(l => Math.Abs(l.Monto));

        // Ganancias netas = ingresos - gastos
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
    /// Genera arrays de labels, ingresos y gastos listos para ApexCharts.
    /// Cada periodo produce diferentes agrupaciones:
    /// - "dia": 8 bloques de 3 horas
    /// - "semana": 7 días individuales
    /// - "mes": 4 semanas
    /// - "anio": 12 meses
    /// </summary>
    public async Task<object> GetChartDataAsync(Guid negocioId, string periodo)
    {
        var hoy = TimeHelper.Now.Date;
        var ahora = TimeHelper.Now;

        var logs = await _context.Logs
            .Where(l => l.NegocioId == negocioId)
            .ToListAsync();

        var labels = new List<string>();
        var ingresos = new List<decimal>();
        var gastos = new List<decimal>();

        switch (periodo.ToLower())
        {
            case "dia":
                // Últimas 24 horas en bloques de 3 horas (8 puntos)
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
                // Últimos 7 días individuales
                for (int i = 6; i >= 0; i--)
                {
                    var fecha = hoy.AddDays(-i);
                    labels.Add(fecha.ToString("dd/MM"));

                    ingresos.Add(logs.Where(l => l.Fecha.Date == fecha && l.Monto > 0).Sum(l => l.Monto));
                    gastos.Add(logs.Where(l => l.Fecha.Date == fecha && l.Monto < 0).Sum(l => Math.Abs(l.Monto)));
                }
                break;

            case "mes":
                // Últimas 4 semanas
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
                // Últimos 12 meses
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

    // ═══════════════════════════════════════════════════════════
    // DATOS PARA GRÁFICO DE CLIENTES NUEVOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Genera arrays de labels y nuevosClientes agrupados por periodo
    /// </summary>
    public async Task<object> GetClientesChartDataAsync(Guid negocioId, string periodo)
    {
        var hoy = TimeHelper.Now.Date;
        var ahora = TimeHelper.Now;

        var clientes = await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
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
