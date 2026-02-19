using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Gimnasio.Helpers;

/// <summary>
/// Genera documentos Word (.docx) de guia para negocios y vendedores.
/// Los documentos se generan en memoria (MemoryStream) y se retornan como byte[]
/// para adjuntarlos directamente a los emails de bienvenida via MimeKit.
/// Usa el SDK Open XML de Microsoft (sin dependencia de Word instalado).
/// </summary>
public static class GuideDocumentGenerator
{
    /// <summary>
    /// Genera la guia de uso para un nuevo negocio registrado en la plataforma.
    /// Incluye: como iniciar sesion, navegacion del dashboard, gestion de clientes,
    /// ventas, notificaciones y soporte.
    /// </summary>
    public static byte[] GenerarGuiaNegocio(string nombreNegocio, string nombreDueno, string email, string telefono)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            // Titulo principal
            AddHeading(body, "Guia de Inicio - My-Negocio", 28, true);
            AddParagraph(body, "");

            // Bienvenida personalizada
            AddHeading(body, $"Bienvenido, {nombreDueno}!", 22, false);
            AddParagraph(body, $"Esta guia le ayudara a comenzar a usar My-Negocio para gestionar {nombreNegocio} de manera eficiente.");
            AddParagraph(body, "");

            // Seccion 1: Credenciales
            AddHeading(body, "1. Sus Credenciales de Acceso", 18, true);
            AddParagraph(body, $"Email: {email}");
            AddParagraph(body, $"Telefono registrado: {telefono}");
            AddParagraph(body, "Puede iniciar sesion con su email O su numero de telefono.");
            AddParagraph(body, "IMPORTANTE: Cambie su contrasena despues del primer inicio de sesion por seguridad.");
            AddParagraph(body, "");

            // Seccion 2: Como ingresar
            AddHeading(body, "2. Como Iniciar Sesion", 18, true);
            AddParagraph(body, "1. Vaya a la pagina de login de My-Negocio");
            AddParagraph(body, "2. Ingrese su email o numero de telefono");
            AddParagraph(body, "3. Ingrese su contrasena");
            AddParagraph(body, "4. Haga clic en 'Iniciar Sesion'");
            AddParagraph(body, "");

            // Seccion 3: Dashboard
            AddHeading(body, "3. Su Panel de Control (Dashboard)", 18, true);
            AddParagraph(body, "Una vez dentro, encontrara:");
            AddParagraph(body, "- Clientes: Registre y gestione todos sus clientes");
            AddParagraph(body, "- Ventas: Registre cobros y vea su historial de ingresos");
            AddParagraph(body, "- Notificaciones: Alertas de membresias por vencer");
            AddParagraph(body, "- Logs: Historial de todas las actividades del sistema");
            AddParagraph(body, "");

            // Seccion 4: Gestion de clientes
            AddHeading(body, "4. Gestion de Clientes", 18, true);
            AddParagraph(body, "- Para agregar un cliente: haga clic en 'Nuevo Cliente'");
            AddParagraph(body, "- Complete nombre, telefono, email y plan de membresia");
            AddParagraph(body, "- El sistema calculara automaticamente la fecha de vencimiento");
            AddParagraph(body, "- Recibira notificaciones cuando las membresias esten por vencer");
            AddParagraph(body, "");

            // Seccion 5: Ventas
            AddHeading(body, "5. Registro de Ventas", 18, true);
            AddParagraph(body, "- Cada cobro que registre queda guardado en el historial");
            AddParagraph(body, "- Puede ver graficos de ingresos por periodo");
            AddParagraph(body, "- Exporte reportes a Excel cuando lo necesite");
            AddParagraph(body, "");

            // Seccion 6: Soporte
            AddHeading(body, "6. Soporte y Ayuda", 18, true);
            AddParagraph(body, "Si tiene alguna pregunta o necesita ayuda:");
            AddParagraph(body, "- Escribanos por WhatsApp al numero de soporte");
            AddParagraph(body, "- Envie un email a soporte@my-negocio.com");
            AddParagraph(body, "- Use la seccion de 'Sugerencias' dentro de su panel");
            AddParagraph(body, "");

            AddParagraph(body, "Gracias por confiar en My-Negocio. Estamos aqui para ayudarle a crecer!");
            AddParagraph(body, "");
            AddParagraph(body, "— El equipo de My-Negocio");

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Genera la guia para un nuevo vendedor que se une al equipo de ventas.
    /// Incluye: como iniciar sesion, gestion de negocios, leads, comisiones y mejores practicas.
    /// </summary>
    public static byte[] GenerarGuiaVendedor(string nombreVendedor, string correo)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            // Titulo principal
            AddHeading(body, "Manual del Vendedor - My-Negocio", 28, true);
            AddParagraph(body, "");

            // Bienvenida
            AddHeading(body, $"Bienvenido al equipo, {nombreVendedor}!", 22, false);
            AddParagraph(body, "Este manual te guiara en todo lo que necesitas saber para vender y gestionar negocios en la plataforma My-Negocio.");
            AddParagraph(body, "");

