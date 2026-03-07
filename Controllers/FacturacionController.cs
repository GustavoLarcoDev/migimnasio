using Gimnasio.Data;
using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

/// <summary>
/// Controller de facturación electrónica SRI Ecuador.
/// Hereda de NegocioBaseController para multi-tenant auth.
/// </summary>
public class FacturacionController : NegocioBaseController
{
    private readonly IFacturacionElectronicaService _facturacionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationDbContext _context;

    public FacturacionController(
        IAuthService authService,
        IFacturacionElectronicaService facturacionService,
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext context) : base(authService)
    {
        _facturacionService = facturacionService;
        _httpClientFactory = httpClientFactory;
        _context = context;
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
            var negocio = await _context.Negocios.FindAsync(nId);
            if (negocio == null) return NotFound();

            negocio.FacturacionElectronicaActiva = activa;
            await _context.SaveChangesAsync();

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
            var negocio = await _context.Negocios.FindAsync(nId);
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

            await _context.SaveChangesAsync();
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

            // Limitar tamaño del certificado a 1MB
            if (certificado.Length > 1_048_576)
                return BadRequest(new { success = false, message = "El certificado no puede exceder 1MB" });

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

    // ═══════════════════════════════════════════════════════════
    // BUSQUEDA SRI & RECIBO PARA FACTURA
    // ═══════════════════════════════════════════════════════════

    [HttpGet("BuscarContribuyenteSri")]
    public Task<IActionResult> BuscarContribuyenteSri(string identificacion)
        => ExecuteSelf(async _ =>
        {
            if (string.IsNullOrWhiteSpace(identificacion))
                return BadRequest(new { success = false, message = "Identificación requerida" });

            identificacion = identificacion.Trim();

            // Validar que solo contenga dígitos
            if (!System.Text.RegularExpressions.Regex.IsMatch(identificacion, @"^\d+$"))
                return BadRequest(new { success = false, message = "La identificación solo debe contener dígitos" });

            // Si es cédula (10 dígitos), convertir a RUC añadiendo "001"
            var rucConsulta = identificacion.Length == 10 ? identificacion + "001" : identificacion;

            if (rucConsulta.Length != 13)
                return Ok(new { success = false, message = "Debe ser cédula (10 dígitos) o RUC (13 dígitos)" });

            try
            {
                var client = _httpClientFactory.CreateClient("SRI");
                var url = $"https://srienlinea.sri.gob.ec/sri-catastro-sujeto-servicio-internet/rest/ConsolidadoContribuyente/obtenerPorNumerosRuc?&ruc={Uri.EscapeDataString(rucConsulta)}";

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return Ok(new { success = false, message = "Contribuyente no encontrado en el SRI" });

                var json = await response.Content.ReadAsStringAsync();
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                // El endpoint retorna un array o un objeto con los datos del contribuyente
                System.Text.Json.JsonElement data;
                if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0)
                    data = root[0];
                else if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                    data = root;
                else
                    return Ok(new { success = false, message = "Contribuyente no encontrado en el SRI" });

                var razonSocial = data.TryGetProperty("razonSocial", out var rs) ? rs.GetString() : "";
                var tipoId = identificacion.Length == 10 ? "05" : "04"; // 05=Cédula, 04=RUC

                return Ok(new
                {
                    success = true,
                    razonSocial,
                    tipoIdentificacion = tipoId,
                    identificacion,
                    estado = data.TryGetProperty("estadoContribuyenteRuc", out var est) ? est.GetString() : ""
                });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService<ILogger<FacturacionController>>();
                logger?.LogWarning(ex, "Error consultando SRI para identificación {Id}", identificacion);
                return Ok(new { success = false, message = "Error al consultar el SRI. Intente de nuevo." });
            }
        });

    [HttpGet("GetReciboParaFactura")]
    public Task<IActionResult> GetReciboParaFactura(Guid reciboId, Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            // Verificar que no tenga factura ya
            var yaFacturado = await _context.FacturasElectronicas
                .AnyAsync(f => f.ReciboId == reciboId && f.NegocioId == nId);
            if (yaFacturado)
                return BadRequest(new { success = false, message = "Este recibo ya tiene una factura electrónica vinculada" });

            var recibo = await _context.Recibos
                .Where(r => r.ReciboId == reciboId && r.NegocioId == nId)
                .FirstOrDefaultAsync();
            if (recibo == null)
                return NotFound(new { success = false, message = "Recibo no encontrado" });

            // Buscar si hay una OrdenVenta vinculada a este recibo
            var orden = await _context.OrdenesVenta
                .Include(o => o.Detalles).ThenInclude(d => d.Producto)
                .Where(o => o.ReciboId == reciboId && o.NegocioId == nId)
                .FirstOrDefaultAsync();

            var items = new List<object>();

            if (orden != null && orden.Detalles.Any())
            {
                foreach (var d in orden.Detalles)
                {
                    items.Add(new
                    {
                        descripcion = d.Producto?.Nombre ?? "Producto",
                        cantidad = (decimal)d.Cantidad,
                        precioUnitario = d.PrecioUnitario,
                        descuento = 0m,
                        codigo = "001"
                    });
                }
            }
            else
            {
                // Fallback: usar concepto y monto del recibo como un solo item
                items.Add(new
                {
                    descripcion = recibo.Concepto ?? "Servicio",
                    cantidad = 1m,
                    precioUnitario = recibo.Monto,
                    descuento = 0m,
                    codigo = "001"
                });
            }

            return Ok(new
            {
                success = true,
                reciboId = recibo.ReciboId,
                items,
                destinatarioNombre = recibo.DestinatarioNombre ?? "",
                destinatarioEmail = recibo.DestinatarioEmail ?? ""
            });
        });
}
