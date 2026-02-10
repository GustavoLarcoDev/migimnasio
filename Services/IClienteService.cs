using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

public interface IClienteService
{
    Task<object> GetDashboardStatsAsync(Guid negocioId);
    Task<object> GetClientesAsync(Guid negocioId);
    Task<Cliente> GetClienteAsync(Guid id, Guid negocioId);
    Task<(bool success, string message)> CrearClienteAsync(ClienteCreateDto model);
    Task<(bool success, string message)> EditarClienteAsync(ClienteCreateDto model);
    Task<(bool success, string message)> EliminarClienteAsync(Guid id, Guid negocioId);
    Task<(bool success, string message)> RenovarClienteAsync(Guid id, Guid negocioId, DateTime nuevaFechaFin, decimal precio);
    Task<byte[]> ExportClientesExcelAsync(Guid negocioId);
    Task<object> ImportarClientesExcelAsync(Guid negocioId, Stream fileStream);
    Task<object> GetClientesDiariosAsync(Guid negocioId);
}
