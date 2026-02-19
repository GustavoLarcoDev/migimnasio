// ═══════════════════════════════════════════════════════════
// Cita.cs — Modelo de cita/appointment para negocios artesanales
//
// Una cita representa una reservación de tiempo en la que un cliente
// recibe un servicio específico de un empleado específico.
//
// Este modelo aplica SOLO para negocios con TipoNegocio = "artesanal"
// (barberías, spas, salones de belleza, etc.).
//
// Ciclo de vida de una cita:
//   pendiente → confirmada → en_progreso → completada
//                         ↘ cancelada (en cualquier punto)
//
// Nota sobre denormalización: algunos campos como NombreCliente,
// NombreEmpleado y NombreServicio se guardan directamente en la cita
// para consultas rápidas sin necesidad de hacer múltiples JOINs.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa una cita agendada en un negocio artesanal (barbería, spa, salón de belleza, etc.).
/// Una cita vincula a un cliente, un empleado y un servicio en una fecha y hora específica.
///
/// Estados válidos: "pendiente", "confirmada", "en_progreso", "completada", "cancelada".
/// Solo las citas en estado "completada" generan un PagoCita asociado.
/// </summary>
public class Cita
{
    /// <summary>
    /// Identificador único de la cita (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla Citas en la base de datos.
    /// </summary>
    [Key]
    public Guid CitaId { get; set; }

    /// <summary>
    /// ID del negocio donde se lleva a cabo esta cita.
    /// [Required] = toda cita pertenece a un negocio; no puede ser nula.
    /// Se usa para filtrar citas por negocio (aislamiento multi-tenant).
    /// Relación muchos-a-uno: muchas citas → un negocio.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// ID del cliente que agendó la cita.
    /// [Required] = no puede existir una cita sin cliente.
    /// Relación muchos-a-uno: un cliente puede tener muchas citas.
    /// </summary>
    [Required]
    public Guid ClienteId { get; set; }

    /// <summary>
    /// ID del empleado que atenderá al cliente en esta cita.
    /// [Required] = toda cita debe tener un empleado asignado.
    /// Relación muchos-a-uno: un empleado puede tener muchas citas.
    /// El empleado debe tener disponibilidad en el horario de la cita.
    /// </summary>
    [Required]
    public Guid EmpleadoId { get; set; }

