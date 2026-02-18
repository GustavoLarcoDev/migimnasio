// ═══════════════════════════════════════════════════════════
// EmpleadoCreateDto.cs — DTO para crear/editar empleados
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class EmpleadoCreateDto
{
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

    [MaxLength(100)]
    public string Especialidad { get; set; }
}
