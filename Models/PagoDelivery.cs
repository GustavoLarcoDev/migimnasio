// ═══════════════════════════════════════════════════════════
// PagoDelivery.cs — Modelo de pago del sistema de delivery
//
// Registra los pagos realizados por motorizados y restaurantes
// para mantener su cuenta activa en el sistema de delivery.
//
// Tipos de pago:
//   "mensual"  → pago de suscripción mensual ($10/mes)
//   "comision" → pago de comisiones acumuladas ($0.20/pedido)
//
// Flujo:
//   Motorizado/Restaurante envía comprobante de pago con número
//   de confirmación → se crea PagoDelivery con Estado="pendiente"
//   → admin revisa → confirma (desbloquea) o rechaza (con notas).
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa un pago realizado por un motorizado o restaurante
/// en el sistema de delivery de My-Negocio.
/// </summary>
public class PagoDelivery
{
    /// <summary>
    /// Identificador único del pago (GUID generado automáticamente).
    /// </summary>
    [Key]
    public Guid PagoDeliveryId { get; set; }

    /// <summary>
    /// Tipo de pagador: "motorizado" o "restaurante".
    /// Determina si MotorizadoId o NegocioId tiene valor.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string TipoPagador { get; set; }

    /// <summary>
    /// ID del motorizado que realizó el pago (null si es restaurante).
    /// </summary>
    public Guid? MotorizadoId { get; set; }

    /// <summary>
    /// ID del negocio/restaurante que realizó el pago (null si es motorizado).
    /// </summary>
    public Guid? NegocioId { get; set; }

    /// <summary>
    /// Tipo de pago: "mensual" (suscripción) o "comision" (liquidación de comisiones).
    /// </summary>
    [Required]
    [StringLength(20)]
    public string TipoPago { get; set; }

    /// <summary>
    /// Monto del pago en dólares.
    /// </summary>
    public decimal Monto { get; set; }

    /// <summary>
    /// Número de confirmación o referencia del comprobante de pago.
    /// Lo ingresa el motorizado/restaurante al registrar el pago.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string NumeroConfirmacion { get; set; }

    /// <summary>
    /// Estado del pago: "pendiente", "confirmado", "rechazado".
    /// Default "pendiente" — el admin debe verificar el comprobante.
    /// </summary>
    [StringLength(20)]
    public string Estado { get; set; } = "pendiente";

    /// <summary>
    /// Notas del admin al revisar el pago (motivo de rechazo, observaciones).
    /// </summary>
    [StringLength(500)]
    public string? Notas { get; set; }

    /// <summary>
    /// Fecha de creación del registro de pago.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    /// <summary>
    /// Fecha en que el admin revisó el pago (confirmó o rechazó).
    /// </summary>
    public DateTime? FechaRevision { get; set; }

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Motorizado que realizó el pago (null si TipoPagador="restaurante").
    /// </summary>
    public Motorizado? Motorizado { get; set; }

    /// <summary>
    /// Negocio/restaurante que realizó el pago (null si TipoPagador="motorizado").
    /// </summary>
    public Gym? Negocio { get; set; }
}
