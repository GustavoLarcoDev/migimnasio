// ═══════════════════════════════════════════════════════════
// PagoCitaDto.cs — DTO para registrar pagos de citas
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class PagoCitaDto
{
    [Required]
    public Guid CitaId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    [MaxLength(20)]
    public string MetodoPago { get; set; } = "efectivo";

    public decimal MontoExtra { get; set; }

    [MaxLength(500)]
    public string DetalleExtra { get; set; }

    public decimal Propina { get; set; }

    public bool EsRegalo { get; set; }

    [MaxLength(500)]
    public string MotivoRegalo { get; set; }
}
