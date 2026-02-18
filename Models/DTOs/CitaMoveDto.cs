// ═══════════════════════════════════════════════════════════
// CitaMoveDto.cs — DTO para mover citas a nueva fecha/hora
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class CitaMoveDto
{
    [Required]
    public Guid CitaId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    public DateTime NuevaFechaHoraInicio { get; set; }
}
