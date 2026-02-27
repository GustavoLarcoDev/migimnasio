// ═══════════════════════════════════════════════════════════
// MenuRestauranteService.cs — Gestión y generación de menus de restaurante
//
// Genera HTML bonito para menus con 5 estilos visuales.
// Para almuerzo/cena: precio fijo del combo.
// Para desayuno/general: precio individual por plato.
// ═══════════════════════════════════════════════════════════

using System.Text;
using System.Text.Json;
using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class MenuRestauranteService : IMenuRestauranteService
{
    private readonly ApplicationDbContext _context;

    public MenuRestauranteService(ApplicationDbContext context) => _context = context;

    public async Task<object> GetMenusAsync(Guid negocioId)
    {
        return await _context.MenusRestaurante
            .Where(m => m.NegocioId == negocioId && m.IsActive)
            .OrderByDescending(m => m.FechaCreacion)
            .Select(m => new
            {
                m.MenuId,
                m.Nombre,
                m.TipoMenu,
                m.PrecioFijo,
                m.Estilo,
                m.Disponible,
                m.FechaCreacion
            })
            .ToListAsync();
    }

    public async Task<MenuRestaurante> GetMenuAsync(Guid menuId, Guid negocioId)
    {
        return await _context.MenusRestaurante
            .FirstOrDefaultAsync(m => m.MenuId == menuId && m.NegocioId == negocioId && m.IsActive);
    }

    public async Task<(bool success, string message)> CrearMenuAsync(MenuRestauranteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return (false, "El nombre del menu es obligatorio");
        if (dto.Nombre?.Length > 200)
            return (false, "El nombre no puede exceder 200 caracteres");

        var tiposValidos = new[] { "desayuno", "almuerzo", "cena", "general" };
        if (!tiposValidos.Contains(dto.TipoMenu))
            return (false, "Tipo de menu no valido");

        if (dto.PrecioFijo.HasValue && dto.PrecioFijo.Value < 0)
            return (false, "El precio no puede ser negativo");

        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.NegocioId == dto.NegocioId);
        if (negocio == null) return (false, "Negocio no encontrado");

        var menu = new MenuRestaurante
        {
            MenuId = Guid.NewGuid(),
            NegocioId = dto.NegocioId,
            Nombre = dto.Nombre.Trim(),
            TipoMenu = dto.TipoMenu,
            PrecioFijo = dto.PrecioFijo,
            ItemsJson = dto.ItemsJson ?? "[]",
            Estilo = dto.Estilo ?? "moderno",
            IsActive = true,
            FechaCreacion = TimeHelper.Now
        };

        menu.ContenidoHtml = GenerarHtml(menu, negocio);

        _context.MenusRestaurante.Add(menu);
        await _context.SaveChangesAsync();
        return (true, "Menu creado exitosamente");
    }

    public async Task<(bool success, string message)> EditarMenuAsync(MenuRestauranteDto dto)
    {
        if (dto.Nombre?.Length > 200)
            return (false, "El nombre no puede exceder 200 caracteres");
        if (dto.PrecioFijo.HasValue && dto.PrecioFijo.Value < 0)
            return (false, "El precio no puede ser negativo");

        var menu = await _context.MenusRestaurante
            .FirstOrDefaultAsync(m => m.MenuId == dto.MenuId && m.NegocioId == dto.NegocioId && m.IsActive);
        if (menu == null) return (false, "Menu no encontrado");

        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.NegocioId == dto.NegocioId);
        if (negocio == null) return (false, "Negocio no encontrado");

        if (!string.IsNullOrWhiteSpace(dto.Nombre)) menu.Nombre = dto.Nombre.Trim();
        if (!string.IsNullOrWhiteSpace(dto.TipoMenu)) menu.TipoMenu = dto.TipoMenu;
        if (!string.IsNullOrWhiteSpace(dto.Estilo)) menu.Estilo = dto.Estilo;
        menu.PrecioFijo = dto.PrecioFijo;
        menu.ItemsJson = dto.ItemsJson ?? menu.ItemsJson;

        menu.ContenidoHtml = GenerarHtml(menu, negocio);

        await _context.SaveChangesAsync();
        return (true, "Menu actualizado exitosamente");
    }

    public async Task<(bool success, string message)> EliminarMenuAsync(Guid menuId, Guid negocioId)
    {
        var menu = await _context.MenusRestaurante
            .FirstOrDefaultAsync(m => m.MenuId == menuId && m.NegocioId == negocioId && m.IsActive);
        if (menu == null) return (false, "Menu no encontrado");

        menu.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, "Menu eliminado exitosamente");
    }

    public async Task<(bool success, string message, bool? disponible)> CambiarDisponibilidadMenuAsync(Guid menuId, Guid negocioId)
    {
        var menu = await _context.MenusRestaurante
            .FirstOrDefaultAsync(m => m.MenuId == menuId && m.NegocioId == negocioId && m.IsActive);
        if (menu == null) return (false, "Menu no encontrado", null);

        menu.Disponible = !menu.Disponible;
        await _context.SaveChangesAsync();

        var estado = menu.Disponible ? "disponible" : "no disponible";
        return (true, $"Menu marcado como {estado}", menu.Disponible);
    }

    public async Task<string> GenerarHtmlMenuAsync(Guid menuId, Guid negocioId)
    {
        var menu = await _context.MenusRestaurante
            .FirstOrDefaultAsync(m => m.MenuId == menuId && m.NegocioId == negocioId && m.IsActive);
        if (menu == null) return null;

        // Obtener stock actual de los productos vinculados al menú
        // para ocultar platos agotados en la vista pública
        var secciones = ParseItemsJson(menu.ItemsJson);
        var productoIds = secciones
            .SelectMany(s => s.Items)
            .Where(i => i.ProductoId.HasValue && i.ProductoId != Guid.Empty)
            .Select(i => i.ProductoId.Value)
            .Distinct()
            .ToList();

        var stockMap = new Dictionary<Guid, int>();
        if (productoIds.Any())
        {
            stockMap = await _context.Productos
                .Where(p => productoIds.Contains(p.ProductoId) && p.IsActive)
                .ToDictionaryAsync(p => p.ProductoId, p => p.Stock);
        }

        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.NegocioId == negocioId);
        return GenerarHtml(menu, negocio, secciones, stockMap);
    }

    // ═══════════════════════════════════════════════════════════
    // GENERACIÓN DE HTML CON 5 ESTILOS
    // ═══════════════════════════════════════════════════════════

    private string GenerarHtml(MenuRestaurante menu, Gym negocio, List<MenuSeccion> secciones = null, Dictionary<Guid, int> stockMap = null)
    {
        Func<string, string> enc = System.Net.WebUtility.HtmlEncode;
        var nombreNeg = enc(negocio?.NegocioNombre ?? "Restaurante");
        var nombreMenu = enc(menu.Nombre);
        var tipo = menu.TipoMenu;
        var estilo = menu.Estilo ?? "moderno";
        var esPrecioFijo = (tipo == "almuerzo" || tipo == "cena") && menu.PrecioFijo.HasValue;

        // Parse items JSON si no se pasaron
        secciones ??= ParseItemsJson(menu.ItemsJson);
        stockMap ??= new Dictionary<Guid, int>();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='es'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'>");
        sb.AppendLine($"<title>{nombreMenu} - {nombreNeg}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(ObtenerCssBase());
        sb.AppendLine(ObtenerCssPorEstilo(estilo));
        sb.AppendLine("</style></head><body>");

        // Hero
        sb.AppendLine("<div class='hero'>");
        sb.AppendLine($"<h1>{nombreNeg}</h1>");
        sb.AppendLine($"<p class='menu-titulo'>{nombreMenu}</p>");
        if (esPrecioFijo)
            sb.AppendLine($"<p class='precio-fijo'>${menu.PrecioFijo.Value:F2}</p>");
        sb.AppendLine("</div>");

        // Contenido
        sb.AppendLine("<div class='container'>");
        foreach (var seccion in secciones)
        {
            var secNombre = enc(seccion.Nombre ?? "");
            if (!string.IsNullOrWhiteSpace(secNombre))
                sb.AppendLine($"<h2 class='section-title'>{secNombre}</h2>");

            foreach (var item in seccion.Items)
            {
                // Ocultar platos agotados (stock = 0) del menú público
                if (item.ProductoId.HasValue && item.ProductoId != Guid.Empty
                    && stockMap.TryGetValue(item.ProductoId.Value, out var stock) && stock <= 0)
                    continue;

                var itemNombre = enc(item.Nombre ?? "");
                sb.AppendLine("<div class='menu-item'>");
                sb.AppendLine($"<span class='item-name'>{itemNombre}</span>");
                sb.AppendLine("<span class='item-dots'></span>");
                if (!esPrecioFijo && item.Precio > 0)
                    sb.AppendLine($"<span class='item-price'>${item.Precio:F2}</span>");
                sb.AppendLine("</div>");
            }
        }

        if (!secciones.Any())
        {
            sb.AppendLine("<p class='empty-msg'>No hay platos en este menu aun.</p>");
        }

        sb.AppendLine("</div>");

        // Footer
        sb.AppendLine("<div class='footer'>");
        sb.AppendLine($"<p>{nombreNeg} &mdash; {ObtenerEtiquetaTipo(tipo)}</p>");
        sb.AppendLine("<p class='brand'>Generado con My-Negocio</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string ObtenerEtiquetaTipo(string tipo) => tipo switch
    {
        "desayuno" => "Menu de Desayuno",
        "almuerzo" => "Menu de Almuerzo",
        "cena" => "Menu de Cena",
        _ => "Menu General"
    };

    // ── Parse JSON de items ──
    private static List<MenuSeccion> ParseItemsJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<MenuSeccion>();
        try
        {
            return JsonSerializer.Deserialize<List<MenuSeccion>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<MenuSeccion>();
        }
        catch { return new List<MenuSeccion>(); }
    }

    private class MenuSeccion
    {
        public string Nombre { get; set; }
        public List<MenuItemDto> Items { get; set; } = new();
    }

    private class MenuItemDto
    {
        public Guid? ProductoId { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
    }

    // ── CSS Base (compartido por todos los estilos) ──
    private static string ObtenerCssBase() => @"
* { margin: 0; padding: 0; box-sizing: border-box; }
body { background: #fff; color: #333; }
.hero { text-align: center; padding: 40px 20px 30px; }
.hero h1 { font-size: 2rem; margin-bottom: 6px; }
.hero .menu-titulo { font-size: 1.1rem; opacity: 0.8; margin-bottom: 8px; }
.hero .precio-fijo { font-size: 1.6rem; font-weight: bold; margin-top: 8px; }
.container { max-width: 700px; margin: 0 auto; padding: 20px 24px 40px; }
.section-title { font-size: 1.2rem; margin: 28px 0 14px; padding-bottom: 6px; }
.menu-item { display: flex; align-items: baseline; gap: 6px; padding: 8px 0; }
.item-name { white-space: nowrap; }
.item-dots { flex: 1; border-bottom: 1px dotted #ccc; min-width: 20px; margin: 0 4px; position: relative; top: -4px; }
.item-price { white-space: nowrap; font-weight: bold; }
.empty-msg { text-align: center; color: #999; padding: 40px 0; }
.footer { text-align: center; padding: 30px 20px; font-size: 0.85rem; color: #999; }
.footer .brand { font-size: 0.75rem; margin-top: 4px; }
@media print { .footer .brand { display: none; } body { -webkit-print-color-adjust: exact; print-color-adjust: exact; } }
@media (max-width: 600px) { .container { padding: 16px; } .hero h1 { font-size: 1.5rem; } }
";

    // ── CSS por estilo ──
    private static string ObtenerCssPorEstilo(string estilo) => estilo switch
    {
        "elegante" => @"
@import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;700&display=swap');
body { font-family: 'Playfair Display', 'Times New Roman', serif; background: #faf8f5; }
.hero { background: #2c3e50; color: #fff; }
.hero .precio-fijo { color: #d4af37; }
.section-title { font-style: italic; border-bottom: 1px solid #2c3e50; color: #2c3e50; letter-spacing: 2px; text-transform: uppercase; font-size: 1rem; }
.item-name { font-style: italic; }
.item-dots { border-bottom-color: #c0a56e; }
.item-price { color: #2c3e50; font-family: 'Courier New', monospace; }
.footer { color: #8b7e6e; }
",
        "minimalista" => @"
body { font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; }
.hero { background: #000; color: #fff; padding: 50px 20px 40px; }
.hero h1 { font-weight: 300; letter-spacing: 4px; text-transform: uppercase; font-size: 1.6rem; }
.hero .menu-titulo { font-weight: 300; letter-spacing: 2px; }
.hero .precio-fijo { color: #fff; font-weight: 300; }
.section-title { font-weight: 400; letter-spacing: 3px; text-transform: uppercase; font-size: 0.9rem; border-bottom: 1px solid #000; }
.item-dots { border-bottom-color: #ddd; }
.item-price { font-weight: 400; }
",
        "vibrante" => @"
@import url('https://fonts.googleapis.com/css2?family=Poppins:wght@400;700;900&display=swap');
body { font-family: 'Poppins', sans-serif; background: #fff5f5; }
.hero { background: linear-gradient(135deg, #FF0076, #590FB7); color: #fff; border-radius: 0 0 30px 30px; }
.hero h1 { font-weight: 900; }
.hero .precio-fijo { color: #FFD700; font-weight: 900; }
.section-title { color: #FF0076; font-weight: 700; border-bottom: 2px dashed #FF0076; }
.item-dots { border-bottom: 2px dotted #FF0076; opacity: 0.4; }
.item-price { color: #590FB7; font-weight: 700; }
.menu-item { padding: 10px 0; }
",
        "clasico" => @"
body { font-family: Georgia, 'Times New Roman', serif; background: #fdf8f0; color: #4a3728; }
.hero { background: #8b5a2b; color: #fdf8f0; }
.hero .precio-fijo { color: #ffd700; }
.section-title { color: #8b5a2b; border-bottom: 2px solid #c09060; font-variant: small-caps; letter-spacing: 1px; }
.item-dots { border-bottom-color: #c09060; }
.item-price { color: #8b5a2b; }
.footer { color: #a08060; }
",
        // moderno (default)
        _ => @"
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700&display=swap');
body { font-family: 'Inter', sans-serif; }
.hero { background: linear-gradient(135deg, #2563eb, #3b82f6); color: #fff; border-radius: 0 0 20px 20px; }
.hero .precio-fijo { color: #bfdbfe; }
.section-title { color: #2563eb; font-weight: 700; border-bottom: 2px solid #2563eb; }
.item-price { color: #2563eb; }
.menu-item { border-bottom: 1px solid #f0f0f0; }
.menu-item:last-child { border-bottom: none; }
"
    };
}
