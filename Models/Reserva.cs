// ═══════════════════════════════════════════════════════════
// Reserva.cs — Modelo de reserva de mesa de restaurante
//
// Almacena la información de una reserva: quién la hizo,
// para cuándo, cuántas personas, qué mesa, y su estado.
// Se integra con el sistema de mesas existente para cambiar
// automáticamente el estado de la mesa a "reservada".
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Reserva
{
    [Key]
    public Guid ReservaId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Mesa asignada para la reserva. Nullable si aún no se asigna mesa.
    /// </summary>
    public Guid? MesaId { get; set; }

    [Required]
    [StringLength(100)]
    public string NombreCliente { get; set; }

    [StringLength(20)]
    public string Telefono { get; set; }

    /// <summary>
    /// Fecha y hora de la reserva (cuándo llega el cliente).
    /// </summary>
    [Required]
    public DateTime FechaHoraReserva { get; set; }

    /// <summary>
    /// Cantidad de personas.
    /// </summary>
    [Range(1, 100)]
    public int CantidadPersonas { get; set; } = 2;

    /// <summary>
    /// Notas adicionales (alergias, celebración, etc.).
    /// </summary>
    [StringLength(500)]
    public string Notas { get; set; }

    /// <summary>
    /// Estado: "confirmada", "completada", "cancelada", "no_presentado".
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Estado { get; set; } = "confirmada";

    public bool IsActive { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    // Navigation
    public Gym Negocio { get; set; }
    public Mesa Mesa { get; set; }
}
