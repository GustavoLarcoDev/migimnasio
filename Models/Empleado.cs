// ═══════════════════════════════════════════════════════════
// Empleado.cs — Modelo de empleado/staff de un negocio artesanal
//
// Un empleado es el prestador de servicios dentro del negocio.
// Puede ser un manicurista, estilista, barbero, masajista, etc.
//
// Este modelo aplica SOLO para negocios con TipoNegocio = "artesanal".
//
// Cada empleado tiene:
//   - Horarios (tabla unificada Horario con TipoHorario="regular" y "excepcion")
//   - Citas asignadas a su agenda (Cita)
//
// El soft delete (IsActive = false) permite desactivar empleados que ya
// no trabajan sin eliminar el historial de sus citas pasadas.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa un empleado o miembro del staff de un negocio artesanal.
/// El empleado es quien ejecuta los servicios y tiene una agenda de citas.
///
/// Solo existe en negocios con TipoNegocio = "artesanal" (barberías, spas, etc.).
/// Los empleados tienen horarios regulares y pueden tener excepciones por fecha.
/// </summary>
public class Empleado
{
    /// <summary>
    /// Identificador único del empleado (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla Empleados en la base de datos.
    /// </summary>
    [Key]
    public Guid EmpleadoId { get; set; }

    /// <summary>
    /// ID del negocio al que pertenece este empleado.
    /// [Required] = todo empleado debe pertenecer a un negocio.
    /// Se usa para filtrar empleados por negocio (aislamiento multi-tenant).
    /// Relación muchos-a-uno: muchos empleados → un negocio.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre(s) del empleado, por ejemplo: "Ana" o "José Luis".
    /// [Required] = obligatorio para identificar al empleado.
    /// [MaxLength(100)] = suficiente para nombres de personas.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; }

    /// <summary>
    /// Apellido(s) del empleado, por ejemplo: "Ramírez" o "Castro Vega".
    /// [Required] = obligatorio.
    /// [MaxLength(100)] = suficiente para apellidos.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Apellido { get; set; }

    /// <summary>
    /// Número de teléfono del empleado (opcional, para contacto interno).
    /// No se usa para envíos automáticos de WhatsApp.
    /// [MaxLength(20)] = suficiente para números con código de país.
    /// </summary>
    [MaxLength(20)]
    public string Telefono { get; set; }

    /// <summary>
    /// Correo electrónico del empleado (opcional, para notificaciones de citas).
    /// Si tiene email registrado, el sistema le enviará recordatorios de cita
    /// 30 minutos antes, similar al recordatorio que recibe el cliente.
    /// [MaxLength(200)] = suficiente para cualquier dirección de email.
    /// </summary>
    [MaxLength(200)]
    public string Email { get; set; }

    /// <summary>
    /// Área de especialización del empleado dentro del negocio.
    /// Ejemplos: "Manicurista", "Pedicurista", "Estilista", "Barbero", "Masajista".
    /// Se muestra en el panel de citas para que el cliente sepa con quién agendar.
    /// [MaxLength(100)] = suficiente para cualquier especialidad.
    /// </summary>
    [MaxLength(100)]
    public string Especialidad { get; set; }

    /// <summary>
    /// Indica si el empleado está activo y puede recibir citas.
    /// true  = empleado activo (aparece en la agenda y puede ser asignado a citas).
    /// false = empleado inactivo/dado de baja (soft delete; no aparece para nuevas citas,
    ///         pero sus citas históricas se conservan en la base de datos).
    /// El dueño del negocio puede desactivar empleados desde el panel de configuración.
    /// Valor por defecto: true.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Fecha y hora en que el empleado fue registrado en el sistema.
    /// Se asigna automáticamente con TimeHelper.Now al crear el registro.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties de Entity Framework)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Negocio al que pertenece este empleado.
    /// Relación muchos-a-uno: muchos empleados → un negocio.
    /// EF Core carga este objeto solo cuando se hace .Include(e => e.Negocio).
    /// </summary>
    public Gym Negocio { get; set; }

    /// <summary>
    /// Lista de horarios del empleado (regulares semanales y excepciones).
    /// Relación uno-a-muchos: un empleado → múltiples registros Horario.
    /// TipoHorario="regular": hasta 7 horarios (uno por día de semana).
    /// TipoHorario="excepcion": N excepciones (una por fecha específica).
    /// EF Core carga esta lista solo cuando se hace .Include(e => e.Horarios).
    /// </summary>
    public ICollection<Horario> Horarios { get; set; } = new List<Horario>();

    /// <summary>
    /// Lista de citas asignadas a este empleado.
    /// Relación uno-a-muchos: un empleado puede tener muchas citas.
    /// Se usa para ver la agenda del empleado y verificar disponibilidad.
    /// EF Core carga esta lista solo cuando se hace .Include(e => e.Citas).
    /// </summary>
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
