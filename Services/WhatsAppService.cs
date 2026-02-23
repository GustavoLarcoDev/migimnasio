// ═══════════════════════════════════════════════════════════════════════════
// WhatsAppService.cs — Integración con Twilio WhatsApp API
//
// CÓMO FUNCIONA TWILIO WHATSAPP API:
//   Twilio ofrece una API REST para enviar mensajes de WhatsApp desde
//   aplicaciones. Para usarla necesitas:
//     1. Una cuenta de Twilio (twilio.com)
//     2. Un número de teléfono habilitado para WhatsApp (sandbox o propio)
//     3. El Account SID y Auth Token del dashboard de Twilio
//
//   El endpoint de envío es:
//   POST https://api.twilio.com/2010-04-01/Accounts/{AccountSid}/Messages.json
//   Authorization: Basic base64(AccountSid:AuthToken)
//   Content-Type: application/x-www-form-urlencoded
//
// CONFIGURACIÓN EN appsettings.json:
//   "WhatsAppSettings": {
//     "Enabled": true,
//     "AccountSid": "ACxxxxxxxx...",       ← Account SID de Twilio
//     "AuthToken": "xxxxxxxx...",           ← Auth Token de Twilio
//     "FromNumber": "whatsapp:+14155238886" ← Número de origen (con prefijo whatsapp:)
//   }
//
// NORMALIZACIÓN DE TELÉFONOS:
//   La API de Twilio WhatsApp exige números en formato "whatsapp:+593987654321".
//   El método LimpiarTelefono() convierte automáticamente cualquier formato:
//   "0987 654-321" → eliminar espacios/guiones → "0987654321" → quitar 0 → "593987654321"
//   Luego EnviarMensajeTwilioAsync() agrega el prefijo "whatsapp:+" al enviar.
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

using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Gimnasio.Models;
using Microsoft.Extensions.Options;

namespace Gimnasio.Services;

