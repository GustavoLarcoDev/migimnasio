// ═══════════════════════════════════════════════════════════
// WhatsAppSettings.cs — Configuración de Twilio WhatsApp API
//
// Esta clase se usa para leer la sección "WhatsAppSettings" de appsettings.json.
// Se inyecta en WhatsAppService via IOptions<WhatsAppSettings>.
//
// La plataforma usa Twilio WhatsApp API para enviar:
//   - Recordatorios de membresía próxima a vencer a los clientes.
//   - Recordatorios de citas agendadas (negocios artesanales).
//   - Recibos de pago, bienvenidas y resúmenes diarios.
//
// Para obtener estas credenciales:
//   1. Crea una cuenta en twilio.com
//   2. Activa el sandbox de WhatsApp o registra un número propio
//   3. Copia el Account SID, Auth Token y el número From del dashboard
//
// Configuración en appsettings.json:
//   "WhatsAppSettings": {
//     "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
//     "AuthToken": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
//     "FromNumber": "whatsapp:+14155238886",
//     "Enabled": true
//   }
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Models;

/// <summary>
/// Contiene la configuración necesaria para conectarse a Twilio WhatsApp API.
/// Esta clase no es una entidad de base de datos; es un objeto de configuración
/// que se lee desde appsettings.json y se inyecta con el patrón IOptions de ASP.NET Core.
///
/// El servicio WhatsAppService usa estas credenciales para enviar mensajes programáticos
/// a los clientes de los negocios registrados en la plataforma.
/// </summary>
public class WhatsAppSettings
{
    /// <summary>
    /// Account SID de Twilio. Es un identificador único de la cuenta.
    /// Formato: "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" (34 caracteres, empieza con "AC").
    /// Se obtiene en el dashboard principal de Twilio (https://console.twilio.com).
    /// Se usa como username en la autenticación Basic Auth de la API.
    /// Valor por defecto: cadena vacía (la integración no funciona sin este valor).
    /// </summary>
    public string AccountSid { get; set; } = "";

    /// <summary>
    /// Auth Token de Twilio. Es la contraseña de la cuenta para la API.
    /// Es un string alfanumérico de 32 caracteres.
    /// Se obtiene en el dashboard principal de Twilio junto al Account SID.
    /// IMPORTANTE: tratar este token como contraseña; no incluir en repositorios públicos.
    /// Usar variables de entorno o appsettings.Production.json para producción.
    /// Valor por defecto: cadena vacía.
    /// </summary>
    public string AuthToken { get; set; } = "";

    /// <summary>
    /// Número de teléfono de origen registrado en Twilio para WhatsApp.
    /// Formato: "whatsapp:+14155238886" (incluye el prefijo "whatsapp:" y código de país con +).
    /// En modo sandbox, Twilio asigna un número compartido.
    /// En producción, se usa un número propio aprobado por Twilio/WhatsApp.
    /// Valor por defecto: cadena vacía.
    /// </summary>
    public string FromNumber { get; set; } = "";

    /// <summary>
    /// Interruptor principal de la integración con WhatsApp.
    /// true  = el sistema enviará mensajes de WhatsApp cuando corresponda.
    /// false = la integración está desactivada; no se enviarán mensajes (modo silencioso).
    ///         Útil para entornos de desarrollo y pruebas donde no se quieren enviar mensajes reales.
    /// Valor por defecto: false (desactivado por seguridad; se activa explícitamente en producción).
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Content Template SIDs de Twilio para mensajes aprobados por Meta/WhatsApp.
    /// Sin templates aprobados, solo se pueden enviar mensajes dentro de la ventana de 24h.
    /// Con templates, los mensajes se pueden enviar en cualquier momento (business-initiated).
    /// </summary>
    public WhatsAppTemplates Templates { get; set; } = new();
}

/// <summary>
/// Mapea los Content SIDs de los templates de Twilio registrados para WhatsApp Business.
/// Cada propiedad corresponde a un tipo de mensaje y contiene el SID del template (HXxxxxxxxxx).
/// </summary>
public class WhatsAppTemplates
{
    public string RecordatorioMembresia { get; set; } = "";
    public string ConfirmacionCita { get; set; } = "";
    public string RecordatorioCita { get; set; } = "";
    public string RecordatorioCobro { get; set; } = "";
    public string ResumenDiario { get; set; } = "";
    public string ResumenDiarioGeneral { get; set; } = "";
    public string RecordatorioCitaNegocio { get; set; } = "";
    public string RecordatorioCitaEmpleado { get; set; } = "";
    public string BienvenidaVendedor { get; set; } = "";
    public string BienvenidaNegocio { get; set; } = "";
    public string AdvertenciaSuscripcion { get; set; } = "";
    public string NegocioBloqueado { get; set; } = "";
    public string NegocioDesbloqueado { get; set; } = "";
    public string Promocion { get; set; } = "";
}
