using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Servicio ofrecido por un negocio artesanal (ej: Manicure, Pedicure, Corte de pelo)
/// </summary>
public class ServicioNegocio
{
    [Key]
    public Guid ServicioId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nombre { get; set; }

    [MaxLength(1000)]
    public string Descripcion { get; set; }

    /// <summary>Items incluidos separados por "|" (ej: "Limado|Cuticula|Esmaltado")</summary>
    [MaxLength(2000)]
    public string ItemsIncluidos { get; set; }

    /// <summary>Duracion del servicio en minutos</summary>
    public int DuracionMinutos { get; set; } = 30;

    [Required]
    public decimal Precio { get; set; }

    /// <summary>True si es un combo de varios servicios</summary>
    public bool EsCombo { get; set; }

    /// <summary>Soft delete</summary>
    public bool IsActive { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;
    public DateTime FechaDeActualizacion { get; set; }

    // Relaciones
    public Gym Negocio { get; set; }
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
