// ═══════════════════════════════════════════════════════════════════════════
// WhatsAppService.cs — Integración con Meta Cloud API (WhatsApp Business)
//
// CÓMO FUNCIONA WHATSAPP BUSINESS API:
//   Meta (Facebook) ofrece una API REST para enviar mensajes de WhatsApp desde
//   aplicaciones. Para usarla necesitas:
//     1. Una cuenta de Meta Business verificada
//     2. Un número de teléfono registrado como WhatsApp Business
//     3. Un Access Token permanente (generado en Meta Developer Console)
//     4. El PhoneNumberId del número (no es el número en sí, es un ID interno de Meta)
//
//   El endpoint de envío es:
//   POST https://graph.facebook.com/{ApiVersion}/{PhoneNumberId}/messages
//   Authorization: Bearer {AccessToken}
//   Content-Type: application/json
//
// CONFIGURACIÓN EN appsettings.json:
//   "WhatsAppSettings": {
//     "Enabled": true,
//     "AccessToken": "EAABsm...",    ← Token de Meta (muy largo, ~200 caracteres)
//     "PhoneNumberId": "12345678",   ← ID del número en Meta (NO el número en sí)
//     "ApiVersion": "v19.0"          ← Versión de la Graph API
//   }
//
// NORMALIZACIÓN DE TELÉFONOS:
//   La API de WhatsApp exige números en formato internacional sin el símbolo +:
//   Correcto:   593987654321  (Ecuador, sin el +)
//   Incorrecto: +593987654321 (con el +)
//   Incorrecto: 0987654321    (con el 0 local de Ecuador)
//
//   El método LimpiarTelefono() convierte automáticamente cualquier formato:
//   "0987 654-321" → eliminar espacios/guiones → "0987654321" → quitar 0 → "593987654321"
//
// ARQUITECTURA DE UN SOLO NÚMERO:
//   Todos los mensajes de todos los negocios salen desde el mismo número de
//   WhatsApp de My-Negocio. Esto es intencional:
//   - Los negocios pequeños no tienen presupuesto para su propia cuenta Business
//   - My-Negocio actúa como intermediario de comunicación
//   - Los mensajes siempre mencionan el nombre del negocio para contexto
//
// IHttpClientFactory:
//   Usamos IHttpClientFactory en lugar de crear HttpClient directamente porque:
//   - HttpClient tiene problemas conocidos de agotamiento de sockets si se crea/destruye
//   - La fábrica gestiona el pool de conexiones HTTP reutilizándolas de forma segura
//   - Se configura en Program.cs con: builder.Services.AddHttpClient("WhatsApp")
// ═══════════════════════════════════════════════════════════════════════════

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gimnasio.Models;
using Microsoft.Extensions.Options;

namespace Gimnasio.Services;

