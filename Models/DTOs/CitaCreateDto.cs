// ═══════════════════════════════════════════════════════════
// CitaCreateDto.cs — DTO para crear citas/reservaciones
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class CitaCreateDto
{
    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    public Guid ClienteId { get; set; }

    [Required]
    public Guid EmpleadoId { get; set; }

    [Required]
    public Guid ServicioId { get; set; }

    [Required]
    public DateTime FechaHoraInicio { get; set; }
}
