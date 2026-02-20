#nullable enable

namespace Gimnasio.Services;

/// <summary>
/// Contrato del servicio de correo electronico transaccional.
/// Implementado por EmailService.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envia correo de bienvenida a un nuevo vendedor con sus credenciales
    /// y el documento de guia para vendedores adjunto (.docx).
    /// </summary>
    Task<bool> EnviarBienvenidaVendedorAsync(string destinatario, string nombreVendedor, string correo, string password);

    /// <summary>
    /// Envia correo de bienvenida al dueno de un negocio con sus credenciales
    /// y el documento de guia para negocios adjunto (.docx).
    /// Si nombreVendedor es null, el admin lo creo directamente.
    /// </summary>
    Task<bool> EnviarBienvenidaNegocioAsync(string destinatario, string nombreNegocio, string nombreDueno, string emailNegocio, string passwordNegocio, string telefonoNegocio, string? nombreVendedor);

    // Reporte diario para negocios de membresías (11 PM)
    Task<bool> EnviarReporteDiarioMembresiaAsync(string destinatario, string nombreNegocio, string nombreDueno,
        decimal ingresosDia, int nuevosClientes, int productosVendidos, decimal totalProductos,
        int clientesPorVencer, int devoluciones, decimal montoDevuelto, DateTime fecha);

    // Reporte diario para negocios artesanales (11 PM)
    Task<bool> EnviarReporteDiarioArtesanalAsync(string destinatario, string nombreNegocio, string nombreDueno,
        decimal ingresosDia, int citasCompletadas, int citasCanceladas, int nuevosClientes,
        string servicioMasPopular, decimal totalProductos, DateTime fecha);

    // Recibo de pago cuando un negocio paga su suscripción SaaS
    Task<(bool enviado, string htmlBody)> EnviarReciboPagoNegocioAsync(string destinatario, string nombreNegocio, string nombreDueno,
        int diasContratados, decimal precio, string? nombreVendedor, string? telefonoVendedor, string numeroRecibo);

    // Recibo de pago de comisión al vendedor
    Task<(bool enviado, string htmlBody)> EnviarReciboComisionAsync(string destinatario, string nombreVendedor,
        decimal montoTotal, int cantidadNegocios, string detalleComisiones, string numeroRecibo);

    // Recibo de pago de cliente (membresía) enviado desde el negocio
    Task<(bool enviado, string htmlBody)> EnviarReciboPagoClienteAsync(string destinatario, string nombreCliente, string nombreNegocio,
        string conceptoPago, decimal monto, int dias, string? emailNegocio, string? telefonoNegocio, string numeroRecibo);

    // Confirmación de reserva para negocio artesanal
    Task<bool> EnviarConfirmacionReservaAsync(string destinatario, string nombreCliente, string nombreNegocio,
        string nombreServicio, string? nombreEmpleado, DateTime fechaHora, int duracionMinutos,
        decimal precio, string? emailNegocio, string? telefonoNegocio);

    // Recordatorio 30 min antes de cita por email
    Task<bool> EnviarRecordatorioCitaEmailAsync(string destinatario, string nombreCliente, string nombreNegocio,
        string nombreServicio, string? nombreEmpleado, DateTime fechaHora,
        string? emailNegocio, string? telefonoNegocio);

    // Recibo al completar servicio artesanal
    Task<(bool enviado, string htmlBody)> EnviarReciboCitaCompletadaAsync(string destinatario, string nombreCliente, string nombreNegocio,
        string nombreServicio, string? nombreEmpleado, decimal montoServicio, decimal montoExtra,
        decimal propina, decimal total, string? emailNegocio, string? telefonoNegocio, string numeroRecibo);

    // Recordatorio de cita al empleado — 30 minutos antes
    Task<bool> EnviarRecordatorioCitaEmpleadoAsync(string destinatario, string nombreEmpleado,
        string nombreCliente, string nombreNegocio, string nombreServicio, DateTime fechaHora,
        string? emailNegocio, string? telefonoNegocio);

    // Envío del Catálogo de la Tienda (por email)
    Task<bool> EnviarCatalogoTiendaAsync(string destinatario, string nombreNegocio, byte[] pdfBytes);

    // Envío genérico de recibo HTML (Tienda POS)
    Task<bool> EnviarReciboPorEmailGenericoAsync(string destinatario, string asunto, string contenidoHtml);
}
