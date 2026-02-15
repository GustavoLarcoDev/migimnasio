// ═══════════════════════════════════════════════════════════
// MembershipReminderService.cs — Servicio de fondo para recordatorios
// Envía mensajes de WhatsApp automáticos desde el número del admin
// a clientes cuya membresía está por vencer (3 días y 1 día antes).
// El mensaje incluye el nombre del negocio para que el cliente
// sepa quién le escribe. Se ejecuta a las 8:00 AM hora Ecuador.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Servicio de fondo que envía recordatorios de vencimiento de membresía
/// vía WhatsApp a los clientes de todos los negocios activos.
/// Todos los mensajes salen desde el número del admin.
/// </summary>
public class MembershipReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MembershipReminderService> _logger;

    public MembershipReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<MembershipReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    // BUCLE PRINCIPAL — Espera hasta las 8:00 AM Ecuador y ejecuta
    // ═══════════════════════════════════════════════════════════

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MembershipReminderService iniciado");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var ecuadorZone = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
                var nowEcuador = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorZone);
                var nextRun = nowEcuador.Date.AddHours(8);

                if (nowEcuador >= nextRun)
                    nextRun = nextRun.AddDays(1);

                var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, ecuadorZone);
                var delay = nextRunUtc - DateTime.UtcNow;

                _logger.LogInformation(
                    "Recordatorios: próxima ejecución en {Delay} ({NextRun} hora Ecuador)",
                    delay, nextRun);

                await Task.Delay(delay, stoppingToken);
                await EnviarRecordatoriosAsync(stoppingToken);
            }
        }
        catch (TaskCanceledException) { /* Apagado normal de la aplicación */ }

        _logger.LogInformation("MembershipReminderService detenido");
    }

    // ═══════════════════════════════════════════════════════════
    // LÓGICA DE ENVÍO — Busca clientes próximos a vencer y notifica
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Para cada negocio activo, busca clientes cuya membresía vence en 3 o 1 día
    /// y les envía un recordatorio por WhatsApp desde el número del admin.
    /// </summary>
    private async Task EnviarRecordatoriosAsync(CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Iniciando envío de recordatorios de membresía...");

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        try
        {
            var hoy = TimeHelper.Now.Date;

            // Obtener negocios activos con sus clientes
            var negociosActivos = await context.Negocios
                .Where(n => n.IsActive)
                .Select(n => new { n.NegocioId, n.NegocioNombre })
                .ToListAsync(stoppingToken);

            var negocioIds = negociosActivos.Select(n => n.NegocioId).ToList();
            var negocioNombres = negociosActivos.ToDictionary(n => n.NegocioId, n => n.NegocioNombre);

            // Obtener todos los clientes de negocios activos
            var clientes = await context.Clientes
                .Where(c => negocioIds.Contains(c.NegocioId))
                .ToListAsync(stoppingToken);

            // Filtrar clientes cuya membresía vence en 3 o 1 día
            var clientesParaNotificar = clientes
                .Where(c =>
                {
                    var dias = (int)(c.FechaQueTermina.Date - hoy).TotalDays;
                    return dias == 3 || dias == 1;
                })
                .ToList();

            _logger.LogInformation(
                "Encontrados {Count} clientes con membresía próxima a vencer",
                clientesParaNotificar.Count);

            if (clientesParaNotificar.Count == 0)
                return;

            var enviados = 0;
            var errores = 0;

            foreach (var cliente in clientesParaNotificar)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    if (string.IsNullOrWhiteSpace(cliente.Telefono))
                    {
                        _logger.LogWarning(
                            "Cliente {ClienteId} ({Nombre} {Apellido}) no tiene teléfono, se omite",
                            cliente.ClienteId, cliente.Nombre, cliente.Apellido);
                        continue;
                    }

                    var diasRestantes = (int)(cliente.FechaQueTermina.Date - hoy).TotalDays;
                    var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
                    var negocioNombre = negocioNombres.GetValueOrDefault(cliente.NegocioId, "Tu negocio");

                    var resultado = await whatsAppService.EnviarRecordatorioMembresiaAsync(
                        cliente.Telefono,
                        nombreCompleto,
                        negocioNombre,
                        diasRestantes);

                    if (resultado)
                    {
                        enviados++;
                        _logger.LogInformation(
                            "Recordatorio enviado a {Nombre} ({Telefono}) — {Dias} día(s) — Negocio: {Negocio}",
                            nombreCompleto, cliente.Telefono, diasRestantes, negocioNombre);
                    }
                    else
                    {
                        errores++;
                    }
                }
                catch (Exception ex)
                {
                    errores++;
                    _logger.LogError(ex,
                        "Error al enviar recordatorio al cliente {ClienteId}",
                        cliente.ClienteId);
                }
            }

            _logger.LogInformation(
                "Recordatorios completados: {Enviados} enviados, {Errores} errores de {Total} total",
                enviados, errores, clientesParaNotificar.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general al procesar recordatorios de membresía");
        }
    }
}
