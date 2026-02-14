// ═══════════════════════════════════════════════════════════
// NegocioEditDto.cs — DTO para editar un negocio existente
// Si Password viene vacío, no se cambia la contraseña
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// Datos para editar un negocio. La contraseña es opcional (si vacía, no se modifica).
/// </summary>
public class NegocioEditDto
{
    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    public string NegocioNombre { get; set; }

    [Required]
    public string DuenoNegocio { get; set; }

    [Required]
    [Phone]
    public string Telefono { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>Si se envía, se re-hashea con BCrypt. Si vacío, se mantiene la actual.</summary>
    public string Password { get; set; }

    public bool IsActive { get; set; }
    public bool EsPrueba { get; set; }
}
