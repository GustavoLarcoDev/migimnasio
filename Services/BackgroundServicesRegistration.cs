// ═══════════════════════════════════════════════════════════
// BackgroundServicesRegistration.cs — Registro de servicios de fondo
//
// PATRÓN: Método de extensión (Extension Method) sobre IServiceCollection.
// Este patrón se usa para encapsular grupos de registros de DI relacionados
// y mantener Program.cs limpio y organizado.
//
// En lugar de escribir en Program.cs:
//   builder.Services.AddHostedService<MembershipReminderService>();
//   builder.Services.AddHostedService<DailyReportService>();
//   builder.Services.AddHostedService<AppointmentReminderService>();
//
// Se escribe simplemente:
//   builder.Services.AddBackgroundServices();
//
// HOSTED SERVICES vs SCOPED SERVICES:
// AddHostedService registra el servicio como singleton y lo inicia automáticamente
// cuando arranca la app. ASP.NET Core llama a ExecuteAsync() en cada uno.
// Son "fire and forget" — corren en segundo plano sin bloquear las peticiones HTTP.
//
// ORDEN DE REGISTRO: El orden en que se registran es el orden en que
// ASP.NET Core los inicia al arrancar la aplicación.
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Services;

/// <summary>
/// Clase estática que extiende IServiceCollection con el método
/// <see cref="AddBackgroundServices"/> para registrar los tres servicios
/// de mensajería automática de WhatsApp como HostedServices.
///
/// Se llama desde Program.cs como: builder.Services.AddBackgroundServices()
///
/// Los HostedServices son singleton y se inician automáticamente al arrancar
/// la aplicación. Cada uno corre en su propio hilo de fondo.
/// </summary>
public static class BackgroundServicesRegistration
{
    /// <summary>
    /// Registra los servicios de fondo de mensajería y reportes automáticos
    /// en el contenedor de inyección de dependencias.
    ///
    /// Servicios registrados:
    /// <list type="bullet">
    ///   <item>
    ///     <see cref="MembershipReminderService"/> — Corre a las 8:00 AM Ecuador.
    ///     Envía recordatorios de vencimiento de membresía a clientes que
    ///     vencen en 3 o 1 día. Solo para negocios de tipo "membresias".
    ///   </item>
    ///   <item>
    ///     <see cref="DailyReportService"/> — Corre a las 9:00 PM Ecuador.
    ///     Envía un resumen financiero del día (ingresos, nuevos clientes,
    ///     membresías por vencer) al dueño de cada negocio activo.
    ///   </item>
    ///   <item>
    ///     <see cref="AppointmentReminderService"/> — Corre cada 5 minutos.
    ///     Envía recordatorios de cita al cliente y al dueño del negocio,
    ///     35 minutos antes del inicio de la cita. Solo para negocios "artesanal".
    ///   </item>
    ///   <item>
    ///     <see cref="DailyEmailReportService"/> — Corre a las 11:00 PM Ecuador.
    ///     Envía un reporte financiero detallado del día por correo electrónico
    ///     al dueño de cada negocio activo (membresias y artesanal).
    ///   </item>
    /// </list>
    ///
    /// Retorna el mismo IServiceCollection para permitir el encadenamiento
    /// fluido de llamadas (builder.Services.AddX().AddY().AddZ()).
    /// </summary>
    /// <param name="services">El contenedor de DI de la aplicación.</param>
    /// <returns>El mismo IServiceCollection para poder encadenar más registros.</returns>
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        // Recordatorios de membresía: 8:00 AM Ecuador
        services.AddHostedService<MembershipReminderService>();

        // Resumen diario al dueño: 9:00 PM Ecuador
        services.AddHostedService<DailyReportService>();

        // Recordatorios de citas: cada 5 minutos (solo artesanal)
        services.AddHostedService<AppointmentReminderService>();

        // Reporte diario detallado al dueño por email: 11:00 PM Ecuador
        services.AddHostedService<DailyEmailReportService>();

        // Recordatorios de expiración de suscripción: 10:00 AM Ecuador
        services.AddHostedService<SubscriptionExpirationReminderService>();

        // Retornar services permite encadenar: builder.Services.AddBackgroundServices().AddOtraCosa()
        return services;
    }
}