            // Seccion 1: Credenciales
            AddHeading(body, "1. Tus Credenciales de Acceso", 18, true);
            AddParagraph(body, $"Email: {correo}");
            AddParagraph(body, "Puedes iniciar sesion con tu email o tu numero de telefono.");
            AddParagraph(body, "IMPORTANTE: Cambia tu contrasena despues del primer inicio de sesion.");
            AddParagraph(body, "");

            // Seccion 2: Tu Dashboard
            AddHeading(body, "2. Tu Panel de Vendedor", 18, true);
            AddParagraph(body, "En tu dashboard encontraras:");
            AddParagraph(body, "- Tus Negocios: Lista de todos los negocios que has creado");
            AddParagraph(body, "- Estadisticas: Total de negocios, activos, en prueba y clientes");
            AddParagraph(body, "- Leads: Prospectos interesados que llegan del sitio web");
            AddParagraph(body, "");

            // Seccion 3: Crear negocio
            AddHeading(body, "3. Como Crear un Nuevo Negocio", 18, true);
            AddParagraph(body, "1. Haz clic en 'Nuevo Negocio' en tu dashboard");
            AddParagraph(body, "2. Completa los datos del negocio:");
            AddParagraph(body, "   - Nombre del negocio");
            AddParagraph(body, "   - Nombre del dueno");
            AddParagraph(body, "   - Email y telefono");
            AddParagraph(body, "   - Contrasena inicial");
            AddParagraph(body, "   - Tipo: Membresias o Artesanal (citas)");
            AddParagraph(body, "3. Selecciona si es periodo de prueba o pago");
            AddParagraph(body, "4. El sistema enviara automaticamente un email de bienvenida al dueno");
            AddParagraph(body, "");

            // Seccion 4: Leads
            AddHeading(body, "4. Gestion de Leads", 18, true);
            AddParagraph(body, "- Los leads son personas que completaron el formulario en el sitio web");
            AddParagraph(body, "- Aparecen en tu panel con un badge de notificacion");
            AddParagraph(body, "- Contacta al lead lo antes posible (los primeros en responder cierran mas ventas)");
            AddParagraph(body, "- Marca el lead como 'Atendido' una vez que lo contactes");
            AddParagraph(body, "");

            // Seccion 5: Impersonar
            AddHeading(body, "5. Ver el Panel de un Negocio", 18, true);
            AddParagraph(body, "- Puedes 'entrar' al panel de cualquier negocio que hayas creado");
            AddParagraph(body, "- Esto te permite ayudar al cliente o verificar que todo funcione");
            AddParagraph(body, "- Usa el boton 'Entrar' junto al nombre del negocio");
            AddParagraph(body, "- Para volver a tu panel, usa el boton 'Volver al Panel de Vendedor'");
            AddParagraph(body, "");

            // Seccion 6: Mejores practicas
            AddHeading(body, "6. Mejores Practicas de Ventas", 18, true);
            AddParagraph(body, "- Responde leads en menos de 5 minutos");
            AddParagraph(body, "- Ofrece siempre un periodo de prueba de 7 dias");
            AddParagraph(body, "- Acompana al cliente en su primera semana de uso");
            AddParagraph(body, "- Agenda una llamada de seguimiento a los 3 dias");
            AddParagraph(body, "- Los clientes satisfechos son tu mejor fuente de referidos");
            AddParagraph(body, "");

            // Seccion 7: Soporte
            AddHeading(body, "7. Soporte Interno", 18, true);
            AddParagraph(body, "Si tienes alguna duda tecnica o necesitas ayuda:");
            AddParagraph(body, "- Contacta al equipo de soporte por WhatsApp");
            AddParagraph(body, "- Escribe a soporte@my-negocio.com");
            AddParagraph(body, "");

            AddParagraph(body, "Exito en tus ventas! Confiamos en ti.");
            AddParagraph(body, "");
            AddParagraph(body, "— El equipo de My-Negocio");

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS PRIVADOS PARA CONSTRUIR EL DOCUMENTO
    // ═══════════════════════════════════════════════════════════

    private static void AddHeading(Body body, string text, int fontSize, bool bold)
    {
        var paragraph = new Paragraph();
        var run = new Run();

        var runProperties = new RunProperties();
        runProperties.Append(new FontSize { Val = (fontSize * 2).ToString() });
        if (bold)
            runProperties.Append(new Bold());
        runProperties.Append(new Color { Val = "333333" });
        runProperties.Append(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });

        run.Append(runProperties);
        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        paragraph.Append(run);
        body.Append(paragraph);
    }

    private static void AddParagraph(Body body, string text)
    {
        var paragraph = new Paragraph();

        if (!string.IsNullOrEmpty(text))
        {
            var run = new Run();
            var runProperties = new RunProperties();
            runProperties.Append(new FontSize { Val = "22" }); // 11pt
            runProperties.Append(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });
            run.Append(runProperties);
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            paragraph.Append(run);
        }

        body.Append(paragraph);
    }
}
