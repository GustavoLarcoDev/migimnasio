using Gimnasio.Models;

namespace Gimnasio.Services;

public interface IGimnasioService
{
    Task<object> GetAllGimnasiosAsync();
    Task<object> GetGimnasioAsync(Guid id);
    Task<(bool success, string message)> CreateGimnasioAsync(string nombre, string dueno, string telefono, string email, string password, bool isActive, bool esPrueba);
    Task<(bool success, string message)> EditarGimnasioAsync(Gym gimnasio);
    Task<(bool success, string message)> EliminarGimnasioAsync(Guid id);
    Task<(bool success, string message, bool? isActive, bool? esPrueba)> CambiarEstadoAsync(Guid id);
    Task<byte[]> ExportExcelAsync();
}
