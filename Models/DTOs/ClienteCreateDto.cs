// ═══════════════════════════════════════════════════════════
// ClienteCreateDto.cs — DTO para crear y editar clientes
// Soporta dos modos de cálculo de membresía:
// 1. FechaFin explícita (prioridad)
// 2. FechaInicio + Días
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class ClienteCreateDto
{
    /// <summary>ID del cliente (solo para edición, vacío para creación)</summary>
    public Guid ClienteId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DATOS PERSONALES
    // ═══════════════════════════════════════════════════════════

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; }

    [Required]
    [StringLength(100)]
    public string Apellido { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [Phone]
    public string Telefono { get; set; }

    public string Direccion { get; set; }

    /// <summary>True si el cliente paga por día</summary>
    public bool EsDiario { get; set; }

    // ═══════════════════════════════════════════════════════════
    // MEMBRESÍA
    // ═══════════════════════════════════════════════════════════

    /// <summary>Fecha de inicio de la membresía (default: ahora)</summary>
    public DateTime? FechaInicio { get; set; }

    /// <summary>Fecha de fin explícita (tiene prioridad sobre Dias)</summary>
    public DateTime? FechaFin { get; set; }

    /// <summary>Duración en días (se usa si FechaFin es null)</summary>
    public int Dias { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal Precio { get; set; }
}
