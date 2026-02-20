// ═══════════════════════════════════════════════════════════
// DailyEmailReportService.cs — Servicio de fondo para reportes diarios por correo
//
// CONCEPTO: BackgroundService es una clase base de ASP.NET Core que
// permite correr código en un hilo separado mientras la app está activa.
// No requiere peticiones HTTP — se ejecuta solo, en el fondo.
//
// FUNCIÓN: Envía un reporte financiero del día por correo electrónico al dueño
// de cada negocio activo. El reporte varía según el tipo de negocio:
//
//   "membresias": ingresos del día, nuevos clientes, productos vendidos,
//                 total de ventas de productos, clientes por vencer mañana,
//                 devoluciones y monto devuelto.
//
//   "artesanal":  ingresos del día (pagos de citas), citas completadas,
//                 citas canceladas, nuevos clientes, servicio más popular
//                 del día y total de ventas de productos.
//
// HORARIO: Se ejecuta a las 11:00 PM hora Ecuador (America/Guayaquil, UTC-5).
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
/// Servicio de fondo (BackgroundService) que envía un reporte diario por correo
/// al dueño de cada negocio activo al cierre del día.
///
/// Para negocios de tipo "membresias" llama a
/// <see cref="IEmailService.EnviarReporteDiarioMembresiaAsync"/> con datos de
/// ingresos, nuevos clientes, productos vendidos, clientes por vencer y devoluciones.
///
/// Para negocios de tipo "artesanal" llama a
/// <see cref="IEmailService.EnviarReporteDiarioArtesanalAsync"/> con datos de
/// ingresos (pagos de citas), citas completadas/canceladas, nuevos clientes,
/// servicio más popular y total de ventas de productos.
///
/// Se ejecuta una vez al día a las 11:00 PM hora Ecuador.
/// Los negocios sin correo registrado se omiten con un warning.
///
/// Para agregar al contenedor de DI, se registra como HostedService en
/// <see cref="BackgroundServicesRegistration.AddBackgroundServices"/>.
/// </summary>
public class DailyEmailReportService : BackgroundService
{
    // IServiceScopeFactory: permite crear un "scope" de DI bajo demanda.
    // Necesario porque DbContext e IEmailService son servicios scoped,
    // y un BackgroundService es singleton — no puede inyectarlos directo.
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyEmailReportService> _logger;

