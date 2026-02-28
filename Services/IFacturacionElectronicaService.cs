using Gimnasio.Models;

namespace Gimnasio.Services;

/// <summary>
/// Request para emitir una factura electrónica al SRI.
/// </summary>
public class FacturaRequest
{
    public Guid NegocioId { get; set; }
    public Guid? ReciboId { get; set; }

    // Comprador
    public string CompradorIdentificacion { get; set; }
    public string CompradorTipoIdentificacion { get; set; }
    public string CompradorRazonSocial { get; set; }
    public string CompradorEmail { get; set; }
    public string CompradorDireccion { get; set; }

    // Forma de pago SRI
    public string FormaPagoSri { get; set; } = "01";

    // Items
    public List<FacturaItemRequest> Items { get; set; } = new();
}

/// <summary>
/// Item individual de una factura electrónica.
/// </summary>
public class FacturaItemRequest
{
    public string CodigoPrincipal { get; set; }
    public string Descripcion { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; } = 0;

    /// <summary>Porcentaje IVA: 0, 15 (Ecuador 2024+).</summary>
    public decimal PorcentajeIva { get; set; } = 15;
}

/// <summary>
/// Resultado de validación de configuración SRI de un negocio.
/// </summary>
public class ValidacionConfigResult
{
    public bool EsValida { get; set; }
    public List<string> Errores { get; set; } = new();
}

/// <summary>
/// Servicio de facturación electrónica SRI Ecuador.
/// Genera XML, firma XAdES-BES, envía SOAP al SRI, genera RIDE PDF.
/// </summary>
public interface IFacturacionElectronicaService
{
    /// <summary>Valida que el negocio tenga toda la configuración SRI necesaria.</summary>
    Task<ValidacionConfigResult> ValidarConfiguracionAsync(Guid negocioId);

    /// <summary>Guarda el certificado .p12 cifrado en la BD.</summary>
    Task<(bool success, string message, DateTime? vencimiento)> GuardarCertificadoAsync(
        Guid negocioId, byte[] p12Bytes, string password);

    /// <summary>Valida que el certificado .p12 actual sea válido y no esté vencido.</summary>
    Task<(bool valid, string message, DateTime? vencimiento)> ValidarCertificadoAsync(Guid negocioId);

    /// <summary>Emite una nueva factura electrónica ante el SRI.</summary>
    Task<(bool success, string message, FacturaElectronica factura)> EmitirFacturaAsync(FacturaRequest request);

    /// <summary>Obtiene el siguiente secuencial disponible para el negocio.</summary>
    Task<int> ObtenerSiguienteSecuencialAsync(Guid negocioId, string establecimiento, string puntoEmision);

    /// <summary>Lista facturas del negocio con filtros opcionales.</summary>
    Task<List<FacturaElectronica>> GetFacturasAsync(Guid negocioId, DateTime? desde = null,
        DateTime? hasta = null, string estado = null);

    /// <summary>Obtiene una factura específica.</summary>
    Task<FacturaElectronica> GetFacturaAsync(Guid negocioId, Guid facturaId);

    /// <summary>Obtiene el XML autorizado descomprimido de una factura.</summary>
    Task<byte[]> GetFacturaXmlAsync(Guid negocioId, Guid facturaId);

    /// <summary>Obtiene el RIDE PDF de una factura.</summary>
    Task<byte[]> GetFacturaRideAsync(Guid negocioId, Guid facturaId);

    /// <summary>Reenvía la factura al SRI (para facturas pendientes o rechazadas).</summary>
    Task<(bool success, string message)> ReenviarFacturaAsync(Guid negocioId, Guid facturaId);

    /// <summary>Envía el RIDE por email al comprador.</summary>
    Task<(bool success, string message)> EnviarFacturaPorEmailAsync(Guid negocioId, Guid facturaId);

    /// <summary>Marca una factura como anulada ante el SRI.</summary>
    Task<(bool success, string message)> AnularFacturaAsync(Guid negocioId, Guid facturaId);

    /// <summary>Reprocesa facturas pendientes (usado por background service).</summary>
    Task<int> ReprocesarPendientesAsync();

    /// <summary>Obtiene estadísticas de facturación del negocio.</summary>
    Task<object> GetEstadisticasAsync(Guid negocioId);
}
