// ═══════════════════════════════════════════════════════════
// Mesa.cs — Modelo de mesa de restaurante
//
// Representa una mesa fisica del restaurante. Cada mesa tiene
// un numero, nombre descriptivo, capacidad y estado visual
// (libre=verde, ocupada=rojo, reservada=amarillo).
//
// El soft delete (IsActive = false) permite dar de baja mesas
// temporalmente sin perder el historial de ordenes asociadas.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Mesa
{
    [Key]
    public Guid MesaId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Numero secuencial de la mesa (1, 2, 3...).
    /// </summary>
    public int Numero { get; set; }

    /// <summary>
    /// Nombre descriptivo. Ej: "Mesa 1", "Terraza 2", "VIP".
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Nombre { get; set; }

    /// <summary>
    /// Cantidad de personas que caben en la mesa.
    /// </summary>
    public int Capacidad { get; set; } = 4;

    /// <summary>
    /// Estado visual: "libre", "ocupada", "reservada".
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Estado { get; set; } = "libre";

    public bool IsActive { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    public Gym Negocio { get; set; }
}