/// <summary>
/// Implementación del servicio de mensajería de WhatsApp Business usando Meta Cloud API.
/// Un solo número de WhatsApp (de la plataforma My-Negocio) envía todos los mensajes:
/// recordatorios de membresía, resúmenes diarios y confirmaciones de citas.
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly WhatsAppSettings    _settings;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly IHttpClientFactory  _httpClientFactory;

    /// <summary>
    /// Constructor con inyección de dependencia.
    /// <paramref name="settings"/> proviene de appsettings.json sección "WhatsAppSettings".
    /// <paramref name="httpClientFactory"/> gestiona el pool de conexiones HTTP de forma segura.
    /// </summary>
    public WhatsAppService(
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _settings          = settings.Value;
        _logger            = logger;
        _httpClientFactory = httpClientFactory;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MÉTODOS PÚBLICOS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Envía un mensaje de texto plano a un número de teléfono.
    /// Es el método base usado por todos los demás métodos de este servicio.
    ///
    /// Flujo:
    ///   1. Si WhatsApp está deshabilitado en config → loguear y retornar true
    ///      (true porque no es un error, es configuración intencional en dev/staging)
    ///   2. Limpiar y normalizar el número de teléfono
    ///   3. Construir el payload JSON que espera la API de Meta
    ///   4. Enviar via EnviarPayloadAsync (que hace la llamada HTTP real)
    ///
    /// El payload JSON que se construye tiene esta forma:
    /// {
    ///   "messaging_product": "whatsapp",
    ///   "to": "593987654321",
    ///   "type": "text",
    ///   "text": { "body": "El mensaje aquí" }
    /// }
    /// </summary>
    public async Task<bool> EnviarMensajeTextoAsync(string telefono, string mensaje)
    {
        // Guard: WhatsApp puede estar deshabilitado en entornos de desarrollo
        // para evitar enviar mensajes reales durante pruebas
        if (!_settings.Enabled)
        {
            _logger.LogWarning("WhatsApp no está habilitado. Mensaje no enviado a {Telefono}", telefono);
            return true;  // true = no es un error, es una decisión de configuración
        }

        // Normalizar el número de teléfono al formato que exige Meta API
        var telefonoLimpio = LimpiarTelefono(telefono);
        if (string.IsNullOrEmpty(telefonoLimpio))
        {
            _logger.LogWarning("Número de teléfono inválido o vacío: {Telefono}", telefono);
            return false;
        }

        // Estructura del payload según la especificación de Meta Cloud API
        // La propiedad "type": "text" indica que es un mensaje de texto simple,
        // a diferencia de templates, imágenes, documentos, etc.
        var payload = new
        {
            messaging_product = "whatsapp",
            to   = telefonoLimpio,
            type = "text",
            text = new { body = mensaje }
        };

        return await EnviarPayloadAsync(payload);
    }

    /// <summary>
    /// Envía un recordatorio de vencimiento de membresía al cliente.
    ///
    /// MENSAJE ADAPTATIVO según días restantes:
    ///   El switch expression de C# elige el texto según la urgencia:
    ///   - 1 día: mensaje urgente con "MAÑANA" en mayúsculas y emoji de advertencia
    ///   - 3 días: mensaje estándar de recordatorio anticipado
    ///   - Cualquier otro valor: formato genérico con el número de días
    ///
    /// AVISO AL FINAL DEL MENSAJE:
    ///   Se agrega un disclaimer explicando que el número es automatizado y que
    ///   si el cliente tiene dudas, debe contactar al negocio directamente.
    ///   Esto es importante porque el número que envía es el de My-Negocio,
    ///   no el del gimnasio/negocio — si el cliente intenta responder,
    ///   llegaría a My-Negocio y no al negocio que le envió el mensaje.
    ///
    /// FORMATO DE WHATSAPP:
    ///   *texto* = negrita
    ///   _texto_ = cursiva
    ///   Estos formatos funcionan nativamente en la app de WhatsApp.
    /// </summary>
    public async Task<bool> EnviarRecordatorioMembresiaAsync(
        string telefono, string nombreCliente, string nombreNegocio, int diasRestantes)
    {
        // Texto principal del recordatorio — varía según la urgencia
        var cuerpo = diasRestantes switch
        {
            1 => $"⚠️ Hola {nombreCliente}, tu membresía en *{nombreNegocio}* vence *MAÑANA*. Renueva para no perder tu acceso.",
            3 => $"📋 Hola {nombreCliente}, te recordamos que tu membresía en *{nombreNegocio}* vence en *3 días*. ¡No olvides renovar!",
            _ => $"📋 Hola {nombreCliente}, te recordamos que tu membresía en *{nombreNegocio}* vence en *{diasRestantes} días*. ¡No olvides renovar!"
        };

        // Disclaimer al final: el cliente debe saber que este número no es del negocio
        var mensaje = cuerpo + "\n\n" +
                      $"_Este es un mensaje automatizado. Por favor no responda a este número. " +
                      $"Si tiene alguna duda o pregunta, comuníquese directamente con *{nombreNegocio}*._";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <summary>
    /// Envía el resumen diario del negocio al dueño por WhatsApp.
    /// Se llama desde DailyReportService en un horario programado.
    ///
    /// FORMATO DEL MENSAJE:
    ///   Usa emojis y formato WhatsApp (*negrita*) para facilitar la lectura
    ///   rápida en el celular. El dueño puede ver el estado de su negocio
    ///   de un vistazo sin necesidad de abrir la aplicación web.
    ///
    ///   "— MiNegocio" al final identifica quién envía el resumen automático.
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

    // ═══════════════════════════════════════════════════════════════════════
    // RECORDATORIOS DE CITAS (MODELO ARTESANAL)
    // Estos mensajes son para negocios de tipo "artesanal" (peluquerías, spas,
    // consultorios, etc.) que tienen un sistema de citas con empleados asignados.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Envía un recordatorio de cita al CLIENTE antes de su turno.
    /// El mensaje incluye quién lo atenderá y la hora para que pueda planificarse.
    /// El disclaimer final es idéntico al de membresías: el número no es del negocio.
    /// </summary>
    public async Task<bool> EnviarRecordatorioCitaClienteAsync(
        string telefono, string nombreCliente, string nombreNegocio, string nombreEmpleado, string hora)
    {
        var mensaje = $"Hola {nombreCliente}, te recordamos la cita en *{nombreNegocio}* con {nombreEmpleado} a las *{hora}*. " +
                      $"Si deseas cancelar, comunícate con nosotros lo más rápido.\n\n" +
                      $"_Este es un mensaje automatizado. Por favor no responda a este número. " +
                      $"Si tiene alguna duda, comuníquese directamente con *{nombreNegocio}*._";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <summary>
    /// Envía un recordatorio de cita al DUEÑO del negocio.
    /// Es un mensaje administrativo interno: informa al dueño las citas programadas
    /// para que el negocio esté preparado con el personal y materiales necesarios.
    /// No incluye el disclaimer de "no responder" porque es un mensaje interno.
    /// </summary>
    public async Task<bool> EnviarRecordatorioCitaNegocioAsync(
        string telefono, string nombreDueno, string nombreServicio, string hora, string nombreEmpleado)
    {
        var mensaje = $"Hola {nombreDueno}, recuerda que tienes una cita para *{nombreServicio}* programada a las *{hora}* para {nombreEmpleado}.\n\n" +
                      $"— MiNegocio";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MÉTODOS PRIVADOS — COMUNICACIÓN HTTP CON META API
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Realiza la llamada HTTP POST a la API de Meta Cloud para enviar el mensaje.
    ///
    /// POR QUÉ IHttpClientFactory EN VEZ DE 'new HttpClient()':
    ///   Crear y destruir HttpClient en cada request agota los sockets del sistema
    ///   (un problema clásico de .NET conocido como "socket exhaustion").
    ///   IHttpClientFactory mantiene un pool de conexiones HTTP reutilizables.
    ///   Con CreateClient("WhatsApp") obtenemos un cliente pre-configurado
    ///   con el nombre "WhatsApp" definido en Program.cs (AddHttpClient).
    ///
    /// AUTENTICACIÓN CON BEARER TOKEN:
    ///   Meta API usa OAuth 2.0 Bearer tokens para autenticar.
    ///   El token se agrega al header Authorization en cada request:
    ///   "Authorization: Bearer EAABsm..."
    ///   El token es de larga duración (no expira frecuentemente), pero debe
    ///   mantenerse secreto (no subirlo a Git — usar appsettings secretos o variables de entorno).
    ///
    /// URL DE LA API:
    ///   https://graph.facebook.com/v19.0/{PhoneNumberId}/messages
    ///   - v19.0 = versión de la Graph API (puede actualizarse)
    ///   - PhoneNumberId = ID interno de Meta del número de WhatsApp Business
    ///     (diferente al número de teléfono real)
    /// </summary>
    private async Task<bool> EnviarPayloadAsync(object payload)
    {
        try
        {
            // Obtener un HttpClient del pool administrado por la fábrica
            var client = _httpClientFactory.CreateClient("WhatsApp");

            // URL del endpoint de mensajes de Meta Graph API
            var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages";

            // Serializar el payload a JSON y empaquetarlo como contenido HTTP
            var json    = JsonSerializer.Serialize(payload);

            // Usar HttpRequestMessage para establecer el header de autenticación por request
            // en lugar de DefaultRequestHeaders, que NO es thread-safe para escrituras concurrentes.
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.AccessToken);

            // Hacer el POST a la API de Meta
            var response     = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Errores comunes:
                //   400 = payload malformado (número inválido, campo faltante)
                //   401 = token de acceso expirado o inválido
                //   403 = número no verificado o sin permisos
                //   429 = límite de rate de la API superado (demasiados mensajes/segundo)
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
            // Excepción de red: servidor de Meta no disponible, timeout, DNS, etc.
            // No propagamos la excepción — un WhatsApp fallido no debe romper
            // el flujo principal de la aplicación.
            _logger.LogError(ex, "Excepción al enviar mensaje de WhatsApp");
            return false;
        }
    }

    /// <summary>
    /// Limpia y normaliza un número de teléfono para que cumpla el formato
    /// que exige la API de Meta: solo dígitos, sin "+", en formato internacional.
    ///
    /// TRANSFORMACIONES QUE APLICA:
    ///   1. Eliminar caracteres no deseados: espacios, guiones, paréntesis, símbolo +
    ///      Regex [\s\-\(\)\+] cubre todos estos casos de una sola pasada
    ///   2. Verificar que solo queden dígitos (si hay letras u otros chars, es inválido)
    ///   3. Si empieza con "0" → reemplazar con "593" (código de país Ecuador)
    ///      "0987654321" → "593987654321"
    ///      Nota: el código de país de Ecuador es +593. El 0 inicial es solo para
    ///      llamadas locales dentro de Ecuador, no es parte del número internacional.
    ///   4. Verificar longitud mínima de 7 dígitos (números locales muy cortos son inválidos)
    ///
    /// EJEMPLOS:
    ///   "+593 987 654-321"  →  "593987654321"  (válido)
    ///   "0987654321"        →  "593987654321"  (válido, código Ecuador agregado)
    ///   "(09) 876-5432"     →  "593987654321"  (válido, limpio y con código)
    ///   "abc123"            →  ""              (inválido, contiene letras)
    ///   "123"               →  ""              (inválido, muy corto)
    /// </summary>
    private string LimpiarTelefono(string telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            return string.Empty;

        // Paso 1: Eliminar todos los caracteres de formato (espacios, guiones, paréntesis, +)
        // El Regex reemplaza cualquier coincidencia con string vacío
        var limpio = Regex.Replace(telefono, @"[\s\-\(\)\+]", "");

        // Paso 2: Verificar que solo queden dígitos
        // Si hay letras u otros caracteres, el número es inválido
        if (!Regex.IsMatch(limpio, @"^\d+$"))
            return string.Empty;

        // Paso 3: Agregar código de país Ecuador si el número empieza con 0
        // (formato local ecuatoriano → formato internacional)
        if (limpio.StartsWith("0"))
            limpio = "593" + limpio.Substring(1);  // Reemplazar el 0 inicial con 593

        // Paso 4: Validar longitud mínima (números demasiado cortos no son válidos)
        if (limpio.Length < 7)
            return string.Empty;

        return limpio;
    }
}
