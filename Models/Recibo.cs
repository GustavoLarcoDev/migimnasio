// ===================================================================
// Recibo.cs -- Modelo de recibo generado para transacciones del negocio
//
// Un recibo se genera cuando un negocio realiza una venta (POS de tienda,
// pago de cita artesanal, etc.) y el cliente solicita comprobante.
// El HTML renderizado se almacena para envio por email o descarga PDF.
// ===================================================================

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa un recibo generado para una transaccion del negocio.
/// Almacena el HTML renderizado del comprobante para envio por email o descarga.
/// </summary>
public class Recibo
{
    /// <summary>
    /// Identificador unico del recibo (GUID generado automaticamente).
    /// </summary>
    [Key]
    public Guid ReciboId { get; set; }

    /// <summary>
    /// Numero secuencial del recibo dentro del negocio.
    /// Se incrementa automaticamente al generar cada recibo.
    /// </summary>
    public int NumeroRecibo { get; set; }

    /// <summary>
    /// ID del negocio que genero este recibo.
    /// Nullable para soportar recibos de plataforma (no asociados a un negocio).
    /// </summary>
    public Guid? NegocioId { get; set; }

    /// <summary>
    /// Tipo de recibo: "venta", "membresia", "cita", etc.
    /// </summary>
    [StringLength(50)]
    public string TipoRecibo { get; set; } = "";

    /// <summary>
    /// Correo electronico del destinatario del recibo.
    /// </summary>
    [StringLength(200)]
    public string DestinatarioEmail { get; set; } = "";

    /// <summary>
    /// Nombre del destinatario del recibo.
    /// </summary>
    [StringLength(200)]
    public string DestinatarioNombre { get; set; } = "";

    /// <summary>
    /// Nombre del negocio que emitio el recibo (denormalizado).
    /// </summary>
    [StringLength(200)]
    public string NegocioNombre { get; set; } = "";

    /// <summary>
    /// Descripcion o concepto del recibo (detalle de la transaccion).
    /// </summary>
    [StringLength(500)]
    public string Concepto { get; set; } = "";

    /// <summary>
    /// Monto total del recibo (en moneda local).
    /// </summary>
    public decimal Monto { get; set; }

    /// <summary>
    /// HTML renderizado del recibo para preview, envio por email o descarga.
    /// </summary>
    public string ContenidoHtml { get; set; } = "";

    /// <summary>
    /// Fecha y hora en que se genero el recibo (UTC-5 Ecuador).
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;
}
