// ═══════════════════════════════════════════════════════════
// DailyReportService.cs — Servicio de fondo para resúmenes diarios
//
// CONCEPTO: BackgroundService es una clase base de ASP.NET Core que
// permite correr código en un hilo separado mientras la app está activa.
// No requiere peticiones HTTP — se ejecuta solo, en el fondo.
//
// FUNCIÓN: Envía un resumen financiero del día por WhatsApp al dueño
// de cada negocio activo de tipo "membresias". El resumen incluye:
//   - Ingresos del día (suma de logs con monto positivo)
//   - Nuevos clientes registrados hoy
//   - Clientes cuya membresía vence mañana
//
// HORARIO: Se ejecuta a las 9:00 PM hora Ecuador (America/Guayaquil, UTC-5).
//
// NOTA SOBRE SCOPE: Los BackgroundServices son singletons (viven toda la
// vida de la app), pero los DbContext son scoped (viven por petición).
// Por eso usamos IServiceScopeFactory para crear un scope manualmente
// cada vez que necesitamos acceder a la base de datos.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Servicio de fondo (BackgroundService) que envía un resumen diario por WhatsApp
/// al dueño de cada negocio activo de tipo "membresias".
///
/// El resumen incluye ingresos del día, nuevos clientes y membresías por vencer.
/// Se ejecuta una vez al día a las 9:00 PM hora Ecuador.
///
/// Para agregar al contenedor de DI, se registra como HostedService en
/// <see cref="BackgroundServicesRegistration.AddBackgroundServices"/>.
/// </summary>
public class DailyReportService : BackgroundService
{
    // IServiceScopeFactory: permite crear un "scope" de DI bajo demanda.
    // Necesario porque DbContext y WhatsAppService son servicios scoped,
    // y un BackgroundService es singleton — no puede inyectarlos directo.
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyReportService> _logger;

    /// <summary>
    /// Constructor: recibe las dependencias por inyección de dependencias.
    /// IServiceScopeFactory es el puente entre un singleton y servicios scoped.
    /// </summary>
    public DailyReportService(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyReportService> logger)
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
    ///   1. Calcula cuánto tiempo falta para las 9:00 PM hora Ecuador.
    ///   2. Duerme (Task.Delay) hasta ese momento sin consumir CPU.
    ///   3. Envía los resúmenes diarios a todos los negocios.
    ///   4. Repite el proceso para el día siguiente.
    ///
    /// TaskCanceledException se captura silenciosamente porque es la señal
    /// normal de apagado de la aplicación (no es un error real).
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyReportService iniciado");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Calcular la próxima ejecución en hora Ecuador (UTC-5).
                // Usamos la zona horaria real en lugar de sumar horas fijas
                // para manejar correctamente el horario de verano si Ecuador lo adopta.
                var ecuadorZone = TimeHelper.EcuadorTz;
                var nowEcuador = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorZone);

                // La próxima ejecución es hoy a las 21:00 (9 PM)
                var nextRun = nowEcuador.Date.AddHours(21);

                // Si ya pasaron las 9 PM de hoy, programar para mañana a las 9 PM
                if (nowEcuador >= nextRun)
                    nextRun = nextRun.AddDays(1);

                // Convertir la hora objetivo de Ecuador a UTC para Task.Delay
                var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, ecuadorZone);
                var delay = nextRunUtc - DateTime.UtcNow;

                _logger.LogInformation(
                    "Resumen diario: próxima ejecución en {Delay} ({NextRun} hora Ecuador)",
                    delay, nextRun);

                // Dormir hasta la hora programada. Si la app se apaga, stoppingToken
                // cancela el delay y lanza TaskCanceledException (capturado abajo).
                await Task.Delay(delay, stoppingToken);

                // Hora de ejecutar: enviar resúmenes a todos los negocios
                await EnviarResumenesDiariosAsync(stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Apagado normal de la aplicación. No es un error.
        }

        _logger.LogInformation("DailyReportService detenido");
    }

    // ═══════════════════════════════════════════════════════════
    // LÓGICA DE ENVÍO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Itera sobre todos los negocios activos de tipo "membresias" y envía
    /// un resumen del día por WhatsApp al número del dueño.
    ///
    /// Los negocios "artesanal" se omiten porque manejan citas, no membresías.
    ///
    /// El resumen contiene:
    ///   - <c>ingresosDia</c>: suma de Logs con monto positivo creados hoy.
    ///   - <c>nuevosClientes</c>: clientes registrados hoy en este negocio.
    ///   - <c>porVencerManana</c>: clientes cuya membresía termina mañana.
    ///
    /// Si un negocio no tiene teléfono registrado, se omite con un warning.
    /// Si WhatsApp falla para un negocio, se continúa con los siguientes.
    /// </summary>
    private async Task EnviarResumenesDiariosAsync(CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Iniciando envío de resúmenes diarios...");

        // Crear un scope de DI para acceder a DbContext y WhatsAppService.
        // El using asegura que el scope se libera al terminar el método,
        // lo que también libera la conexión a la base de datos.
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        try
        {
            var hoy = TimeHelper.Now.Date;

            // Solo procesar negocios activos de tipo membresías.
            // Los artesanales (peluquerías, spas) manejan citas, no membresías.
            var negociosActivos = await context.Negocios
                .Where(n => n.IsActive && n.TipoNegocio == "membresias")
                .ToListAsync(stoppingToken);

            _logger.LogInformation(
                "Procesando resúmenes para {Count} negocios activos",
                negociosActivos.Count);

            var enviados = 0;
            var errores = 0;

            foreach (var negocio in negociosActivos)
            {
                // Verificar cancelación antes de procesar cada negocio.
                // Esto permite que la app se apague limpiamente sin esperar
                // a que termine el bucle completo.
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    // Sin teléfono no podemos enviar el WhatsApp — lo omitimos.
                    if (string.IsNullOrWhiteSpace(negocio.Telefono))
                    {
                        _logger.LogWarning(
                            "Negocio {NegocioId} ({Nombre}) no tiene teléfono registrado, se omite",
                            negocio.NegocioId, negocio.NegocioNombre);
                        continue;
                    }

                    // Ingresos del día: suma de todos los Logs con monto positivo.
                    // Los Logs son la fuente inmutable de verdad financiera —
                    // si un cliente se elimina, su pago sigue registrado aquí.
                    var ingresosDia = await context.Logs
                        .Where(l => l.NegocioId == negocio.NegocioId
                                    && l.Fecha.Date == hoy
                                    && l.Monto > 0)
                        .SumAsync(l => l.Monto, stoppingToken);

                    // Clientes registrados hoy en este negocio
                    var nuevosClientes = await context.Clientes
                        .CountAsync(c => c.NegocioId == negocio.NegocioId
                                         && c.FechaDeCreacion.Date == hoy,
                            stoppingToken);

                    // Clientes cuya membresía vence exactamente mañana.
                    // Esto le da al dueño tiempo de contactarlos antes de que venzan.
                    var manana = hoy.AddDays(1);
                    var porVencerManana = await context.Clientes
                        .CountAsync(c => c.NegocioId == negocio.NegocioId
                                         && c.FechaQueTermina.Date == manana,
                            stoppingToken);

                    // Enviar el resumen por WhatsApp al número del dueño del negocio
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

                    // Capturar la excepción individualmente para que un fallo en un
                    // negocio no detenga el proceso para los demás negocios.
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
