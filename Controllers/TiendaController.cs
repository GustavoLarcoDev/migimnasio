// ═══════════════════════════════════════════════════════════
// TiendaController.cs — Catálogo público y envío de marketing
// ═══════════════════════════════════════════════════════════

using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Tienda")]
[Authorize]
public class TiendaController : Controller
{
    private readonly IAuthService _authService;
    private readonly ICatalogoService _catalogoService;
    private readonly IWhatsAppService _whatsappService;
    private readonly IEmailService _emailService;

    public TiendaController(
        IAuthService authService,
        ICatalogoService catalogoService,
        IWhatsAppService whatsappService,
        IEmailService emailService)
    {
        _authService = authService;
        _catalogoService = catalogoService;
        _whatsappService = whatsappService;
        _emailService = emailService;
    }

    [HttpGet("VerCatalogo")]
    [AllowAnonymous]
    public async Task<IActionResult> VerCatalogo(Guid negocioId, string estilo = "moderno")
    {
        var html = await _catalogoService.GenerarCatalogoHtmlAsync(negocioId, estilo);
        return Content(html, "text/html");
    }

    [HttpGet("DescargarCatalogo")]
    public async Task<IActionResult> DescargarCatalogo(string estilo = "moderno")
    {
        var nId = _authService.GetNegocioId(User);
        if (nId == null) return Unauthorized();

        var html = await _catalogoService.GenerarCatalogoHtmlAsync(nId.Value, estilo);
        // Inject auto-print script — browser's "Save as PDF" option works cross-platform
        html = html.Replace("</body>", "<script>window.onload=function(){setTimeout(function(){window.print();},500);}</script></body>");
        return Content(html, "text/html");
    }

    [HttpPost("EnviarCatalogo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarCatalogo([FromForm] string destino, [FromForm] string tipo, [FromForm] string estilo = "moderno")
    {
        var nId = _authService.GetNegocioId(User);
        if (nId == null) return Unauthorized();
        var companyName = User.Claims.FirstOrDefault(c => c.Type == "NegocioNombre")?.Value ?? "Mi Negocio";

        if (string.IsNullOrWhiteSpace(destino) || string.IsNullOrWhiteSpace(tipo))
            return Json(new { success = false, message = "Datos de destino inválidos." });

        if (tipo == "email")
        {
            var host = Request.Host.Value;
            var scheme = Request.Scheme;
            var catalogUrl = $"{scheme}://{host}/Tienda/VerCatalogo?negocioId={nId.Value}&estilo={estilo}";
            var htmlBody = $@"<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
                <div style='background:linear-gradient(135deg,#3E97FF,#1B74E4);padding:30px;border-radius:12px;text-align:center;'>
                    <h1 style='color:white;margin:0;font-size:24px;'>{System.Net.WebUtility.HtmlEncode(companyName)}</h1>
                    <p style='color:rgba(255,255,255,0.9);margin:8px 0 0;'>Te comparte su catalogo de productos</p>
                </div>
                <div style='padding:30px;text-align:center;'>
                    <p style='color:#333;font-size:16px;'>Haz clic en el boton para ver el catalogo completo:</p>
                    <a href='{catalogUrl}' style='display:inline-block;padding:14px 40px;background:#3E97FF;color:white;text-decoration:none;border-radius:8px;font-weight:bold;font-size:16px;margin:16px 0;'>Ver Catalogo</a>
                </div>
                <p style='color:#999;font-size:12px;text-align:center;'>Enviado a traves de My-Negocio</p>
            </div>";
            var enviado = await _emailService.EnviarReciboPorEmailGenericoAsync(destino, $"Catalogo de {companyName}", htmlBody);
            return Json(new { success = enviado, message = enviado ? "Catalogo enviado por correo exitosamente." : "Hubo un error al enviar el correo." });
        }
        else if (tipo == "whatsapp")
        {
            var host = Request.Host.Value;
            var scheme = Request.Scheme;
            var url = $"{scheme}://{host}/Tienda/VerCatalogo?negocioId={nId.Value}&estilo={estilo}";

            var enviado = await _whatsappService.EnviarLinkCatalogoTiendaAsync(destino, companyName, url);
            return Json(new { success = enviado, message = enviado ? "Link del catálogo enviado por WhatsApp." : "Hubo un error al enviar el WhatsApp." });
        }

        return Json(new { success = false, message = "Método de envío no soportado." });
    }
}
