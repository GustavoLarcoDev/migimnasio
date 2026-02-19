// ═══════════════════════════════════════════════════════════════════════════
// IWhatsAppService.cs — Contrato del servicio de mensajería de WhatsApp
//
// CONTEXTO ARQUITECTURAL:
//   Este sistema usa UN solo número de WhatsApp Business (el de la plataforma
//   My-Negocio) para enviar mensajes en nombre de TODOS los negocios clientes.
//   Los mensajes incluyen el nombre del negocio para que el cliente final sepa
//   de qué empresa le está escribiendo.
//
//   Ejemplo: el gimnasio "FitZone" no tiene su propio número de WhatsApp Business.
//   El mensaje que recibe su cliente dice:
//   "Hola María, tu membresía en *FitZone* vence en 3 días..."
//   Pero el remitente técnico es el número de My-Negocio.
//
// API UTILIZADA: Meta Cloud API (WhatsApp Business Platform)
//   La implementación llama directamente a:
//   https://graph.facebook.com/{versión}/{phoneNumberId}/messages
//   con autenticación Bearer token.
//
// TIPOS DE MENSAJE SOPORTADOS:
//   - Texto plano: para recordatorios de membresía y resumen diario
//   - Recordatorio de cita (cliente): avisa de una cita próxima al cliente final
//   - Recordatorio de cita (negocio): avisa al dueño sobre citas del día siguiente
//
// VER: WhatsAppService.cs para la implementación y configuración de appsettings.json
// ═══════════════════════════════════════════════════════════════════════════

namespace Gimnasio.Services;

/// <summary>
/// Contrato del servicio de mensajería de WhatsApp Business (Meta Cloud API).
/// Un solo número de WhatsApp (el de la plataforma) envía todos los mensajes
/// en nombre de cada negocio registrado en el sistema.
/// Implementado por <see cref="WhatsAppService"/>.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Envía un mensaje de texto plano a cualquier número de teléfono.
    /// Es el método base que todos los demás métodos usan internamente.
    /// El número de teléfono se limpia y normaliza automáticamente:
    ///   - Se eliminan espacios, guiones, paréntesis y el símbolo +
    ///   - Si empieza con "0" (formato Ecuador), se reemplaza con "593"
    /// </summary>
    /// <param name="telefono">Número de teléfono (cualquier formato: +593987654321, 0987654321, etc.)</param>
    /// <param name="mensaje">Texto del mensaje. Soporta formato de WhatsApp: *negrita*, _cursiva_</param>
    /// <returns><c>true</c> si la API de Meta respondió con éxito. <c>false</c> si hubo error de red o API.</returns>
    Task<bool> EnviarMensajeTextoAsync(string telefono, string mensaje);

    /// <summary>
    /// Envía un recordatorio de vencimiento de membresía al cliente final.
    /// El mensaje varía según cuántos días faltan:
    ///   - 1 día: alerta urgente con "vence MAÑANA"
    ///   - 3 días: recordatorio anticipado con días específicos
    ///   - Otros: mensaje genérico con la cantidad de días
    /// El mensaje siempre aclara que es automatizado y que no deben responder
    /// al número (ya que es el número de My-Negocio, no del negocio directamente).
    /// </summary>
    /// <param name="telefono">Número del cliente</param>
    /// <param name="nombreCliente">Nombre del cliente para personalizar el saludo</param>
    /// <param name="nombreNegocio">Nombre del negocio (ej. "Gimnasio FitZone") para identificación</param>
    /// <param name="diasRestantes">Días que faltan para que venza la membresía (0, 1, 2 o 3)</param>
    Task<bool> EnviarRecordatorioMembresiaAsync(string telefono, string nombreCliente, string nombreNegocio, int diasRestantes);

    /// <summary>
    /// Envía el resumen diario del negocio al dueño.
    /// Se llama desde el servicio de reporte diario (DailyReportService) en un
    /// horario programado (típicamente al cierre del día o a primera hora).
    /// Incluye: ingresos del día, nuevos clientes y membresías que vencen mañana.
    /// </summary>
    /// <param name="telefono">Número del dueño del negocio</param>
    /// <param name="nombreNegocio">Nombre del negocio para el encabezado del mensaje</param>
    /// <param name="ingresosDia">Suma total de ingresos del día</param>
    /// <param name="nuevosClientes">Cantidad de clientes nuevos registrados hoy</param>
    /// <param name="porVencerManana">Membresías que vencen el día siguiente</param>
    Task<bool> EnviarResumenDiarioAsync(string telefono, string nombreNegocio, decimal ingresosDia, int nuevosClientes, int porVencerManana);

    /// <summary>
    /// Envía un recordatorio de cita al CLIENTE antes de su turno.
    /// Informa el negocio, el empleado que lo atenderá y la hora.
    /// Aclara que es un mensaje automatizado y que contacten al negocio si necesitan cancelar.
    /// Usado por negocios de tipo "artesanal" (peluquerías, spas, etc.) que manejan citas.
    /// </summary>
    /// <param name="telefono">Número del cliente</param>
    /// <param name="nombreCliente">Nombre del cliente para el saludo</param>
    /// <param name="nombreNegocio">Nombre del negocio para identificación</param>
    /// <param name="nombreEmpleado">Nombre del empleado/profesional que atenderá al cliente</param>
    /// <param name="hora">Hora de la cita en formato legible (ej. "10:30 AM")</param>
    Task<bool> EnviarRecordatorioCitaClienteAsync(string telefono, string nombreCliente, string nombreNegocio, string nombreEmpleado, string hora);

    /// <summary>
    /// Envía un recordatorio de cita al DUEÑO del negocio para que esté preparado.
    /// Informa el servicio que se realizará, la hora y el empleado que lo ejecutará.
    /// Firmado con "— MiNegocio" ya que es un mensaje administrativo interno.
    /// </summary>
    /// <param name="telefono">Número del dueño del negocio</param>
    /// <param name="nombreDueno">Nombre del dueño para el saludo</param>
    /// <param name="nombreServicio">Nombre del servicio a realizar (ej. "Corte + Color")</param>
    /// <param name="hora">Hora de la cita</param>
    /// <param name="nombreEmpleado">Nombre del empleado que realizará el servicio</param>
    Task<bool> EnviarRecordatorioCitaNegocioAsync(string telefono, string nombreDueno, string nombreServicio, string hora, string nombreEmpleado);
}
