// ═══════════════════════════════════════════════════════════
// DetalleOrdenVenta.cs — Items individuales vendidos en una Orden
// ═══════════════════════════════════════════════════════════
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gimnasio.Models;

public class DetalleOrdenVenta
{
    [Key]
    public Guid DetalleId { get; set; }

    [Required]
    public Guid OrdenVentaId { get; set; }

    [Required]
    public Guid ProductoId { get; set; }

    public int Cantidad { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecioUnitario { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    public OrdenVenta OrdenVenta { get; set; }
    public Producto Producto { get; set; }
}
