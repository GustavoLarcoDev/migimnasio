// ═══════════════════════════════════════════════════════════
// ICatalogoService.cs — Contrato para Catálogos Dinámicos
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;

namespace Gimnasio.Services;

public interface ICatalogoService
{
    /// <summary>
    /// Genera un catálogo en memoria de todos los productos activos de una Tienda.
    /// Soporta 5 estilos de diseño ('clasico', 'moderno', 'elegante', 'minimalista', 'vibrante').
    /// El catálogo incluye Categorías e Imágenes.
    /// Retorna un arreglo de bytes (PDF) para descarga o envío por correo.
    /// </summary>
    Task<byte[]> GenerarCatalogoPdfAsync(Guid negocioId, string estiloDiseño = "moderno");

    /// <summary>
    /// Genera la vista HTML completa del catálogo de una Tienda.
    /// Útil para visualizarlo directamente en navegador (Web/Compartir por link).
    /// </summary>
    Task<string> GenerarCatalogoHtmlAsync(Guid negocioId, string estiloDiseño = "moderno");
}
