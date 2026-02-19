#nullable enable

using Gimnasio.Helpers;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Gimnasio.Services;

/// <summary>
/// Configuracion SMTP leida desde appsettings.json bajo la seccion "EmailSettings".
/// </summary>
public class EmailSettings
{
    public string SmtpHost  { get; set; } = "smtp.gmail.com";
    public int    SmtpPort  { get; set; } = 587;
    public string FromEmail { get; set; } = "";
    public string FromName  { get; set; } = "My-Negocio";
    public string Password  { get; set; } = "";
}

/// <summary>
/// Servicio de correo electronico transaccional para My-Negocio.
/// Envia correos de bienvenida con credenciales y guias adjuntas en Word (.docx).
/// Usa MailKit para conectarse a Gmail via SMTP con STARTTLS.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings       _settings;
    private readonly ILogger<EmailService> _logger;

    private static readonly string[] FrasesVendedor = new[]
    {
        "El exito no es la clave de la felicidad. La felicidad es la clave del exito.",
        "Cada venta que cierras es una familia que transformas.",
        "Los grandes logros requieren grandes equipos. Bienvenido al nuestro.",
        "El unico limite es el que te pones tu mismo. A conquistar el mercado!",
        "No vendemos software, vendemos el futuro de los negocios.",
        "Hoy empieza un nuevo capitulo. Hazlo memorable.",
        "El talento gana partidos, pero el trabajo en equipo gana campeonatos.",
        "Cree en ti, nosotros ya creemos en ti."
    };

    private static readonly string[] FrasesNegocio = new[]
    {
        "Un negocio organizado es un negocio que crece.",
        "La tecnologia es el mejor aliado de un emprendedor con vision.",
        "Cada cliente satisfecho es la mejor publicidad que existe.",
        "El orden es el primer paso hacia el exito.",
        "Automatiza lo repetitivo y enfocate en lo que importa: tus clientes.",
        "Los negocios que se adaptan son los que perduran.",
        "Tu negocio merece las mejores herramientas. Aqui las tienes.",
        "El futuro de tu negocio empieza hoy."
    };

    public EmailService(Microsoft.Extensions.Options.IOptions<EmailSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger   = logger;
    }

    // ═══════════════════════════════════════════════════════════
    // METODO CENTRAL — ENVIO SMTP CON ADJUNTOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Envia un email con cuerpo HTML y opcionalmente un archivo adjunto.
    /// </summary>
    private async Task<bool> EnviarEmailAsync(string destinatario, string subject, string htmlBody,
        byte[]? attachmentBytes = null, string? attachmentFileName = null)
    {
        if (string.IsNullOrEmpty(_settings.Password) || _settings.Password.Contains("YOUR_"))
        {
            _logger.LogWarning("EmailService: No se ha configurado la App Password de Gmail. Correo no enviado.");
            return false;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(destinatario));
            message.Subject = subject;

            // Si hay adjunto, construir mensaje multipart; si no, solo HTML
            if (attachmentBytes != null && !string.IsNullOrEmpty(attachmentFileName))
            {
                var builder = new BodyBuilder();
                builder.HtmlBody = htmlBody;
                builder.Attachments.Add(attachmentFileName, attachmentBytes,
                    new ContentType("application", "vnd.openxmlformats-officedocument.wordprocessingml.document"));
                message.Body = builder.ToMessageBody();
            }
            else
            {
                message.Body = new TextPart("html") { Text = htmlBody };
            }

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.FromEmail, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email enviado exitosamente a {Email} (adjunto: {HasAttachment})",
                destinatario, attachmentBytes != null);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando email a {Email}", destinatario);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CORREO DE BIENVENIDA — VENDEDOR
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Envia el correo de bienvenida a un nuevo vendedor con:
    /// - Sus credenciales de acceso (correo y contrasena)
    /// - Documento de guia para vendedores (.docx) adjunto
    /// </summary>
    public async Task<bool> EnviarBienvenidaVendedorAsync(string destinatario, string nombreVendedor, string correo, string password)
    {
        var frase = FrasesVendedor[Random.Shared.Next(FrasesVendedor.Length)];

        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff6b35, #f7931e); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 28px;'>Bienvenido a My-Negocio</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0; font-size: 16px;'>Equipo de Ventas</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none;'>
        <h2 style='color: #333;'>Hola {nombreVendedor}!</h2>
        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
            Estamos muy agradecidos de que te unas a nuestro equipo de vendedores.
            Tu talento y dedicacion son exactamente lo que necesitamos para seguir creciendo juntos.
        </p>

        <div style='background: #f8f9fa; border: 2px solid #ff6b35; border-radius: 8px; padding: 20px; margin: 25px 0;'>
            <h3 style='color: #ff6b35; margin: 0 0 15px 0; font-size: 18px;'>Tus Credenciales de Acceso</h3>
            <table style='width: 100%; font-size: 15px;'>
                <tr>
                    <td style='padding: 5px 0; color: #666; font-weight: bold;'>Email:</td>
                    <td style='padding: 5px 0; color: #333;'>{correo}</td>
                </tr>
                <tr>
                    <td style='padding: 5px 0; color: #666; font-weight: bold;'>Contrasena:</td>
                    <td style='padding: 5px 0; color: #333; font-family: monospace; font-size: 16px;'>{password}</td>
                </tr>
            </table>
            <p style='color: #dc3545; font-size: 13px; margin: 12px 0 0 0;'>
                * Te recomendamos cambiar tu contrasena despues del primer inicio de sesion.
            </p>
        </div>

        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
            Tambien puedes iniciar sesion con tu numero de telefono en lugar de tu correo.
        </p>

        <div style='background: #fff8f0; border-left: 4px solid #ff6b35; padding: 15px 20px; margin: 25px 0; border-radius: 0 8px 8px 0;'>
            <p style='color: #333; font-size: 16px; font-style: italic; margin: 0;'>
                &ldquo;{frase}&rdquo;
            </p>
        </div>

        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
            Adjunto encontraras el <strong>Manual del Vendedor</strong> con toda la informacion que necesitas para empezar.
        </p>

        <p style='color: #555; font-size: 16px;'>
            Con mucho entusiasmo,<br>
            <strong>El equipo de My-Negocio</strong>
        </p>
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px; border-radius: 0 0 12px 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

        // Generar el documento Word de guia para vendedores
        byte[]? guideDoc = null;
        try
        {
            guideDoc = GuideDocumentGenerator.GenerarGuiaVendedor(nombreVendedor, correo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generando guia Word para vendedor. Se enviara email sin adjunto.");
        }

        return await EnviarEmailAsync(destinatario, "Bienvenido al equipo de My-Negocio!",
            body, guideDoc, "Manual_Vendedor_MyNegocio.docx");
    }

    // ═══════════════════════════════════════════════════════════
    // CORREO DE BIENVENIDA — NEGOCIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Envia el correo de bienvenida al dueno de un negocio con:
    /// - Sus credenciales de acceso (email, telefono, contrasena)
    /// - Documento de guia para negocios (.docx) adjunto
    /// </summary>
    public async Task<bool> EnviarBienvenidaNegocioAsync(string destinatario, string nombreNegocio, string nombreDueno,
        string emailNegocio, string passwordNegocio, string telefonoNegocio, string? nombreVendedor)
    {
        var frase = FrasesNegocio[Random.Shared.Next(FrasesNegocio.Length)];
        var creadoPorAdmin = string.IsNullOrEmpty(nombreVendedor);

        string saludo, mensaje, despedida;

        if (creadoPorAdmin)
        {
            saludo = $"Hola {nombreDueno}!";
            mensaje = $@"
        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
            Le damos la mas calurosa bienvenida a <strong>My-Negocio</strong>. Estamos muy agradecidos
            de que haya elegido nuestra plataforma para gestionar <strong>{nombreNegocio}</strong>.
        </p>
        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
            Cualquier pregunta, duda o sugerencia que tenga, estare muy feliz de atenderlo personalmente.
            Su exito es nuestra prioridad.
        </p>";
            despedida = @"
        <p style='color: #555; font-size: 16px;'>
            Con mucho gusto,<br>
            <strong>Gustavo Larco</strong><br>
            <span style='color: #888;'>C.E.O de My-Negocio.com</span>
        </p>";
        }
        else
        {
            saludo = $"Hola {nombreDueno}!";
            mensaje = $@"
        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
            Queremos agradecerle de corazon por su confianza, su tiempo y su compra.
            Bienvenido a <strong>My-Negocio</strong>! Estamos emocionados de que
            <strong>{nombreNegocio}</strong> sea parte de nuestra familia.
        </p>
        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
            Su asesor <strong>{nombreVendedor}</strong> estara disponible para ayudarle en todo
            lo que necesite. No dude en contactarlo.
        </p>";
            despedida = $@"
        <p style='color: #555; font-size: 16px;'>
            Con mucho agradecimiento,<br>
            <strong>{nombreVendedor}</strong><br>
            <span style='color: #888;'>Asesor de My-Negocio</span>
        </p>";
        }

        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff6b35, #f7931e); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 28px;'>Bienvenido a My-Negocio</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0; font-size: 16px;'>{nombreNegocio}</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none;'>
        <h2 style='color: #333;'>{saludo}</h2>
        {mensaje}

        <div style='background: #f8f9fa; border: 2px solid #ff6b35; border-radius: 8px; padding: 20px; margin: 25px 0;'>
            <h3 style='color: #ff6b35; margin: 0 0 15px 0; font-size: 18px;'>Sus Credenciales de Acceso</h3>
            <table style='width: 100%; font-size: 15px;'>
                <tr>
                    <td style='padding: 5px 0; color: #666; font-weight: bold;'>Email:</td>
                    <td style='padding: 5px 0; color: #333;'>{emailNegocio}</td>
                </tr>
                <tr>
                    <td style='padding: 5px 0; color: #666; font-weight: bold;'>Telefono:</td>
                    <td style='padding: 5px 0; color: #333;'>{telefonoNegocio}</td>
                </tr>
                <tr>
                    <td style='padding: 5px 0; color: #666; font-weight: bold;'>Contrasena:</td>
                    <td style='padding: 5px 0; color: #333; font-family: monospace; font-size: 16px;'>{passwordNegocio}</td>
                </tr>
            </table>
            <p style='color: #dc3545; font-size: 13px; margin: 12px 0 0 0;'>
                * Puede iniciar sesion con su email o su numero de telefono.
            </p>
            <p style='color: #dc3545; font-size: 13px; margin: 5px 0 0 0;'>
                * Le recomendamos cambiar su contrasena despues del primer inicio de sesion.
            </p>
        </div>

        <div style='background: #fff8f0; border-left: 4px solid #ff6b35; padding: 15px 20px; margin: 25px 0; border-radius: 0 8px 8px 0;'>
            <p style='color: #333; font-size: 16px; font-style: italic; margin: 0;'>
                &ldquo;{frase}&rdquo;
            </p>
        </div>

        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
            Adjunto encontrara la <strong>Guia de Inicio</strong> con toda la informacion para comenzar a usar My-Negocio.
        </p>

        {despedida}
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px; border-radius: 0 0 12px 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

        // Generar el documento Word de guia para negocios
        byte[]? guideDoc = null;
        try
        {
            guideDoc = GuideDocumentGenerator.GenerarGuiaNegocio(nombreNegocio, nombreDueno, emailNegocio, telefonoNegocio);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generando guia Word para negocio. Se enviara email sin adjunto.");
        }

        return await EnviarEmailAsync(destinatario, $"Bienvenido a My-Negocio, {nombreDueno}!",
            body, guideDoc, $"Guia_Inicio_MyNegocio.docx");
    }
}
