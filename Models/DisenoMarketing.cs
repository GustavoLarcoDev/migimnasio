using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class DisenoMarketing
{
    [Key]
    public Guid DisenoId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    [StringLength(200)]
    public string Nombre { get; set; } = "Sin nombre";

    /// <summary>
    /// JSON serializado del canvas Fabric.js (objects, background, dimensions).
    /// </summary>
    public string CanvasJson { get; set; }

    /// <summary>
    /// Data URI (base64) del thumbnail para la galeria.
    /// </summary>
    public string ThumbnailDataUri { get; set; }

    public int Ancho { get; set; } = 1080;

    public int Alto { get; set; } = 1080;

    public bool IsActive { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    public DateTime FechaModificacion { get; set; } = TimeHelper.Now;

}
