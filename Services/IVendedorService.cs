#nullable enable
using Gimnasio.Models;

namespace Gimnasio.Services;

public interface IVendedorService
{
    Task<List<object>> GetAllVendedoresAsync();
    Task<Vendedor?> GetVendedorAsync(Guid id);
    Task<(bool success, string message)> CrearVendedorAsync(string nombre, string apellido, string correo, string telefono, string password);
    Task<(bool success, string message)> EditarVendedorAsync(Guid id, string nombre, string apellido, string correo, string telefono, string? password);
    Task<(bool success, string message)> EliminarVendedorAsync(Guid id);
    Task<Vendedor?> LoginVendedorAsync(string correo, string password);
    Task<List<object>> GetNegociosByVendedorAsync(Guid vendedorId);
    Task<object> GetVendedorStatsAsync(Guid vendedorId);
    Task<List<object>> GetLeadsAsync();
    Task<(bool success, string message)> MarcarLeadAtendidoAsync(Guid leadId, Guid vendedorId, string vendedorNombre);
    Task<int> GetLeadsCountAsync();
}
