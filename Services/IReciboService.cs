// ═══════════════════════════════════════════════════════════
// IReciboService.cs — Contrato del servicio de recibos digitales
//
// Define las operaciones para gestionar recibos de pago:
// generacion de numeros secuenciales, almacenamiento, consulta,
// busqueda por numero, exportacion Excel y limpieza de datos.
// ═══════════════════════════════════════════════════════════

#nullable enable

namespace Gimnasio.Services;

public interface IReciboService
{
    /// <summary>
    /// Obtiene el siguiente numero secuencial de recibo para el negocio (formato D6: 000001).
    /// </summary>
    Task<string> ObtenerSiguienteNumeroAsync(Guid? negocioId);

    /// <summary>
    /// Crea un nuevo recibo con todo su contenido HTML almacenado para re-impresion.
    /// </summary>
    Task CrearReciboAsync(Guid? negocioId, string numeroRecibo, string tipoRecibo,
        string destinatarioEmail, string destinatarioNombre, string negocioNombre,
        string concepto, decimal monto, string contenidoHtml);

    /// <summary>
    /// Obtiene todos los recibos del negocio ordenados del mas reciente al mas antiguo.
    /// </summary>
    Task<List<object>> GetRecibosAsync(Guid negocioId);

    /// <summary>
    /// Obtiene un recibo especifico con su contenido HTML completo (filtrado por NegocioId).
    /// </summary>
    Task<object?> GetReciboAsync(Guid reciboId, Guid negocioId);

    /// <summary>
    /// Busca un recibo por su numero secuencial dentro del negocio.
    /// </summary>
    Task<object?> BuscarPorNumeroAsync(int numero, Guid negocioId);

    /// <summary>
    /// Exporta los recibos de un mes especifico a Excel con totales.
    /// </summary>
    Task<byte[]> ExportRecibosExcelAsync(Guid negocioId, int anio, int mes);

    /// <summary>
    /// Obtiene la fecha del recibo mas antiguo del negocio (para UI de limpieza).
    /// </summary>
    Task<DateTime?> GetFechaReciboMasAntiguoAsync(Guid negocioId);

    /// <summary>
    /// Elimina recibos anteriores a una fecha (limpieza de datos antiguos).
    /// </summary>
    Task<int> EliminarRecibosAntiguosAsync(Guid negocioId, DateTime anteriorA);

    /// <summary>
    /// Obtiene todos los recibos del admin (NegocioId = null): comisiones y pagos SaaS.
    /// </summary>
    Task<List<object>> GetRecibosAdminAsync();

    /// <summary>
    /// Obtiene un recibo especifico del admin con su contenido HTML completo.
    /// </summary>
    Task<object?> GetReciboAdminAsync(Guid reciboId);
}
