using System.Security.Cryptography;
using System.Text;
using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gimnasio.Controllers;

[Route("Webhooks")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class WebhookController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly WhatsAppSettings _whatsAppSettings;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        ApplicationDbContext context,
        IOptions<WhatsAppSettings> whatsAppSettings,
        ILogger<WebhookController> logger)
    {
        _context = context;
        _whatsAppSettings = whatsAppSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Webhook de Twilio para mensajes WhatsApp entrantes.
    /// Cuando un destinatario responde a una promoción, Twilio envía
    /// un POST aquí con From, Body, ProfileName. Se crea un LeadVendedor
    /// con Origen="whatsapp" para que aparezca en el dashboard de vendedores.
    /// </summary>
    [HttpPost("TwilioWhatsApp")]
    [EnableRateLimiting("webhook")]
    public async Task<IActionResult> TwilioWhatsApp()
    {
        try
        {
            // Validar firma HMAC de Twilio
            var signature = Request.Headers["X-Twilio-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Webhook Twilio recibido sin firma X-Twilio-Signature");
                return Ok();
            }

            // Leer form fields
            var form = await Request.ReadFormAsync();
            var from = form["From"].FirstOrDefault() ?? "";
            var body = form["Body"].FirstOrDefault() ?? "";
            var profileName = form["ProfileName"].FirstOrDefault() ?? "Sin nombre";

            // Validar firma
            var requestUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
            if (!ValidarFirmaTwilio(requestUrl, form, signature))
            {
                _logger.LogWarning("Webhook Twilio con firma inválida desde {From}", from);
                return Ok();
            }

            // Limpiar teléfono: quitar "whatsapp:" y "+"
            var telefono = from
                .Replace("whatsapp:", "", StringComparison.OrdinalIgnoreCase)
                .Replace("+", "")
                .Trim();

            if (string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("Webhook Twilio con datos vacíos: From={From}, Body vacío={BodyEmpty}", from, string.IsNullOrWhiteSpace(body));
                return Ok();
            }

            // Anti-duplicados: si existe lead mismo teléfono + origen=whatsapp en últimos 5 min, ignorar
            var hace5Min = TimeHelper.Now.AddMinutes(-5);
            var duplicado = await _context.LeadsVendedor.AnyAsync(l =>
                l.Telefono == telefono
                && l.Origen == "whatsapp"
                && l.FechaCreacion >= hace5Min);

            if (duplicado)
            {
                _logger.LogInformation("Webhook Twilio: lead duplicado ignorado para {Telefono}", telefono);
                return Ok();
            }

            // Crear lead
            var lead = new LeadVendedor
            {
                Id = Guid.NewGuid(),
                Nombre = profileName.Length > 120 ? profileName[..120] : profileName,
                NombreNegocio = "WhatsApp",
                Email = null,
                Telefono = telefono,
                Mensaje = body.Length > 1200 ? body[..1200] : body,
                Origen = "whatsapp",
                FechaCreacion = TimeHelper.Now
            };

            _context.LeadsVendedor.Add(lead);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Lead WhatsApp creado: {Nombre} ({Telefono})", profileName, telefono);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando webhook Twilio WhatsApp");
        }

        // Siempre retornar 200 para que Twilio no reintente
        return Ok();
    }

    /// <summary>
    /// Valida la firma HMAC-SHA1 de Twilio.
    /// URL + params ordenados alfabéticamente → HMAC-SHA1 con AuthToken → Base64 → comparar con header.
    /// </summary>
    private bool ValidarFirmaTwilio(string url, IFormCollection form, string expectedSignature)
    {
        if (string.IsNullOrEmpty(_whatsAppSettings.AuthToken))
            return false;

        // Construir string para firmar: URL + params ordenados alfabéticamente concatenados
        var sortedParams = form.Keys
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        var dataToSign = new StringBuilder(url);
        foreach (var key in sortedParams)
        {
            dataToSign.Append(key);
            dataToSign.Append(form[key].FirstOrDefault() ?? "");
        }

        // HMAC-SHA1 con AuthToken
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_whatsAppSettings.AuthToken));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign.ToString()));
        var computedSignature = Convert.ToBase64String(hash);

        return string.Equals(computedSignature, expectedSignature, StringComparison.Ordinal);
    }
}
