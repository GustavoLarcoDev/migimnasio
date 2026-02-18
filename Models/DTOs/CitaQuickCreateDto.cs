// ═══════════════════════════════════════════════════════════
// CitaQuickCreateDto.cs — DTO para crear citas rápidas con cliente nuevo o existente
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para crear cita + cliente en un solo paso.
/// Si ClienteId tiene valor, usa el cliente existente.
/// Si ClienteId es null, crea un cliente nuevo con Nombre/Apellido/Telefono.
/// </summary>
public class CitaQuickCreateDto
{
    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    public Guid EmpleadoId { get; set; }

    [Required]
    public Guid ServicioId { get; set; }

    [Required]
    public DateTime FechaHoraInicio { get; set; }

    /// <summary>Si tiene valor, usa cliente existente. Si es null, crea nuevo.</summary>
    public Guid? ClienteId { get; set; }

    /// <summary>Nombre del cliente nuevo (obligatorio si ClienteId es null)</summary>
    [MaxLength(100)]
    public string Nombre { get; set; }

    /// <summary>Apellido del cliente nuevo (obligatorio si ClienteId es null)</summary>
    [MaxLength(100)]
    public string Apellido { get; set; }

    /// <summary>Telefono del cliente nuevo</summary>
    [MaxLength(20)]
    public string Telefono { get; set; }

    /// <summary>Email del cliente nuevo</summary>
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; }
}
