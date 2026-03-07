namespace Gimnasio.Services;

/// <summary>
/// Servicio para gestionar el registro, pagos y comisiones del sistema de delivery.
/// Maneja solicitudes de registro (motorizados y restaurantes), pagos de suscripción/comisión,
/// y el bloqueo/desbloqueo automático por deuda.
/// </summary>
public interface IDeliveryAdminService
{
    // ═══════════════════════════════════════════════════════════
    // REGISTRO — Formularios públicos
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Registra una solicitud de motorizado con datos personales, vehículo y 5 fotos.
    /// Valida unicidad de cédula y email. Hash BCrypt del password.
    /// </summary>
    Task<(bool success, string message, Guid? solicitudId)> RegistrarMotorizadoAsync(
        string nombre, string apellido, string email, string password,
        string telefono, string cedula, string tipoVehiculo, string placa,
        string fotoCedulaFrontal, string fotoCedulaTrasera,
        string fotoLicencia, string fotoSelfie, string fotoVehiculo);

    /// <summary>
    /// Registra una solicitud de restaurante con datos del negocio y ubicación GPS.
    /// Valida unicidad de email. Hash BCrypt del password.
    /// </summary>
    Task<(bool success, string message, Guid? solicitudId)> RegistrarRestauranteAsync(
        string nombre, string apellido, string email, string password,
        string telefono, string nombreNegocio, string duenoNegocio,
        string direccion, string ciudad, double? latitud, double? longitud,
        string tiposComida, string logoUrl);

    /// <summary>
    /// Consulta el estado de una solicitud por su ID (para la pantalla de seguimiento).
    /// </summary>
    Task<object?> GetEstadoSolicitudAsync(Guid solicitudId);

    // ═══════════════════════════════════════════════════════════
    // ADMIN — Gestión de solicitudes
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todas las solicitudes pendientes de revisión (motorizados + restaurantes).
    /// </summary>
    Task<object> GetSolicitudesPendientesAsync();

    /// <summary>
    /// Obtiene el detalle completo de una solicitud específica.
    /// </summary>
    Task<object?> GetSolicitudAsync(Guid solicitudId);

    /// <summary>
    /// Aprueba una solicitud: crea Motorizado o Gym según el tipo y marca como aprobada.
    /// </summary>
    Task<(bool success, string message)> AprobarSolicitudAsync(Guid solicitudId);

    /// <summary>
    /// Rechaza una solicitud con motivo explicativo.
    /// </summary>
    Task<(bool success, string message)> RechazarSolicitudAsync(Guid solicitudId, string motivo);

    // ═══════════════════════════════════════════════════════════
    // ADMIN — Gestión de pagos
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los pagos de delivery pendientes de confirmación.
    /// </summary>
    Task<object> GetPagosDeliveryPendientesAsync();

    /// <summary>
    /// Confirma un pago: desbloquea al motorizado/restaurante y resetea comisiones.
    /// </summary>
    Task<(bool success, string message)> ConfirmarPagoDeliveryAsync(Guid pagoId);

    /// <summary>
    /// Rechaza un pago con notas explicativas.
    /// </summary>
    Task<(bool success, string message)> RechazarPagoDeliveryAsync(Guid pagoId, string notas);

    // ═══════════════════════════════════════════════════════════
    // COMISIONES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Registra comisiones de $0.20 para motorizado y/o restaurante al entregar un pedido.
    /// </summary>
    Task RegistrarComisionDeliveryAsync(Guid pedidoId);

    // ═══════════════════════════════════════════════════════════
    // BLOQUEO Y PAGOS (motorizado/restaurante)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Verifica si un motorizado está bloqueado (por comisiones o suscripción vencida).
    /// </summary>
    Task<bool> IsMotorizadoBloqueadoAsync(Guid motorizadoId);

    /// <summary>
    /// Envía una confirmación de pago (crea PagoDelivery pendiente de revisión admin).
    /// </summary>
    Task<(bool success, string message)> EnviarConfirmacionPagoAsync(
        string tipoPagador, Guid pagadorId, string tipoPago, decimal monto, string numConfirmacion);

    // ═══════════════════════════════════════════════════════════
    // ESTADÍSTICAS Y CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene estadísticas del sistema de delivery para el dashboard admin.
    /// </summary>
    Task<object> GetDeliveryAdminStatsAsync();

    /// <summary>
    /// Obtiene el estado de suscripción de un motorizado (plan, deuda, bloqueo).
    /// </summary>
    Task<object?> GetEstadoSuscripcionMotorizadoAsync(Guid motorizadoId);

    /// <summary>
    /// Obtiene el historial de pagos de un motorizado.
    /// </summary>
    Task<object> GetHistorialPagosMotorizadoAsync(Guid motorizadoId);
}
