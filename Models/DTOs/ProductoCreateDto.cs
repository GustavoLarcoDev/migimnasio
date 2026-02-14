using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class ProductoCreateDto
{
    public Guid ProductoId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    [StringLength(200)]
    public string Nombre { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal PrecioVenta { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal CostoCompra { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [Range(0, int.MaxValue)]
    public int StockMinimo { get; set; } = 5;
}
