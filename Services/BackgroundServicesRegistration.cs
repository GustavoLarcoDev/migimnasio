// ═══════════════════════════════════════════════════════════
// BackgroundServicesRegistration.cs — Registro de servicios de fondo
// Extensión para IServiceCollection que registra los servicios
// de mensajería automática de WhatsApp como HostedServices.
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Services;

/// <summary>
/// Métodos de extensión para registrar los servicios de fondo
/// de mensajería automática de WhatsApp en el contenedor de DI.
/// </summary>
public static class BackgroundServicesRegistration
{
    /// <summary>
    /// Registra los servicios de fondo para mensajes automáticos de WhatsApp:
    /// - MembershipReminderService: recordatorios de vencimiento a las 8:00 AM
    /// - DailyReportService: resumen diario del negocio a las 9:00 PM
    /// </summary>
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<MembershipReminderService>();
        services.AddHostedService<DailyReportService>();
        services.AddHostedService<AppointmentReminderService>();
        return services;
    }
}
