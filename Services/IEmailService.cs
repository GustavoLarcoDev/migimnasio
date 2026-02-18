#nullable enable
namespace Gimnasio.Services;

public interface IEmailService
{
    Task<bool> EnviarBienvenidaVendedorAsync(string destinatario, string nombreVendedor);
    Task<bool> EnviarBienvenidaNegocioAsync(string destinatario, string nombreNegocio, string nombreDueno, string? nombreVendedor);
}
