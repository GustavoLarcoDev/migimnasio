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

        const int maxAttempts = 3;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
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
                if (attempt < maxAttempts - 1)
                {
                    var delaySeconds = (int)Math.Pow(2, attempt + 1);
                    _logger.LogWarning(ex,
                        "Error enviando email a {Email} (intento {Attempt}/{Max}). Reintentando en {Delay}s...",
                        destinatario, attempt + 1, maxAttempts, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
                else
                {
                    _logger.LogError(ex, "Error enviando email a {Email} tras {Max} intentos",
                        destinatario, maxAttempts);
                    return false;
                }
            }
        }

        return false;
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

    // ═══════════════════════════════════════════════════════════
    // HELPER PRIVADO — LINKS DE CONTACTO DEL NEGOCIO
    // ═══════════════════════════════════════════════════════════

    private static string GenerarLinksContacto(string? emailNegocio, string? telefonoNegocio)
    {
        var links = new List<string>();
        if (!string.IsNullOrWhiteSpace(emailNegocio))
            links.Add($"<a href='mailto:{emailNegocio}' style='color: #ff6b35; text-decoration: none;'>{emailNegocio}</a>");
        if (!string.IsNullOrWhiteSpace(telefonoNegocio))
        {
            var waPhone = telefonoNegocio.Replace("+", "").Replace(" ", "");
            links.Add($"<a href='https://wa.me/{waPhone}' style='color: #25D366; text-decoration: none; font-weight: bold;'>Contactar por WhatsApp</a>");
        }
        return links.Count > 0 ? string.Join(" &nbsp;|&nbsp; ", links) : "";
    }

    // ═══════════════════════════════════════════════════════════
    // REPORTE DIARIO — NEGOCIO DE MEMBRESIAS
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> EnviarReporteDiarioMembresiaAsync(string destinatario, string nombreNegocio, string nombreDueno,
        decimal ingresosDia, int nuevosClientes, int productosVendidos, decimal totalProductos,
        int clientesPorVencer, int devoluciones, decimal montoDevuelto, DateTime fecha)
    {
        var ingresoTotal = ingresosDia + totalProductos - montoDevuelto;

        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff6b35, #f7931e); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 26px;'>Reporte del Dia</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0; font-size: 15px;'>{nombreNegocio} &mdash; {fecha:dd/MM/yyyy}</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none;'>
        <p style='color: #555; font-size: 16px; margin: 0 0 20px;'>Hola <strong>{nombreDueno}</strong>, aqui esta tu resumen de hoy:</p>

        <div style='background: #fff8f0; border: 2px solid #ff6b35; border-radius: 8px; padding: 20px; margin-bottom: 20px; text-align: center;'>
            <p style='color: #888; font-size: 13px; margin: 0 0 4px;'>INGRESO NETO DEL DIA</p>
            <p style='color: #ff6b35; font-size: 34px; font-weight: bold; margin: 0;'>${ingresoTotal:F2}</p>
        </div>

        <table style='width: 100%; border-collapse: collapse; font-size: 15px;'>
            <tr style='border-bottom: 1px solid #f0f0f0;'>
                <td style='padding: 10px 5px; color: #555;'>Ingresos por membresias</td>
                <td style='padding: 10px 5px; color: #333; font-weight: bold; text-align: right;'>${ingresosDia:F2}</td>
            </tr>
            <tr style='border-bottom: 1px solid #f0f0f0;'>
                <td style='padding: 10px 5px; color: #555;'>Venta de productos ({productosVendidos} items)</td>
                <td style='padding: 10px 5px; color: #333; font-weight: bold; text-align: right;'>${totalProductos:F2}</td>
            </tr>
            <tr style='border-bottom: 1px solid #f0f0f0;'>
                <td style='padding: 10px 5px; color: #555;'>Nuevos clientes</td>
                <td style='padding: 10px 5px; color: #28a745; font-weight: bold; text-align: right;'>{nuevosClientes}</td>
            </tr>
            <tr style='border-bottom: 1px solid #f0f0f0;'>
                <td style='padding: 10px 5px; color: #555;'>Clientes por vencer (proximos 3 dias)</td>
                <td style='padding: 10px 5px; color: {(clientesPorVencer > 0 ? "#dc3545" : "#28a745")}; font-weight: bold; text-align: right;'>{clientesPorVencer}</td>
            </tr>
            <tr>
                <td style='padding: 10px 5px; color: #555;'>Devoluciones ({devoluciones})</td>
                <td style='padding: 10px 5px; color: #dc3545; font-weight: bold; text-align: right;'>-${montoDevuelto:F2}</td>
            </tr>
        </table>

        <p style='color: #999; font-size: 12px; margin: 25px 0 0; text-align: center;'>
            Este correo fue enviado automaticamente. No responda a este mensaje.
        </p>
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px; border-radius: 0 0 12px 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

        return await EnviarEmailAsync(destinatario, $"Reporte del dia {fecha:dd/MM/yyyy} - {nombreNegocio}", body);
    }

    // ═══════════════════════════════════════════════════════════
    // REPORTE DIARIO — NEGOCIO ARTESANAL
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> EnviarReporteDiarioArtesanalAsync(string destinatario, string nombreNegocio, string nombreDueno,
        decimal ingresosDia, int citasCompletadas, int citasCanceladas, int nuevosClientes,
        string servicioMasPopular, decimal totalProductos, DateTime fecha)
    {
        var ingresoTotal = ingresosDia + totalProductos;

        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff6b35, #f7931e); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 26px;'>Reporte del Dia</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0; font-size: 15px;'>{nombreNegocio} &mdash; {fecha:dd/MM/yyyy}</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none;'>
        <p style='color: #555; font-size: 16px; margin: 0 0 20px;'>Hola <strong>{nombreDueno}</strong>, aqui esta tu resumen de hoy:</p>

        <div style='background: #fff8f0; border: 2px solid #ff6b35; border-radius: 8px; padding: 20px; margin-bottom: 20px; text-align: center;'>
            <p style='color: #888; font-size: 13px; margin: 0 0 4px;'>INGRESO TOTAL DEL DIA</p>
            <p style='color: #ff6b35; font-size: 34px; font-weight: bold; margin: 0;'>${ingresoTotal:F2}</p>
        </div>

        <table style='width: 100%; border-collapse: collapse; font-size: 15px;'>
            <tr style='border-bottom: 1px solid #f0f0f0;'>
                <td style='padding: 10px 5px; color: #555;'>Ingresos por servicios</td>
                <td style='padding: 10px 5px; color: #333; font-weight: bold; text-align: right;'>${ingresosDia:F2}</td>
            </tr>
            <tr style='border-bottom: 1px solid #f0f0f0;'>
                <td style='padding: 10px 5px; color: #555;'>Venta de productos</td>
                <td style='padding: 10px 5px; color: #333; font-weight: bold; text-align: right;'>${totalProductos:F2}</td>
            </tr>
            <tr style='border-bottom: 1px solid #f0f0f0;'>
                <td style='padding: 10px 5px; color: #555;'>Citas completadas</td>
                <td style='padding: 10px 5px; color: #28a745; font-weight: bold; text-align: right;'>{citasCompletadas}</td>
            </tr>
            <tr style='border-bottom: 1px solid #f0f0f0;'>
                <td style='padding: 10px 5px; color: #555;'>Citas canceladas</td>
                <td style='padding: 10px 5px; color: {(citasCanceladas > 0 ? "#dc3545" : "#28a745")}; font-weight: bold; text-align: right;'>{citasCanceladas}</td>
            </tr>
            <tr style='border-bottom: 1px solid #f0f0f0;'>
                <td style='padding: 10px 5px; color: #555;'>Nuevos clientes</td>
                <td style='padding: 10px 5px; color: #28a745; font-weight: bold; text-align: right;'>{nuevosClientes}</td>
            </tr>
            <tr>
                <td style='padding: 10px 5px; color: #555;'>Servicio mas popular</td>
                <td style='padding: 10px 5px; color: #333; font-weight: bold; text-align: right;'>{servicioMasPopular}</td>
            </tr>
        </table>

        <p style='color: #999; font-size: 12px; margin: 25px 0 0; text-align: center;'>
            Este correo fue enviado automaticamente. No responda a este mensaje.
        </p>
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px; border-radius: 0 0 12px 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

        return await EnviarEmailAsync(destinatario, $"Reporte del dia {fecha:dd/MM/yyyy} - {nombreNegocio}", body);
    }

    // ═══════════════════════════════════════════════════════════
    // RECIBO DE PAGO — SUSCRIPCION SAAS DEL NEGOCIO
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool enviado, string htmlBody)> EnviarReciboPagoNegocioAsync(string destinatario, string nombreNegocio, string nombreDueno,
        int diasContratados, decimal precio, string? nombreVendedor, string? telefonoVendedor, string numeroRecibo)
    {
        var fechaPago  = TimeHelper.Now;
        var fechaVence = fechaPago.AddDays(diasContratados);
        var linksVendedor = "";
        if (!string.IsNullOrWhiteSpace(telefonoVendedor))
        {
            var waPhone = telefonoVendedor.Replace("+", "").Replace(" ", "");
            linksVendedor = $"<a href='https://wa.me/{waPhone}' style='color: #25D366; text-decoration: none; font-weight: bold;'>Contactar a {nombreVendedor} por WhatsApp</a>";
        }

        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff6b35, #f7931e); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 26px;'>Recibo de Pago</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0; font-size: 15px;'>Suscripcion My-Negocio</p>
        <p style='color: rgba(255,255,255,0.8); margin: 6px 0 0; font-size: 13px;'>#{numeroRecibo}</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none;'>
        <p style='color: #555; font-size: 16px; margin: 0 0 20px;'>Hola <strong>{nombreDueno}</strong>, confirmamos tu pago:</p>

        <table style='width: 100%; border-collapse: collapse; font-size: 15px; margin-bottom: 20px;'>
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Negocio</td>
                <td style='padding: 10px 12px; color: #333;'>{nombreNegocio}</td>
            </tr>
            <tr>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Fecha de pago</td>
                <td style='padding: 10px 12px; color: #333;'>{fechaPago:dd/MM/yyyy}</td>
            </tr>
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Dias contratados</td>
                <td style='padding: 10px 12px; color: #333;'>{diasContratados} dias</td>
            </tr>
            <tr>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Vence el</td>
                <td style='padding: 10px 12px; color: #333;'>{fechaVence:dd/MM/yyyy}</td>
            </tr>
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Vendedor</td>
                <td style='padding: 10px 12px; color: #333;'>{(string.IsNullOrWhiteSpace(nombreVendedor) ? "My-Negocio (Admin)" : nombreVendedor)}</td>
            </tr>
            <tr style='border-top: 2px solid #ff6b35;'>
                <td style='padding: 12px; color: #ff6b35; font-weight: bold; font-size: 17px;'>TOTAL PAGADO</td>
                <td style='padding: 12px; color: #ff6b35; font-weight: bold; font-size: 17px;'>${precio:F2}</td>
            </tr>
        </table>

        {(string.IsNullOrWhiteSpace(linksVendedor) ? "" : $"<p style='color: #555; font-size: 14px; text-align: center;'>{linksVendedor}</p>")}

        <p style='color: #888; font-size: 12px; margin: 20px 0 5px; text-align: center;'>
            Este documento NO es una factura legal. Si necesita una factura, contacte directamente al negocio.
        </p>
        <p style='color: #999; font-size: 12px; margin: 0; text-align: center;'>
            Este correo fue enviado automaticamente. No responda a este mensaje.
        </p>
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px; border-radius: 0 0 12px 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

        var enviado = await EnviarEmailAsync(destinatario, $"Recibo de pago #{numeroRecibo} - {nombreNegocio}", body);
        return (enviado, body);
    }

    // ═══════════════════════════════════════════════════════════
    // RECIBO DE COMISION — VENDEDOR
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool enviado, string htmlBody)> EnviarReciboComisionAsync(string destinatario, string nombreVendedor,
        decimal montoTotal, int cantidadNegocios, string detalleComisiones, string numeroRecibo)
    {
        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff6b35, #f7931e); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 26px;'>Recibo de Comision</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0; font-size: 15px;'>My-Negocio &mdash; {TimeHelper.Now:dd/MM/yyyy}</p>
        <p style='color: rgba(255,255,255,0.8); margin: 6px 0 0; font-size: 13px;'>#{numeroRecibo}</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none;'>
        <p style='color: #555; font-size: 16px; margin: 0 0 20px;'>Hola <strong>{nombreVendedor}</strong>, aqui esta el detalle de tu comision:</p>

        <div style='background: #fff8f0; border: 2px solid #ff6b35; border-radius: 8px; padding: 20px; margin-bottom: 20px; text-align: center;'>
            <p style='color: #888; font-size: 13px; margin: 0 0 4px;'>TOTAL A COBRAR</p>
            <p style='color: #ff6b35; font-size: 34px; font-weight: bold; margin: 0;'>${montoTotal:F2}</p>
            <p style='color: #888; font-size: 13px; margin: 6px 0 0;'>{cantidadNegocios} negocio{(cantidadNegocios != 1 ? "s" : "")}</p>
        </div>

        <div style='background: #f8f9fa; border-radius: 8px; padding: 20px; margin-bottom: 20px;'>
            <h3 style='color: #333; font-size: 15px; margin: 0 0 12px;'>Detalle de comisiones:</h3>
            <div style='color: #555; font-size: 14px; line-height: 1.8; white-space: pre-line;'>{detalleComisiones}</div>
        </div>

        <p style='color: #888; font-size: 12px; margin: 20px 0 5px; text-align: center;'>
            Este documento NO es una factura legal. Si necesita una factura, contacte directamente al negocio.
        </p>
        <p style='color: #999; font-size: 12px; margin: 0; text-align: center;'>
            Este correo fue enviado automaticamente. No responda a este mensaje.
        </p>
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px; border-radius: 0 0 12px 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

        var enviado = await EnviarEmailAsync(destinatario, $"Recibo de comision #{numeroRecibo} - {TimeHelper.Now:dd/MM/yyyy}", body);
        return (enviado, body);
    }

    // ═══════════════════════════════════════════════════════════
    // RECIBO DE PAGO — CLIENTE (MEMBRESIA)
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool enviado, string htmlBody)> EnviarReciboPagoClienteAsync(string destinatario, string nombreCliente, string nombreNegocio,
        string conceptoPago, decimal monto, int dias, string? emailNegocio, string? telefonoNegocio, string numeroRecibo)
    {
        var fechaPago  = TimeHelper.Now;
        var fechaVence = fechaPago.AddDays(dias);
        var linksContacto = GenerarLinksContacto(emailNegocio, telefonoNegocio);

        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff6b35, #f7931e); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 26px;'>Recibo de Pago</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0; font-size: 15px;'>{nombreNegocio}</p>
        <p style='color: rgba(255,255,255,0.8); margin: 6px 0 0; font-size: 13px;'>#{numeroRecibo}</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none;'>
        <p style='color: #555; font-size: 16px; margin: 0 0 20px;'>Hola <strong>{nombreCliente}</strong>, aqui esta tu recibo:</p>

        <table style='width: 100%; border-collapse: collapse; font-size: 15px; margin-bottom: 20px;'>
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Negocio</td>
                <td style='padding: 10px 12px; color: #333;'>{nombreNegocio}</td>
            </tr>
            <tr>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Concepto</td>
                <td style='padding: 10px 12px; color: #333;'>{conceptoPago}</td>
            </tr>
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Fecha de pago</td>
                <td style='padding: 10px 12px; color: #333;'>{fechaPago:dd/MM/yyyy}</td>
            </tr>
            <tr>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Dias</td>
                <td style='padding: 10px 12px; color: #333;'>{dias} dias</td>
            </tr>
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Vence el</td>
                <td style='padding: 10px 12px; color: #333;'>{fechaVence:dd/MM/yyyy}</td>
            </tr>
            <tr style='border-top: 2px solid #ff6b35;'>
                <td style='padding: 12px; color: #ff6b35; font-weight: bold; font-size: 17px;'>TOTAL PAGADO</td>
                <td style='padding: 12px; color: #ff6b35; font-weight: bold; font-size: 17px;'>${monto:F2}</td>
            </tr>
        </table>

        {(string.IsNullOrWhiteSpace(linksContacto) ? "" : $"<p style='color: #555; font-size: 14px; text-align: center; margin-bottom: 15px;'>{linksContacto}</p>")}

        <p style='color: #888; font-size: 12px; margin: 20px 0 5px; text-align: center;'>
            Este documento NO es una factura legal. Si necesita una factura, contacte directamente al negocio.
        </p>
        <p style='color: #999; font-size: 12px; margin: 0; text-align: center;'>
            Este correo fue enviado automaticamente. No responda a este mensaje.
        </p>
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px; border-radius: 0 0 12px 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

        var enviado = await EnviarEmailAsync(destinatario, $"Recibo de pago #{numeroRecibo} - {nombreNegocio}", body);
        return (enviado, body);
    }

    // ═══════════════════════════════════════════════════════════
    // CONFIRMACION DE RESERVA — NEGOCIO ARTESANAL
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> EnviarConfirmacionReservaAsync(string destinatario, string nombreCliente, string nombreNegocio,
        string nombreServicio, string? nombreEmpleado, DateTime fechaHora, int duracionMinutos,
        decimal precio, string? emailNegocio, string? telefonoNegocio)
    {
        var linksContacto = GenerarLinksContacto(emailNegocio, telefonoNegocio);

        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff6b35, #f7931e); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 26px;'>Reserva Confirmada</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0; font-size: 15px;'>{nombreNegocio}</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none;'>
        <p style='color: #555; font-size: 16px; margin: 0 0 20px;'>Hola <strong>{nombreCliente}</strong>, tu reserva ha sido confirmada:</p>

        <table style='width: 100%; border-collapse: collapse; font-size: 15px; margin-bottom: 20px;'>
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Servicio</td>
                <td style='padding: 10px 12px; color: #333;'>{nombreServicio}</td>
            </tr>
            {(string.IsNullOrWhiteSpace(nombreEmpleado) ? "" : $@"
            <tr>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Profesional</td>
                <td style='padding: 10px 12px; color: #333;'>{nombreEmpleado}</td>
            </tr>")}
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Fecha</td>
                <td style='padding: 10px 12px; color: #333;'>{fechaHora:dd/MM/yyyy}</td>
            </tr>
            <tr>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Hora</td>
                <td style='padding: 10px 12px; color: #333; font-weight: bold;'>{fechaHora:hh:mm tt}</td>
            </tr>
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Duracion</td>
                <td style='padding: 10px 12px; color: #333;'>{duracionMinutos} minutos</td>
            </tr>
            <tr style='border-top: 2px solid #ff6b35;'>
                <td style='padding: 12px; color: #ff6b35; font-weight: bold; font-size: 17px;'>PRECIO</td>
                <td style='padding: 12px; color: #ff6b35; font-weight: bold; font-size: 17px;'>${precio:F2}</td>
            </tr>
        </table>

        {(string.IsNullOrWhiteSpace(linksContacto) ? "" : $"<p style='color: #555; font-size: 14px; text-align: center; margin-bottom: 15px;'>{linksContacto}</p>")}

        <p style='color: #999; font-size: 12px; margin: 20px 0 0; text-align: center;'>
            Este correo fue enviado automaticamente. No responda a este mensaje.
        </p>
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px; border-radius: 0 0 12px 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

        return await EnviarEmailAsync(destinatario, $"Reserva confirmada - {nombreServicio} en {nombreNegocio}", body);
    }

    // ═══════════════════════════════════════════════════════════
    // RECORDATORIO DE CITA — 30 MINUTOS ANTES
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> EnviarRecordatorioCitaEmailAsync(string destinatario, string nombreCliente, string nombreNegocio,
        string nombreServicio, string? nombreEmpleado, DateTime fechaHora,
        string? emailNegocio, string? telefonoNegocio)
    {
        var linksContacto = GenerarLinksContacto(emailNegocio, telefonoNegocio);

        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff6b35, #f7931e); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 26px;'>Recordatorio de Cita</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0; font-size: 15px;'>{nombreNegocio}</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none;'>
        <p style='color: #555; font-size: 16px; margin: 0 0 10px;'>Hola <strong>{nombreCliente}</strong>,</p>
        <p style='color: #333; font-size: 18px; font-weight: bold; margin: 0 0 25px;'>Tu cita es en 30 minutos.</p>

        <div style='background: #fff8f0; border: 2px solid #ff6b35; border-radius: 8px; padding: 20px; margin-bottom: 20px; text-align: center;'>
            <p style='color: #555; font-size: 14px; margin: 0 0 6px;'>{nombreServicio}{(string.IsNullOrWhiteSpace(nombreEmpleado) ? "" : $" con {nombreEmpleado}")}</p>
            <p style='color: #ff6b35; font-size: 28px; font-weight: bold; margin: 0;'>{fechaHora:hh:mm tt}</p>
            <p style='color: #888; font-size: 13px; margin: 4px 0 0;'>{fechaHora:dd/MM/yyyy}</p>
        </div>

        {(string.IsNullOrWhiteSpace(linksContacto) ? "" : $"<p style='color: #555; font-size: 14px; text-align: center; margin-bottom: 15px;'>{linksContacto}</p>")}

        <p style='color: #999; font-size: 12px; margin: 20px 0 0; text-align: center;'>
            Este correo fue enviado automaticamente. No responda a este mensaje.
        </p>
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px; border-radius: 0 0 12px 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

        return await EnviarEmailAsync(destinatario, $"Recordatorio: tu cita en {nombreNegocio} es en 30 minutos", body);
    }

    // ═══════════════════════════════════════════════════════════
    // RECIBO DE CITA COMPLETADA — NEGOCIO ARTESANAL
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool enviado, string htmlBody)> EnviarReciboCitaCompletadaAsync(string destinatario, string nombreCliente, string nombreNegocio,
        string nombreServicio, string? nombreEmpleado, decimal montoServicio, decimal montoExtra,
        decimal propina, decimal total, string? emailNegocio, string? telefonoNegocio, string numeroRecibo)
    {
        var linksContacto = GenerarLinksContacto(emailNegocio, telefonoNegocio);

        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff6b35, #f7931e); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 26px;'>Recibo de Servicio</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0; font-size: 15px;'>{nombreNegocio} &mdash; {TimeHelper.Now:dd/MM/yyyy}</p>
        <p style='color: rgba(255,255,255,0.8); margin: 6px 0 0; font-size: 13px;'>#{numeroRecibo}</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none;'>
        <p style='color: #555; font-size: 16px; margin: 0 0 20px;'>Gracias, <strong>{nombreCliente}</strong>. Aqui esta tu recibo:</p>

        <table style='width: 100%; border-collapse: collapse; font-size: 15px; margin-bottom: 20px;'>
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Servicio</td>
                <td style='padding: 10px 12px; color: #333;'>{nombreServicio}</td>
            </tr>
            {(string.IsNullOrWhiteSpace(nombreEmpleado) ? "" : $@"
            <tr>
                <td style='padding: 10px 12px; color: #666; font-weight: bold;'>Profesional</td>
                <td style='padding: 10px 12px; color: #333;'>{nombreEmpleado}</td>
            </tr>")}
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #555;'>Servicio</td>
                <td style='padding: 10px 12px; color: #333; text-align: right;'>${montoServicio:F2}</td>
            </tr>
            {(montoExtra > 0 ? $@"
            <tr>
                <td style='padding: 10px 12px; color: #555;'>Cargo extra</td>
                <td style='padding: 10px 12px; color: #333; text-align: right;'>${montoExtra:F2}</td>
            </tr>" : "")}
            {(propina > 0 ? $@"
            <tr style='background: #f8f9fa;'>
                <td style='padding: 10px 12px; color: #555;'>Propina</td>
                <td style='padding: 10px 12px; color: #28a745; text-align: right;'>${propina:F2}</td>
            </tr>" : "")}
            <tr style='border-top: 2px solid #ff6b35;'>
                <td style='padding: 12px; color: #ff6b35; font-weight: bold; font-size: 17px;'>TOTAL</td>
                <td style='padding: 12px; color: #ff6b35; font-weight: bold; font-size: 17px; text-align: right;'>${total:F2}</td>
            </tr>
        </table>

        {(string.IsNullOrWhiteSpace(linksContacto) ? "" : $"<p style='color: #555; font-size: 14px; text-align: center; margin-bottom: 15px;'>{linksContacto}</p>")}

        <p style='color: #888; font-size: 12px; margin: 20px 0 5px; text-align: center;'>
            Este documento NO es una factura legal. Si necesita una factura, contacte directamente al negocio.
        </p>
        <p style='color: #999; font-size: 12px; margin: 0; text-align: center;'>
            Este correo fue enviado automaticamente. No responda a este mensaje.
        </p>
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px; border-radius: 0 0 12px 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

        var enviado = await EnviarEmailAsync(destinatario, $"Recibo de servicio #{numeroRecibo} - {nombreNegocio}", body);
        return (enviado, body);
    }

    // ═══════════════════════════════════════════════════════════
    // RECORDATORIO DE CITA — EMPLEADO (30 MINUTOS ANTES)
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> EnviarRecordatorioCitaEmpleadoAsync(string destinatario, string nombreEmpleado,
        string nombreCliente, string nombreNegocio, string nombreServicio, DateTime fechaHora,
        string? emailNegocio, string? telefonoNegocio)
    {
        var linksContacto = GenerarLinksContacto(emailNegocio, telefonoNegocio);

        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff6b35, #f7931e); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 26px;'>Recordatorio de Cita</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0; font-size: 15px;'>{nombreNegocio}</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none;'>
        <p style='color: #555; font-size: 16px; margin: 0 0 10px;'>Hola <strong>{nombreEmpleado}</strong>,</p>
        <p style='color: #333; font-size: 18px; font-weight: bold; margin: 0 0 25px;'>Tienes una cita en 30 minutos.</p>

        <div style='background: #fff8f0; border: 2px solid #ff6b35; border-radius: 8px; padding: 20px; margin-bottom: 20px; text-align: center;'>
            <p style='color: #555; font-size: 14px; margin: 0 0 6px;'>{nombreServicio} con {nombreCliente}</p>
            <p style='color: #ff6b35; font-size: 28px; font-weight: bold; margin: 0;'>{fechaHora:hh:mm tt}</p>
            <p style='color: #888; font-size: 13px; margin: 4px 0 0;'>{fechaHora:dd/MM/yyyy}</p>
        </div>

        <p style='color: #555; font-size: 14px; text-align: center; margin-bottom: 15px;'>
            Si tienes algun problema, contacta al dueno del negocio:
        </p>
        {(string.IsNullOrWhiteSpace(linksContacto) ? "" : $"<p style='color: #555; font-size: 14px; text-align: center; margin-bottom: 15px;'>{linksContacto}</p>")}

        <p style='color: #999; font-size: 12px; margin: 20px 0 0; text-align: center;'>
            Este correo fue enviado automaticamente. No responda a este mensaje.
        </p>
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px; border-radius: 0 0 12px 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

        return await EnviarEmailAsync(destinatario, $"Recordatorio: tienes una cita en {nombreNegocio} en 30 minutos", body);
    }
}
