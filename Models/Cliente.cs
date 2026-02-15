// ═══════════════════════════════════════════════════════════
// Cliente.cs — Modelo de cliente y modelo de creación
// Representa un cliente registrado en un negocio con su
// membresía (fechas, días, precio) y datos de contacto
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Entidad principal de cliente en la base de datos
/// </summary>
public class Cliente
{
    [Key]
    public Guid ClienteId { get; set; }

    /// <summary>ID del negocio al que pertenece este cliente</summary>
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

    [Phone]
    public string Telefono { get; set; }

    public string Direccion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // MEMBRESÍA
    // ═══════════════════════════════════════════════════════════

    /// <summary>True si el cliente paga por día (no tiene membresía mensual)</summary>
    public bool EsDiario { get; set; }

    /// <summary>Fecha en que se registró o inició la membresía</summary>
    public DateTime FechaDeCreacion { get; set; } = TimeHelper.Now;

    public DateTime FechaDeActualizacion { get; set; }

    /// <summary>Fecha en que vence la membresía</summary>
    public DateTime FechaQueTermina { get; set; }

    /// <summary>Duración de la membresía en días</summary>
    public int Dias { get; set; }

    /// <summary>Precio pagado por la membresía actual</summary>
    public decimal Precio { get; set; }
}

/// <summary>
/// Modelo alternativo de creación de cliente (usado en formularios legacy).
/// Para nuevos formularios usar ClienteCreateDto.
/// </summary>
public class ClienteCreateModel
{
    public Guid ClienteId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

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

    public bool EsDiario { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Los días deben ser mayor a 0")]
    public int Dias { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal Precio { get; set; }
}
