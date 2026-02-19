// ═══════════════════════════════════════════════════════════════════════════
// INotificationService.cs — Contrato del servicio de notificaciones automáticas
//
// PROPÓSITO:
//   Gestiona las notificaciones internas del dashboard de cada negocio.
//   Estas notificaciones son DIFERENTES a los mensajes de WhatsApp o email:
//   - WhatsApp/Email → van al cliente final (el socio del gimnasio)
//   - Notificaciones → aparecen en el panel de control del dueño del negocio
//
// CUÁNDO SE GENERAN:
//   GenerarNotificacionesAsync() se llama cada vez que el dueño carga su dashboard.
//   Busca membresías que vencen en los próximos 3 días y crea alertas visibles
//   en el panel con el nombre del cliente y cuántos días faltan.
//
// ANTI-DUPLICADOS:
//   El sistema verifica si ya existe una notificación para ese cliente en el día
//   de hoy antes de crear una nueva. Esto evita que si el dueño recarga el
//   dashboard 10 veces en el día, se generen 10 notificaciones iguales.
//
// TIPOS DE NEGOCIO:
//   Solo los negocios de membresías generan estas notificaciones.
//   Los negocios "artesanal" (peluquerías, spas) no tienen membresías → se omiten.
// ═══════════════════════════════════════════════════════════════════════════

namespace Gimnasio.Services;

/// <summary>
/// Contrato del servicio de notificaciones del dashboard.
/// Implementado por <see cref="NotificationService"/>.
/// </summary>
public interface INotificationService
{
    // ═══════════════════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las últimas 50 notificaciones del negocio, ordenadas de más
    /// reciente a más antigua. El límite de 50 evita cargar miles de registros
    /// en el JSON si el negocio lleva mucho tiempo usando la plataforma.
    /// Devuelve <c>object</c> (tipo anónimo) para serialización JSON eficiente.
    /// </summary>
    Task<object> GetNotificacionesAsync(Guid negocioId);

    /// <summary>
    /// Cuenta las notificaciones no leídas del negocio.
    /// Se usa para mostrar el badge (círculo rojo con número) en el sidebar
    /// del dashboard. Solo cuenta las no leídas para no saturar al usuario
    /// con alertas que ya revisó.
    /// </summary>
    Task<int> GetNotificacionesCountAsync(Guid negocioId);

    // ═══════════════════════════════════════════════════════════════════════
    // GESTIÓN DE ESTADO LEÍDO/NO LEÍDO
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Marca una notificación específica como leída.
    /// Se llama cuando el usuario hace clic en una notificación individual.
    /// Filtra por negocioId para seguridad (un negocio no puede marcar
    /// notificaciones de otro negocio).
    /// </summary>
    Task<(bool success, string message)> MarcarLeidaAsync(Guid id, Guid negocioId);

    /// <summary>
    /// Marca TODAS las notificaciones no leídas del negocio como leídas.
    /// Se llama cuando el usuario hace clic en "Marcar todas como leídas".
    /// Útil cuando hay muchas notificaciones acumuladas y el usuario quiere
    /// limpiar el badge de un solo clic.
    /// Devuelve cuántas notificaciones fueron marcadas en el mensaje de respuesta.
    /// </summary>
    Task<(bool success, string message)> MarcarTodasLeidasAsync(Guid negocioId);

    // ═══════════════════════════════════════════════════════════════════════
    // GENERACIÓN AUTOMÁTICA
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Genera automáticamente notificaciones de alerta para membresías próximas a vencer.
    ///
    /// VENTANA DE TIEMPO:
    ///   Detecta clientes cuya membresía vence entre hoy y 3 días en el futuro.
    ///   Esto da al negocio tiempo para contactar al cliente antes de que pierda acceso.
    ///
    /// PROTECCIÓN ANTI-DUPLICADOS:
    ///   Por cada cliente encontrado, verifica si ya existe una notificación para él
    ///   con fecha de hoy. Si ya existe → la omite. Si no existe → crea una nueva.
    ///   Esto permite que el método se llame múltiples veces al día sin generar spam.
    ///
    /// NEGOCIOS ARTESANALES:
    ///   Si el negocio es de tipo "artesanal" (no maneja membresías), el método
    ///   retorna inmediatamente sin hacer consultas adicionales a la base de datos.
    ///
    /// RETORNO:
    ///   Devuelve una tupla con tres valores:
    ///   - success: siempre true (no hay forma de "fallar" en este proceso)
    ///   - message: descripción de cuántas notificaciones se generaron
    ///   - count: número entero de notificaciones nuevas creadas (puede ser 0)
    /// </summary>
    Task<(bool success, string message, int count)> GenerarNotificacionesAsync(Guid negocioId);
}
