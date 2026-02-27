using Gimnasio.Models;

namespace Gimnasio.Services;

public interface IReservaService
{
    Task<object> GetReservasAsync(Guid negocioId, DateTime? fecha = null);
    Task<Reserva> GetReservaAsync(Guid reservaId, Guid negocioId);
    Task<(bool success, string message)> CrearReservaAsync(Guid negocioId, string nombreCliente, string telefono, DateTime fechaHoraReserva, int cantidadPersonas, Guid? mesaId, string notas);
    Task<(bool success, string message)> EditarReservaAsync(Guid reservaId, Guid negocioId, string nombreCliente, string telefono, DateTime fechaHoraReserva, int cantidadPersonas, Guid? mesaId, string notas);
    Task<(bool success, string message)> CambiarEstadoReservaAsync(Guid reservaId, Guid negocioId, string estado);
    Task<(bool success, string message)> CancelarReservaAsync(Guid reservaId, Guid negocioId);
    Task<object> GetReservasHoyAsync(Guid negocioId);
}
