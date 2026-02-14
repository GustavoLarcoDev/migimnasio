// ═══════════════════════════════════════════════════════════
// Sugerencia.cs — Modelo de sugerencia/feedback
// Los negocios pueden enviar sugerencias al administrador.
// Máximo 1000 caracteres por mensaje.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Sugerencia
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>ID del negocio que envió la sugerencia</summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>Nombre del negocio (para referencia sin hacer join)</summary>
    [Required]
    [MaxLength(200)]
    public string NegocioNombre { get; set; }

    /// <summary>Contenido de la sugerencia (máx. 1000 caracteres)</summary>
    [Required]
    [MaxLength(1000)]
    public string Mensaje { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    /// <summary>Si el admin ya leyó esta sugerencia</summary>
    public bool Leida { get; set; } = false;
}
