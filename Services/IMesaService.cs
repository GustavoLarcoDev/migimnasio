// ═══════════════════════════════════════════════════════════
// IMesaService.cs — Contrato del servicio de mesas de restaurante
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;

namespace Gimnasio.Services;

public interface IMesaService
{
    Task<object> GetMesasAsync(Guid negocioId);
    Task<Mesa> GetMesaAsync(Guid mesaId, Guid negocioId);
    Task<(bool success, string message)> CrearMesaAsync(Guid negocioId, string nombre, int numero, int capacidad);
    Task<(bool success, string message)> EditarMesaAsync(Guid mesaId, Guid negocioId, string nombre, int numero, int capacidad);
    Task<(bool success, string message)> CambiarEstadoMesaAsync(Guid mesaId, Guid negocioId, string estado);
    Task<(bool success, string message)> EliminarMesaAsync(Guid mesaId, Guid negocioId);
}
