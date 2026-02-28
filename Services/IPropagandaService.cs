using Gimnasio.Models;

namespace Gimnasio.Services;

public interface IPropagandaService
{
    Task<List<object>> GetDisenosAsync(Guid negocioId);
    Task<DisenoMarketing> GetDisenoAsync(Guid disenoId, Guid negocioId);
    Task<(bool success, string message, Guid? dataId)> CrearDisenoAsync(Guid negocioId, string nombre, string canvasJson, string thumbnailDataUri, int ancho, int alto);
    Task<(bool success, string message)> GuardarDisenoAsync(Guid disenoId, Guid negocioId, string nombre, string canvasJson, string thumbnailDataUri);
    Task<(bool success, string message)> EliminarDisenoAsync(Guid disenoId, Guid negocioId);
    Task<(bool success, string message)> DuplicarDisenoAsync(Guid disenoId, Guid negocioId);
}
