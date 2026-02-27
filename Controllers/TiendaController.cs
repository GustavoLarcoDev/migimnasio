// ═══════════════════════════════════════════════════════════════════════════════
// TiendaController.cs — Catalogo publico y envio de marketing (modelo Tienda)
//
// Este controlador gestiona el catalogo de productos para negocios tipo "tienda".
// Funcionalidades principales:
//   - Ver catalogo publico (sin autenticacion, accesible por link compartido)
//   - Descargar catalogo como PDF (via "Imprimir > Guardar como PDF" del navegador)
//   - Enviar catalogo por email o WhatsApp a clientes potenciales
//
// La ruta base es "/Tienda" (diferente a "/Negocios" de los demas controladores)
// porque el catalogo publico necesita una URL limpia y descriptiva para compartir
// con clientes que no estan autenticados en el sistema.
// ═══════════════════════════════════════════════════════════════════════════════

using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Tienda")]
[Authorize]
public class TiendaController : Controller
{
    // ── Dependencias inyectadas ──────────────────────────────────────────────
    private readonly IAuthService _authService;
    private readonly ICatalogoService _catalogoService;
    private readonly IEmailService _emailService;

    public TiendaController(
        IAuthService authService,
        ICatalogoService catalogoService,
        IEmailService emailService)
    {
        _authService = authService;
        _catalogoService = catalogoService;
        _emailService = emailService;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SECCION 1 — VISUALIZACION DEL CATALOGO
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Muestra el catalogo publico de productos del negocio como pagina HTML.
    /// Este endpoint es [AllowAnonymous] porque los clientes finales acceden
    /// al catalogo a traves de un link compartido sin necesidad de cuenta.
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyo catalogo se quiere ver</param>
    /// <param name="estilo">Estilo visual: moderno, elegante, minimalista, vibrante o clasico</param>
    [HttpGet("VerCatalogo")]
    [AllowAnonymous]
    public async Task<IActionResult> VerCatalogo(Guid negocioId, string estilo = "moderno")
    {
        var html = await _catalogoService.GenerarCatalogoHtmlAsync(negocioId, estilo);

        // Si el negocio no existe, CatalogoService devuelve HTML con mensaje de error
        if (html.Contains("Negocio no encontrado"))
        {
            Response.StatusCode = 404;
            return Content(html, "text/html");
        }

        return Content(html, "text/html");
    }

    /// <summary>
    /// Genera el catalogo con un script de auto-impresion inyectado.
    /// Al abrir esta URL el navegador muestra el dialogo "Imprimir" automaticamente,
    /// donde el usuario puede elegir "Guardar como PDF" (funciona en todos los navegadores).
    /// </summary>
    [HttpGet("DescargarCatalogo")]
    public async Task<IActionResult> DescargarCatalogo(string estilo = "moderno")
    {
        var nId = _authService.GetNegocioId(User);
        if (nId == null) return Forbid();

        var html = await _catalogoService.GenerarCatalogoHtmlAsync(nId.Value, estilo);
        // Inyectar script que dispara window.print() al cargar la pagina
        html = html.Replace("</body>", "<script>window.onload=function(){setTimeout(function(){window.print();},500);}</script></body>");
        return Content(html, "text/html");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SECCION 2 — ENVIO DE CATALOGO (marketing por email y WhatsApp)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Envia el link del catalogo a un cliente por email o WhatsApp.
    /// Por email: envia un correo HTML con boton para ver el catalogo.
    /// Por WhatsApp: envia un mensaje de texto con el link directo.
    /// Protegido con [ValidateAntiForgeryToken] porque modifica estado (envio de mensajes).
    /// </summary>
    [HttpPost("EnviarCatalogo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarCatalogo([FromForm] string destino, [FromForm] string tipo, [FromForm] string estilo = "moderno")
    {
        var nId = _authService.GetNegocioId(User);
        if (nId == null) return Forbid();

        // Obtener el nombre del negocio desde los claims para personalizar el mensaje
        var companyName = User.Identity?.Name ?? "My-Negocio";

        if (string.IsNullOrWhiteSpace(destino) || string.IsNullOrWhiteSpace(tipo))
            return Json(new { success = false, message = "Datos de destino inválidos." });

        if (tipo == "email")
        {
            // Construir la URL publica del catalogo para incluirla en el correo
            var host = Request.Host.Value;
            var scheme = Request.Scheme;
            var catalogUrl = $"{scheme}://{host}/Tienda/VerCatalogo?negocioId={nId.Value}&estilo={estilo}";

            // Plantilla HTML del correo con boton de accion
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

        return Json(new { success = false, message = "Método de envío no soportado." });
    }
}
