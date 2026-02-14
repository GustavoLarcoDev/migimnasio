namespace Gimnasio.Models;

/// <summary>
/// Configuración para la API de WhatsApp Business (Meta Cloud API)
/// </summary>
public class WhatsAppSettings
{
    public string PhoneNumberId { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string ApiVersion { get; set; } = "v21.0";
    public bool Enabled { get; set; } = false;
}
