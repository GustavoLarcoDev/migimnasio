// ═══════════════════════════════════════════════════════════
// LogCreateDto.cs — DTO para crear un log manual de ingreso/gasto
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// Datos para crear un registro manual desde el dashboard.
/// El tipo (ingreso/gasto) se determina por el signo del monto.
/// </summary>
public class LogCreateDto
{
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>Descripción del ingreso o gasto</summary>
    [Required]
    [MaxLength(300)]
    public string Message { get; set; }

    /// <summary>Monto: positivo = ingreso, negativo = gasto</summary>
    [Required]
    public decimal Monto { get; set; }
}
