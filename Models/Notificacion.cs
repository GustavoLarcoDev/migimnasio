// ═══════════════════════════════════════════════════════════
// Notificacion.cs — Modelo de notificación automática de vencimiento
//
// El sistema genera notificaciones automáticamente cuando la membresía
// de un cliente está próxima a vencer (dentro de 3 días o menos).
//
// Flujo de generación (ejecutado al abrir el dashboard):
//   1. Se buscan clientes con FechaQueTermina en los próximos 3 días.
//   2. Se verifica que no exista ya una notificación para ese cliente HOY.
//   3. Si no existe, se crea una nueva Notificacion con Leida = false.
//   4. Se muestra al dueño del negocio en el panel de notificaciones.
//
// La verificación de duplicados (paso 2) evita crear múltiples
// notificaciones para el mismo cliente en el mismo día.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa una notificación automática generada por el sistema cuando la membresía
/// de un cliente está a punto de vencer.
///
/// Las notificaciones se muestran en el panel del negocio y se marcan como "leídas"
/// cuando el dueño las revisa. Solo se crea una notificación por cliente por día.
/// </summary>
public class Notificacion
{
    /// <summary>
    /// Identificador único de la notificación (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla Notificaciones.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// ID del negocio al que pertenece esta notificación.
    /// [Required] = toda notificación debe estar asociada a un negocio.
    /// Se usa para mostrar solo las notificaciones del negocio que inició sesión.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Texto de la notificación que verá el dueño del negocio.
    /// Ejemplo: "La membresía de Juan Pérez vence en 2 día(s)".
    /// [Required] = toda notificación debe tener un mensaje.
    /// [MaxLength(500)] = suficiente para el mensaje con nombre del cliente y días.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Mensaje { get; set; }

    /// <summary>
    /// Categoría de la notificación para agrupación o iconos diferentes en el futuro.
    /// Actualmente solo se usa el tipo "vencimiento".
    /// Otros tipos podrían agregarse en el futuro (ej: "pago_pendiente", "stock_bajo").
    /// [MaxLength(50)] = suficiente para los tipos enumerados.
    /// Valor por defecto: "vencimiento".
    /// </summary>
    [MaxLength(50)]
    public string Tipo { get; set; } = "vencimiento";

    /// <summary>
    /// ID del cliente cuya membresía está por vencer (si aplica).
    /// Es nullable (?) para soportar futuros tipos de notificación no relacionados a clientes.
    /// Se usa para enlazar la notificación al registro del cliente en la UI.
    /// </summary>
    public Guid? ClienteId { get; set; }

    /// <summary>
    /// Nombre completo del cliente relacionado (denormalizado).
    /// Se guarda como texto para mostrarlo rápidamente sin hacer JOIN.
    /// Ejemplo: "Juan Pérez".
    /// [MaxLength(200)] = suficiente para nombre + apellido completos.
    /// </summary>
    [MaxLength(200)]
    public string NombreCliente { get; set; }

    /// <summary>
    /// Indica si el dueño del negocio ya vio y leyó esta notificación.
    /// false = notificación nueva (se muestra con indicador de no leída en el panel).
    /// true  = notificación ya revisada (el dueño la marcó como leída o la descartó).
    /// El conteo de notificaciones no leídas en el topbar usa este campo.
    /// Valor por defecto: false.
    /// </summary>
    public bool Leida { get; set; } = false;

    /// <summary>
    /// Fecha y hora en que se generó la notificación.
    /// Se asigna automáticamente con TimeHelper.Now al crear el registro.
    /// Se usa para evitar duplicados: no se crea una nueva notificación si ya existe
    /// una con el mismo ClienteId creada en el mismo día.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;
}
