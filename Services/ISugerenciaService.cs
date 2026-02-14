// ═══════════════════════════════════════════════════════════
// ISugerenciaService.cs — Contrato del servicio de sugerencias
// Define las operaciones para el sistema de feedback:
// los negocios envían sugerencias y el admin las gestiona
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Services;

public interface ISugerenciaService
{
    /// <summary>
    /// Crea una nueva sugerencia enviada por un negocio (máx. 1000 caracteres)
    /// </summary>
    Task<(bool success, string message)> CrearSugerenciaAsync(Guid negocioId, string negocioNombre, string mensaje);

    /// <summary>
    /// Obtiene todas las sugerencias ordenadas por fecha (para el admin)
    /// </summary>
    Task<object> GetSugerenciasAsync();

    /// <summary>
    /// Marca una sugerencia como leída (solo admin)
    /// </summary>
    Task<(bool success, string message)> MarcarLeidaAsync(Guid id);
}
