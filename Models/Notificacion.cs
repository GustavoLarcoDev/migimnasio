// ═══════════════════════════════════════════════════════════
// Notificacion.cs — Modelo de notificación automática
// Se genera cuando la membresía de un cliente está próxima
// a vencer (dentro de 3 días). Evita duplicados por día.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Notificacion
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>ID del negocio dueño de esta notificación</summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>Texto de la notificación (ej: "La membresía de Juan vence en 2 día(s)")</summary>
    [Required]
    [MaxLength(500)]
    public string Mensaje { get; set; }

    /// <summary>Tipo de notificación. Actualmente solo "vencimiento".</summary>
    [MaxLength(50)]
    public string Tipo { get; set; } = "vencimiento";

    /// <summary>ID del cliente relacionado</summary>
    public Guid? ClienteId { get; set; }

    /// <summary>Nombre del cliente para referencia rápida</summary>
    [MaxLength(200)]
    public string NombreCliente { get; set; }

    /// <summary>Si el usuario ya vio esta notificación</summary>
    public bool Leida { get; set; } = false;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}
