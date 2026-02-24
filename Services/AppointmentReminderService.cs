// ═══════════════════════════════════════════════════════════
// AppointmentReminderService.cs — Servicio de fondo para recordatorios de citas
//
// FUNCIÓN: Envía recordatorios de cita por WhatsApp a dos destinatarios:
//   1. Al CLIENTE: "Tienes cita en {negocio} con {empleado} a las {hora}"
//   2. Al DUEÑO del negocio: "Tienes cita para {servicio} a las {hora}"
//
// HORARIO: A diferencia de los otros servicios (que corren una vez al día),
// este se ejecuta CADA 5 MINUTOS. Esto es necesario porque las citas
// pueden ocurrir a cualquier hora del día con precisión de minutos.
//
// VENTANA DE TIEMPO: Busca citas que ocurren en los próximos 35 minutos.
// Con el intervalo de 5 minutos, esto garantiza que toda cita recibe
// su recordatorio entre 30 y 35 minutos antes de comenzar.
//
// PREVENCIÓN DE DUPLICADOS: El campo RecordatorioEnviado (bool) en la
// tabla Citas evita que el mismo recordatorio se envíe más de una vez,
// incluso si el servicio se reinicia.
//
// MODELO ARTESANAL: Este servicio aplica SOLO a negocios de tipo "artesanal"
// (peluquerías, spas, etc.) que manejan citas con empleados y servicios.
// Los negocios de membresías usan MembershipReminderService.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Servicio de fondo (BackgroundService) que envía recordatorios de citas
/// por WhatsApp al cliente y al dueño del negocio.
///
/// Se ejecuta cada 5 minutos y busca citas en los próximos 35 minutos
/// que aún no tienen recordatorio enviado (RecordatorioEnviado = false).
///
/// Para agregar al contenedor de DI, se registra en
/// <see cref="BackgroundServicesRegistration.AddBackgroundServices"/>.
/// </summary>
public class AppointmentReminderService : BackgroundService
{
    // IServiceScopeFactory: necesario porque un BackgroundService es singleton
    // y no puede recibir DbContext (scoped) directamente en el constructor.
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentReminderService> _logger;

    /// <summary>
    /// Constructor: recibe las dependencias por inyección de dependencias.
    /// </summary>
    public AppointmentReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<AppointmentReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    // BUCLE PRINCIPAL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Punto de entrada del servicio de fondo. Se ejecuta al iniciar la app
    /// y corre en un bucle infinito hasta que la app se apaga (stoppingToken).
    ///
    /// Patrón de ejecución: ejecutar → esperar 5 minutos → ejecutar → ...
    ///
    /// A diferencia de DailyReportService y MembershipReminderService que
    /// calculan una hora específica del día, este servicio simplemente duerme
    /// 5 minutos entre cada ejecución porque las citas pueden ocurrir a
    /// cualquier hora.
    ///
    /// TaskCanceledException se captura silenciosamente — es la señal
    /// normal de apagado, no un error.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AppointmentReminderService iniciado");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Ejecutar primero, luego esperar.
                // Esto garantiza que el servicio procese citas inmediatamente
                // al arrancar la app, sin esperar 5 minutos.
                await EnviarRecordatoriosCitasAsync(stoppingToken);

