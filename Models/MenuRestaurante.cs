// ═══════════════════════════════════════════════════════════
// MenuRestaurante.cs — Modelo de menu guardado para restaurantes
//
// Almacena menus creados por el dueno (desayuno, almuerzo, cena, general).
// Guarda tanto el HTML renderizado (para preview/descarga/envio)
// como el JSON de items (para poder editar despues en el builder).
//
// Estilos disponibles: moderno, elegante, minimalista, vibrante, clasico
// (mismos 5 estilos del catalogo de tienda).
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class MenuRestaurante
{
    [Key]
    public Guid MenuId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre del menu. Ej: "Menu del dia", "Menu ejecutivo".
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Nombre { get; set; }

    /// <summary>
    /// Tipo: "desayuno", "almuerzo", "cena", "general".
    /// </summary>
    [Required]
    [StringLength(20)]
    public string TipoMenu { get; set; }

    /// <summary>
    /// Precio fijo del combo (solo para almuerzo/cena).
    /// Null para desayuno/general donde cada plato tiene su precio.
    /// </summary>
    public decimal? PrecioFijo { get; set; }

    /// <summary>
    /// HTML renderizado del menu para preview, descarga e impresion.
    /// </summary>
    public string ContenidoHtml { get; set; }

    /// <summary>
    /// JSON con items, secciones y orden para poder editar despues.
    /// Estructura: { secciones: [{ nombre, items: [{ productoId, nombre, precio }] }] }
    /// </summary>
    public string ItemsJson { get; set; }

    /// <summary>
    /// Estilo visual: "moderno", "elegante", "minimalista", "vibrante", "clasico".
    /// </summary>
    [StringLength(30)]
    public string Estilo { get; set; } = "moderno";

    public bool IsActive { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    public Gym Negocio { get; set; }
}
