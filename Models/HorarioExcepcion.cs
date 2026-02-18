using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Excepcion de horario para una fecha especifica (dia libre, extension de horario)
/// </summary>
public class HorarioExcepcion
{
    [Key]
    public Guid ExcepcionId { get; set; }

    [Required]
    public Guid EmpleadoId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>Fecha especifica de la excepcion</summary>
    public DateTime Fecha { get; set; }

    /// <summary>True si es dia libre (no trabaja)</summary>
    public bool EsDiaLibre { get; set; }

    /// <summary>Hora inicio override (null si EsDiaLibre)</summary>
    [MaxLength(5)]
    public string HoraInicio { get; set; }

    /// <summary>Hora fin override (null si EsDiaLibre)</summary>
    [MaxLength(5)]
    public string HoraFin { get; set; }

    /// <summary>Motivo de la excepcion</summary>
    [MaxLength(300)]
    public string Motivo { get; set; }

    // Relaciones
    public Empleado Empleado { get; set; }
}
