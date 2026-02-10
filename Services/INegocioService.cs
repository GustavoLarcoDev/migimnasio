using Gimnasio.Models;

namespace Gimnasio.Services;

public interface INegocioService
{
    Task<object> GetAllNegociosAsync();
    Task<object> GetNegocioAsync(Guid id);
    Task<(bool success, string message)> CreateNegocioAsync(string nombre, string dueno, string telefono, string email, string password, bool isActive, bool esPrueba);
    Task<(bool success, string message)> EditarNegocioAsync(Gym negocio);
    Task<(bool success, string message)> EliminarNegocioAsync(Guid id);
    Task<(bool success, string message, bool? isActive, bool? esPrueba)> CambiarEstadoAsync(Guid id);
    Task<byte[]> ExportExcelAsync();
    Task<Gym> GetNegocioForImpersonationAsync(Guid id);
}
