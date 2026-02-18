// ═══════════════════════════════════════════════════════════
// ClienteArtesanalCreateDto.cs — DTO para crear/editar clientes del modelo artesanal
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para crear clientes en negocios artesanales (sin campos de membresia)
/// </summary>
public class ClienteArtesanalCreateDto
{
    public Guid ClienteId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; }

    [Required]
    [MaxLength(100)]
    public string Apellido { get; set; }

    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; }

    [Phone]
    [MaxLength(20)]
    public string Telefono { get; set; }

    [MaxLength(500)]
    public string Direccion { get; set; }
}
