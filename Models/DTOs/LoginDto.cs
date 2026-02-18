// ═══════════════════════════════════════════════════════════
// LoginDto.cs — DTO para el formulario de login
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// Datos transferidos desde el formulario de login
/// </summary>
public class LoginDto
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; }

    [Required]
    [MaxLength(200)]
    public string Password { get; set; }
}
