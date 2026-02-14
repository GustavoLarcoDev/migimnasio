namespace Gimnasio.Services;

public interface ISugerenciaService
{
    Task<(bool success, string message)> CrearSugerenciaAsync(Guid negocioId, string negocioNombre, string mensaje);
    Task<object> GetSugerenciasAsync();
    Task<(bool success, string message)> MarcarLeidaAsync(Guid id);
}
