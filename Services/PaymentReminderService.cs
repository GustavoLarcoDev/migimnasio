// ═══════════════════════════════════════════════════════════
// PaymentReminderService.cs — Recordatorio automático de cobro
//
// FUNCIÓN: Envía recordatorios de cobro por WhatsApp a clientes
// cuya membresía venció hace 0, 1, 3 o 7 días.
//
// HORARIO: Se ejecuta una vez al día a las 9:00 AM hora Ecuador
// (1 hora después del MembershipReminderService que corre a las 8 AM).
//
// PREVENCIÓN DE DUPLICADOS: Usa el campo UltimoRecordatorioCobro
// del modelo Cliente para no enviar más de un recordatorio por día.
//
// NEGOCIOS: Solo aplica a negocios de tipo "membresias" activos.
// Excluye clientes diarios (EsDiario = true).
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class PaymentReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentReminderService> _logger;

    public PaymentReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentReminderService iniciado");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var ecuadorZone = TimeHelper.EcuadorTz;
                var nowEcuador = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorZone);

                // Ejecutar a las 9:00 AM Ecuador (1 hora después del membership reminder)
                var nextRun = nowEcuador.Date.AddHours(9);

                if (nowEcuador >= nextRun)
                    nextRun = nextRun.AddDays(1);

                var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, ecuadorZone);
                var delay = nextRunUtc - DateTime.UtcNow;

                _logger.LogInformation(
                    "Cobros: próxima ejecución en {Delay} ({NextRun} hora Ecuador)",
                    delay, nextRun);

                await Task.Delay(delay, stoppingToken);

                await EnviarRecordatoriosCobroAsync(stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Apagado normal
        }

        _logger.LogInformation("PaymentReminderService detenido");
    }

    private async Task EnviarRecordatoriosCobroAsync(CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Iniciando envío de recordatorios de cobro...");

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        try
        {
            var hoy = TimeHelper.Now.Date;

            // Días de vencimiento en los que se envían recordatorios
            var diasObjetivo = new[] { 0, 1, 3, 7 };

            var negociosActivos = await context.Negocios
                .Where(n => n.IsActive && n.TipoNegocio == "membresias")
                .Select(n => new { n.NegocioId, n.NegocioNombre, n.Telefono })
                .ToListAsync(stoppingToken);

            var negocioIds = negociosActivos.Select(n => n.NegocioId).ToList();
            var negocioNombres = negociosActivos.ToDictionary(n => n.NegocioId, n => n.NegocioNombre);
            var negocioTelefonos = negociosActivos.ToDictionary(n => n.NegocioId, n => n.Telefono);

            // Cargar clientes con membresía vencida (no diarios)
            var clientes = await context.Clientes
                .Where(c => negocioIds.Contains(c.NegocioId) && !c.EsDiario)
                .ToListAsync(stoppingToken);

            // Filtrar: solo clientes vencidos hace exactamente 0, 1, 3 o 7 días
            // y que no hayan recibido recordatorio hoy
            var clientesParaCobro = clientes
                .Where(c =>
                {
                    var diasVencido = (int)(hoy - c.FechaQueTermina.Date).TotalDays;
                    if (!diasObjetivo.Contains(diasVencido)) return false;
                    // No enviar si ya se envió hoy
                    if (c.UltimoRecordatorioCobro.HasValue && c.UltimoRecordatorioCobro.Value.Date == hoy)
                        return false;
                    return true;
                })
                .ToList();

            _logger.LogInformation(
                "Encontrados {Count} clientes con membresía vencida para recordatorio de cobro",
                clientesParaCobro.Count);

            if (clientesParaCobro.Count == 0)
                return;

            var enviados = 0;
            var errores = 0;

            foreach (var cliente in clientesParaCobro)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    if (string.IsNullOrWhiteSpace(cliente.Telefono))
                    {
                        _logger.LogWarning(
                            "Cliente {ClienteId} ({Nombre} {Apellido}) no tiene teléfono, se omite cobro",
                            cliente.ClienteId, cliente.Nombre, cliente.Apellido);
                        continue;
                    }

                    var diasVencido = (int)(hoy - cliente.FechaQueTermina.Date).TotalDays;
                    var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
                    var negocioNombre = negocioNombres.GetValueOrDefault(cliente.NegocioId, "Tu negocio");
                    var negocioTelefono = negocioTelefonos.GetValueOrDefault(cliente.NegocioId);

                    var resultado = await whatsAppService.EnviarRecordatorioCobroWhatsAppAsync(
                        cliente.Telefono,
                        nombreCompleto,
                        negocioNombre,
                        diasVencido,
                        negocioTelefono);

                    if (resultado)
                    {
                        enviados++;
                        cliente.UltimoRecordatorioCobro = TimeHelper.Now;
                        _logger.LogInformation(
                            "Cobro enviado a {Nombre} ({Telefono}) — {Dias} día(s) vencido — Negocio: {Negocio}",
                            nombreCompleto, cliente.Telefono, diasVencido, negocioNombre);
                    }
                    else
                    {
                        errores++;
                    }

                    // Delay entre envíos para no saturar la API de Twilio
                    await Task.Delay(500, stoppingToken);
                }
                catch (Exception ex)
                {
                    errores++;
                    _logger.LogError(ex,
                        "Error al enviar recordatorio de cobro al cliente {ClienteId}",
                        cliente.ClienteId);
                }
            }

            // Guardar cambios de UltimoRecordatorioCobro
            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Cobros completados: {Enviados} enviados, {Errores} errores de {Total} total",
                enviados, errores, clientesParaCobro.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general al procesar recordatorios de cobro");
        }
    }
}
