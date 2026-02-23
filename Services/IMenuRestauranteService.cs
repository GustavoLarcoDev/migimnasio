// ═══════════════════════════════════════════════════════════
// IMenuRestauranteService.cs — Contrato del servicio de menus de restaurante
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;

namespace Gimnasio.Services;

public interface IMenuRestauranteService
{
    Task<object> GetMenusAsync(Guid negocioId);
    Task<MenuRestaurante> GetMenuAsync(Guid menuId, Guid negocioId);
    Task<(bool success, string message)> CrearMenuAsync(MenuRestauranteDto dto);
    Task<(bool success, string message)> EditarMenuAsync(MenuRestauranteDto dto);
    Task<(bool success, string message)> EliminarMenuAsync(Guid menuId, Guid negocioId);
    Task<(bool success, string message, bool? disponible)> CambiarDisponibilidadMenuAsync(Guid menuId, Guid negocioId);
    Task<string> GenerarHtmlMenuAsync(Guid menuId, Guid negocioId);
}

public class MenuRestauranteDto
{
    public Guid MenuId { get; set; }
    public Guid NegocioId { get; set; }
    public string Nombre { get; set; }
    public string TipoMenu { get; set; }
    public decimal? PrecioFijo { get; set; }
    public string ItemsJson { get; set; }
    public string Estilo { get; set; } = "moderno";
}
