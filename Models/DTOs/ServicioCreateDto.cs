// ═══════════════════════════════════════════════════════════
// ServicioCreateDto.cs — DTO para crear/editar servicios del negocio
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class ServicioCreateDto
{
    public Guid ServicioId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nombre { get; set; }

    [MaxLength(1000)]
    public string Descripcion { get; set; }

    /// <summary>Items incluidos separados por "|"</summary>
    [MaxLength(2000)]
    public string ItemsIncluidos { get; set; }

    public int DuracionMinutos { get; set; } = 30;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Precio { get; set; }

    public bool EsCombo { get; set; }
}
