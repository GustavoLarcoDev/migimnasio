#nullable enable

namespace Gimnasio.Services;

public interface IReciboService
{
    Task<string> ObtenerSiguienteNumeroAsync(Guid? negocioId);

    Task CrearReciboAsync(Guid? negocioId, string numeroRecibo, string tipoRecibo,
        string destinatarioEmail, string destinatarioNombre, string negocioNombre,
        string concepto, decimal monto, string contenidoHtml);

    Task<List<object>> GetRecibosAsync(Guid negocioId);

    Task<object?> GetReciboAsync(Guid reciboId, Guid negocioId);

    Task<object?> BuscarPorNumeroAsync(int numero, Guid negocioId);

    Task<byte[]> ExportRecibosExcelAsync(Guid negocioId, int anio, int mes);

    Task<DateTime?> GetFechaReciboMasAntiguoAsync(Guid negocioId);

    Task<int> EliminarRecibosAntiguosAsync(Guid negocioId, DateTime anteriorA);

    Task<List<object>> GetRecibosAdminAsync();

    Task<object?> GetReciboAdminAsync(Guid reciboId);
}