                // Esperar 5 minutos antes del siguiente ciclo.
                // TimeSpan.FromMinutes(5) = 300,000 ms.
                // stoppingToken cancela el delay si la app se apaga.
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Apagado normal de la aplicación. No es un error.
        }

        _logger.LogInformation("AppointmentReminderService detenido");
    }

    // ═══════════════════════════════════════════════════════════
    // LÓGICA DE ENVÍO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Busca citas que ocurren en los próximos 35 minutos con RecordatorioEnviado = false,
    /// y envía notificaciones por WhatsApp tanto al cliente como al dueño del negocio.
    ///
    /// Después de enviar, marca la cita con RecordatorioEnviado = true para que
    /// en el próximo ciclo (5 minutos después) no se vuelva a procesar.
    ///
    /// La cita solo se marca como enviada si AMBOS envíos fueron exitosos.
    /// Si alguno falla, se reintentará en el próximo ciclo de 5 minutos.
    ///
    /// Citas canceladas o completadas se excluyen de la búsqueda.
    /// </summary>
    private async Task EnviarRecordatoriosCitasAsync(CancellationToken stoppingToken)
    {
        // Crear scope de DI para acceder a DbContext, WhatsAppService e IEmailService.
        // El using libera el scope y la conexión a la DB al finalizar.
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            var ahora = TimeHelper.Now;
            // Ventana de 35 minutos hacia el futuro: buscar citas que empiezan
            // entre ahora y 35 minutos después.
            var en35Min = ahora.AddMinutes(35);

            // Buscar citas pendientes en la ventana de tiempo.
            // Se excluyen citas canceladas y completadas — no tienen sentido recordarlas.
            var citas = await context.Citas
                .Where(c => !c.RecordatorioEnviado          // No enviado aún
                    && c.Estado != "cancelada"              // Excluir canceladas
                    && c.Estado != "completada"             // Excluir completadas
                    && c.FechaHoraInicio >= ahora           // No pasadas
                    && c.FechaHoraInicio <= en35Min)        // Dentro de 35 minutos
                .ToListAsync(stoppingToken);

            // Si no hay citas próximas, no hacer nada (ocurre la mayoría de los ciclos)
            if (citas.Count == 0) return;

            _logger.LogInformation("Encontradas {Count} citas para recordatorio", citas.Count);

            // Cargar datos de negocios y clientes en una sola consulta cada uno,
            // en lugar de consultar la DB por cada cita (mucho más eficiente).
            var negocioIds = citas.Select(c => c.NegocioId).Distinct().ToList();
            var negocios = await context.Negocios
                .Where(n => negocioIds.Contains(n.NegocioId))
                .ToDictionaryAsync(n => n.NegocioId, stoppingToken);

            var clienteIds = citas.Select(c => c.ClienteId).Distinct().ToList();
            var clientes = await context.Clientes
                .Where(c => clienteIds.Contains(c.ClienteId))
                .ToDictionaryAsync(c => c.ClienteId, stoppingToken);

            // Cargar empleados para enviarles recordatorios por email
            var empleadoIds = citas.Select(c => c.EmpleadoId).Distinct().ToList();
            var empleados = await context.Empleados
                .Where(e => empleadoIds.Contains(e.EmpleadoId))
                .ToDictionaryAsync(e => e.EmpleadoId, stoppingToken);

            foreach (var cita in citas)
            {
                // Respetar señal de apagado para salir limpiamente
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    // Si no se encuentran los datos del negocio o cliente, omitir la cita
                    var negocio = negocios.GetValueOrDefault(cita.NegocioId);
                    var cliente = clientes.GetValueOrDefault(cita.ClienteId);

                    if (negocio == null || cliente == null) continue;

                    // Formatear la hora en formato "12:30 PM" (AmPm, invariant culture)
                    var hora = cita.FechaHoraInicio.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);

                    // Asumir éxito hasta que alguno falle
                    var envioExitoso = true;

                    // ENVÍO 1: Al cliente — le recuerda su cita en el negocio
                    if (!string.IsNullOrWhiteSpace(cliente.Telefono))
                    {
                        var resultCliente = await whatsAppService.EnviarRecordatorioCitaClienteAsync(
                            cliente.Telefono,
                            cita.NombreCliente,
                            negocio.NegocioNombre,
                            cita.NombreEmpleado,
                            hora,
                            negocio.Telefono);
                        if (!resultCliente) envioExitoso = false;
                    }

                    // ENVÍO 2: Al dueño del negocio — le recuerda que tiene una cita entrante
                    if (!string.IsNullOrWhiteSpace(negocio.Telefono))
                    {
                        var resultNegocio = await whatsAppService.EnviarRecordatorioCitaNegocioAsync(
                            negocio.Telefono,
                            negocio.DuenoNegocio,
                            cita.NombreServicio,
                            hora,
                            cita.NombreEmpleado);
                        if (!resultNegocio) envioExitoso = false;
                    }

                    // ENVÍO 3: Email de recordatorio al cliente (si tiene email registrado).
                    // El email es adicional a WhatsApp, no afecta el flag envioExitoso para
                    // no bloquear el marcado de RecordatorioEnviado por un fallo de email.
                    if (!string.IsNullOrWhiteSpace(cliente.Email))
                    {
                        try
                        {
                            await emailService.EnviarRecordatorioCitaEmailAsync(
                                cliente.Email, cita.NombreCliente, negocio.NegocioNombre,
                                cita.NombreServicio, cita.NombreEmpleado, cita.FechaHoraInicio,
                                negocio.Email, negocio.Telefono);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error enviando email de recordatorio para cita {CitaId}", cita.CitaId);
                        }
                    }

                    // ENVÍO 4: Email de recordatorio al empleado (si tiene email registrado).
                    // Le avisa que tiene una cita próxima, con datos del cliente y el servicio,
                    // e incluye los datos de contacto del dueño por si necesita comunicarse.
                    var empleado = empleados.GetValueOrDefault(cita.EmpleadoId);
                    if (empleado != null && !string.IsNullOrWhiteSpace(empleado.Email))
                    {
                        try
                        {
                            await emailService.EnviarRecordatorioCitaEmpleadoAsync(
                                empleado.Email,
                                cita.NombreEmpleado,
                                cita.NombreCliente,
                                negocio.NegocioNombre,
                                cita.NombreServicio,
                                cita.FechaHoraInicio,
                                negocio.Email,
                                negocio.Telefono);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error enviando email de recordatorio al empleado para cita {CitaId}", cita.CitaId);
                        }
                    }

                    // ENVÍO 5: WhatsApp de recordatorio al empleado (si tiene teléfono registrado).
                    if (empleado != null && !string.IsNullOrWhiteSpace(empleado.Telefono))
                    {
                        try
                        {
                            await whatsAppService.EnviarRecordatorioCitaEmpleadoWhatsAppAsync(
                                empleado.Telefono,
                                cita.NombreEmpleado,
                                cita.NombreCliente,
                                cita.NombreServicio,
                                hora);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error enviando WhatsApp de recordatorio al empleado para cita {CitaId}", cita.CitaId);
                        }
                    }

                    // Marcar como enviada solo si ambos envíos de WhatsApp fueron exitosos.
                    // Si alguno falló, se reintentará en el próximo ciclo de 5 minutos.
                    if (envioExitoso)
                        cita.RecordatorioEnviado = true;

                    _logger.LogInformation(
                        "Recordatorio de cita enviado: {Servicio} - {Cliente} a las {Hora}",
                        cita.NombreServicio, cita.NombreCliente, hora);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar recordatorio de cita {CitaId}", cita.CitaId);
                    // El error se registra pero no interrumpe el bucle —
                    // las demás citas siguen siendo procesadas.
                }
            }

            // Guardar los cambios de RecordatorioEnviado en la base de datos
            // de una sola vez para todas las citas procesadas (más eficiente).
            await context.SaveChangesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general al procesar recordatorios de citas");
        }
    }
}
