// ═══════════════════════════════════════════════════════════
// ComisionDelivery.cs — Modelo de comisión por pedido de delivery
//
// Cada vez que un pedido se marca como "entregado", se genera
// automáticamente una comisión de $0.20 para el motorizado
// y/o el restaurante (dependiendo de su plan).
//
// Planes:
//   "comision" → paga $0.20 por pedido completado
//   "mensual"  → paga $10/mes fijo, sin comisión por pedido
//
// Las comisiones se acumulan hasta que el motorizado/restaurante
// las liquida con un pago (PagoDelivery).
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa una comisión generada por un pedido de delivery completado.
/// Se cobra $0.20 por cada entrega exitosa al motorizado y/o restaurante
/// que esté en plan "comision".
/// </summary>
public class ComisionDelivery
{
    /// <summary>
    /// Identificador único de la comisión (GUID generado automáticamente).
    /// </summary>
    [Key]
    public Guid ComisionDeliveryId { get; set; }

    /// <summary>
    /// Tipo de pagador de esta comisión: "motorizado" o "restaurante".
    /// Un pedido puede generar dos comisiones: una para el motorizado
    /// y otra para el restaurante, si ambos están en plan "comision".
    /// </summary>
    [Required]
    [StringLength(20)]
    public string TipoPagador { get; set; }

    /// <summary>
    /// ID del motorizado al que se le cobra esta comisión (null si es restaurante).
    /// </summary>
    public Guid? MotorizadoId { get; set; }

    /// <summary>
    /// ID del negocio/restaurante al que se le cobra esta comisión (null si es motorizado).
    /// </summary>
    public Guid? NegocioId { get; set; }

    /// <summary>
    /// ID del pedido que generó esta comisión.
    /// </summary>
    public Guid PedidoId { get; set; }

    /// <summary>
    /// Monto de la comisión. Default $0.20 por pedido.
    /// </summary>
    public decimal Monto { get; set; } = 0.20m;

    /// <summary>
    /// Indica si la comisión ya fue pagada/liquidada.
    /// Se marca como true cuando el admin confirma un PagoDelivery.
    /// </summary>
    public bool Pagada { get; set; } = false;

    /// <summary>
    /// Fecha en que se generó la comisión (al entregar el pedido).
    /// </summary>
    public DateTime Fecha { get; set; } = TimeHelper.Now;

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Motorizado al que corresponde la comisión.
    /// </summary>
    public Motorizado? Motorizado { get; set; }

    /// <summary>
    /// Negocio/restaurante al que corresponde la comisión.
    /// </summary>
    public Gym? Negocio { get; set; }

    /// <summary>
    /// Pedido que generó esta comisión.
    /// </summary>
    public Pedido? Pedido { get; set; }
}
