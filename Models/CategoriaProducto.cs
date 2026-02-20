// ═══════════════════════════════════════════════════════════
// CategoriaProducto.cs — Categorías para agrupar productos (Tabs/Clasificación)
// ═══════════════════════════════════════════════════════════
using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class CategoriaProducto
{
    [Key]
    public Guid CategoriaId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; }

    /// <summary>
    /// Orden en el que aparece la categoría como tab (para drag & drop).
    /// </summary>
    public int Orden { get; set; }

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    public Gym Negocio { get; set; }
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
