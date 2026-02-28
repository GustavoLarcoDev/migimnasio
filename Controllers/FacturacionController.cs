using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

/// <summary>
/// Controller de facturación electrónica SRI Ecuador.
/// Hereda de NegocioBaseController para multi-tenant auth.
/// </summary>
public class FacturacionController : NegocioBaseController
{
    private readonly IFacturacionElectronicaService _facturacionService;

    public FacturacionController(
        IAuthService authService,
        IFacturacionElectronicaService facturacionService) : base(authService)
    {
        _facturacionService = facturacionService;
    }

    // ═══════════════════════════════════════════════════════════
    // CONFIGURACION
    // ═══════════════════════════════════════════════════════════

    [HttpGet("GetConfigFacturacion")]
    public Task<IActionResult> GetConfigFacturacion(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var validacion = await _facturacionService.ValidarConfiguracionAsync(nId);
            var certResult = await _facturacionService.ValidarCertificadoAsync(nId);

            return Ok(new
            {
                configuracionValida = validacion.EsValida,
                erroresConfig = validacion.Errores,
                certificadoValido = certResult.valid,
                certificadoMensaje = certResult.message,
                certificadoVencimiento = certResult.vencimiento
            });
        });

    [HttpPost("ToggleFacturacion")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> ToggleFacturacion(Guid negocioId, bool activa)
        => Execute(negocioId, async nId =>
        {
            var db = HttpContext.RequestServices.GetRequiredService<Gimnasio.Data.ApplicationDbContext>();
            var negocio = await db.Negocios.FindAsync(nId);
            if (negocio == null) return NotFound();

            negocio.FacturacionElectronicaActiva = activa;
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = activa ? "Facturación electrónica activada" : "Facturación electrónica desactivada" });
        });

    [HttpPost("GuardarConfigFacturacion")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> GuardarConfigFacturacion(Guid negocioId,
        string ruc, string razonSocial, string nombreComercial, string direccionMatriz,
        string codigoEstablecimiento, string puntoEmision, int sriAmbiente,
        bool obligadoContabilidad, string contribuyenteEspecial, string regimenContribuyente,
        string agenteRetencion)
        => Execute(negocioId, async nId =>
        {
            var db = HttpContext.RequestServices.GetRequiredService<Gimnasio.Data.ApplicationDbContext>();
            var negocio = await db.Negocios.FindAsync(nId);
            if (negocio == null) return NotFound();

            negocio.Ruc = ruc?.Trim();
            negocio.RazonSocial = razonSocial?.Trim();
            negocio.NombreComercial = nombreComercial?.Trim();
            negocio.DireccionMatriz = direccionMatriz?.Trim();
            negocio.CodigoEstablecimiento = (codigoEstablecimiento ?? "001").Trim();
            negocio.PuntoEmision = (puntoEmision ?? "001").Trim();
            negocio.SriAmbiente = sriAmbiente;
            negocio.ObligadoContabilidad = obligadoContabilidad;
            negocio.ContribuyenteEspecial = contribuyenteEspecial?.Trim();
            negocio.RegimenContribuyente = regimenContribuyente?.Trim();
            negocio.AgenteRetencion = agenteRetencion?.Trim();

            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "Configuración SRI guardada" });
        });

    // ═══════════════════════════════════════════════════════════
    // CERTIFICADO
    // ═══════════════════════════════════════════════════════════

    [HttpPost("SubirCertificado")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> SubirCertificado([FromForm] Guid negocioId, IFormFile certificado, [FromForm] string password)
        => Execute(negocioId, async nId =>
        {
            if (certificado == null || certificado.Length == 0)
                return BadRequest(new { success = false, message = "No se proporcionó certificado" });

            if (!certificado.FileName.EndsWith(".p12", StringComparison.OrdinalIgnoreCase)
                && !certificado.FileName.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "El archivo debe ser .p12 o .pfx" });

            if (string.IsNullOrWhiteSpace(password))
                return BadRequest(new { success = false, message = "La contraseña del certificado es requerida" });

            using var ms = new MemoryStream();
            await certificado.CopyToAsync(ms);
            var p12Bytes = ms.ToArray();

            var (success, message, vencimiento) = await _facturacionService.GuardarCertificadoAsync(nId, p12Bytes, password);

            return success
                ? Ok(new { success = true, message, vencimiento })
                : BadRequest(new { success = false, message });
        });

    [HttpGet("ValidarCertificado")]
    public Task<IActionResult> ValidarCertificado(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var (valid, message, vencimiento) = await _facturacionService.ValidarCertificadoAsync(nId);
            return Ok(new { valid, message, vencimiento });
        });

    // ═══════════════════════════════════════════════════════════
    // EMISION DE FACTURAS
    // ═══════════════════════════════════════════════════════════

    [HttpPost("EmitirFactura")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> EmitirFactura([FromBody] FacturaRequest request)
        => Execute(request.NegocioId, async nId =>
        {
            request.NegocioId = nId;
            var (success, message, factura) = await _facturacionService.EmitirFacturaAsync(request);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new
            {
                success = true,
                message,
                facturaId = factura.FacturaId,
                numeroCompleto = factura.NumeroCompleto,
                claveAcceso = factura.ClaveAcceso
            });
        });

    [HttpPost("EmitirFacturaDesdeRecibo")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> EmitirFacturaDesdeRecibo([FromBody] FacturaRequest request)
        => Execute(request.NegocioId, async nId =>
        {
            request.NegocioId = nId;
            var (success, message, factura) = await _facturacionService.EmitirFacturaAsync(request);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new
            {
                success = true,
                message,
                facturaId = factura.FacturaId,
                numeroCompleto = factura.NumeroCompleto,
                claveAcceso = factura.ClaveAcceso
            });
        });

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    [HttpGet("GetFacturas")]
    public Task<IActionResult> GetFacturas(Guid negocioId, DateTime? desde = null, DateTime? hasta = null, string estado = null)
        => Execute(negocioId, async nId =>
        {
            var facturas = await _facturacionService.GetFacturasAsync(nId, desde, hasta, estado);
            return Ok(facturas.Select(f => new
            {
                f.FacturaId,
                f.NumeroCompleto,
                f.FechaEmision,
                f.CompradorRazonSocial,
                f.CompradorIdentificacion,
                f.CompradorTipoIdentificacion,
                f.ImporteTotal,
                f.TotalSinImpuestos,
                f.MontoIva,
                f.EstadoSri,
                f.NumeroAutorizacion,
                f.FechaAutorizacion,
                f.IntentosEnvio,
                f.ClaveAcceso,
                f.FormaPagoSri
            }));
        });

    [HttpGet("GetFactura")]
    public Task<IActionResult> GetFactura(Guid negocioId, Guid facturaId)
        => Execute(negocioId, async nId =>
        {
            var f = await _facturacionService.GetFacturaAsync(nId, facturaId);
            if (f == null) return NotFound(new { success = false, message = "Factura no encontrada" });
            return Ok(f);
        });

    [HttpGet("GetEstadisticasFacturacion")]
    public Task<IActionResult> GetEstadisticasFacturacion(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var stats = await _facturacionService.GetEstadisticasAsync(nId);
            return Ok(stats);
        });

    // ═══════════════════════════════════════════════════════════
    // DESCARGAS
    // ═══════════════════════════════════════════════════════════

    [HttpGet("DescargarFacturaXml")]
    public Task<IActionResult> DescargarFacturaXml(Guid negocioId, Guid facturaId)
        => Execute(negocioId, async nId =>
        {
            var xml = await _facturacionService.GetFacturaXmlAsync(nId, facturaId);
            if (xml == null) return NotFound(new { success = false, message = "XML no disponible" });

            var factura = await _facturacionService.GetFacturaAsync(nId, facturaId);
            return File(xml, "application/xml", $"factura_{factura?.NumeroCompleto ?? facturaId.ToString()}.xml");
        });

    [HttpGet("DescargarFacturaRide")]
    public Task<IActionResult> DescargarFacturaRide(Guid negocioId, Guid facturaId)
        => Execute(negocioId, async nId =>
        {
            var pdf = await _facturacionService.GetFacturaRideAsync(nId, facturaId);
            if (pdf == null) return NotFound(new { success = false, message = "RIDE no disponible" });

            var factura = await _facturacionService.GetFacturaAsync(nId, facturaId);
            return File(pdf, "application/pdf", $"RIDE_{factura?.NumeroCompleto ?? facturaId.ToString()}.pdf");
        });

    // ═══════════════════════════════════════════════════════════
    // ACCIONES
    // ═══════════════════════════════════════════════════════════

    [HttpPost("EnviarFacturaEmail")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> EnviarFacturaEmail(Guid negocioId, Guid facturaId)
        => Execute(negocioId, async nId =>
        {
            var (success, message) = await _facturacionService.EnviarFacturaPorEmailAsync(nId, facturaId);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        });

    [HttpPost("ReintentarFactura")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> ReintentarFactura(Guid negocioId, Guid facturaId)
        => Execute(negocioId, async nId =>
        {
            var (success, message) = await _facturacionService.ReenviarFacturaAsync(nId, facturaId);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        });

    [HttpPost("AnularFactura")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> AnularFactura(Guid negocioId, Guid facturaId)
        => Execute(negocioId, async nId =>
        {
            var (success, message) = await _facturacionService.AnularFacturaAsync(nId, facturaId);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        });
}
