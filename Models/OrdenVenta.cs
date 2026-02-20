// ═══════════════════════════════════════════════════════════
// OrdenVenta.cs — Encabezado de una venta realizada en el POS
// ═══════════════════════════════════════════════════════════
using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class OrdenVenta
{
    [Key]
    public Guid OrdenVentaId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre opcional del cliente en mostrador.
    /// </summary>
    [StringLength(200)]
    public string NombreCliente { get; set; }

    /// <summary>
    /// Número secuencial de orden
    /// </summary>
    public int NumeroOrden { get; set; }

    public decimal Subtotal { get; set; }
    public decimal PorcentajeIva { get; set; } = 0;
    public decimal MontoIva { get; set; }
    public decimal GastosAdicionales { get; set; }
    public decimal Total { get; set; }

    /// <summary>
    /// ID del Recibo generado si el cliente solicita uno.
    /// </summary>
    public Guid? ReciboId { get; set; }

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    public Gym Negocio { get; set; }
    public Recibo Recibo { get; set; }
    public ICollection<DetalleOrdenVenta> Detalles { get; set; } = new List<DetalleOrdenVenta>();
}
