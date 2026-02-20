// ═══════════════════════════════════════════════════════════════════════════════
// CategoriaProducto.cs — Categorias para agrupar productos (modelo Tienda)
//
// Las categorias permiten organizar los productos en pestanas (tabs) dentro
// del catalogo publico y del POS. El orden es configurable via drag & drop.
// Cada categoria pertenece a un unico negocio (multi-tenant por NegocioId).
//
// Relaciones:
//   - Un negocio tiene muchas categorias (1:N con Gym)
//   - Una categoria tiene muchos productos (1:N con Producto)
//   - Si se elimina la categoria, los productos quedan con CategoriaProductoId = null
// ═══════════════════════════════════════════════════════════════════════════════
using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Categoria de productos para agrupacion visual en el catalogo y POS.
/// Ejemplo: "Bebidas", "Suplementos", "Accesorios".
/// </summary>
public class CategoriaProducto
{
    /// <summary>Identificador unico de la categoria.</summary>
    [Key]
    public Guid CategoriaId { get; set; }

    /// <summary>ID del negocio duenio de esta categoria (aislamiento multi-tenant).</summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>Nombre visible de la categoria (maximo 100 caracteres).</summary>
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; }

    /// <summary>
    /// Posicion en la que aparece la categoria como tab.
    /// Se actualiza via drag and drop desde el frontend (ReordenarCategorias).
    /// </summary>
    public int Orden { get; set; }

    /// <summary>Fecha de creacion de la categoria (UTC-5 Ecuador).</summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    // ── Navegacion EF Core ───────────────────────────────────────────────────
    public Gym Negocio { get; set; }
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
