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

    /// <summary>
    /// Tipo de orden: "local", "para_llevar", "delivery".
    /// Default "local" para retrocompatibilidad con Tienda.
    /// </summary>
    [StringLength(20)]
    public string TipoOrden { get; set; } = "local";

    /// <summary>
    /// Mesa asignada si TipoOrden = "local" (solo restaurantes).
    /// </summary>
    public Guid? MesaId { get; set; }

    /// <summary>
    /// Repartidor asignado si TipoOrden = "delivery" (reutiliza modelo Empleado).
    /// </summary>
    public Guid? EmpleadoId { get; set; }

    /// <summary>
    /// Direccion de entrega si TipoOrden = "delivery".
    /// </summary>
    [StringLength(500)]
    public string DireccionEntrega { get; set; }

    /// <summary>
    /// Metodo de pago utilizado (ej: "Efectivo", "Banco Pichincha", "Tarjeta Visa").
    /// Default "Efectivo" para retrocompatibilidad con ventas existentes.
    /// </summary>
    [StringLength(100)]
    public string MetodoPago { get; set; } = "Efectivo";

    /// <summary>
    /// Numero de confirmacion/referencia de la transaccion (transferencia, deposito, etc).
    /// Null para pagos en efectivo que no requieren confirmacion.
    /// </summary>
    [StringLength(100)]
    public string NumeroConfirmacion { get; set; }

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    public Gym Negocio { get; set; }
    public Recibo Recibo { get; set; }
    public Mesa Mesa { get; set; }
    public Empleado Repartidor { get; set; }
    public ICollection<DetalleOrdenVenta> Detalles { get; set; } = new List<DetalleOrdenVenta>();
}
