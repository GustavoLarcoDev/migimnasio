using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

public interface IClienteService
{
    Task<object> GetDashboardStatsAsync(Guid gimnasioId);
    Task<object> GetClientesAsync(Guid gimnasioId);
    Task<Cliente> GetClienteAsync(Guid id, Guid gimnasioId);
    Task<(bool success, string message)> CrearClienteAsync(ClienteCreateDto model);
    Task<(bool success, string message)> EditarClienteAsync(ClienteCreateDto model);
    Task<(bool success, string message)> EliminarClienteAsync(Guid id, Guid gimnasioId);
    Task<(bool success, string message)> RenovarClienteAsync(Guid id, Guid gimnasioId, int dias, decimal precio);
    Task<byte[]> ExportClientesExcelAsync(Guid gimnasioId);
    Task<object> ImportarClientesExcelAsync(Guid gimnasioId, Stream fileStream);
    Task<object> GetClientesDiariosAsync(Guid gimnasioId);
}
