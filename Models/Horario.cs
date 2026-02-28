// =====================================================================
// Horario.cs — Modelo unificado de horarios (regular + excepcion)
//
// UNIFICA las antiguas tablas HorariosEmpleado y HorariosExcepcion
// en una sola tabla "Horarios" usando un discriminador TipoHorario.
//
// TipoHorario = "regular":
//   Equivale al antiguo HorarioEmpleado. Define el horario semanal
//   recurrente de un empleado (un registro por dia de la semana).
//   DiaSemana tiene valor (0-6), Fecha es null.
//
// TipoHorario = "excepcion":
//   Equivale al antiguo HorarioExcepcion. Define una excepcion
//   puntual para una fecha especifica (vacaciones, horario especial).
//   Fecha tiene valor, DiaSemana es null.
//
// PRIORIDAD: excepcion > regular > sin horario (no trabaja).
// =====================================================================

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Modelo unificado que combina horarios semanales regulares y excepciones
/// de horario en una sola tabla. El campo TipoHorario ("regular" o "excepcion")
/// determina cual de los dos comportamientos aplica.
/// </summary>
public class Horario
{
    /// <summary>
    /// Identificador unico del horario (GUID generado automaticamente).
    /// [Key] = clave primaria de la tabla Horarios.
    /// </summary>
    [Key]
    public Guid HorarioId { get; set; }

    /// <summary>
    /// ID del empleado al que pertenece este horario.
    /// [Required] = todo horario debe estar ligado a un empleado.
    /// </summary>
    [Required]
    public Guid EmpleadoId { get; set; }

    /// <summary>
    /// ID del negocio al que pertenece el empleado.
    /// [Required] = obligatorio para el aislamiento multi-tenant.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Tipo de horario: "regular" para horario semanal recurrente,
    /// "excepcion" para override puntual de una fecha especifica.
    /// [Required] = obligatorio para saber como interpretar el registro.
    /// [MaxLength(10)] = suficiente para "regular" (7) o "excepcion" (9).
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string TipoHorario { get; set; }

    /// <summary>
    /// Dia de la semana (solo para TipoHorario="regular").
    /// Usa la convencion de .NET DayOfWeek: 0=Domingo, 1=Lunes, ..., 6=Sabado.
    /// Es null cuando TipoHorario="excepcion".
    /// [Range(0, 6)] = valida que sea un dia valido.
    /// </summary>
    [Range(0, 6)]
    public int? DiaSemana { get; set; }

    /// <summary>
    /// Fecha especifica de la excepcion (solo para TipoHorario="excepcion").
    /// Es null cuando TipoHorario="regular".
    /// Solo la parte de fecha importa; la hora se ignora.
    /// </summary>
    public DateTime? Fecha { get; set; }

    /// <summary>
    /// Hora de inicio de la jornada laboral. Formato "HH:mm".
    /// Para "regular": hora de entrada del dia de semana.
    /// Para "excepcion": hora de entrada del horario especial (null si EsDiaLibre=true).
    /// </summary>
    [MaxLength(5)]
    public string HoraInicio { get; set; } = "09:00";

    /// <summary>
    /// Hora de fin de la jornada laboral. Formato "HH:mm".
    /// Para "regular": hora de salida del dia de semana.
    /// Para "excepcion": hora de salida del horario especial (null si EsDiaLibre=true).
    /// </summary>
    [MaxLength(5)]
    public string HoraFin { get; set; } = "17:00";

    /// <summary>
    /// Indica si el empleado trabaja este dia/fecha.
    /// Para "regular": true=trabaja, false=dia libre semanal (ej: domingo).
    /// Para "excepcion": se usa en conjunto con EsDiaLibre.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Solo para TipoHorario="excepcion". Indica si el empleado tiene dia libre completo.
    /// true = no trabaja ese dia (HoraInicio/HoraFin se ignoran).
    /// false = trabaja con horario diferente al habitual.
    /// Para TipoHorario="regular", siempre es false.
    /// </summary>
    public bool EsDiaLibre { get; set; }

    /// <summary>
    /// Motivo de la excepcion (solo para TipoHorario="excepcion").
    /// Ejemplos: "Vacaciones", "Cita medica", "Festivo local".
    /// Es null para TipoHorario="regular".
    /// [MaxLength(300)] = suficiente para una descripcion breve.
    /// </summary>
    [MaxLength(300)]
    public string Motivo { get; set; }

    // =====================================================================
    // RELACIONES (navigation properties de Entity Framework)
    // =====================================================================

    /// <summary>
    /// Empleado al que pertenece este horario.
    /// Relacion muchos-a-uno: un empleado tiene varios Horario.
    /// EF Core carga este objeto solo cuando se hace .Include(h => h.Empleado).
    /// </summary>
    public Empleado Empleado { get; set; }
}
