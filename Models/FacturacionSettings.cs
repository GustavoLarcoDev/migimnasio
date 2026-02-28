namespace Gimnasio.Models;

/// <summary>
/// Configuración de facturación electrónica SRI Ecuador.
/// Se carga desde appsettings.json sección "FacturacionSettings".
/// </summary>
public class FacturacionSettings
{
    /// <summary>
    /// Clave AES-256 en Base64 (32 bytes) para cifrar contraseñas de certificados .p12.
    /// NUNCA commitear en appsettings.json — solo en appsettings.Production.json.
    /// </summary>
    public string EncryptionKey { get; set; }

    /// <summary>Máximo de reintentos automáticos de envío al SRI.</summary>
    public int MaxReintentosEnvio { get; set; } = 3;

    /// <summary>Timeout en segundos para llamadas SOAP al SRI.</summary>
    public int TimeoutSriSegundos { get; set; } = 30;

    /// <summary>Endpoints SOAP del SRI (pruebas y producción).</summary>
    public SriEndpoints Endpoints { get; set; } = new();
}

public class SriEndpoints
{
    public string RecepcionPruebas { get; set; } = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl";
    public string AutorizacionPruebas { get; set; } = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl";
    public string RecepcionProduccion { get; set; } = "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl";
    public string AutorizacionProduccion { get; set; } = "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl";
}
