using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Horario semanal de un empleado (1 fila por dia de semana).
/// DiaSemana: 0=Domingo, 1=Lunes, ..., 6=Sabado
/// </summary>
public class HorarioEmpleado
{
    [Key]
    public Guid HorarioId { get; set; }

    [Required]
    public Guid EmpleadoId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>Dia de la semana: 0=Domingo, 1=Lunes, ..., 6=Sabado</summary>
    [Range(0, 6)]
    public int DiaSemana { get; set; }

    /// <summary>Hora de inicio en formato "HH:mm" (ej: "09:00")</summary>
    [MaxLength(5)]
    public string HoraInicio { get; set; } = "09:00";

    /// <summary>Hora de fin en formato "HH:mm" (ej: "17:00")</summary>
    [MaxLength(5)]
    public string HoraFin { get; set; } = "17:00";

    /// <summary>True si el empleado trabaja este dia</summary>
    public bool Activo { get; set; } = true;

    // Relaciones
    public Empleado Empleado { get; set; }
}