/// <summary>
/// Implementación del servicio de mensajería de WhatsApp Business usando Twilio API.
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
    ///   3. Enviar via EnviarMensajeTwilioAsync (que hace la llamada HTTP real a Twilio)
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

        // Normalizar el número de teléfono al formato internacional (solo dígitos)
        var telefonoLimpio = LimpiarTelefono(telefono);
        if (string.IsNullOrEmpty(telefonoLimpio))
        {
            _logger.LogWarning("Número de teléfono inválido o vacío: {Telefono}", telefono);
            return false;
        }

        return await EnviarMensajeTwilioAsync(telefonoLimpio, mensaje);
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

    /// <summary>
    /// Envía el link del Catálogo de la Tienda al cliente vía WhatsApp.
    /// El mensaje es amigable y va directo al grano con la URL.
    /// </summary>
    public async Task<bool> EnviarLinkCatalogoTiendaAsync(
        string telefono, string nombreNegocio, string linkCatalogo)
    {
        var mensaje = $"Hola👋 Somos *{nombreNegocio}*.\n\n" +
                      $"Te compartimos nuestro *Catálogo de Productos* actualizado.\n" +
                      $"Puedes verlo (y descargarlo) directamente en el siguiente enlace:\n" +
                      $"{linkCatalogo}\n\n" +
                      $"¡Cualquier pedido o consulta, escríbenos por aquí mismo! 🛍️✨";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MENSAJES PAREADOS CON EMAIL
    // Cada uno de estos métodos se llama en paralelo con su email equivalente.
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> EnviarBienvenidaVendedorWhatsAppAsync(
        string telefono, string nombre, string email, string password)
    {
        var mensaje = $"🎉 *¡Bienvenido a My-Negocio, {nombre}!*\n\n" +
                      $"Ya puedes acceder al panel de vendedor con estas credenciales:\n" +
                      $"📧 Email: {email}\n" +
                      $"🔑 Contraseña: {password}\n\n" +
                      $"Ingresa en: *app.mi-negocio.net*\n\n" +
                      $"— My-Negocio";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
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

        var mensaje = $"🎉 *¡Bienvenido a My-Negocio, {dueno}!*\n\n" +
                      $"Tu negocio *{negocio}* ({tipo}) ya está listo.\n\n" +
                      $"📧 Email: {email}\n" +
                      $"🔑 Contraseña: {password}\n\n" +
                      $"Ingresa en: *app.mi-negocio.net*\n\n" +
                      $"_Si tienes dudas, escríbenos. ¡Estamos para ayudarte!_\n\n" +
                      $"— My-Negocio";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <inheritdoc />
    public async Task<bool> EnviarReciboPagoSuscripcionWhatsAppAsync(
        string telefono, string negocio, int dias, decimal precio, string numRecibo, string metodoPago = "Efectivo")
    {
        var mensaje = $"🧾 *Recibo de Pago — My-Negocio*\n\n" +
                      $"Negocio: *{negocio}*\n" +
                      $"Concepto: Suscripción x{dias} días\n" +
                      $"Monto: *${precio:N2}*\n" +
                      $"💳 Método de Pago: {metodoPago}\n" +
                      $"Recibo #: {numRecibo}\n\n" +
                      $"¡Gracias por tu pago! 🙌\n\n" +
                      $"— My-Negocio";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <inheritdoc />
    public async Task<bool> EnviarReciboComisionWhatsAppAsync(
        string telefono, string nombre, decimal monto, int cantidad, string numRecibo, string metodoPago = "Efectivo")
    {
        var mensaje = $"💰 *Pago de Comisión — My-Negocio*\n\n" +
                      $"Vendedor: *{nombre}*\n" +
                      $"Negocios: {cantidad}\n" +
                      $"Total pagado: *${monto:N2}*\n" +
                      $"💳 Método de Pago: {metodoPago}\n" +
                      $"Recibo #: {numRecibo}\n\n" +
                      $"¡Gracias por tu trabajo! 🎯\n\n" +
                      $"— My-Negocio";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <inheritdoc />
    public async Task<bool> EnviarReciboPagoClienteWhatsAppAsync(
        string telefono, string cliente, string negocio, string concepto, decimal monto, string numRecibo, string metodoPago = "Efectivo")
    {
        var mensaje = $"🧾 *Recibo de Pago*\n\n" +
                      $"Hola {cliente}, tu pago en *{negocio}* fue registrado:\n\n" +
                      $"📋 Concepto: {concepto}\n" +
                      $"💵 Monto: *${monto:N2}*\n" +
                      $"💳 Método de Pago: {metodoPago}\n" +
                      $"#️⃣ Recibo: {numRecibo}\n\n" +
                      $"¡Gracias! 🙌\n\n" +
                      $"_Este es un mensaje automatizado de *{negocio}*. " +
                      $"Por favor no responda a este número._";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <inheritdoc />
    public async Task<bool> EnviarConfirmacionReservaWhatsAppAsync(
        string telefono, string cliente, string negocio, string servicio, string empleado, DateTime fechaHora, decimal precio)
    {
        var fecha = fechaHora.ToString("dd/MM/yyyy");
        var hora = fechaHora.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);

        var mensaje = $"✅ *Cita Confirmada*\n\n" +
                      $"Hola {cliente}, tu cita en *{negocio}* está confirmada:\n\n" +
                      $"💇 Servicio: {servicio}\n" +
                      $"👤 Con: {empleado}\n" +
                      $"📅 Fecha: {fecha}\n" +
                      $"🕐 Hora: {hora}\n" +
                      $"💵 Precio: ${precio:N2}\n\n" +
                      $"Si necesitas cancelar, comunícate directamente con *{negocio}*.\n\n" +
                      $"_Este es un mensaje automatizado. Por favor no responda a este número._";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <inheritdoc />
    public async Task<bool> EnviarReciboCitaCompletadaWhatsAppAsync(
        string telefono, string cliente, string negocio, string servicio, decimal total, string numRecibo, string metodoPago = "Efectivo")
    {
        var mensaje = $"🧾 *Recibo de Servicio*\n\n" +
                      $"Hola {cliente}, gracias por tu visita a *{negocio}*:\n\n" +
                      $"💇 Servicio: {servicio}\n" +
                      $"💵 Total: *${total:N2}*\n" +
                      $"💳 Método de Pago: {metodoPago}\n" +
                      $"#️⃣ Recibo: {numRecibo}\n\n" +
                      $"¡Esperamos verte pronto! 😊\n\n" +
                      $"_Este es un mensaje automatizado de *{negocio}*. " +
                      $"Por favor no responda a este número._";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <inheritdoc />
    public async Task<bool> EnviarResumenDiarioGeneralWhatsAppAsync(
        string telefono, string negocio, decimal ingresos, decimal gastos, decimal ganancia, int nuevosClientes)
    {
        var mensaje = $"📊 *Resumen del día — {negocio}*\n\n" +
                      $"💰 Ingresos: ${ingresos:N2}\n" +
                      $"💸 Gastos: ${gastos:N2}\n" +
                      $"📈 Ganancia: ${ganancia:N2}\n" +
                      $"👤 Nuevos clientes: {nuevosClientes}\n\n" +
                      $"— MiNegocio";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <inheritdoc />
    public async Task<bool> EnviarRecordatorioCitaEmpleadoWhatsAppAsync(
        string telefono, string empleado, string cliente, string servicio, string hora)
    {
        var mensaje = $"⏰ *Recordatorio de Cita*\n\n" +
                      $"Hola {empleado}, tienes una cita próxima:\n\n" +
                      $"👤 Cliente: {cliente}\n" +
                      $"💇 Servicio: {servicio}\n" +
                      $"🕐 Hora: {hora}\n\n" +
                      $"¡Prepárate! 💪\n\n" +
                      $"— MiNegocio";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NOTIFICACIONES DE ESTADO DE CITAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> EnviarNotificacionCitaCanceladaWhatsAppAsync(
        string telefono, string cliente, string negocio, string servicio, DateTime fechaHora, string motivo)
    {
        var fecha = fechaHora.ToString("dd/MM/yyyy");
        var hora = fechaHora.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);

        var mensaje = $"❌ *Cita Cancelada*\n\n" +
                      $"Hola {cliente}, tu cita en *{negocio}* ha sido cancelada:\n\n" +
                      $"💇 Servicio: {servicio}\n" +
                      $"📅 Fecha: {fecha}\n" +
                      $"🕐 Hora: {hora}\n" +
                      (!string.IsNullOrWhiteSpace(motivo) ? $"📝 Motivo: {motivo}\n\n" : "\n") +
                      $"Si deseas reagendar, comunícate directamente con *{negocio}*.\n\n" +
                      $"_Este es un mensaje automatizado. Por favor no responda a este número._";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <inheritdoc />
    public async Task<bool> EnviarNotificacionCitaReprogramadaWhatsAppAsync(
        string telefono, string cliente, string negocio, string servicio, string empleado, DateTime nuevaFechaHora)
    {
        var fecha = nuevaFechaHora.ToString("dd/MM/yyyy");
        var hora = nuevaFechaHora.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);

        var mensaje = $"🔄 *Cita Reprogramada*\n\n" +
                      $"Hola {cliente}, tu cita en *{negocio}* ha sido reprogramada:\n\n" +
                      $"💇 Servicio: {servicio}\n" +
                      $"👤 Con: {empleado}\n" +
                      $"📅 Nueva fecha: {fecha}\n" +
                      $"🕐 Nueva hora: {hora}\n\n" +
                      $"Si no puedes asistir, comunícate directamente con *{negocio}*.\n\n" +
                      $"_Este es un mensaje automatizado. Por favor no responda a este número._";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <inheritdoc />
    public async Task<bool> EnviarNotificacionNoShowWhatsAppAsync(
        string telefono, string cliente, string negocio, string servicio, DateTime fechaHora)
    {
        var fecha = fechaHora.ToString("dd/MM/yyyy");
        var hora = fechaHora.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);

        var mensaje = $"📝 *Aviso de Inasistencia*\n\n" +
                      $"Hola {cliente}, notamos que no asististe a tu cita en *{negocio}*:\n\n" +
                      $"💇 Servicio: {servicio}\n" +
                      $"📅 Fecha: {fecha}\n" +
                      $"🕐 Hora: {hora}\n\n" +
                      $"Si deseas reagendar, comunícate directamente con *{negocio}*.\n\n" +
                      $"_Este es un mensaje automatizado. Por favor no responda a este número._";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NOTIFICACIONES DE SUSCRIPCIÓN Y NEGOCIO
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> EnviarAdvertenciaSuscripcionWhatsAppAsync(
        string telefono, string negocio, string dueno, int diasRestantes, DateTime fechaExpiracion)
    {
        var fecha = fechaExpiracion.ToString("dd/MM/yyyy");

        var urgencia = diasRestantes switch
        {
            1 => "⚠️ *¡ATENCIÓN! Tu suscripción vence MAÑANA*",
            3 => "📋 *Recordatorio de Suscripción*",
            _ => "📋 *Recordatorio de Suscripción*"
        };

        var mensaje = $"{urgencia}\n\n" +
                      $"Hola {dueno}, la suscripción de *{negocio}* en My-Negocio vence en *{diasRestantes} día(s)* ({fecha}).\n\n" +
                      $"Renueva para seguir usando el sistema sin interrupciones.\n\n" +
                      $"— My-Negocio";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <inheritdoc />
    public async Task<bool> EnviarNotificacionNegocioBloqueadoWhatsAppAsync(
        string telefono, string negocio, string dueno)
    {
        var mensaje = $"🔒 *Negocio Bloqueado*\n\n" +
                      $"Hola {dueno}, tu negocio *{negocio}* ha sido bloqueado en My-Negocio.\n\n" +
                      $"Esto puede deberse a una suscripción vencida. " +
                      $"Comunícate con nosotros para restaurar tu acceso.\n\n" +
                      $"— My-Negocio";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <inheritdoc />
    public async Task<bool> EnviarNotificacionNegocioDesbloqueadoWhatsAppAsync(
        string telefono, string negocio, string dueno)
    {
        var mensaje = $"✅ *Negocio Desbloqueado*\n\n" +
                      $"Hola {dueno}, tu negocio *{negocio}* ha sido desbloqueado en My-Negocio.\n\n" +
                      $"Ya puedes acceder normalmente al sistema. ¡Bienvenido de vuelta!\n\n" +
                      $"— My-Negocio";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    /// <inheritdoc />
    public async Task<bool> EnviarAlertaStockBajoWhatsAppAsync(
        string telefono, string negocio, string producto, int stockActual, int stockMinimo)
    {
        var mensaje = $"🚨 *Alerta de Stock Bajo*\n\n" +
                      $"*{negocio}* — El producto *{producto}* tiene stock bajo:\n\n" +
                      $"📦 Stock actual: *{stockActual}* unidades\n" +
                      $"⚠️ Stock mínimo: {stockMinimo} unidades\n\n" +
                      $"Considera reabastecer pronto.\n\n" +
                      $"— My-Negocio";

        return await EnviarMensajeTextoAsync(telefono, mensaje);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MÉTODOS PRIVADOS — COMUNICACIÓN HTTP CON TWILIO API
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Realiza la llamada HTTP POST a la API de Twilio para enviar el mensaje de WhatsApp.
    ///
    /// POR QUÉ IHttpClientFactory EN VEZ DE 'new HttpClient()':
    ///   Crear y destruir HttpClient en cada request agota los sockets del sistema
    ///   (un problema clásico de .NET conocido como "socket exhaustion").
    ///   IHttpClientFactory mantiene un pool de conexiones HTTP reutilizables.
    ///   Con CreateClient("WhatsApp") obtenemos un cliente pre-configurado
    ///   con el nombre "WhatsApp" definido en Program.cs (AddHttpClient).
    ///
    /// AUTENTICACIÓN CON BASIC AUTH:
    ///   Twilio usa HTTP Basic Authentication.
    ///   El header Authorization contiene: "Basic base64(AccountSid:AuthToken)"
    ///
    /// URL DE LA API:
    ///   https://api.twilio.com/2010-04-01/Accounts/{AccountSid}/Messages.json
    ///
    /// CONTENT-TYPE:
    ///   application/x-www-form-urlencoded (no JSON como Meta)
    ///   Body: From=whatsapp:+NUMBER&amp;To=whatsapp:+{telefono}&amp;Body={mensaje}
    ///
    /// RESPUESTA EXITOSA:
    ///   HTTP 201 Created (no 200 como Meta)
    /// </summary>
    private async Task<bool> EnviarMensajeTwilioAsync(string telefonoLimpio, string mensaje)
    {
        const int maxAttempts = 3;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("WhatsApp");
                var url = $"https://api.twilio.com/2010-04-01/Accounts/{_settings.AccountSid}/Messages.json";

                // Twilio usa Basic Auth: base64(AccountSid:AuthToken)
                var authBytes = Encoding.ASCII.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}");
                var authHeader = Convert.ToBase64String(authBytes);

                // Twilio espera form-urlencoded, no JSON
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("From", _settings.FromNumber),
                    new KeyValuePair<string, string>("To", $"whatsapp:+{telefonoLimpio}"),
                    new KeyValuePair<string, string>("Body", mensaje)
                });

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = formData;
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                // Twilio retorna HTTP 201 Created para mensajes enviados exitosamente
                if (response.StatusCode == System.Net.HttpStatusCode.Created || response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Mensaje de WhatsApp enviado exitosamente vía Twilio");
                    return true;
                }

                // HTTP 429: respetar Retry-After header
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
    /// Limpia y normaliza un número de teléfono para que cumpla el formato
    /// internacional: solo dígitos, sin "+", con código de país.
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
