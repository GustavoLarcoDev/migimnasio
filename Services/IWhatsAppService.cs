// ═══════════════════════════════════════════════════════════
// IWhatsAppService.cs — Contrato del servicio de WhatsApp
// Todos los mensajes salen desde UN solo número (el del admin).
// Los mensajes se personalizan con el nombre del negocio.
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Services;

/// <summary>
/// Interfaz para el servicio de mensajería de WhatsApp Business (Meta Cloud API).
/// Un solo número de WhatsApp (admin) envía todo.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Envía un mensaje de texto plano a un número de teléfono
    /// </summary>
    Task<bool> EnviarMensajeTextoAsync(string telefono, string mensaje);

    /// <summary>
    /// Envía un recordatorio de vencimiento de membresía al cliente
    /// (el mensaje dice de qué negocio viene)
    /// </summary>
    Task<bool> EnviarRecordatorioMembresiaAsync(string telefono, string nombreCliente, string nombreNegocio, int diasRestantes);

    /// <summary>
    /// Envía un resumen diario del negocio al dueño
    /// </summary>
    Task<bool> EnviarResumenDiarioAsync(string telefono, string nombreNegocio, decimal ingresosDia, int nuevosClientes, int porVencerManana);
}
