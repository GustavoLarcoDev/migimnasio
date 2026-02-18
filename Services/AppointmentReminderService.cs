// ═══════════════════════════════════════════════════════════
// AppointmentReminderService.cs — Servicio de fondo para recordatorios de citas
// Se ejecuta cada 5 minutos y envía recordatorios por WhatsApp
// a clientes y dueños de negocios con citas en los próximos 35 min.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Servicio de fondo que envía recordatorios de citas via WhatsApp.
/// Se ejecuta cada 5 minutos. Busca citas en los próximos 35 minutos
/// que aún no tienen recordatorio enviado.
/// </summary>
public class AppointmentReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentReminderService> _logger;

    public AppointmentReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<AppointmentReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    // BUCLE PRINCIPAL — Ejecuta cada 5 minutos
    // ═══════════════════════════════════════════════════════════

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AppointmentReminderService iniciado");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await EnviarRecordatoriosCitasAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (TaskCanceledException) { /* Apagado normal */ }

        _logger.LogInformation("AppointmentReminderService detenido");
    }

    // ═══════════════════════════════════════════════════════════
    // LÓGICA DE ENVÍO — Busca citas próximas y notifica
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Busca citas en los próximos 35 minutos sin recordatorio enviado
    /// y envía notificación por WhatsApp al cliente y al dueño del negocio
    /// </summary>
    private async Task EnviarRecordatoriosCitasAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        try
        {
            var ahora = TimeHelper.Now;
            var en35Min = ahora.AddMinutes(35);

            // Buscar citas próximas sin recordatorio
            var citas = await context.Citas
                .Where(c => !c.RecordatorioEnviado
                    && c.Estado != "cancelada"
                    && c.Estado != "completada"
                    && c.FechaHoraInicio >= ahora
                    && c.FechaHoraInicio <= en35Min)
                .ToListAsync(stoppingToken);

            if (citas.Count == 0) return;

            _logger.LogInformation("Encontradas {Count} citas para recordatorio", citas.Count);

            // Obtener datos de negocios para los recordatorios
            var negocioIds = citas.Select(c => c.NegocioId).Distinct().ToList();
            var negocios = await context.Negocios
                .Where(n => negocioIds.Contains(n.NegocioId))
                .ToDictionaryAsync(n => n.NegocioId, stoppingToken);

            // Obtener datos de clientes para teléfonos
            var clienteIds = citas.Select(c => c.ClienteId).Distinct().ToList();
            var clientes = await context.Clientes
                .Where(c => clienteIds.Contains(c.ClienteId))
                .ToDictionaryAsync(c => c.ClienteId, stoppingToken);

            foreach (var cita in citas)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    var negocio = negocios.GetValueOrDefault(cita.NegocioId);
                    var cliente = clientes.GetValueOrDefault(cita.ClienteId);

                    if (negocio == null || cliente == null) continue;

                    var hora = cita.FechaHoraInicio.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);

                    var envioExitoso = true;

                    // Enviar al cliente
                    if (!string.IsNullOrWhiteSpace(cliente.Telefono))
                    {
                        var resultCliente = await whatsAppService.EnviarRecordatorioCitaClienteAsync(
                            cliente.Telefono, cita.NombreCliente,
                            negocio.NegocioNombre, cita.NombreEmpleado, hora);
                        if (!resultCliente) envioExitoso = false;
                    }

                    // Enviar al dueño del negocio
                    if (!string.IsNullOrWhiteSpace(negocio.Telefono))
                    {
                        var resultNegocio = await whatsAppService.EnviarRecordatorioCitaNegocioAsync(
                            negocio.Telefono, negocio.DuenoNegocio,
                            cita.NombreServicio, hora, cita.NombreEmpleado);
                        if (!resultNegocio) envioExitoso = false;
                    }

                    // Solo marcar si al menos un envio fue exitoso
                    if (envioExitoso)
                        cita.RecordatorioEnviado = true;

                    _logger.LogInformation(
                        "Recordatorio de cita enviado: {Servicio} - {Cliente} a las {Hora}",
                        cita.NombreServicio, cita.NombreCliente, hora);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar recordatorio de cita {CitaId}", cita.CitaId);
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general al procesar recordatorios de citas");
        }
    }
}
