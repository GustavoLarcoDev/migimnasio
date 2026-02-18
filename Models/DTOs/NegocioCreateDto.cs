// ═══════════════════════════════════════════════════════════
// NegocioCreateDto.cs — DTO para crear un nuevo negocio
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// Datos necesarios para registrar un nuevo negocio en la plataforma
/// </summary>
public class NegocioCreateDto
{
    [Required]
    [MaxLength(200)]
    public string NombreNegocio { get; set; }

    [Required]
    [MaxLength(200)]
    public string DuenoNegocio { get; set; }

    [Required]
    [Phone]
    [MaxLength(20)]
    public string Telefono { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string EmailNegocio { get; set; }

    [Required]
    [MaxLength(200)]
    public string PasswordNegocio { get; set; }

    /// <summary>True = negocio de pago activo</summary>
    public bool IsActive { get; set; }

    /// <summary>True = negocio en periodo de prueba</summary>
    public bool EsPrueba { get; set; }
}
