using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Empleado/staff de un negocio artesanal (ej: Manicurista, Estilista)
/// </summary>
public class Empleado
{
    [Key]
    public Guid EmpleadoId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; }

    [Required]
    [MaxLength(100)]
    public string Apellido { get; set; }

    [MaxLength(20)]
    public string Telefono { get; set; }

    /// <summary>Especialidad del empleado (ej: "Manicurista", "Estilista")</summary>
    [MaxLength(100)]
    public string Especialidad { get; set; }

    /// <summary>Soft delete</summary>
    public bool IsActive { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    // Relaciones
    public Gym Negocio { get; set; }
    public ICollection<HorarioEmpleado> Horarios { get; set; } = new List<HorarioEmpleado>();
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
