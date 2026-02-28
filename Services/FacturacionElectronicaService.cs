using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Xml;
using Gimnasio.Data;
using Gimnasio.Helpers;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gimnasio.Services;

public class FacturacionElectronicaService : IFacturacionElectronicaService
{
    private readonly ApplicationDbContext _db;
    private readonly FacturacionSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEmailService _emailService;
    private readonly ILogger<FacturacionElectronicaService> _logger;

    // Códigos IVA SRI
    private const string CodigoIva15 = "4";   // IVA 15%
    private const string CodigoIva0 = "0";    // IVA 0%
    private const string TarifaIva15 = "15";
    private const string TarifaIva0 = "0";

    public FacturacionElectronicaService(
        ApplicationDbContext db,
        IOptions<FacturacionSettings> settings,
        IHttpClientFactory httpClientFactory,
        IEmailService emailService,
        ILogger<FacturacionElectronicaService> logger)
    {
        _db = db;
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _emailService = emailService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDACION DE CONFIGURACION
    // ═══════════════════════════════════════════════════════════

    public async Task<ValidacionConfigResult> ValidarConfiguracionAsync(Guid negocioId)
    {
        var negocio = await _db.Negocios.FindAsync(negocioId);
        var result = new ValidacionConfigResult { EsValida = true };

        if (negocio == null)
        {
            result.EsValida = false;
            result.Errores.Add("Negocio no encontrado");
            return result;
        }

        if (string.IsNullOrWhiteSpace(negocio.Ruc) || negocio.Ruc.Length != 13)
            result.Errores.Add("RUC inválido (debe tener 13 dígitos)");

        if (string.IsNullOrWhiteSpace(negocio.RazonSocial))
            result.Errores.Add("Razón social es requerida");

        if (string.IsNullOrWhiteSpace(negocio.DireccionMatriz))
            result.Errores.Add("Dirección de la matriz es requerida");

        if (string.IsNullOrWhiteSpace(negocio.CodigoEstablecimiento) || negocio.CodigoEstablecimiento.Length != 3)
            result.Errores.Add("Código de establecimiento debe tener 3 dígitos");

        if (string.IsNullOrWhiteSpace(negocio.PuntoEmision) || negocio.PuntoEmision.Length != 3)
            result.Errores.Add("Punto de emisión debe tener 3 dígitos");

        if (string.IsNullOrWhiteSpace(negocio.CertificadoP12Base64))
            result.Errores.Add("Certificado digital .p12 no configurado");

        if (string.IsNullOrWhiteSpace(negocio.CertificadoPasswordEncriptado))
            result.Errores.Add("Contraseña del certificado no configurada");

        if (result.Errores.Count > 0)
            result.EsValida = false;

        return result;
    }

    // ═══════════════════════════════════════════════════════════
    // GESTION DE CERTIFICADO .P12
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool success, string message, DateTime? vencimiento)> GuardarCertificadoAsync(
        Guid negocioId, byte[] p12Bytes, string password)
    {
        var negocio = await _db.Negocios.FindAsync(negocioId);
        if (negocio == null)
            return (false, "Negocio no encontrado", null);

        // Validar que el .p12 se pueda abrir con la contraseña
        try
        {
            using var cert = X509CertificateLoader.LoadPkcs12(p12Bytes, password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            if (cert.NotAfter < TimeHelper.Now)
                return (false, $"El certificado venció el {cert.NotAfter:dd/MM/yyyy}", cert.NotAfter);

            // Cifrar contraseña y guardar
            negocio.CertificadoP12Base64 = Convert.ToBase64String(p12Bytes);
            negocio.CertificadoPasswordEncriptado = CertificadoEncryptionHelper.Encrypt(password, _settings.EncryptionKey);
            await _db.SaveChangesAsync();

            return (true, $"Certificado guardado. Vence: {cert.NotAfter:dd/MM/yyyy}", cert.NotAfter);
        }
        catch (CryptographicException)
        {
            return (false, "Contraseña del certificado incorrecta o archivo inválido", null);
        }
    }

    public async Task<(bool valid, string message, DateTime? vencimiento)> ValidarCertificadoAsync(Guid negocioId)
    {
        var negocio = await _db.Negocios.FindAsync(negocioId);
        if (negocio == null)
            return (false, "Negocio no encontrado", null);

        if (string.IsNullOrWhiteSpace(negocio.CertificadoP12Base64))
            return (false, "No hay certificado configurado", null);

        try
        {
            var p12Bytes = Convert.FromBase64String(negocio.CertificadoP12Base64);
            var password = CertificadoEncryptionHelper.Decrypt(negocio.CertificadoPasswordEncriptado, _settings.EncryptionKey);

            using var cert = X509CertificateLoader.LoadPkcs12(p12Bytes, password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            if (cert.NotAfter < TimeHelper.Now)
                return (false, $"Certificado vencido el {cert.NotAfter:dd/MM/yyyy}", cert.NotAfter);

            var diasRestantes = (cert.NotAfter - TimeHelper.Now).Days;
            return (true, $"Certificado válido. Vence en {diasRestantes} días ({cert.NotAfter:dd/MM/yyyy})", cert.NotAfter);
        }
        catch (Exception ex)
        {
            return (false, $"Error al validar certificado: {ex.Message}", null);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // EMISION DE FACTURA
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool success, string message, FacturaElectronica factura)> EmitirFacturaAsync(FacturaRequest request)
    {
        var negocio = await _db.Negocios.FindAsync(request.NegocioId);
        if (negocio == null)
            return (false, "Negocio no encontrado", null);

        if (!negocio.FacturacionElectronicaActiva)
            return (false, "La facturación electrónica no está activa", null);

        // Validar configuración
        var validacion = await ValidarConfiguracionAsync(request.NegocioId);
        if (!validacion.EsValida)
            return (false, "Configuración SRI incompleta: " + string.Join(", ", validacion.Errores), null);

        // Validar items
        if (request.Items == null || request.Items.Count == 0)
            return (false, "La factura debe tener al menos un item", null);

        try
        {
            var estab = negocio.CodigoEstablecimiento;
            var ptoEmi = negocio.PuntoEmision;
            var secuencial = await ObtenerSiguienteSecuencialAsync(request.NegocioId, estab, ptoEmi);
            var fechaEmision = TimeHelper.Now;

            // Calcular totales
            decimal totalSinImpuestos = 0;
            decimal totalDescuento = 0;
            decimal montoIva = 0;

            foreach (var item in request.Items)
            {
                var subtotalItem = item.Cantidad * item.PrecioUnitario - item.Descuento;
                totalSinImpuestos += subtotalItem;
                totalDescuento += item.Descuento;
                montoIva += subtotalItem * item.PorcentajeIva / 100m;
            }

            var importeTotal = totalSinImpuestos + montoIva;

            // Generar clave de acceso (49 dígitos)
            var claveAcceso = GenerarClaveAcceso(
                fechaEmision, "01", negocio.Ruc, negocio.SriAmbiente,
                estab, ptoEmi, secuencial);

            var numeroCompleto = $"{estab}-{ptoEmi}-{secuencial:D9}";

            // Generar XML SRI
            var xml = GenerarXmlFactura(negocio, request, claveAcceso, secuencial,
                fechaEmision, totalSinImpuestos, totalDescuento, montoIva, importeTotal);

            // Firmar XML con XAdES-BES
            var xmlFirmado = FirmarXml(xml, negocio);

            // Crear registro de factura
            var factura = new FacturaElectronica
            {
                FacturaId = Guid.NewGuid(),
                NegocioId = request.NegocioId,
                ReciboId = request.ReciboId,
                Establecimiento = estab,
                PuntoEmision = ptoEmi,
                Secuencial = secuencial,
                NumeroCompleto = numeroCompleto,
                ClaveAcceso = claveAcceso,
                CompradorIdentificacion = request.CompradorIdentificacion,
                CompradorTipoIdentificacion = request.CompradorTipoIdentificacion,
                CompradorRazonSocial = request.CompradorRazonSocial,
                CompradorEmail = request.CompradorEmail,
                CompradorDireccion = request.CompradorDireccion,
                TotalSinImpuestos = totalSinImpuestos,
                TotalDescuento = totalDescuento,
                MontoIva = montoIva,
                ImporteTotal = importeTotal,
                FormaPagoSri = request.FormaPagoSri,
                EstadoSri = "Pendiente",
                FechaEmision = fechaEmision,
                FechaCreacion = TimeHelper.Now,
                IntentosEnvio = 0,
                DetalleItemsJson = JsonSerializer.Serialize(request.Items)
            };

            _db.FacturasElectronicas.Add(factura);
            await _db.SaveChangesAsync();

            // Enviar al SRI (asíncrono, no bloquear al usuario)
            _ = Task.Run(async () =>
            {
                try
                {
                    await EnviarAlSriAsync(factura.FacturaId, xmlFirmado, negocio.SriAmbiente);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando factura {FacturaId} al SRI", factura.FacturaId);
                }
            });

            return (true, $"Factura {numeroCompleto} creada. Enviándose al SRI...", factura);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emitiendo factura para negocio {NegocioId}", request.NegocioId);
            return (false, $"Error al emitir factura: {ex.Message}", null);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // SECUENCIAL
    // ═══════════════════════════════════════════════════════════

    public async Task<int> ObtenerSiguienteSecuencialAsync(Guid negocioId, string establecimiento, string puntoEmision)
    {
        var maxSecuencial = await _db.FacturasElectronicas
            .Where(f => f.NegocioId == negocioId
                        && f.Establecimiento == establecimiento
                        && f.PuntoEmision == puntoEmision)
            .MaxAsync(f => (int?)f.Secuencial) ?? 0;

        return maxSecuencial + 1;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    public async Task<List<FacturaElectronica>> GetFacturasAsync(Guid negocioId,
        DateTime? desde = null, DateTime? hasta = null, string estado = null)
    {
        var query = _db.FacturasElectronicas
            .Where(f => f.NegocioId == negocioId)
            .AsNoTracking();

        if (desde.HasValue)
            query = query.Where(f => f.FechaEmision >= desde.Value);
        if (hasta.HasValue)
            query = query.Where(f => f.FechaEmision <= hasta.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(f => f.EstadoSri == estado);

        return await query.OrderByDescending(f => f.FechaEmision).ToListAsync();
    }

    public async Task<FacturaElectronica> GetFacturaAsync(Guid negocioId, Guid facturaId)
    {
        return await _db.FacturasElectronicas
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FacturaId == facturaId && f.NegocioId == negocioId);
    }

    public async Task<byte[]> GetFacturaXmlAsync(Guid negocioId, Guid facturaId)
    {
        var factura = await _db.FacturasElectronicas
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FacturaId == facturaId && f.NegocioId == negocioId);

        if (factura?.XmlAutorizadoComprimido == null) return null;

        return DescomprimirGzip(factura.XmlAutorizadoComprimido);
    }

    public async Task<byte[]> GetFacturaRideAsync(Guid negocioId, Guid facturaId)
    {
        var factura = await _db.FacturasElectronicas
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FacturaId == facturaId && f.NegocioId == negocioId);

        return factura?.RidePdf;
    }

    // ═══════════════════════════════════════════════════════════
    // REENVIO Y ANULACION
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool success, string message)> ReenviarFacturaAsync(Guid negocioId, Guid facturaId)
    {
        var factura = await _db.FacturasElectronicas
            .FirstOrDefaultAsync(f => f.FacturaId == facturaId && f.NegocioId == negocioId);

        if (factura == null)
            return (false, "Factura no encontrada");

        if (factura.EstadoSri == "Autorizada")
            return (false, "La factura ya está autorizada");

        if (factura.EstadoSri == "Anulada")
            return (false, "La factura está anulada");

        if (factura.IntentosEnvio >= _settings.MaxReintentosEnvio)
            return (false, $"Se alcanzó el máximo de {_settings.MaxReintentosEnvio} intentos");

        var negocio = await _db.Negocios.FindAsync(negocioId);
        if (negocio == null)
            return (false, "Negocio no encontrado");

        try
        {
            // Regenerar XML y firmar
            var items = JsonSerializer.Deserialize<List<FacturaItemRequest>>(factura.DetalleItemsJson);
            var request = new FacturaRequest
            {
                NegocioId = negocioId,
                CompradorIdentificacion = factura.CompradorIdentificacion,
                CompradorTipoIdentificacion = factura.CompradorTipoIdentificacion,
                CompradorRazonSocial = factura.CompradorRazonSocial,
                CompradorEmail = factura.CompradorEmail,
                CompradorDireccion = factura.CompradorDireccion,
                FormaPagoSri = factura.FormaPagoSri,
                Items = items
            };

            var xml = GenerarXmlFactura(negocio, request, factura.ClaveAcceso, factura.Secuencial,
                factura.FechaEmision, factura.TotalSinImpuestos, factura.TotalDescuento,
                factura.MontoIva, factura.ImporteTotal);

            var xmlFirmado = FirmarXml(xml, negocio);
            await EnviarAlSriAsync(factura.FacturaId, xmlFirmado, negocio.SriAmbiente);

            return (true, "Factura reenviada al SRI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reenviando factura {FacturaId}", facturaId);
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> EnviarFacturaPorEmailAsync(Guid negocioId, Guid facturaId)
    {
        var factura = await _db.FacturasElectronicas
            .Include(f => f.Negocio)
            .FirstOrDefaultAsync(f => f.FacturaId == facturaId && f.NegocioId == negocioId);

        if (factura == null)
            return (false, "Factura no encontrada");

        if (string.IsNullOrWhiteSpace(factura.CompradorEmail))
            return (false, "El comprador no tiene email registrado");

        if (factura.EstadoSri != "Autorizada")
            return (false, "Solo se pueden enviar facturas autorizadas");

        try
        {
            var xmlBytes = factura.XmlAutorizadoComprimido != null
                ? DescomprimirGzip(factura.XmlAutorizadoComprimido) : null;

            var subject = $"Factura Electrónica {factura.NumeroCompleto} - {factura.Negocio?.NegocioNombre ?? ""}";
            var body = $@"
                <h2>Factura Electrónica</h2>
                <p>Estimado/a {factura.CompradorRazonSocial},</p>
                <p>Adjunto encontrará su factura electrónica:</p>
                <ul>
                    <li><strong>Número:</strong> {factura.NumeroCompleto}</li>
                    <li><strong>Fecha:</strong> {factura.FechaEmision:dd/MM/yyyy}</li>
                    <li><strong>Total:</strong> ${factura.ImporteTotal:N2}</li>
                    <li><strong>Autorización SRI:</strong> {factura.NumeroAutorizacion}</li>
                </ul>
                <p>Emisor: {factura.Negocio?.RazonSocial ?? factura.Negocio?.NegocioNombre}</p>";

            await _emailService.SendEmailWithAttachmentsAsync(
                factura.CompradorEmail, subject, body,
                xmlBytes, $"factura_{factura.NumeroCompleto}.xml",
                factura.RidePdf, $"RIDE_{factura.NumeroCompleto}.pdf");

            return (true, "Factura enviada por email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando factura por email {FacturaId}", facturaId);
            return (false, $"Error al enviar email: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> AnularFacturaAsync(Guid negocioId, Guid facturaId)
    {
        var factura = await _db.FacturasElectronicas
            .FirstOrDefaultAsync(f => f.FacturaId == facturaId && f.NegocioId == negocioId);

        if (factura == null)
            return (false, "Factura no encontrada");

        if (factura.EstadoSri == "Anulada")
            return (false, "La factura ya está anulada");

        factura.EstadoSri = "Anulada";
        factura.MensajesSri = JsonSerializer.Serialize(new { motivo = "Anulada por el usuario", fecha = TimeHelper.Now });
        await _db.SaveChangesAsync();

        return (true, "Factura marcada como anulada");
    }

    // ═══════════════════════════════════════════════════════════
    // REPROCESAMIENTO (BACKGROUND SERVICE)
    // ═══════════════════════════════════════════════════════════

    public async Task<int> ReprocesarPendientesAsync()
    {
        var pendientes = await _db.FacturasElectronicas
            .Include(f => f.Negocio)
            .Where(f => (f.EstadoSri == "Pendiente" || f.EstadoSri == "Recibida")
                        && f.IntentosEnvio < _settings.MaxReintentosEnvio)
            .ToListAsync();

        var procesadas = 0;
        foreach (var factura in pendientes)
        {
            try
            {
                var (success, _) = await ReenviarFacturaAsync(factura.NegocioId, factura.FacturaId);
                if (success) procesadas++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reprocesando factura {FacturaId}", factura.FacturaId);
            }

            await Task.Delay(1000); // Respetar rate limits del SRI
        }

        return procesadas;
    }

    // ═══════════════════════════════════════════════════════════
    // ESTADISTICAS
    // ═══════════════════════════════════════════════════════════

    public async Task<object> GetEstadisticasAsync(Guid negocioId)
    {
        var facturas = await _db.FacturasElectronicas
            .Where(f => f.NegocioId == negocioId)
            .AsNoTracking()
            .ToListAsync();

        return new
        {
            totalEmitidas = facturas.Count,
            autorizadas = facturas.Count(f => f.EstadoSri == "Autorizada"),
            pendientes = facturas.Count(f => f.EstadoSri == "Pendiente" || f.EstadoSri == "Recibida"),
            noAutorizadas = facturas.Count(f => f.EstadoSri == "NoAutorizada"),
            anuladas = facturas.Count(f => f.EstadoSri == "Anulada"),
            montoTotal = facturas.Where(f => f.EstadoSri == "Autorizada").Sum(f => f.ImporteTotal),
            montoIvaTotal = facturas.Where(f => f.EstadoSri == "Autorizada").Sum(f => f.MontoIva)
        };
    }

    // ═══════════════════════════════════════════════════════════
    // GENERACION XML SRI v1.1.0
    // ═══════════════════════════════════════════════════════════

    private string GenerarXmlFactura(Gym negocio, FacturaRequest request,
        string claveAcceso, int secuencial, DateTime fechaEmision,
        decimal totalSinImpuestos, decimal totalDescuento, decimal montoIva, decimal importeTotal)
    {
        var doc = new XmlDocument();
        var factura = doc.CreateElement("factura");
        factura.SetAttribute("id", "comprobante");
        factura.SetAttribute("version", "1.1.0");
        doc.AppendChild(factura);

        // ── infoTributaria ──
        var infoTrib = doc.CreateElement("infoTributaria");
        AppendElement(doc, infoTrib, "ambiente", negocio.SriAmbiente.ToString());
        AppendElement(doc, infoTrib, "tipoEmision", "1"); // Normal
        AppendElement(doc, infoTrib, "razonSocial", negocio.RazonSocial);
        if (!string.IsNullOrWhiteSpace(negocio.NombreComercial))
            AppendElement(doc, infoTrib, "nombreComercial", negocio.NombreComercial);
        AppendElement(doc, infoTrib, "ruc", negocio.Ruc);
        AppendElement(doc, infoTrib, "claveAcceso", claveAcceso);
        AppendElement(doc, infoTrib, "codDoc", "01"); // Factura
        AppendElement(doc, infoTrib, "estab", negocio.CodigoEstablecimiento);
        AppendElement(doc, infoTrib, "ptoEmi", negocio.PuntoEmision);
        AppendElement(doc, infoTrib, "secuencial", secuencial.ToString("D9"));
        AppendElement(doc, infoTrib, "dirMatriz", negocio.DireccionMatriz);
        if (!string.IsNullOrWhiteSpace(negocio.RegimenContribuyente))
            AppendElement(doc, infoTrib, "regimenMicroempresas", negocio.RegimenContribuyente);
        if (!string.IsNullOrWhiteSpace(negocio.AgenteRetencion))
            AppendElement(doc, infoTrib, "agenteRetencion", negocio.AgenteRetencion);
        if (!string.IsNullOrWhiteSpace(negocio.ContribuyenteEspecial))
            AppendElement(doc, infoTrib, "contribuyenteEspecial", negocio.ContribuyenteEspecial);
        factura.AppendChild(infoTrib);

        // ── infoFactura ──
        var infoFact = doc.CreateElement("infoFactura");
        AppendElement(doc, infoFact, "fechaEmision", fechaEmision.ToString("dd/MM/yyyy"));
        AppendElement(doc, infoFact, "dirEstablecimiento", negocio.DireccionMatriz);
        AppendElement(doc, infoFact, "obligadoContabilidad", negocio.ObligadoContabilidad ? "SI" : "NO");
        AppendElement(doc, infoFact, "tipoIdentificacionComprador", request.CompradorTipoIdentificacion);
        AppendElement(doc, infoFact, "razonSocialComprador", request.CompradorRazonSocial);
        AppendElement(doc, infoFact, "identificacionComprador", request.CompradorIdentificacion);
        if (!string.IsNullOrWhiteSpace(request.CompradorDireccion))
            AppendElement(doc, infoFact, "direccionComprador", request.CompradorDireccion);
        AppendElement(doc, infoFact, "totalSinImpuestos", totalSinImpuestos.ToString("F2"));
        AppendElement(doc, infoFact, "totalDescuento", totalDescuento.ToString("F2"));

        // totalConImpuestos
        var totalConImpuestos = doc.CreateElement("totalConImpuestos");

        // IVA 15%
        var itemsConIva = request.Items.Where(i => i.PorcentajeIva > 0).ToList();
        if (itemsConIva.Any())
        {
            var baseIva15 = itemsConIva.Sum(i => i.Cantidad * i.PrecioUnitario - i.Descuento);
            var totalImpuesto15 = doc.CreateElement("totalImpuesto");
            AppendElement(doc, totalImpuesto15, "codigo", "2"); // IVA
            AppendElement(doc, totalImpuesto15, "codigoPorcentaje", CodigoIva15);
            AppendElement(doc, totalImpuesto15, "baseImponible", baseIva15.ToString("F2"));
            AppendElement(doc, totalImpuesto15, "tarifa", TarifaIva15);
            AppendElement(doc, totalImpuesto15, "valor", montoIva.ToString("F2"));
            totalConImpuestos.AppendChild(totalImpuesto15);
        }

        // IVA 0%
        var itemsSinIva = request.Items.Where(i => i.PorcentajeIva == 0).ToList();
        if (itemsSinIva.Any())
        {
            var baseIva0 = itemsSinIva.Sum(i => i.Cantidad * i.PrecioUnitario - i.Descuento);
            var totalImpuesto0 = doc.CreateElement("totalImpuesto");
            AppendElement(doc, totalImpuesto0, "codigo", "2");
            AppendElement(doc, totalImpuesto0, "codigoPorcentaje", CodigoIva0);
            AppendElement(doc, totalImpuesto0, "baseImponible", baseIva0.ToString("F2"));
            AppendElement(doc, totalImpuesto0, "tarifa", TarifaIva0);
            AppendElement(doc, totalImpuesto0, "valor", "0.00");
            totalConImpuestos.AppendChild(totalImpuesto0);
        }

        infoFact.AppendChild(totalConImpuestos);
        AppendElement(doc, infoFact, "propina", "0.00");
        AppendElement(doc, infoFact, "importeTotal", importeTotal.ToString("F2"));
        AppendElement(doc, infoFact, "moneda", "DOLAR");

        // pagos
        var pagos = doc.CreateElement("pagos");
        var pago = doc.CreateElement("pago");
        AppendElement(doc, pago, "formaPago", request.FormaPagoSri);
        AppendElement(doc, pago, "total", importeTotal.ToString("F2"));
        pagos.AppendChild(pago);
        infoFact.AppendChild(pagos);

        factura.AppendChild(infoFact);

        // ── detalles ──
        var detalles = doc.CreateElement("detalles");
        foreach (var item in request.Items)
        {
            var detalle = doc.CreateElement("detalle");
            AppendElement(doc, detalle, "codigoPrincipal", item.CodigoPrincipal ?? "001");
            AppendElement(doc, detalle, "descripcion", item.Descripcion);
            AppendElement(doc, detalle, "cantidad", item.Cantidad.ToString("F2"));
            AppendElement(doc, detalle, "precioUnitario", item.PrecioUnitario.ToString("F2"));
            AppendElement(doc, detalle, "descuento", item.Descuento.ToString("F2"));
            var precioTotalSinImpuesto = item.Cantidad * item.PrecioUnitario - item.Descuento;
            AppendElement(doc, detalle, "precioTotalSinImpuesto", precioTotalSinImpuesto.ToString("F2"));

            var impuestos = doc.CreateElement("impuestos");
            var impuesto = doc.CreateElement("impuesto");
            AppendElement(doc, impuesto, "codigo", "2"); // IVA
            AppendElement(doc, impuesto, "codigoPorcentaje", item.PorcentajeIva > 0 ? CodigoIva15 : CodigoIva0);
            AppendElement(doc, impuesto, "tarifa", item.PorcentajeIva.ToString("F0"));
            AppendElement(doc, impuesto, "baseImponible", precioTotalSinImpuesto.ToString("F2"));
            var valorImpuesto = precioTotalSinImpuesto * item.PorcentajeIva / 100m;
            AppendElement(doc, impuesto, "valor", valorImpuesto.ToString("F2"));
            impuestos.AppendChild(impuesto);
            detalle.AppendChild(impuestos);

            detalles.AppendChild(detalle);
        }
        factura.AppendChild(detalles);

        // ── infoAdicional ──
        var infoAdicional = doc.CreateElement("infoAdicional");
        if (!string.IsNullOrWhiteSpace(request.CompradorEmail))
            AppendCampoAdicional(doc, infoAdicional, "email", request.CompradorEmail);
        if (!string.IsNullOrWhiteSpace(request.CompradorDireccion))
            AppendCampoAdicional(doc, infoAdicional, "direccion", request.CompradorDireccion);
        AppendCampoAdicional(doc, infoAdicional, "plataforma", "My-Negocio");
        factura.AppendChild(infoAdicional);

        return doc.OuterXml;
    }

    private static void AppendElement(XmlDocument doc, XmlElement parent, string name, string value)
    {
        var el = doc.CreateElement(name);
        el.InnerText = value ?? "";
        parent.AppendChild(el);
    }

    private static void AppendCampoAdicional(XmlDocument doc, XmlElement parent, string nombre, string valor)
    {
        var campo = doc.CreateElement("campoAdicional");
        campo.SetAttribute("nombre", nombre);
        campo.InnerText = valor;
        parent.AppendChild(campo);
    }

    // ═══════════════════════════════════════════════════════════
    // CLAVE DE ACCESO (49 digitos) — Algoritmo SRI
    // ═══════════════════════════════════════════════════════════

    private string GenerarClaveAcceso(DateTime fecha, string tipoDoc, string ruc,
        int ambiente, string estab, string ptoEmi, int secuencial)
    {
        // fecha(8) + tipoDoc(2) + ruc(13) + ambiente(1) + estab(3) + ptoEmi(3)
        // + secuencial(9) + codigoNumerico(8) + tipoEmision(1)
        var codigoNumerico = Random.Shared.Next(10000000, 99999999).ToString();

        var clave = fecha.ToString("ddMMyyyy")
                    + tipoDoc.PadLeft(2, '0')
                    + ruc
                    + ambiente.ToString()
                    + estab
                    + ptoEmi
                    + secuencial.ToString("D9")
                    + codigoNumerico
                    + "1"; // Tipo emisión normal

        // Dígito verificador módulo 11
        var digitoVerificador = CalcularModulo11(clave);
        return clave + digitoVerificador;
    }

    private static int CalcularModulo11(string cadena)
    {
        var pesos = new[] { 2, 3, 4, 5, 6, 7 };
        var suma = 0;
        var pesoIndex = 0;

        for (int i = cadena.Length - 1; i >= 0; i--)
        {
            suma += (cadena[i] - '0') * pesos[pesoIndex];
            pesoIndex = (pesoIndex + 1) % pesos.Length;
        }

        var residuo = suma % 11;
        var resultado = 11 - residuo;

        if (resultado == 11) return 0;
        if (resultado == 10) return 1;
        return resultado;
    }

    // ═══════════════════════════════════════════════════════════
    // FIRMA XAdES-BES (enveloped signature)
    // ═══════════════════════════════════════════════════════════

    private string FirmarXml(string xml, Gym negocio)
    {
        var p12Bytes = Convert.FromBase64String(negocio.CertificadoP12Base64);
        var password = CertificadoEncryptionHelper.Decrypt(
            negocio.CertificadoPasswordEncriptado, _settings.EncryptionKey);

        using var cert = X509CertificateLoader.LoadPkcs12(p12Bytes, password,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);

        var signedXml = new SignedXml(doc)
        {
            SigningKey = cert.GetRSAPrivateKey()
        };

        // Referencia al documento completo
        var reference = new Reference { Uri = "#comprobante" };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        signedXml.AddReference(reference);

        // KeyInfo con el certificado X509
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();

        doc.DocumentElement.AppendChild(doc.ImportNode(signedXml.GetXml(), true));

        return doc.OuterXml;
    }

    // ═══════════════════════════════════════════════════════════
    // ENVIO SOAP AL SRI
    // ═══════════════════════════════════════════════════════════

    private async Task EnviarAlSriAsync(Guid facturaId, string xmlFirmado, int ambiente)
    {
        using var scope = _db.Database.GetDbConnection().CreateCommand(); // Para el scope
        var factura = await _db.FacturasElectronicas.FindAsync(facturaId);
        if (factura == null) return;

        factura.IntentosEnvio++;
        factura.FechaUltimoIntento = TimeHelper.Now;

        var client = _httpClientFactory.CreateClient("SRI");
        client.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSriSegundos);

        try
        {
            // 1. Enviar comprobante (validarComprobante)
            var urlRecepcion = ambiente == 2
                ? _settings.Endpoints.RecepcionProduccion
                : _settings.Endpoints.RecepcionPruebas;

            var xmlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xmlFirmado));

            var soapRecepcion = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ec=""http://ec.gob.sri.ws.recepcion"">
   <soapenv:Body>
      <ec:validarComprobante>
         <xml>{xmlBase64}</xml>
      </ec:validarComprobante>
   </soapenv:Body>
</soapenv:Envelope>";

            var respRecepcion = await client.PostAsync(urlRecepcion,
                new StringContent(soapRecepcion, Encoding.UTF8, "text/xml"));

            var bodyRecepcion = await respRecepcion.Content.ReadAsStringAsync();

            if (!respRecepcion.IsSuccessStatusCode)
            {
                factura.EstadoSri = "NoAutorizada";
                factura.MensajesSri = $"HTTP {(int)respRecepcion.StatusCode}: {bodyRecepcion[..Math.Min(500, bodyRecepcion.Length)]}";
                await _db.SaveChangesAsync();
                return;
            }

            // Parsear respuesta de recepción
            var docResp = new XmlDocument();
            docResp.LoadXml(bodyRecepcion);
            var estadoNodo = docResp.GetElementsByTagName("estado");
            var estadoRecepcion = estadoNodo.Count > 0 ? estadoNodo[0].InnerText : "";

            if (estadoRecepcion == "DEVUELTA")
            {
                var mensajes = docResp.GetElementsByTagName("mensaje");
                var errores = new List<string>();
                foreach (XmlNode msg in mensajes)
                {
                    var tipo = msg["tipo"]?.InnerText ?? "";
                    var ident = msg["identificador"]?.InnerText ?? "";
                    var mensaje = msg["mensaje"]?.InnerText ?? "";
                    var info = msg["informacionAdicional"]?.InnerText ?? "";
                    errores.Add($"[{tipo}] {ident}: {mensaje}. {info}");
                }
                factura.EstadoSri = "NoAutorizada";
                factura.MensajesSri = JsonSerializer.Serialize(errores);
                await _db.SaveChangesAsync();
                return;
            }

            factura.EstadoSri = "Recibida";
            factura.XmlAutorizadoComprimido = ComprimirGzip(Encoding.UTF8.GetBytes(xmlFirmado));
            await _db.SaveChangesAsync();

            // 2. Esperar y consultar autorización
            await Task.Delay(3000);

            var urlAutorizacion = ambiente == 2
                ? _settings.Endpoints.AutorizacionProduccion
                : _settings.Endpoints.AutorizacionPruebas;

            var soapAutorizacion = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ec=""http://ec.gob.sri.ws.autorizacion"">
   <soapenv:Body>
      <ec:autorizacionComprobante>
         <claveAccesoComprobante>{factura.ClaveAcceso}</claveAccesoComprobante>
      </ec:autorizacionComprobante>
   </soapenv:Body>
</soapenv:Envelope>";

            var respAutorizacion = await client.PostAsync(urlAutorizacion,
                new StringContent(soapAutorizacion, Encoding.UTF8, "text/xml"));

            var bodyAutorizacion = await respAutorizacion.Content.ReadAsStringAsync();

            var docAuth = new XmlDocument();
            docAuth.LoadXml(bodyAutorizacion);
            var estadoAuth = docAuth.GetElementsByTagName("estado");
            var authEstado = estadoAuth.Count > 0 ? estadoAuth[0].InnerText : "";

            if (authEstado == "AUTORIZADO")
            {
                factura.EstadoSri = "Autorizada";
                var numAuth = docAuth.GetElementsByTagName("numeroAutorizacion");
                factura.NumeroAutorizacion = numAuth.Count > 0 ? numAuth[0].InnerText : factura.ClaveAcceso;
                var fechaAuth = docAuth.GetElementsByTagName("fechaAutorizacion");
                if (fechaAuth.Count > 0 && DateTime.TryParse(fechaAuth[0].InnerText, out var fa))
                    factura.FechaAutorizacion = fa;
                else
                    factura.FechaAutorizacion = TimeHelper.Now;

                // Guardar XML autorizado comprimido
                var comprobante = docAuth.GetElementsByTagName("comprobante");
                if (comprobante.Count > 0)
                    factura.XmlAutorizadoComprimido = ComprimirGzip(Encoding.UTF8.GetBytes(comprobante[0].InnerText));

                factura.MensajesSri = null;
            }
            else
            {
                factura.EstadoSri = "NoAutorizada";
                var mensajes = docAuth.GetElementsByTagName("mensaje");
                var errores = new List<string>();
                foreach (XmlNode msg in mensajes)
                {
                    var mensaje = msg["mensaje"]?.InnerText ?? "";
                    var info = msg["informacionAdicional"]?.InnerText ?? "";
                    errores.Add($"{mensaje}. {info}");
                }
                factura.MensajesSri = JsonSerializer.Serialize(errores);
            }

            await _db.SaveChangesAsync();
        }
        catch (TaskCanceledException)
        {
            factura.MensajesSri = "Timeout al conectar con el SRI";
            await _db.SaveChangesAsync();
        }
        catch (HttpRequestException ex)
        {
            factura.MensajesSri = $"Error de conexión al SRI: {ex.Message}";
            await _db.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // COMPRESION GZIP
    // ═══════════════════════════════════════════════════════════

    private static byte[] ComprimirGzip(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Optimal))
            gz.Write(data, 0, data.Length);
        return ms.ToArray();
    }

    private static byte[] DescomprimirGzip(byte[] comprimido)
    {
        using var input = new MemoryStream(comprimido);
        using var gz = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gz.CopyTo(output);
        return output.ToArray();
    }
}
