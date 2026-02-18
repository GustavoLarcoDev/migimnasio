// ═══════════════════════════════════════════════════════════
// DailyReportService.cs — Servicio de fondo para resúmenes diarios
// Envía un resumen financiero diario por WhatsApp al dueño de
// cada negocio activo. Se ejecuta a las 9:00 PM hora Ecuador.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Servicio de fondo que envía un resumen diario (ingresos, nuevos clientes,
/// membresías por vencer) al dueño de cada negocio activo vía WhatsApp.
/// </summary>
public class DailyReportService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyReportService> _logger;

    public DailyReportService(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyReportService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    // BUCLE PRINCIPAL — Espera hasta las 9:00 PM Ecuador y ejecuta
    // ═══════════════════════════════════════════════════════════

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyReportService iniciado");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var ecuadorZone = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
                var nowEcuador = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorZone);
                var nextRun = nowEcuador.Date.AddHours(21);

                if (nowEcuador >= nextRun)
                    nextRun = nextRun.AddDays(1);

                var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, ecuadorZone);
                var delay = nextRunUtc - DateTime.UtcNow;

                _logger.LogInformation(
                    "Resumen diario: próxima ejecución en {Delay} ({NextRun} hora Ecuador)",
                    delay, nextRun);

                await Task.Delay(delay, stoppingToken);
                await EnviarResumenesDiariosAsync(stoppingToken);
            }
        }
        catch (TaskCanceledException) { /* Apagado normal de la aplicación */ }

        _logger.LogInformation("DailyReportService detenido");
    }

    // ═══════════════════════════════════════════════════════════
    // LÓGICA DE ENVÍO — Calcula estadísticas y envía resumen
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Para cada negocio activo, calcula las estadísticas del día
    /// (ingresos, nuevos clientes, membresías por vencer) y envía
    /// un resumen al dueño por WhatsApp.
    /// </summary>
    private async Task EnviarResumenesDiariosAsync(CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Iniciando envío de resúmenes diarios...");

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        try
        {
            var hoy = TimeHelper.Now.Date;

            // Obtener negocios activos de tipo membresias (artesanal usa otro reporte)
            var negociosActivos = await context.Negocios
                .Where(n => n.IsActive && n.TipoNegocio != "artesanal")
                .ToListAsync(stoppingToken);

            _logger.LogInformation(
                "Procesando resúmenes para {Count} negocios activos",
                negociosActivos.Count);

            var enviados = 0;
            var errores = 0;

            foreach (var negocio in negociosActivos)
            {
                // Verificar cancelación antes de procesar cada negocio
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    // Validar que el negocio tenga teléfono del dueño
                    if (string.IsNullOrWhiteSpace(negocio.Telefono))
                    {
                        _logger.LogWarning(
                            "Negocio {NegocioId} ({Nombre}) no tiene teléfono registrado, se omite",
                            negocio.NegocioId, negocio.NegocioNombre);
                        continue;
                    }

                    // Calcular ingresos del día: suma de montos positivos en logs de hoy
                    var ingresosDia = await context.Logs
                        .Where(l => l.NegocioId == negocio.NegocioId
                                    && l.Fecha.Date == hoy
                                    && l.Monto > 0)
                        .SumAsync(l => l.Monto, stoppingToken);

                    // Calcular nuevos clientes registrados hoy
                    var nuevosClientes = await context.Clientes
                        .CountAsync(c => c.NegocioId == negocio.NegocioId
                                         && c.FechaDeCreacion.Date == hoy,
                            stoppingToken);

                    // Calcular clientes cuya membresía vence mañana (1 día restante)
                    var manana = hoy.AddDays(1);
                    var porVencerManana = await context.Clientes
                        .CountAsync(c => c.NegocioId == negocio.NegocioId
                                         && c.FechaQueTermina.Date == manana,
                            stoppingToken);

                    // Enviar resumen al dueño del negocio
                    var resultado = await whatsAppService.EnviarResumenDiarioAsync(
                        negocio.Telefono,
                        negocio.NegocioNombre,
                        ingresosDia,
                        nuevosClientes,
                        porVencerManana);

                    if (resultado)
                    {
                        enviados++;
                        _logger.LogInformation(
                            "Resumen enviado a {Negocio} ({Telefono}) — Ingresos: ${Ingresos}, Nuevos: {Nuevos}, Por vencer: {PorVencer}",
                            negocio.NegocioNombre, negocio.Telefono,
                            ingresosDia, nuevosClientes, porVencerManana);
                    }
                    else
                    {
                        errores++;
                        _logger.LogWarning(
                            "No se pudo enviar resumen a {Negocio} ({Telefono})",
                            negocio.NegocioNombre, negocio.Telefono);
                    }
                }
                catch (Exception ex)
                {
                    errores++;
                    _logger.LogError(ex,
                        "Error al enviar resumen diario al negocio {NegocioId} ({Nombre})",
                        negocio.NegocioId, negocio.NegocioNombre);
                    // Continuar con el siguiente negocio sin interrumpir el proceso
                }
            }

            _logger.LogInformation(
                "Resúmenes diarios completados: {Enviados} enviados, {Errores} errores de {Total} total",
                enviados, errores, negociosActivos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general al procesar resúmenes diarios");
        }
    }
}
