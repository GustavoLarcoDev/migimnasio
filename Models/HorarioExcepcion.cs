// ═══════════════════════════════════════════════════════════
// HorarioExcepcion.cs — Excepción de horario para una fecha específica
//
// Permite sobrescribir el horario regular de un empleado para un día concreto.
// Casos de uso comunes:
//   - Día libre puntual: el empleado pide el martes 20 de febrero libre.
//     → EsDiaLibre = true, HoraInicio/HoraFin quedan nulos.
//   - Horario extendido: ese sábado trabajan hasta las 8pm en vez de las 6pm.
//     → EsDiaLibre = false, HoraInicio="09:00", HoraFin="20:00".
//   - Entrada tardía: ese día entra a las 11am por una cita médica.
//     → EsDiaLibre = false, HoraInicio="11:00", HoraFin="18:00".
//
// PRIORIDAD: HorarioExcepcion tiene prioridad sobre HorarioEmpleado.
// Si existe una excepción para una fecha, el sistema la usa en lugar del horario regular.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa una excepción puntual al horario semanal de un empleado.
/// Permite marcar un día específico como libre o con horario diferente al habitual.
///
/// El sistema de disponibilidad consulta primero si existe una HorarioExcepcion
/// para la fecha solicitada; si la hay, la usa en lugar del HorarioEmpleado regular.
/// </summary>
public class HorarioExcepcion
{
    /// <summary>
    /// Identificador único de la excepción (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla HorariosExcepcion.
    /// </summary>
    [Key]
    public Guid ExcepcionId { get; set; }

    /// <summary>
    /// ID del empleado al que aplica esta excepción de horario.
    /// [Required] = toda excepción debe pertenecer a un empleado.
    /// Relación muchos-a-uno: un empleado puede tener muchas excepciones.
    /// </summary>
    [Required]
    public Guid EmpleadoId { get; set; }

    /// <summary>
    /// ID del negocio al que pertenece el empleado.
    /// Se guarda directamente para filtrar por negocio sin hacer JOIN.
    /// [Required] = obligatorio para el aislamiento multi-tenant.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Fecha específica a la que aplica esta excepción.
    /// Ejemplo: 2026-02-20 (ese viernes el empleado no trabaja).
    /// Solo la parte de fecha importa; la hora de este campo se ignora.
    /// El sistema busca excepciones comparando solo la fecha (sin hora) contra esta propiedad.
    /// </summary>
    public DateTime Fecha { get; set; }

    /// <summary>
    /// Indica si el empleado tiene día libre completo en esta fecha.
    /// true  = el empleado no trabaja ese día; no hay slots disponibles.
    ///         HoraInicio y HoraFin quedan nulos (no se usan).
    /// false = el empleado trabaja, pero con horario diferente al habitual.
    ///         Las horas de HoraInicio y HoraFin reemplazan al horario regular.
    /// </summary>
    public bool EsDiaLibre { get; set; }

    /// <summary>
    /// Hora de inicio del horario modificado para esta fecha.
    /// Solo aplica cuando EsDiaLibre = false.
    /// Formato: "HH:mm" (24 horas), por ejemplo: "11:00" o "14:00".
    /// Es null si EsDiaLibre = true.
    /// [MaxLength(5)] = exactamente 5 caracteres para el formato "HH:mm".
    /// </summary>
    [MaxLength(5)]
    public string HoraInicio { get; set; }

    /// <summary>
    /// Hora de fin del horario modificado para esta fecha.
    /// Solo aplica cuando EsDiaLibre = false.
    /// Formato: "HH:mm" (24 horas), por ejemplo: "20:00".
    /// Es null si EsDiaLibre = true.
    /// [MaxLength(5)] = exactamente 5 caracteres para el formato "HH:mm".
    /// </summary>
    [MaxLength(5)]
    public string HoraFin { get; set; }

    /// <summary>
    /// Texto explicativo de por qué existe esta excepción (opcional).
    /// Ejemplos: "Vacaciones", "Cita médica", "Festivo local", "Evento especial".
    /// Se muestra al dueño del negocio en la vista de horarios del empleado.
    /// [MaxLength(300)] = suficiente para una descripción breve.
    /// </summary>
    [MaxLength(300)]
    public string Motivo { get; set; }

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties de Entity Framework)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Empleado al que pertenece esta excepción de horario.
    /// Relación muchos-a-uno: un empleado puede tener muchas excepciones.
    /// EF Core carga este objeto solo cuando se hace .Include(e => e.Empleado).
    /// </summary>
    public Empleado Empleado { get; set; }
}
