#nullable enable
using System.Net;
using System.Net.Mail;

namespace Gimnasio.Services;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "My-Negocio";
    public string Password { get; set; } = "";
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
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
        _logger = logger;
    }

    public async Task<bool> EnviarBienvenidaVendedorAsync(string destinatario, string nombreVendedor)
    {
        if (string.IsNullOrEmpty(_settings.Password) || _settings.Password == "PONER_APP_PASSWORD_AQUI")
        {
            _logger.LogWarning("EmailService: No se ha configurado la App Password de Gmail. Correo no enviado.");
            return false;
        }

        try
        {
            var frase = FrasesVendedor[Random.Shared.Next(FrasesVendedor.Length)];

            var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #ff6b35, #f7931e); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 28px;'>Bienvenido a My-Negocio</h1>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 12px 12px;'>
        <h2 style='color: #333;'>Hola {nombreVendedor}! 👋</h2>
        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
            Estamos muy agradecidos de que te unas a nuestro equipo de vendedores.
            Tu talento y dedicacion son exactamente lo que necesitamos para seguir creciendo juntos.
        </p>
        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
            En My-Negocio creemos que cada miembro del equipo marca la diferencia,
            y estamos seguros de que contigo vamos a llegar muy lejos.
        </p>
        <div style='background: #fff8f0; border-left: 4px solid #ff6b35; padding: 15px 20px; margin: 25px 0; border-radius: 0 8px 8px 0;'>
            <p style='color: #333; font-size: 16px; font-style: italic; margin: 0;'>
                &ldquo;{frase}&rdquo;
            </p>
        </div>
        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
            Si tienes alguna pregunta, no dudes en contactarnos. Estamos aqui para apoyarte en todo momento.
        </p>
        <p style='color: #555; font-size: 16px;'>
            Con mucho entusiasmo,<br>
            <strong>El equipo de My-Negocio</strong>
        </p>
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

            using var message = new MailMessage();
            message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
            message.To.Add(new MailAddress(destinatario));
            message.Subject = "Bienvenido al equipo de My-Negocio! 🎉";
            message.Body = body;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort);
            client.Credentials = new NetworkCredential(_settings.FromEmail, _settings.Password);
            client.EnableSsl = true;

            await client.SendMailAsync(message);
            _logger.LogInformation("Email de bienvenida enviado a {Email}", destinatario);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando email de bienvenida a {Email}", destinatario);
            return false;
        }
    }

    public async Task<bool> EnviarBienvenidaNegocioAsync(string destinatario, string nombreNegocio, string nombreDueno, string? nombreVendedor)
    {
        if (string.IsNullOrEmpty(_settings.Password) || _settings.Password == "PONER_APP_PASSWORD_AQUI")
        {
            _logger.LogWarning("EmailService: No se ha configurado la App Password de Gmail. Correo no enviado.");
            return false;
        }

        try
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
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 12px 12px;'>
        <h2 style='color: #333;'>{saludo} 👋</h2>
        {mensaje}
        <div style='background: #fff8f0; border-left: 4px solid #ff6b35; padding: 15px 20px; margin: 25px 0; border-radius: 0 8px 8px 0;'>
            <p style='color: #333; font-size: 16px; font-style: italic; margin: 0;'>
                &ldquo;{frase}&rdquo;
            </p>
        </div>
        {despedida}
    </div>
    <div style='text-align: center; padding: 15px; color: #999; font-size: 12px;'>
        My-Negocio - Gestion inteligente para tu negocio
    </div>
</div>";

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_settings.FromEmail, _settings.FromName);
            mailMessage.To.Add(new MailAddress(destinatario));
            mailMessage.Subject = $"Bienvenido a My-Negocio, {nombreDueno}! 🚀";
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = true;

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort);
            client.Credentials = new NetworkCredential(_settings.FromEmail, _settings.Password);
            client.EnableSsl = true;

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email de bienvenida negocio enviado a {Email}", destinatario);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando email de bienvenida negocio a {Email}", destinatario);
            return false;
        }
    }
}
