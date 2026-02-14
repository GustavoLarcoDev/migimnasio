using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class MovimientoInventario
{
    [Key]
    public Guid MovimientoId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    public Guid ProductoId { get; set; }

    /// <summary>Nombre del producto al momento de la transaccion (denormalizado)</summary>
    [Required]
    [MaxLength(200)]
    public string NombreProducto { get; set; }

    /// <summary>Tipo: "venta", "devolucion", "restock", "ajuste"</summary>
    [Required]
    [MaxLength(50)]
    public string Tipo { get; set; }

    /// <summary>Unidades movidas (siempre positivo)</summary>
    public int Cantidad { get; set; }

    /// <summary>Precio unitario al momento de la transaccion</summary>
    public decimal PrecioUnitario { get; set; }

    /// <summary>Cantidad x PrecioUnitario</summary>
    public decimal Total { get; set; }

    /// <summary>Stock antes del movimiento</summary>
    public int StockAnterior { get; set; }

    /// <summary>Stock despues del movimiento</summary>
    public int StockNuevo { get; set; }

    /// <summary>Razon o nota opcional (obligatoria para ajustes y devoluciones)</summary>
    [MaxLength(500)]
    public string Nota { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;
}
