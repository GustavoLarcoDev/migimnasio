// ═══════════════════════════════════════════════════════════════════════════
// IWhatsAppService.cs — Contrato del servicio de mensajería de WhatsApp
//
// CONTEXTO ARQUITECTURAL:
//   Este sistema usa UN solo número de WhatsApp Business (el de la plataforma
//   My-Negocio) para enviar mensajes en nombre de TODOS los negocios clientes.
//   Los mensajes incluyen el nombre del negocio para que el cliente final sepa
//   de qué empresa le está escribiendo.
//
// API UTILIZADA: Twilio WhatsApp API
//
// MENSAJES A CLIENTES incluyen un link wa.me/ para contactar al negocio.
// MENSAJES AL DUEÑO/ADMIN no incluyen link wa.me/.
// ═══════════════════════════════════════════════════════════════════════════

namespace Gimnasio.Services;

/// <summary>
/// Contrato del servicio de mensajería de WhatsApp Business (Twilio API).
/// Un solo número de WhatsApp (el de la plataforma) envía todos los mensajes
/// en nombre de cada negocio registrado en el sistema.
/// Implementado por <see cref="WhatsAppService"/>.
/// </summary>
public interface IWhatsAppService
{
    // ═══════════════════════════════════════════════════════════
    // MÉTODO BASE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Envía un mensaje de texto plano a cualquier número de teléfono.
    /// Es el método base que todos los demás métodos usan internamente.
    /// </summary>
    Task<bool> EnviarMensajeTextoAsync(string telefono, string mensaje);

    // ═══════════════════════════════════════════════════════════
    // MENSAJES A CLIENTES (incluyen link wa.me/ del negocio)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Envía un recordatorio de vencimiento de membresía al cliente.
    /// Incluye link wa.me/ del negocio para contacto directo.
    /// </summary>
    Task<bool> EnviarRecordatorioMembresiaAsync(string telefono, string nombreCliente, string nombreNegocio, int diasRestantes, string telefonoNegocio = null);

    /// <summary>
    /// Envía confirmación de reserva/cita al cliente.
    /// Incluye link wa.me/ del negocio para contacto directo.
    /// </summary>
    Task<bool> EnviarConfirmacionReservaWhatsAppAsync(string telefono, string cliente, string negocio, string servicio, string empleado, DateTime fechaHora, decimal precio, string telefonoNegocio = null);

    /// <summary>
    /// Envía un recordatorio de cita al CLIENTE antes de su turno.
    /// Incluye link wa.me/ del negocio para contacto directo.
    /// </summary>
    Task<bool> EnviarRecordatorioCitaClienteAsync(string telefono, string nombreCliente, string nombreNegocio, string nombreEmpleado, string hora, string telefonoNegocio = null);

    /// <summary>
    /// Envía un recordatorio de cobro al cliente con membresía vencida.
    /// Incluye link wa.me/ del negocio para contacto directo.
    /// </summary>
    Task<bool> EnviarRecordatorioCobroWhatsAppAsync(string telefono, string nombreCliente, string nombreNegocio, int diasVencido, string telefonoNegocio = null);

    // ═══════════════════════════════════════════════════════════
    // MENSAJES AL DUEÑO/ADMIN (NO incluyen link wa.me/)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Envía el resumen diario del negocio al dueño (tipo membresías).
    /// </summary>
    Task<bool> EnviarResumenDiarioAsync(string telefono, string nombreNegocio, decimal ingresosDia, int nuevosClientes, int porVencerManana);

    /// <summary>
    /// Envía resumen diario para negocios artesanal/tienda/restaurante.
    /// </summary>
    Task<bool> EnviarResumenDiarioGeneralWhatsAppAsync(string telefono, string negocio, decimal ingresos, decimal gastos, decimal ganancia, int nuevosClientes);

    /// <summary>
    /// Envía un recordatorio de cita al DUEÑO del negocio.
    /// </summary>
    Task<bool> EnviarRecordatorioCitaNegocioAsync(string telefono, string nombreDueno, string nombreServicio, string hora, string nombreEmpleado);

    /// <summary>
    /// Envía recordatorio de cita al empleado que atenderá al cliente.
    /// </summary>
    Task<bool> EnviarRecordatorioCitaEmpleadoWhatsAppAsync(string telefono, string empleado, string cliente, string servicio, string hora);

    // ═══════════════════════════════════════════════════════════
    // MENSAJES PAREADOS CON EMAIL (al dueño/vendedor)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Envía bienvenida al vendedor con sus credenciales de acceso.
    /// </summary>
    Task<bool> EnviarBienvenidaVendedorWhatsAppAsync(string telefono, string nombre, string email, string password);

    /// <summary>
    /// Envía bienvenida al negocio nuevo con sus credenciales de acceso.
    /// </summary>
    Task<bool> EnviarBienvenidaNegocioWhatsAppAsync(string telefono, string negocio, string dueno, string email, string password, string tipoNegocio);

    // ═══════════════════════════════════════════════════════════
    // NOTIFICACIONES DE SUSCRIPCIÓN Y NEGOCIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Advierte al dueño que su suscripción está próxima a expirar.
    /// </summary>
    Task<bool> EnviarAdvertenciaSuscripcionWhatsAppAsync(string telefono, string negocio, string dueno, int diasRestantes, DateTime fechaExpiracion);

    /// <summary>
    /// Notifica al dueño que su negocio fue bloqueado.
    /// </summary>
    Task<bool> EnviarNotificacionNegocioBloqueadoWhatsAppAsync(string telefono, string negocio, string dueno);

    /// <summary>
    /// Notifica al dueño que su negocio fue desbloqueado.
    /// </summary>
    Task<bool> EnviarNotificacionNegocioDesbloqueadoWhatsAppAsync(string telefono, string negocio, string dueno);

    // ═══════════════════════════════════════════════════════════
    // MARKETING / PROMOCIÓN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Envía un mensaje promocional de My-Negocio a un potencial cliente.
    /// Usa el template de Promoción con link de WhatsApp para responder.
    /// </summary>
    Task<bool> EnviarPromocionWhatsAppAsync(string telefono, string nombreNegocio);
}
