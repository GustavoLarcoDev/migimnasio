// ═══════════════════════════════════════════════════════════
// SubscriptionExpirationReminderService.cs — Recordatorios de expiración de suscripción
//
// Envía alertas por WhatsApp a los dueños de negocios cuya suscripción
// está próxima a vencer (7, 3 o 1 día antes).
//
// HORARIO: Se ejecuta diariamente a las 10:00 AM hora Ecuador.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class SubscriptionExpirationReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionExpirationReminderService> _logger;

    public SubscriptionExpirationReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionExpirationReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionExpirationReminderService iniciado");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var ecuadorZone = TimeHelper.EcuadorTz;
                var nowEcuador = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorZone);

                // Ejecutar a las 10:00 AM Ecuador
                var nextRun = nowEcuador.Date.AddHours(10);
                if (nowEcuador >= nextRun)
                    nextRun = nextRun.AddDays(1);

                var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, ecuadorZone);
                var delay = nextRunUtc - DateTime.UtcNow;

                _logger.LogInformation(
                    "Suscripción reminder: próxima ejecución en {Delay} ({NextRun} hora Ecuador)",
                    delay, nextRun);

                await Task.Delay(delay, stoppingToken);
                await EnviarRecordatoriosSuscripcionAsync(stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Apagado normal
        }

        _logger.LogInformation("SubscriptionExpirationReminderService detenido");
    }

    private async Task EnviarRecordatoriosSuscripcionAsync(CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Iniciando envío de recordatorios de expiración de suscripción...");

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        try
        {
            var hoy = TimeHelper.Now.Date;

            // Negocios activos no bloqueados con fecha de expiración definida
            var negocios = await context.Negocios
                .Where(n => n.IsActive && !n.NegocioBloqueado && n.FechaExpiracion.HasValue)
                .Select(n => new
                {
                    n.NegocioId,
                    n.NegocioNombre,
                    n.DuenoNegocio,
                    n.Telefono,
                    n.FechaExpiracion
                })
                .ToListAsync(stoppingToken);

            // Filtrar: solo los que vencen en exactamente 7, 3 o 1 días
            var negociosParaNotificar = negocios
                .Where(n =>
                {
                    var dias = (int)(n.FechaExpiracion!.Value.Date - hoy).TotalDays;
                    return dias == 7 || dias == 3 || dias == 1;
                })
                .ToList();

            _logger.LogInformation(
                "Encontrados {Count} negocios con suscripción próxima a vencer",
                negociosParaNotificar.Count);

            if (negociosParaNotificar.Count == 0)
                return;

            var enviados = 0;
            var errores = 0;

            foreach (var neg in negociosParaNotificar)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    if (string.IsNullOrWhiteSpace(neg.Telefono))
                    {
                        _logger.LogWarning(
                            "Negocio {NegocioId} ({Nombre}) no tiene teléfono, se omite",
                            neg.NegocioId, neg.NegocioNombre);
                        continue;
                    }

                    var diasRestantes = (int)(neg.FechaExpiracion!.Value.Date - hoy).TotalDays;
                    var dueno = neg.DuenoNegocio ?? "Estimado cliente";

                    var resultado = await whatsAppService.EnviarAdvertenciaSuscripcionWhatsAppAsync(
                        neg.Telefono,
                        neg.NegocioNombre,
                        dueno,
                        diasRestantes,
                        neg.FechaExpiracion.Value);

                    if (resultado)
                    {
                        enviados++;
                        _logger.LogInformation(
                            "Recordatorio de suscripción enviado a {Negocio} ({Telefono}) — {Dias} día(s)",
                            neg.NegocioNombre, neg.Telefono, diasRestantes);
                    }
                    else
                    {
                        errores++;
                    }

                    // Delay entre envíos para no saturar Twilio
                    await Task.Delay(500, stoppingToken);
                }
                catch (Exception ex)
                {
                    errores++;
                    _logger.LogError(ex,
                        "Error al enviar recordatorio de suscripción al negocio {NegocioId}",
                        neg.NegocioId);
                }
            }

            _logger.LogInformation(
                "Recordatorios de suscripción completados: {Enviados} enviados, {Errores} errores de {Total} total",
                enviados, errores, negociosParaNotificar.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general al procesar recordatorios de expiración de suscripción");
        }
    }
}