    /// <summary>
    /// ID del servicio que se realizará en esta cita.
    /// [Required] = toda cita debe tener un servicio definido.
    /// Relación muchos-a-uno: un servicio puede estar en muchas citas.
    /// El servicio define la duración y el precio base de la cita.
    /// </summary>
    [Required]
    public Guid ServicioId { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DATOS DENORMALIZADOS (copias de datos para consultas rápidas)
    // Estos campos guardan copias de datos de otras tablas para evitar
    // JOINs costosos al mostrar listas de citas. Si el nombre del cliente
    // cambia después, la cita conserva el nombre original del momento.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Nombre completo del cliente al momento de agendar la cita (ej: "Juan Pérez").
    /// Se copia desde el objeto Cliente para mostrar en listas sin hacer JOIN.
    /// [MaxLength(200)] = suficiente para nombre + apellido completos.
    /// </summary>
    [MaxLength(200)]
    public string NombreCliente { get; set; }

    /// <summary>
    /// Nombre completo del empleado asignado (ej: "María García").
    /// Se copia desde el objeto Empleado para mostrar en el calendario sin hacer JOIN.
    /// [MaxLength(200)] = suficiente para nombre + apellido.
    /// </summary>
    [MaxLength(200)]
    public string NombreEmpleado { get; set; }

    /// <summary>
    /// Nombre del servicio a realizar (ej: "Manicure Clásico", "Corte de cabello").
    /// Se copia desde ServicioNegocio para mostrar en el calendario sin hacer JOIN.
    /// [MaxLength(200)] = igual al límite del nombre del servicio.
    /// </summary>
    [MaxLength(200)]
    public string NombreServicio { get; set; }

    /// <summary>
    /// Precio base del servicio en el momento de agendar la cita (en moneda local).
    /// Se copia desde ServicioNegocio.Precio al crear la cita.
    /// Si el precio del servicio cambia en el futuro, la cita conserva el precio original.
    /// Es el monto base; pueden sumarse extras o propina en el PagoCita.
    /// </summary>
    public decimal PrecioServicio { get; set; }

    // ═══════════════════════════════════════════════════════════
    // FECHA Y HORA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Fecha y hora de inicio de la cita.
    /// Ejemplo: 2026-02-18 10:00 AM.
    /// Se usa para verificar disponibilidad del empleado y mostrar en el calendario.
    /// </summary>
    public DateTime FechaHoraInicio { get; set; }

    /// <summary>
    /// Fecha y hora en que termina la cita.
    /// Se calcula como: FechaHoraInicio + DuracionMinutos.
    /// Se usa para verificar que no haya traslapes con otras citas del mismo empleado.
    /// </summary>
    public DateTime FechaHoraFin { get; set; }

    /// <summary>
    /// Duración del servicio en minutos.
    /// Se copia desde ServicioNegocio.DuracionMinutos al crear la cita.
    /// Ejemplo: 30 = media hora, 60 = una hora completa.
    /// Se usa para calcular FechaHoraFin y para mostrar en el calendario.
    /// </summary>
    public int DuracionMinutos { get; set; }

    // ═══════════════════════════════════════════════════════════
    // ESTADO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Estado actual de la cita en su ciclo de vida.
    /// Valores válidos:
    ///   "pendiente"    = cita recién creada, esperando confirmación (estado inicial).
    ///   "confirmada"   = el negocio confirmó que atenderá la cita.
    ///   "en_progreso"  = el cliente está siendo atendido en este momento.
    ///   "completada"   = el servicio terminó exitosamente; se puede generar el pago.
    ///   "cancelada"    = la cita fue cancelada (requiere MotivoCancelacion).
    /// [Required] = el estado es obligatorio.
    /// [MaxLength(20)] = suficiente para los valores enumerados.
    /// Valor por defecto: "pendiente".
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Estado { get; set; } = "pendiente";

    /// <summary>
    /// Razón por la que se canceló la cita.
    /// Solo aplica cuando Estado = "cancelada"; en otros estados es null.
    /// Ejemplo: "El cliente no se presentó" o "Empleado enfermo".
    /// [MaxLength(500)] = suficiente para una explicación detallada.
    /// </summary>
    [MaxLength(500)]
    public string MotivoCancelacion { get; set; }

    /// <summary>
    /// Indica si ya se envió al cliente un recordatorio por WhatsApp antes de la cita.
    /// false = el recordatorio aún no ha sido enviado.
    /// true  = el recordatorio ya fue enviado (evita enviarlo más de una vez).
    /// El servicio AppointmentReminderService revisa este campo periódicamente.
    /// </summary>
    public bool RecordatorioEnviado { get; set; }

    // ═══════════════════════════════════════════════════════════
    // AUDITORÍA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Fecha y hora en que se creó la cita en el sistema.
    /// Se asigna automáticamente con TimeHelper.Now.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    /// <summary>
    /// Fecha y hora de la última modificación de la cita.
    /// Se actualiza cada vez que cambia el estado, se mueve la cita, etc.
    /// </summary>
    public DateTime FechaDeActualizacion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties de Entity Framework)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Negocio al que pertenece esta cita.
    /// Relación muchos-a-uno: muchas citas → un negocio.
    /// EF Core carga este objeto solo cuando se hace .Include(c => c.Negocio).
    /// </summary>
    public Gym Negocio { get; set; }

    /// <summary>
    /// Cliente que agendó esta cita.
    /// Relación muchos-a-uno: un cliente puede tener muchas citas.
    /// </summary>
    public Cliente Cliente { get; set; }

    /// <summary>
    /// Empleado que atenderá (o atendió) esta cita.
    /// Relación muchos-a-uno: un empleado puede tener muchas citas.
    /// </summary>
    public Empleado Empleado { get; set; }

    /// <summary>
    /// Servicio que se realizará (o realizó) en esta cita.
    /// Relación muchos-a-uno: un servicio puede estar en muchas citas.
    /// </summary>
    public ServicioNegocio Servicio { get; set; }

    /// <summary>
    /// Pago registrado para esta cita (solo existe si Estado = "completada").
    /// Relación uno-a-uno: cada cita completada tiene un único pago.
    /// Es null si la cita no ha sido completada o si aún no se registró el pago.
    /// </summary>
    public PagoCita Pago { get; set; }
}
