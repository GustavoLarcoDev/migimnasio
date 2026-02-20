// ═══════════════════════════════════════════════════════════
// MembershipReminderService.cs — Servicio de fondo para recordatorios de membresía
//
// FUNCIÓN: Envía mensajes de WhatsApp automáticos a clientes cuya
// membresía está próxima a vencer. Notifica exactamente 2 veces:
//   - 3 días antes del vencimiento (aviso preventivo)
//   - 1 día antes del vencimiento (aviso urgente)
//
// HORARIO: Se ejecuta una vez al día a las 8:00 AM hora Ecuador.
// Corre antes del resumen diario (DailyReportService corre a las 9 PM)
// para que los dueños lleguen al negocio ya con los recordatorios enviados.
//
// NÚMERO DE ENVÍO: Todos los mensajes salen desde el número WhatsApp
// del administrador (configurado en appsettings.json → WhatsAppSettings).
// El mensaje menciona el nombre del negocio para que el cliente
// sepa a quién pertenece el recordatorio.
//
// NEGOCIOS ARTESANAL: Se omiten porque no manejan membresías —
// ellos trabajan con citas (ver AppointmentReminderService).
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Servicio de fondo (BackgroundService) que envía recordatorios de vencimiento
/// de membresía por WhatsApp a los clientes de todos los negocios activos.
///
/// Se ejecuta diariamente a las 8:00 AM hora Ecuador. Notifica a clientes
/// que vencen en exactamente 3 días o en exactamente 1 día.
///
/// Para agregar al contenedor de DI, se registra en
/// <see cref="BackgroundServicesRegistration.AddBackgroundServices"/>.
/// </summary>
public class MembershipReminderService : BackgroundService
{
    // IServiceScopeFactory: permite crear scopes de DI bajo demanda.
    // Un BackgroundService es singleton, pero DbContext y WhatsAppService
    // son scoped — no se pueden inyectar directamente en un singleton.
    // La factory resuelve este problema creando un scope temporal.
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MembershipReminderService> _logger;

    /// <summary>
    /// Constructor: recibe las dependencias por inyección de dependencias.
    /// </summary>
    public MembershipReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<MembershipReminderService> logger)
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
    /// En cada iteración:
    ///   1. Calcula cuánto tiempo falta para las 8:00 AM hora Ecuador.
    ///   2. Duerme (Task.Delay) hasta ese momento sin consumir CPU.
    ///   3. Envía recordatorios a los clientes que corresponde notificar hoy.
    ///   4. Repite para el día siguiente.
    ///
    /// TaskCanceledException se captura silenciosamente — es la señal
    /// normal de apagado de la aplicación, no un error.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MembershipReminderService iniciado");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Calcular la próxima ejecución en hora Ecuador.
                // Se usa la zona horaria real (no offset fijo) para mayor precisión.
                var ecuadorZone = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
                var nowEcuador = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorZone);

                // La próxima ejecución es hoy a las 08:00 AM
                var nextRun = nowEcuador.Date.AddHours(8);

                // Si ya pasaron las 8 AM de hoy, programar para mañana
                if (nowEcuador >= nextRun)
                    nextRun = nextRun.AddDays(1);

                // Convertir a UTC para que Task.Delay use la misma referencia temporal
                var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, ecuadorZone);
                var delay = nextRunUtc - DateTime.UtcNow;

                _logger.LogInformation(
                    "Recordatorios: próxima ejecución en {Delay} ({NextRun} hora Ecuador)",
                    delay, nextRun);

                // Dormir sin consumir CPU. stoppingToken cancela el delay
                // si la app se apaga antes de la hora programada.
                await Task.Delay(delay, stoppingToken);

                // Hora de ejecutar: enviar recordatorios a clientes próximos a vencer
                await EnviarRecordatoriosAsync(stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Apagado normal de la aplicación. No es un error.
        }

        _logger.LogInformation("MembershipReminderService detenido");
    }

    // ═══════════════════════════════════════════════════════════
    // LÓGICA DE ENVÍO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Busca todos los clientes cuya membresía vence en exactamente 3 o 1 día
    /// y envía un recordatorio personalizado por WhatsApp.
    ///
    /// Por qué 3 y 1 (no 2): los dueños de negocios pidieron estos dos puntos
    /// de contacto específicamente — uno preventivo y uno urgente.
    ///
    /// El mensaje incluye el nombre del negocio para que el cliente reconozca
    /// quién le está escribiendo (el número es del admin, no del negocio).
    ///
    /// Clientes sin teléfono registrado son omitidos con un warning en el log.
    /// Un error de WhatsApp en un cliente no detiene el proceso para los demás.
    /// </summary>
    private async Task EnviarRecordatoriosAsync(CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Iniciando envío de recordatorios de membresía...");

        // Crear scope de DI para acceder a DbContext y WhatsAppService.
        // El using libera el scope (y la conexión a la DB) al finalizar.
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        try
        {
            var hoy = TimeHelper.Now.Date;

            // Obtener solo negocios activos de tipo membresias.
            // Los artesanales no tienen membresías — usan citas (AppointmentReminderService).
            var negociosActivos = await context.Negocios
                .Where(n => n.IsActive && n.TipoNegocio == "membresias")
                .Select(n => new { n.NegocioId, n.NegocioNombre })
                .ToListAsync(stoppingToken);

            // Preparar estructuras de datos eficientes para el bucle siguiente.
            // Esto evita consultar el nombre del negocio por cada cliente individualmente.
            var negocioIds = negociosActivos.Select(n => n.NegocioId).ToList();
            var negocioNombres = negociosActivos.ToDictionary(n => n.NegocioId, n => n.NegocioNombre);

            // Cargar todos los clientes de estos negocios en una sola consulta.
            // El filtrado de días se hace en memoria (más flexible que SQL para lógica de fechas).
            var clientes = await context.Clientes
                .Where(c => negocioIds.Contains(c.NegocioId))
                .ToListAsync(stoppingToken);

            // Filtrar: solo los que vencen exactamente en 3 o 1 día.
            // Se usa TotalDays truncado a entero para comparación exacta de fechas.
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

            // Si no hay clientes para notificar hoy, terminar sin hacer nada más
            if (clientesParaNotificar.Count == 0)
                return;

            var enviados = 0;
            var errores = 0;

            foreach (var cliente in clientesParaNotificar)
            {
                // Respetar señal de apagado para no bloquear el cierre de la app
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    // Sin teléfono no hay forma de enviar el WhatsApp — omitir
                    if (string.IsNullOrWhiteSpace(cliente.Telefono))
                    {
                        _logger.LogWarning(
                            "Cliente {ClienteId} ({Nombre} {Apellido}) no tiene teléfono, se omite",
                            cliente.ClienteId, cliente.Nombre, cliente.Apellido);
                        continue;
                    }

                    var diasRestantes = (int)(cliente.FechaQueTermina.Date - hoy).TotalDays;
                    var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";

                    // Buscar el nombre del negocio al que pertenece este cliente.
                    // Si por alguna razón no se encuentra, usar un texto genérico.
                    var negocioNombre = negocioNombres.GetValueOrDefault(cliente.NegocioId, "Tu negocio");

                    // Enviar el recordatorio al cliente. El mensaje se personaliza en WhatsAppService:
                    // 1 día = mensaje urgente, 3 días = aviso preventivo.
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

                    // El error se registra pero no interrumpe el bucle —
                    // los demás clientes siguen siendo procesados.
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
