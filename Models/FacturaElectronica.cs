using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gimnasio.Models;

/// <summary>
/// Factura electrónica emitida a través del SRI Ecuador.
/// Cada factura puede estar vinculada opcionalmente a un Recibo interno.
/// </summary>
public class FacturaElectronica
{
    [Key]
    public Guid FacturaId { get; set; }

    /// <summary>FK al negocio emisor (multi-tenant).</summary>
    public Guid NegocioId { get; set; }

    /// <summary>FK opcional al recibo interno que originó esta factura.</summary>
    public Guid? ReciboId { get; set; }

    // ═══════════════════════════════════════════════════════════
    // NUMERACION SRI
    // ═══════════════════════════════════════════════════════════

    /// <summary>Código de establecimiento (3 dígitos, ej: "001").</summary>
    [MaxLength(3)]
    public string Establecimiento { get; set; }

    /// <summary>Punto de emisión (3 dígitos, ej: "001").</summary>
    [MaxLength(3)]
    public string PuntoEmision { get; set; }

    /// <summary>Secuencial numérico auto-incrementado por negocio.</summary>
    public int Secuencial { get; set; }

    /// <summary>Número completo formateado: "001-001-000000001".</summary>
    [MaxLength(21)]
    public string NumeroCompleto { get; set; }

    /// <summary>Clave de acceso de 49 dígitos generada según algoritmo SRI.</summary>
    [MaxLength(49)]
    public string ClaveAcceso { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DATOS DEL COMPRADOR
    // ═══════════════════════════════════════════════════════════

    /// <summary>Número de identificación del comprador (RUC, cédula, pasaporte).</summary>
    [MaxLength(20)]
    public string CompradorIdentificacion { get; set; }

    /// <summary>
    /// Tipo de identificación SRI:
    /// "04"=RUC, "05"=Cédula, "06"=Pasaporte, "07"=Consumidor Final.
    /// </summary>
    [MaxLength(2)]
    public string CompradorTipoIdentificacion { get; set; }

    /// <summary>Razón social o nombre completo del comprador.</summary>
    [MaxLength(300)]
    public string CompradorRazonSocial { get; set; }

    /// <summary>Email del comprador para envío de RIDE.</summary>
    [MaxLength(200)]
    public string CompradorEmail { get; set; }

    /// <summary>Dirección del comprador (opcional).</summary>
    [MaxLength(500)]
    public string CompradorDireccion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // MONTOS
    // ═══════════════════════════════════════════════════════════

    public decimal TotalSinImpuestos { get; set; }
    public decimal TotalDescuento { get; set; }

    /// <summary>Monto de IVA 15% calculado.</summary>
    public decimal MontoIva { get; set; }

    /// <summary>Importe total de la factura (subtotal + IVA).</summary>
    public decimal ImporteTotal { get; set; }

    /// <summary>
    /// Código de forma de pago SRI:
    /// "01"=SIN UTILIZACION, "15"=Compensación deudas, "16"=Tarjeta débito,
    /// "17"=Dinero electrónico, "18"=Tarjeta prepago, "19"=Tarjeta crédito,
    /// "20"=Otros con utilización del SF, "21"=Endoso de títulos.
    /// </summary>
    [MaxLength(2)]
    public string FormaPagoSri { get; set; }

    // ═══════════════════════════════════════════════════════════
    // SRI - ESTADO Y AUTORIZACION
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// XML firmado y autorizado, almacenado comprimido con GZip.
    /// </summary>
    public byte[] XmlAutorizadoComprimido { get; set; }

    /// <summary>
    /// Estado actual de la factura ante el SRI:
    /// "Pendiente", "Recibida", "Autorizada", "NoAutorizada", "Anulada".
    /// </summary>
    [MaxLength(20)]
    public string EstadoSri { get; set; } = "Pendiente";

    /// <summary>Número de autorización devuelto por el SRI.</summary>
    [MaxLength(49)]
    public string NumeroAutorizacion { get; set; }

    /// <summary>Fecha y hora de autorización del SRI.</summary>
    public DateTime? FechaAutorizacion { get; set; }

    /// <summary>Mensajes de error o información devueltos por el SRI (JSON).</summary>
    public string MensajesSri { get; set; }

    // ═══════════════════════════════════════════════════════════
    // RIDE (Representación Impresa del Documento Electrónico)
    // ═══════════════════════════════════════════════════════════

    /// <summary>PDF del RIDE almacenado en bytes.</summary>
    public byte[] RidePdf { get; set; }

    /// <summary>JSON con el detalle de items de la factura para regenerar RIDE.</summary>
    public string DetalleItemsJson { get; set; }

    // ═══════════════════════════════════════════════════════════
    // CONTROL
    // ═══════════════════════════════════════════════════════════

    /// <summary>Cantidad de intentos de envío al SRI.</summary>
    public int IntentosEnvio { get; set; } = 0;

    /// <summary>Fecha de emisión de la factura.</summary>
    public DateTime FechaEmision { get; set; }

    /// <summary>Fecha de creación del registro en la BD.</summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    /// <summary>Fecha del último intento de envío al SRI.</summary>
    public DateTime? FechaUltimoIntento { get; set; }

    // ═══════════════════════════════════════════════════════════
    // NAVIGATION PROPERTIES
    // ═══════════════════════════════════════════════════════════

    [ForeignKey("NegocioId")]
    public Gym Negocio { get; set; }

    [ForeignKey("ReciboId")]
    public Recibo Recibo { get; set; }
}
