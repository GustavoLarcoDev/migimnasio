// ═══════════════════════════════════════════════════════════════════════════
// WhatsAppService.cs — Integración con Twilio WhatsApp API (Templates)
//
// ARQUITECTURA: Usa Content Templates aprobados por Meta para enviar
// mensajes fuera de la ventana de 24h. Cada tipo de mensaje tiene
// su template con variables dinámicas ({{1}}, {{2}}, etc.).
//
// Si un template no está configurado, cae a envío de texto libre
// (solo funciona si el destinatario escribió en las últimas 24h).
//
// CONFIGURACIÓN EN appsettings.json:
//   "WhatsAppSettings": {
//     "Enabled": true,
//     "AccountSid": "ACxxxxxxxx...",
//     "AuthToken": "xxxxxxxx...",
//     "FromNumber": "whatsapp:+593997143142",
//     "Templates": { "RecordatorioMembresia": "HXxxxxxxxx", ... }
//   }
// ═══════════════════════════════════════════════════════════════════════════

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gimnasio.Models;
using Microsoft.Extensions.Options;

namespace Gimnasio.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly WhatsAppSettings    _settings;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly IHttpClientFactory  _httpClientFactory;

    public WhatsAppService(
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _settings          = settings.Value;
        _logger            = logger;
        _httpClientFactory = httpClientFactory;
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTODO BASE (texto libre — solo funciona dentro de ventana 24h)
    // ═══════════════════════════════════════════════════════════

    /// <inheritdoc />
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

        return await EnviarMensajeTwilioAsync(telefonoLimpio, body: mensaje);
    }

    // ═══════════════════════════════════════════════════════════
    // MENSAJES A CLIENTES (usan templates)
    // ═══════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> EnviarRecordatorioMembresiaAsync(
        string telefono, string nombreCliente, string nombreNegocio, int diasRestantes, string telefonoNegocio = null)
    {
        var vencimiento = diasRestantes switch
        {
            1 => "vence MAÑANA",
            _ => $"vence en {diasRestantes} días"
        };

        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.RecordatorioMembresia,
            new Dictionary<string, string>
            {
                ["1"] = nombreCliente,
                ["2"] = nombreNegocio,
                ["3"] = vencimiento
            },
            // Fallback si template no aprobado
            $"Hola {nombreCliente}, te recordamos que tu membresía en {nombreNegocio} {vencimiento}. Renueva para mantener tu acceso."
        );
    }

    /// <inheritdoc />
    public async Task<bool> EnviarConfirmacionReservaWhatsAppAsync(
        string telefono, string cliente, string negocio, string servicio, string empleado, DateTime fechaHora, decimal precio, string telefonoNegocio = null)
    {
        var fecha = fechaHora.ToString("dd/MM/yyyy");
        var hora = fechaHora.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);

        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.ConfirmacionCita,
            new Dictionary<string, string>
            {
                ["1"] = cliente,
                ["2"] = negocio,
                ["3"] = servicio,
                ["4"] = empleado,
                ["5"] = fecha,
                ["6"] = hora,
                ["7"] = precio.ToString("N2")
            },
            $"Cita Confirmada\n\nHola {cliente}, tu cita en {negocio} está confirmada:\n\nServicio: {servicio}\nCon: {empleado}\nFecha: {fecha}\nHora: {hora}\nPrecio: ${precio:N2}"
        );
    }

    /// <inheritdoc />
    public async Task<bool> EnviarRecordatorioCitaClienteAsync(
        string telefono, string nombreCliente, string nombreNegocio, string nombreEmpleado, string hora, string telefonoNegocio = null)
    {
        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.RecordatorioCita,
            new Dictionary<string, string>
            {
                ["1"] = nombreCliente,
                ["2"] = nombreNegocio,
                ["3"] = nombreEmpleado,
                ["4"] = hora
            },
            $"Hola {nombreCliente}, te recordamos tu cita en {nombreNegocio} con {nombreEmpleado} a las {hora}. Si deseas cancelar, comunícate con el negocio."
        );
    }

    /// <inheritdoc />
    public async Task<bool> EnviarRecordatorioCobroWhatsAppAsync(
        string telefono, string nombreCliente, string nombreNegocio, int diasVencido, string telefonoNegocio = null)
    {
        var estado = diasVencido switch
        {
            0 => "venció hoy",
            1 => "venció ayer",
            _ => $"venció hace {diasVencido} días"
        };

        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.RecordatorioCobro,
            new Dictionary<string, string>
            {
                ["1"] = nombreCliente,
                ["2"] = nombreNegocio,
                ["3"] = estado
            },
            $"Recordatorio de Pago\n\nHola {nombreCliente}, tu membresía en {nombreNegocio} {estado}. Te invitamos a renovar para seguir disfrutando de nuestros servicios."
        );
    }

    // ═══════════════════════════════════════════════════════════
    // MENSAJES AL DUEÑO/ADMIN (usan templates)
    // ═══════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> EnviarResumenDiarioAsync(
        string telefono, string nombreNegocio, decimal ingresosDia, int nuevosClientes, int porVencerManana)
    {
        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.ResumenDiario,
            new Dictionary<string, string>
            {
                ["1"] = nombreNegocio,
                ["2"] = ingresosDia.ToString("N2"),
                ["3"] = nuevosClientes.ToString(),
                ["4"] = porVencerManana.ToString()
            },
            $"Resumen del día — {nombreNegocio}\n\nIngresos: ${ingresosDia:N2}\nNuevos clientes: {nuevosClientes}\nPor vencer mañana: {porVencerManana}\n\n— My-Negocio"
        );
    }

    /// <inheritdoc />
    public async Task<bool> EnviarResumenDiarioGeneralWhatsAppAsync(
        string telefono, string negocio, decimal ingresos, decimal gastos, decimal ganancia, int nuevosClientes)
    {
        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.ResumenDiarioGeneral,
            new Dictionary<string, string>
            {
                ["1"] = negocio,
                ["2"] = ingresos.ToString("N2"),
                ["3"] = gastos.ToString("N2"),
                ["4"] = ganancia.ToString("N2"),
                ["5"] = nuevosClientes.ToString()
            },
            $"Resumen del día — {negocio}\n\nIngresos: ${ingresos:N2}\nGastos: ${gastos:N2}\nGanancia: ${ganancia:N2}\nNuevos clientes: {nuevosClientes}\n\n— My-Negocio"
        );
    }

    /// <inheritdoc />
    public async Task<bool> EnviarRecordatorioCitaNegocioAsync(
        string telefono, string nombreDueno, string nombreServicio, string hora, string nombreEmpleado)
    {
        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.RecordatorioCitaNegocio,
            new Dictionary<string, string>
            {
                ["1"] = nombreDueno,
                ["2"] = nombreServicio,
                ["3"] = hora,
                ["4"] = nombreEmpleado
            },
            $"Hola {nombreDueno}, recuerda que tienes una cita para {nombreServicio} programada a las {hora} con {nombreEmpleado}.\n\n— My-Negocio"
        );
    }

    /// <inheritdoc />
    public async Task<bool> EnviarRecordatorioCitaEmpleadoWhatsAppAsync(
        string telefono, string empleado, string cliente, string servicio, string hora)
    {
        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.RecordatorioCitaEmpleado,
            new Dictionary<string, string>
            {
                ["1"] = empleado,
                ["2"] = cliente,
                ["3"] = servicio,
                ["4"] = hora
            },
            $"Recordatorio de Cita\n\nHola {empleado}, tienes una cita próxima:\n\nCliente: {cliente}\nServicio: {servicio}\nHora: {hora}\n\n— My-Negocio"
        );
    }

    // ═══════════════════════════════════════════════════════════
    // MENSAJES PAREADOS CON EMAIL (usan templates)
    // ═══════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> EnviarBienvenidaVendedorWhatsAppAsync(
        string telefono, string nombre, string email, string password)
    {
        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.BienvenidaVendedor,
            new Dictionary<string, string>
            {
                ["1"] = nombre,
                ["2"] = email,
                ["3"] = password
            },
            $"Bienvenido al equipo de ventas de My-Negocio, {nombre}.\n\nAccede al panel de vendedor con:\nEmail: {email}\nClave: {password}\n\nIngresa en app.mi-negocio.net\n\n— My-Negocio"
        );
    }

    /// <inheritdoc />
    public async Task<bool> EnviarBienvenidaNegocioWhatsAppAsync(
        string telefono, string negocio, string dueno, string email, string password, string tipoNegocio)
    {
        var tipo = tipoNegocio switch
        {
            "artesanal" => "Servicios y Citas",
            "tienda" => "Tienda e Inventario",
            "restaurante" => "Restaurante",
            _ => "Membresías"
        };

        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.BienvenidaNegocio,
            new Dictionary<string, string>
            {
                ["1"] = dueno,
                ["2"] = negocio,
                ["3"] = tipo,
                ["4"] = email,
                ["5"] = password
            },
            $"Bienvenido a My-Negocio, {dueno}.\n\nTu negocio {negocio} ({tipo}) ya está listo.\n\nEmail: {email}\nClave: {password}\n\nIngresa en app.mi-negocio.net\n\nSi tienes dudas, escríbenos.\n\n— My-Negocio"
        );
    }

    // ═══════════════════════════════════════════════════════════
    // NOTIFICACIONES DE SUSCRIPCIÓN Y NEGOCIO (usan templates)
    // ═══════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> EnviarAdvertenciaSuscripcionWhatsAppAsync(
        string telefono, string negocio, string dueno, int diasRestantes, DateTime fechaExpiracion)
    {
        var fecha = fechaExpiracion.ToString("dd/MM/yyyy");

        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.AdvertenciaSuscripcion,
            new Dictionary<string, string>
            {
                ["1"] = dueno,
                ["2"] = negocio,
                ["3"] = diasRestantes.ToString(),
                ["4"] = fecha
            },
            $"Recordatorio de Suscripción\n\nHola {dueno}, la suscripción de {negocio} en My-Negocio vence en {diasRestantes} día(s) ({fecha}).\n\nRenueva para seguir usando el sistema sin interrupciones.\n\n— My-Negocio"
        );
    }

    /// <inheritdoc />
    public async Task<bool> EnviarNotificacionNegocioBloqueadoWhatsAppAsync(
        string telefono, string negocio, string dueno)
    {
        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.NegocioBloqueado,
            new Dictionary<string, string>
            {
                ["1"] = dueno,
                ["2"] = negocio
            },
            $"Negocio Suspendido\n\nHola {dueno}, tu negocio {negocio} ha sido suspendido en My-Negocio por vencimiento de suscripción.\n\nComunícate con nosotros para restaurar tu acceso.\n\n— My-Negocio"
        );
    }

    /// <inheritdoc />
    public async Task<bool> EnviarNotificacionNegocioDesbloqueadoWhatsAppAsync(
        string telefono, string negocio, string dueno)
    {
        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.NegocioDesbloqueado,
            new Dictionary<string, string>
            {
                ["1"] = dueno,
                ["2"] = negocio
            },
            $"Negocio Reactivado\n\nHola {dueno}, tu negocio {negocio} ha sido reactivado en My-Negocio.\n\nYa puedes acceder normalmente al sistema.\n\n— My-Negocio"
        );
    }

    // ═══════════════════════════════════════════════════════════
    // MARKETING / PROMOCIÓN
    // ═══════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> EnviarPromocionWhatsAppAsync(string telefono, string nombreNegocio)
    {
        var mensajePreLlenado = Uri.EscapeDataString(
            "Hola, vi lo de My-Negocio.com y me interesa saber más sobre el sistema con IA para mi negocio. ¿Me pueden dar más información?");
        var linkWhatsApp = $"https://wa.me/593988757851?text={mensajePreLlenado}";

        var fallback =
            $"Hola *{nombreNegocio}* 👋\n\n" +
            "Te saludamos de *My-Negocio.com*\n\n" +
            "Somos un sistema con *Inteligencia Artificial* diseñado para negocios en Ecuador que *automatiza todo* por ti:\n\n" +
            "🤖 *IA que trabaja por ti* — reportes automáticos, recordatorios a clientes y análisis inteligente sin que hagas nada\n" +
            "📲 *WhatsApp automático* — tu negocio le escribe a tus clientes solo, recordándoles citas, pagos y más\n" +
            "📊 *Dashboard en tiempo real* — ventas, clientes, inventario, citas, todo en un solo lugar desde tu celular\n" +
            "💰 *Control total* — ingresos, gastos, membresías, empleados y reportes diarios automáticos\n\n" +
            "Estamos en *precio de lanzamiento* 🚀\n\n" +
            $"Escríbenos al link o *responde este mensaje* para que un agente se ponga en contacto contigo:\n👉 {linkWhatsApp}\n\n" +
            "— *My-Negocio.com* | Sistema inteligente para tu negocio";

        return await EnviarConTemplateAsync(
            telefono,
            _settings.Templates.Promocion,
            new Dictionary<string, string>
            {
                ["1"] = nombreNegocio
            },
            fallback
        );
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PRIVADOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Envía un mensaje usando Content Template si está configurado,
    /// o cae a texto libre como fallback.
    /// </summary>
    private async Task<bool> EnviarConTemplateAsync(
        string telefono, string contentSid, Dictionary<string, string> variables, string fallbackBody)
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

        // Si hay template configurado, enviar con ContentSid + ContentVariables
        if (!string.IsNullOrWhiteSpace(contentSid))
        {
            var varsJson = JsonSerializer.Serialize(variables);
            return await EnviarMensajeTwilioAsync(telefonoLimpio, contentSid: contentSid, contentVariables: varsJson);
        }

        // Fallback: texto libre (solo funciona dentro de ventana 24h)
        _logger.LogWarning("Template no configurado, usando texto libre (requiere ventana 24h)");
        return await EnviarMensajeTwilioAsync(telefonoLimpio, body: fallbackBody);
    }

    /// <summary>
    /// Realiza la llamada HTTP POST a la API de Twilio para enviar el mensaje de WhatsApp.
    /// Soporta dos modos: texto libre (Body) o template (ContentSid + ContentVariables).
    /// Incluye retry con backoff exponencial y respeto de Retry-After header.
    /// </summary>
    private async Task<bool> EnviarMensajeTwilioAsync(
        string telefonoLimpio, string body = null, string contentSid = null, string contentVariables = null)
    {
        const int maxAttempts = 3;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("WhatsApp");
                var url = $"https://api.twilio.com/2010-04-01/Accounts/{_settings.AccountSid}/Messages.json";

                var authBytes = Encoding.ASCII.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}");
                var authHeader = Convert.ToBase64String(authBytes);

                // Construir form data según el modo de envío
                var formFields = new List<KeyValuePair<string, string>>
                {
                    new("From", _settings.FromNumber),
                    new("To", $"whatsapp:+{telefonoLimpio}")
                };

                if (!string.IsNullOrWhiteSpace(contentSid))
                {
                    // Modo template: ContentSid + ContentVariables
                    formFields.Add(new("ContentSid", contentSid));
                    if (!string.IsNullOrWhiteSpace(contentVariables))
                        formFields.Add(new("ContentVariables", contentVariables));
                }
                else if (!string.IsNullOrWhiteSpace(body))
                {
                    // Modo texto libre
                    formFields.Add(new("Body", body));
                }

                var formData = new FormUrlEncodedContent(formFields);

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = formData;
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.Created || response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Mensaje de WhatsApp enviado exitosamente vía Twilio");
                    return true;
                }

                if ((int)response.StatusCode == 429 && attempt < maxAttempts - 1)
                {
                    var retryAfterSeconds = 0;
                    if (response.Headers.RetryAfter?.Delta.HasValue == true)
                        retryAfterSeconds = (int)response.Headers.RetryAfter.Delta.Value.TotalSeconds;

                    var delaySeconds = retryAfterSeconds > 0 ? retryAfterSeconds : (int)Math.Pow(2, attempt + 1);
                    _logger.LogWarning(
                        "Twilio WhatsApp API rate limited (429). Reintentando en {Delay}s (intento {Attempt}/{Max})...",
                        delaySeconds, attempt + 1, maxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    continue;
                }

                if (attempt < maxAttempts - 1)
                {
                    var delaySeconds = (int)Math.Pow(2, attempt + 1);
                    _logger.LogWarning(
                        "Error Twilio WhatsApp API. Código: {StatusCode} (intento {Attempt}/{Max}). Reintentando en {Delay}s...",
                        (int)response.StatusCode, attempt + 1, maxAttempts, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
                else
                {
                    _logger.LogError(
                        "Error Twilio WhatsApp API tras {Max} intentos. Código: {StatusCode}, Respuesta: {ResponseBody}",
                        maxAttempts, (int)response.StatusCode, responseBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (attempt < maxAttempts - 1)
                {
                    var delaySeconds = (int)Math.Pow(2, attempt + 1);
                    _logger.LogWarning(ex,
                        "Excepción Twilio WhatsApp (intento {Attempt}/{Max}). Reintentando en {Delay}s...",
                        attempt + 1, maxAttempts, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
                else
                {
                    _logger.LogError(ex, "Excepción al enviar WhatsApp vía Twilio tras {Max} intentos", maxAttempts);
                    return false;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Limpia y normaliza un número de teléfono para formato internacional.
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
