// ═══════════════════════════════════════════════════════════
// HorarioEmpleado.cs — Horario semanal recurrente de un empleado
//
// Define los días y horas en que un empleado trabaja regularmente.
// Se guarda una fila por cada día activo de la semana.
//
// Ejemplo de configuración para un empleado que trabaja Lun-Vie:
//   DiaSemana=1 (Lunes),    HoraInicio="09:00", HoraFin="18:00", Activo=true
//   DiaSemana=2 (Martes),   HoraInicio="09:00", HoraFin="18:00", Activo=true
//   ...
//   DiaSemana=6 (Sábado),   HoraInicio="09:00", HoraFin="14:00", Activo=false
//   DiaSemana=0 (Domingo),  Activo=false
//
// Para días con horario distinto al habitual (vacaciones, festivos),
// ver HorarioExcepcion.cs que sobrescribe el horario de una fecha específica.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Define el horario de trabajo semanal recurrente de un empleado.
/// Cada registro representa un día de la semana con su hora de entrada y salida.
///
/// El sistema de agenda usa estos horarios para calcular qué slots de tiempo
/// están disponibles para agendar nuevas citas con el empleado.
/// Una HorarioExcepcion puede sobrescribir este horario para una fecha específica.
/// </summary>
public class HorarioEmpleado
{
    /// <summary>
    /// Identificador único del horario (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla HorariosEmpleado.
    /// </summary>
    [Key]
    public Guid HorarioId { get; set; }

    /// <summary>
    /// ID del empleado al que pertenece este horario.
    /// [Required] = todo horario debe estar ligado a un empleado.
    /// Relación muchos-a-uno: un empleado puede tener varios horarios (uno por día).
    /// </summary>
    [Required]
    public Guid EmpleadoId { get; set; }

    /// <summary>
    /// ID del negocio al que pertenece el empleado.
    /// Se guarda directamente en el horario para filtrar por negocio sin hacer JOIN.
    /// [Required] = obligatorio para el aislamiento multi-tenant.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Día de la semana al que aplica este horario.
    /// Usa la convención estándar de .NET (DayOfWeek):
    ///   0 = Domingo
    ///   1 = Lunes
    ///   2 = Martes
    ///   3 = Miércoles
    ///   4 = Jueves
    ///   5 = Viernes
    ///   6 = Sábado
    /// [Range(0, 6)] = valida que el valor sea un día válido de la semana.
    /// </summary>
    [Range(0, 6)]
    public int DiaSemana { get; set; }

    /// <summary>
    /// Hora en que el empleado empieza su jornada este día.
    /// Formato: "HH:mm" (24 horas), por ejemplo: "09:00" o "14:30".
    /// [MaxLength(5)] = exactamente 5 caracteres para el formato "HH:mm".
    /// Valor por defecto: "09:00" (9 de la mañana).
    /// </summary>
    [MaxLength(5)]
    public string HoraInicio { get; set; } = "09:00";

    /// <summary>
    /// Hora en que el empleado termina su jornada este día.
    /// Formato: "HH:mm" (24 horas), por ejemplo: "17:00" o "21:00".
    /// [MaxLength(5)] = exactamente 5 caracteres para el formato "HH:mm".
    /// Valor por defecto: "17:00" (5 de la tarde).
    /// </summary>
    [MaxLength(5)]
    public string HoraFin { get; set; } = "17:00";

    /// <summary>
    /// Indica si el empleado trabaja en este día de la semana.
    /// true  = el empleado trabaja este día (entre HoraInicio y HoraFin).
    /// false = el empleado descansa este día (día libre regular).
    /// El sistema no ofrecerá slots de cita en días con Activo = false.
    /// Valor por defecto: true.
    /// </summary>
    public bool Activo { get; set; } = true;

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties de Entity Framework)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Empleado al que pertenece este horario.
    /// Relación muchos-a-uno: un empleado tiene varios HorarioEmpleado (uno por día).
    /// EF Core carga este objeto solo cuando se hace .Include(h => h.Empleado).
    /// </summary>
    public Empleado Empleado { get; set; }
}