    /// <summary>
    /// Constructor: recibe las dependencias por inyección de dependencias.
    /// IServiceScopeFactory es el puente entre un singleton y servicios scoped.
    /// </summary>
    public DailyEmailReportService(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyEmailReportService> logger)
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
    ///   1. Calcula cuánto tiempo falta para las 11:00 PM hora Ecuador.
    ///   2. Duerme (Task.Delay) hasta ese momento sin consumir CPU.
    ///   3. Envía los reportes diarios a todos los negocios activos.
    ///   4. Repite el proceso para el día siguiente.
    ///
    /// TaskCanceledException se captura silenciosamente porque es la señal
    /// normal de apagado de la aplicación (no es un error real).
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyEmailReportService iniciado");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Calcular la próxima ejecución en hora Ecuador (UTC-5).
                // Usamos la zona horaria real en lugar de sumar horas fijas
                // para manejar correctamente el horario de verano si Ecuador lo adopta.
                var ecuadorZone = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
                var nowEcuador = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorZone);

                // La próxima ejecución es hoy a las 23:00 (11 PM)
                var nextRun = nowEcuador.Date.AddHours(23);

                // Si ya pasaron las 11 PM de hoy, programar para mañana a las 11 PM
                if (nowEcuador >= nextRun)
                    nextRun = nextRun.AddDays(1);

                // Convertir la hora objetivo de Ecuador a UTC para Task.Delay
                var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, ecuadorZone);
                var delay = nextRunUtc - DateTime.UtcNow;

                _logger.LogInformation(
                    "Reporte diario por email: próxima ejecución en {Delay} ({NextRun} hora Ecuador)",
                    delay, nextRun);

                // Dormir hasta la hora programada. Si la app se apaga, stoppingToken
                // cancela el delay y lanza TaskCanceledException (capturado abajo).
                await Task.Delay(delay, stoppingToken);

                // Hora de ejecutar: enviar reportes a todos los negocios activos
                await EnviarReportesDiariosAsync(stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Apagado normal de la aplicación. No es un error.
        }

        _logger.LogInformation("DailyEmailReportService detenido");
    }

    // ═══════════════════════════════════════════════════════════
    // LÓGICA DE ENVÍO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Itera sobre todos los negocios activos y envía el reporte del día
    /// por correo electrónico al dueño de cada negocio.
    ///
    /// El contenido del reporte depende del tipo de negocio:
    ///   "membresias": datos financieros de membresías, productos e inventario.
    ///   "artesanal":  datos de citas, pagos y servicios más populares.
    ///
    /// Los negocios sin correo registrado se omiten con un warning.
    /// Si el envío de email falla para un negocio, se continúa con los siguientes.
    /// </summary>
    private async Task EnviarReportesDiariosAsync(CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Iniciando envío de reportes diarios por email...");

        // Crear un scope de DI para acceder a DbContext e IEmailService.
        // El using asegura que el scope se libera al terminar el método,
        // lo que también libera la conexión a la base de datos.
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            var hoy = TimeHelper.Now.Date;

            // Procesar todos los negocios activos (membresias y artesanal).
            var negociosActivos = await context.Negocios
                .Where(n => n.IsActive)
                .ToListAsync(stoppingToken);

            _logger.LogInformation(
                "Procesando reportes por email para {Count} negocios activos",
                negociosActivos.Count);

            var enviados = 0;
            var errores = 0;
            var omitidos = 0;

            foreach (var negocio in negociosActivos)
            {
                // Verificar cancelación antes de procesar cada negocio.
                // Esto permite que la app se apague limpiamente sin esperar
                // a que termine el bucle completo.
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    // Sin correo no podemos enviar el reporte — lo omitimos.
                    if (string.IsNullOrWhiteSpace(negocio.Email))
                    {
                        _logger.LogWarning(
                            "Negocio {NegocioId} ({Nombre}) no tiene correo registrado, se omite",
                            negocio.NegocioId, negocio.NegocioNombre);
                        omitidos++;
                        continue;
                    }

                    bool resultado;

                    if (negocio.TipoNegocio == "artesanal")
                    {
                        resultado = await EnviarReporteArtesanalAsync(
                            context, emailService, negocio.NegocioId, negocio.NegocioNombre,
                            negocio.DuenoNegocio, negocio.Email, hoy, stoppingToken);
                    }
                    else if (negocio.TipoNegocio == "tienda" || negocio.TipoNegocio == "restaurante")
                    {
                        // Tiendas y restaurantes reutilizan el formato de membresías (ingresos/gastos del día)
                        resultado = await EnviarReporteMembresiaAsync(
                            context, emailService, negocio.NegocioId, negocio.NegocioNombre,
                            negocio.DuenoNegocio, negocio.Email, hoy, stoppingToken);
                    }
                    else
                    {
                        resultado = await EnviarReporteMembresiaAsync(
                            context, emailService, negocio.NegocioId, negocio.NegocioNombre,
                            negocio.DuenoNegocio, negocio.Email, hoy, stoppingToken);
                    }

                    if (resultado)
                    {
                        enviados++;
                        _logger.LogInformation(
                            "Reporte diario enviado a {Negocio} ({Email}) [{Tipo}]",
                            negocio.NegocioNombre, negocio.Email, negocio.TipoNegocio);
                    }
                    else
                    {
                        errores++;
                        _logger.LogWarning(
                            "No se pudo enviar reporte diario a {Negocio} ({Email})",
                            negocio.NegocioNombre, negocio.Email);
                    }
                }
                catch (Exception ex)
                {
                    errores++;
                    _logger.LogError(ex,
                        "Error al enviar reporte diario por email al negocio {NegocioId} ({Nombre})",
                        negocio.NegocioId, negocio.NegocioNombre);

                    // Capturar la excepción individualmente para que un fallo en un
                    // negocio no detenga el proceso para los demás negocios.
                }
            }

            _logger.LogInformation(
                "Reportes diarios por email completados: {Enviados} enviados, {Errores} errores, {Omitidos} omitidos de {Total} total",
                enviados, errores, omitidos, negociosActivos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general al procesar reportes diarios por email");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // REPORTE PARA NEGOCIOS DE MEMBRESÍAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Recopila los datos del día para un negocio de tipo "membresias" y envía
    /// el reporte por correo electrónico llamando a
    /// <see cref="IEmailService.EnviarReporteDiarioMembresiaAsync"/>.
    ///
    /// Datos recopilados:
    ///   - <c>ingresosDia</c>: suma de Logs con monto positivo creados hoy.
    ///   - <c>nuevosClientes</c>: clientes registrados hoy en este negocio.
    ///   - <c>productosVendidos</c>: cantidad total de unidades vendidas hoy (MovimientosInventario tipo "venta").
    ///   - <c>totalProductos</c>: suma del total monetario de ventas de productos hoy.
    ///   - <c>clientesPorVencer</c>: clientes cuya membresía termina exactamente mañana.
    ///   - <c>devoluciones</c>: número de movimientos de tipo "devolucion" hoy.
    ///   - <c>montoDevuelto</c>: suma de los montos absolutos devueltos hoy.
    /// </summary>
    private async Task<bool> EnviarReporteMembresiaAsync(
        ApplicationDbContext context,
        IEmailService emailService,
        Guid negocioId,
        string nombreNegocio,
        string nombreDueno,
        string email,
        DateTime hoy,
        CancellationToken stoppingToken)
    {
        // Ingresos del día: suma de todos los Logs con monto positivo.
        // Los Logs son la fuente inmutable de verdad financiera —
        // si un cliente se elimina, su pago sigue registrado aquí.
        var ingresosDia = await context.Logs
            .Where(l => l.NegocioId == negocioId && l.Fecha.Date == hoy && l.Monto > 0)
            .SumAsync(l => l.Monto, stoppingToken);

        // Clientes registrados hoy en este negocio
        var nuevosClientes = await context.Clientes
            .CountAsync(c => c.NegocioId == negocioId && c.FechaDeCreacion.Date == hoy, stoppingToken);

        // Ventas de productos hoy (movimientos de inventario tipo "venta")
        var movimientos = await context.MovimientosInventario
            .Where(m => m.NegocioId == negocioId && m.Fecha.Date == hoy && m.Tipo == "venta")
            .ToListAsync(stoppingToken);
        var productosVendidos = movimientos.Sum(m => m.Cantidad);
        var totalProductos = movimientos.Sum(m => m.Total);

        // Clientes cuya membresía vence exactamente mañana.
        // Esto le da al dueño tiempo de contactarlos antes de que venzan.
        var manana = hoy.AddDays(1);
        var clientesPorVencer = await context.Clientes
            .CountAsync(c => c.NegocioId == negocioId && c.FechaQueTermina.Date == manana, stoppingToken);

        // Devoluciones del día (movimientos de inventario tipo "devolucion")
        var devMov = await context.MovimientosInventario
            .Where(m => m.NegocioId == negocioId && m.Fecha.Date == hoy && m.Tipo == "devolucion")
            .ToListAsync(stoppingToken);
        var devoluciones = devMov.Count;
        var montoDevuelto = devMov.Sum(m => Math.Abs(m.Total));

        return await emailService.EnviarReporteDiarioMembresiaAsync(
            email,
            nombreNegocio,
            nombreDueno,
            ingresosDia,
            nuevosClientes,
            productosVendidos,
            totalProductos,
            clientesPorVencer,
            devoluciones,
            montoDevuelto,
            hoy);
    }

    // ═══════════════════════════════════════════════════════════
    // REPORTE PARA NEGOCIOS ARTESANALES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Recopila los datos del día para un negocio de tipo "artesanal" y envía
    /// el reporte por correo electrónico llamando a
    /// <see cref="IEmailService.EnviarReporteDiarioArtesanalAsync"/>.
    ///
    /// Datos recopilados:
    ///   - <c>citasCompletadas</c>: citas con estado "completada" que iniciaron hoy.
    ///   - <c>citasCanceladas</c>: citas con estado "cancelada" que iniciaron hoy.
    ///   - <c>ingresosDia</c>: suma del Total de PagosCita registrados hoy.
    ///   - <c>nuevosClientes</c>: clientes registrados hoy en este negocio.
    ///   - <c>servicioMasPopular</c>: nombre del servicio con más citas hoy (o "N/A").
    ///   - <c>totalProductos</c>: suma del total monetario de ventas de productos hoy.
    /// </summary>
    private async Task<bool> EnviarReporteArtesanalAsync(
        ApplicationDbContext context,
        IEmailService emailService,
        Guid negocioId,
        string nombreNegocio,
        string nombreDueno,
        string email,
        DateTime hoy,
        CancellationToken stoppingToken)
    {
        // Cargar todas las citas de hoy en memoria para poder hacer
        // cálculos de agrupamiento (GroupBy) sin múltiples round-trips a la BD.
        var citasHoy = await context.Citas
            .Where(c => c.NegocioId == negocioId && c.FechaHoraInicio.Date == hoy)
            .ToListAsync(stoppingToken);

        var citasCompletadas = citasHoy.Count(c => c.Estado == "completada");
        var citasCanceladas = citasHoy.Count(c => c.Estado == "cancelada");

        // Ingresos del día: suma del Total de todos los PagosCita registrados hoy.
        // PagosCita es la fuente de verdad financiera para negocios artesanales.
        var ingresosDia = await context.PagosCita
            .Where(p => p.NegocioId == negocioId && p.FechaCreacion.Date == hoy)
            .SumAsync(p => p.Total, stoppingToken);

        // Clientes registrados hoy en este negocio
        var nuevosClientes = await context.Clientes
            .CountAsync(c => c.NegocioId == negocioId && c.FechaDeCreacion.Date == hoy, stoppingToken);

        // Servicio más popular: el nombre de servicio que aparece más veces
        // en las citas de hoy. Si no hubo citas, devuelve "N/A".
        var servicioMasPopular = citasHoy
            .GroupBy(c => c.NombreServicio)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? "N/A";

        // Ventas de productos hoy (movimientos de inventario tipo "venta")
        var movimientos = await context.MovimientosInventario
            .Where(m => m.NegocioId == negocioId && m.Fecha.Date == hoy && m.Tipo == "venta")
            .ToListAsync(stoppingToken);
        var totalProductos = movimientos.Sum(m => m.Total);

        return await emailService.EnviarReporteDiarioArtesanalAsync(
            email,
            nombreNegocio,
            nombreDueno,
            ingresosDia,
            citasCompletadas,
            citasCanceladas,
            nuevosClientes,
            servicioMasPopular,
            totalProductos,
            hoy);
    }
}
