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
}
