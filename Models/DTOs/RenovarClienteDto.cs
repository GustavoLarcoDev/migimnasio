// ═══════════════════════════════════════════════════════════
// RenovarClienteDto.cs — DTO para renovar membresía de un cliente
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// Datos necesarios para renovar la membresía de un cliente
/// </summary>
public class RenovarClienteDto
{
    /// <summary>ID del cliente a renovar</summary>
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>Días adicionales de membresía</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int Dias { get; set; }

    /// <summary>Precio de la renovación</summary>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Precio { get; set; }
}
