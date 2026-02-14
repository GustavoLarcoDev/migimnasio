// ═══════════════════════════════════════════════════════════
// WhatsAppService.cs — Implementación del servicio de WhatsApp
// Envía TODOS los mensajes desde UN solo número (el del admin)
// configurado en appsettings.json → WhatsAppSettings.
// Los mensajes se personalizan con el nombre del negocio.
// ═══════════════════════════════════════════════════════════

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gimnasio.Models;
using Microsoft.Extensions.Options;

namespace Gimnasio.Services;

/// <summary>
/// Implementación del servicio de mensajería de WhatsApp Business usando Meta Cloud API.
/// Un solo número de WhatsApp (admin) envía todo: resúmenes a dueños y recordatorios a clientes.
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public WhatsAppService(
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PÚBLICOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Envía un mensaje de texto plano a un número de teléfono
    /// </summary>
    public async Task<bool> EnviarMensajeTextoAsync(string telefono, string mensaje)
    {
        if (!_settings.Enabled)
        {
            _logger.LogWarning("WhatsApp no está habilitado. Mensaje no enviado a {Telefono}", telefono);
            return true;
        }

        var telefonoLimpio = LimpiarTelefono(telefono);
        if (string.IsNullOrEmpty(telefonoLimpio))
        {
            _logger.LogWarning("Número de teléfono inválido o vacío: {Telefono}", telefono);
            return false;
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to = telefonoLimpio,
            type = "text",
            text = new { body = mensaje }
        };

        return await EnviarPayloadAsync(payload);
    }

    /// <summary>
    /// Envía un recordatorio de membresía al cliente.
    /// El mensaje incluye el nombre del negocio para que el cliente sepa quién le escribe.
    /// </summary>
    public async Task<bool> EnviarRecordatorioMembresiaAsync(
        string telefono, string nombreCliente, string nombreNegocio, int diasRestantes)
    {
        var cuerpo = diasRestantes switch
        {
            1 => $"⚠️ Hola {nombreCliente}, tu membresía en *{nombreNegocio}* vence *MAÑANA*. Renueva para no perder tu acceso.",
            3 => $"📋 Hola {nombreCliente}, te recordamos que tu membresía en *{nombreNegocio}* vence en *3 días*. ¡No olvides renovar!",
            _ => $"📋 Hola {nombreCliente}, te recordamos que tu membresía en *{nombreNegocio}* vence en *{diasRestantes} días*. ¡No olvides renovar!"
        };

        var mensaje = cuerpo + "\n\n" +
                      $"_Este es un mensaje automatizado. Por favor no responda a este número. " +
                      $"Si tiene alguna duda o pregunta, comuníquese directamente con *{nombreNegocio}*._";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <summary>
    /// Envía un resumen diario al dueño del negocio
    /// </summary>
    public async Task<bool> EnviarResumenDiarioAsync(
        string telefono, string nombreNegocio, decimal ingresosDia, int nuevosClientes, int porVencerManana)
    {
        var mensaje = $"📊 *Resumen del día — {nombreNegocio}*\n\n" +
                      $"💰 Ingresos: ${ingresosDia:N2}\n" +
                      $"👤 Nuevos clientes: {nuevosClientes}\n" +
                      $"⏰ Por vencer mañana: {porVencerManana}\n\n" +
                      $"— MiNegocio";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PRIVADOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Envía el payload JSON a la API de Meta Cloud (WhatsApp Business)
    /// </summary>
    private async Task<bool> EnviarPayloadAsync(object payload)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("WhatsApp");
            var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages";

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.AccessToken);

            var response = await client.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Error WhatsApp API. Código: {StatusCode}, Respuesta: {ResponseBody}",
                    (int)response.StatusCode, responseBody);
                return false;
            }

            _logger.LogInformation("Mensaje de WhatsApp enviado exitosamente");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al enviar mensaje de WhatsApp");
            return false;
        }
    }

    /// <summary>
    /// Limpia y normaliza un número de teléfono para la API de WhatsApp.
    /// Si comienza con "0", lo reemplaza con el código de país de Ecuador (593).
    /// </summary>
    private string LimpiarTelefono(string telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            return string.Empty;

        var limpio = Regex.Replace(telefono, @"[\s\-\(\)\+]", "");

        if (!Regex.IsMatch(limpio, @"^\d+$"))
            return string.Empty;

        if (limpio.StartsWith("0"))
            limpio = "593" + limpio.Substring(1);

        if (limpio.Length < 7)
            return string.Empty;

        return limpio;
    }
}
