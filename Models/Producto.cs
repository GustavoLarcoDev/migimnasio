using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Producto
{
    [Key]
    public Guid ProductoId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    [StringLength(200)]
    public string Nombre { get; set; }

    /// <summary>Precio de venta al publico por unidad</summary>
    [Required]
    public decimal PrecioVenta { get; set; }

    /// <summary>Costo de compra por unidad (lo que pago el dueno)</summary>
    [Required]
    public decimal CostoCompra { get; set; }

    /// <summary>Cantidad actual en inventario</summary>
    public int Stock { get; set; }

    /// <summary>Cantidad minima antes de generar alerta</summary>
    public int StockMinimo { get; set; } = 5;

    /// <summary>Soft delete: false = producto eliminado logicamente</summary>
    public bool IsActive { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime FechaDeActualizacion { get; set; } = DateTime.Now;
}
