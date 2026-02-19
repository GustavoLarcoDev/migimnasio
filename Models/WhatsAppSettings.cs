// ═══════════════════════════════════════════════════════════
// WhatsAppSettings.cs — Configuración de la API de WhatsApp Business
//
// Esta clase se usa para leer la sección "WhatsApp" de appsettings.json.
// Se inyecta en WhatsAppService via IOptions<WhatsAppSettings>.
//
// La plataforma usa la Meta Cloud API (WhatsApp Business API) para enviar:
//   - Recordatorios de membresía próxima a vencer a los clientes.
//   - Recordatorios de citas agendadas (negocios artesanales).
//
// Para obtener estas credenciales:
//   1. Crea una app en developers.facebook.com
//   2. Agrega el producto "WhatsApp"
//   3. Copia el PhoneNumberId y genera un AccessToken permanente.
//
// Configuración en appsettings.json:
//   "WhatsApp": {
//     "PhoneNumberId": "123456789",
//     "AccessToken": "EAAxxxxxxx...",
//     "ApiVersion": "v21.0",
//     "Enabled": true
//   }
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Models;

/// <summary>
/// Contiene la configuración necesaria para conectarse a la API de WhatsApp Business (Meta Cloud API).
/// Esta clase no es una entidad de base de datos; es un objeto de configuración
/// que se lee desde appsettings.json y se inyecta con el patrón IOptions de ASP.NET Core.
///
/// El servicio WhatsAppService usa estas credenciales para enviar mensajes programáticos
/// a los clientes de los negocios registrados en la plataforma.
/// </summary>
public class WhatsAppSettings
{
    /// <summary>
    /// ID del número de teléfono de WhatsApp Business registrado en Meta.
    /// Es un número numérico de varios dígitos, por ejemplo: "123456789012345".
    /// Se obtiene en el panel de Meta for Developers al configurar la app de WhatsApp.
    /// Se incluye en la URL del endpoint de la API: /v21.0/{PhoneNumberId}/messages
    /// Valor por defecto: cadena vacía (la integración no funciona sin este valor).
    /// </summary>
    public string PhoneNumberId { get; set; } = "";

    /// <summary>
    /// Token de autenticación para la API de Meta (WhatsApp Business API).
    /// Es un string largo que empieza con "EAA...".
    /// Se genera en el panel de Meta for Developers. Puede ser temporal (24h) o permanente.
    /// IMPORTANTE: tratar este token como contraseña; no incluir en repositorios públicos.
    /// Usar variables de entorno o appsettings.Production.json para producción.
    /// Valor por defecto: cadena vacía.
    /// </summary>
    public string AccessToken { get; set; } = "";

    /// <summary>
    /// Versión de la API de Meta Graph que se usará para las peticiones.
    /// Formato: "vXX.0" donde XX es el número de versión (ej: "v21.0", "v20.0").
    /// Meta actualiza la API periódicamente; se recomienda revisar la versión más reciente.
    /// Valor por defecto: "v21.0".
    /// </summary>
    public string ApiVersion { get; set; } = "v21.0";

    /// <summary>
    /// Interruptor principal de la integración con WhatsApp.
    /// true  = el sistema enviará mensajes de WhatsApp cuando corresponda.
    /// false = la integración está desactivada; no se enviarán mensajes (modo silencioso).
    ///         Útil para entornos de desarrollo y pruebas donde no se quieren enviar mensajes reales.
    /// Valor por defecto: false (desactivado por seguridad; se activa explícitamente en producción).
    /// </summary>
    public bool Enabled { get; set; } = false;
}
