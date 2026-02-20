// ═══════════════════════════════════════════════════════════
// CatalogoService.cs — Generador de Catálogos (Tienda)
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;
using SelectPdf;
using System.Text;

namespace Gimnasio.Services;

public class CatalogoService : ICatalogoService
{
    private readonly ApplicationDbContext _context;

    public CatalogoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerarCatalogoHtmlAsync(Guid negocioId, string estiloDiseño = "moderno")
    {
        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.NegocioId == negocioId);
        if (negocio == null) return "<h1>Negocio no encontrado</h1>";

        var categorias = await _context.CategoriasProducto
            .Include(c => c.Productos.Where(p => p.IsActive))
            .Where(c => c.NegocioId == negocioId)
            .OrderBy(c => c.Orden)
            .ToListAsync();

        var productosSinCategoria = await _context.Productos
            .Where(p => p.NegocioId == negocioId && p.IsActive && p.CategoriaProductoId == null)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        // Si hay productos sin categoria, creamos una categoria "dummy" para mostrarlos al final
        if (productosSinCategoria.Any())
        {
            categorias.Add(new CategoriaProducto
            {
                Nombre = "Otros / Sin Categorizar",
                Productos = productosSinCategoria
            });
        }

        var sb = new StringBuilder();

        // Cabecera común obligatoria HTML5
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"UTF-8\">");
        sb.AppendLine($"<title>Catálogo - {negocio.NegocioNombre}</title>");
        
        // CSS inyectado según el Estilo Elegido
        sb.AppendLine("<style>");
        sb.AppendLine(ObtenerCssPorEstilo(estiloDiseño));
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");

        // HERO SECTION (Portada)
        sb.AppendLine("<div class='hero'>");
        sb.AppendLine($"  <h1>{negocio.NegocioNombre}</h1>");
        sb.AppendLine("  <p>Catálogo de Productos</p>");
        sb.AppendLine("</div>");

        // BODY
        sb.AppendLine("<div class='container'>");

        foreach (var cat in categorias)
        {
            if (!cat.Productos.Any()) continue; // No mostrar categorías vacías

            sb.AppendLine($"<h2 class='category-title'>{cat.Nombre}</h2>");
            sb.AppendLine("<div class='grid'>");

            foreach(var prod in cat.Productos)
            {
                sb.AppendLine("  <div class='card'>");
                
                string imagen = string.IsNullOrEmpty(prod.ImagenUrl) 
                    ? "https://via.placeholder.com/300x300?text=Sin+Imagen" 
                    : prod.ImagenUrl;

                sb.AppendLine($"    <img src='{imagen}' alt='{prod.Nombre}' />");
                sb.AppendLine("    <div class='card-body'>");
                sb.AppendLine($"      <h3 class='product-title'>{prod.Nombre}</h3>");
                sb.AppendLine($"      <p class='product-price'>${prod.PrecioVenta:F2}</p>");
                sb.AppendLine("    </div>");
                sb.AppendLine("  </div>");
            }

            sb.AppendLine("</div>"); // Fin grid
        }

        sb.AppendLine("</div>"); // Fin container

        // FOOTER
        sb.AppendLine("<div class='footer'>");
        sb.AppendLine($"  <p>© {TimeHelper.Now.Year} {negocio.NegocioNombre}. Todos los derechos reservados.</p>");
        sb.AppendLine("  <p>Generado a través de My-Negocio</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    public async Task<byte[]> GenerarCatalogoPdfAsync(Guid negocioId, string estiloDiseño = "moderno")
    {
        var htmlContent = await GenerarCatalogoHtmlAsync(negocioId, estiloDiseño);

        var converter = new HtmlToPdf();
        converter.Options.PdfPageSize = PdfPageSize.A4;
        converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
        converter.Options.WebPageWidth = 1024;
        converter.Options.MarginLeft = 20;
        converter.Options.MarginRight = 20;
        converter.Options.MarginTop = 20;
        converter.Options.MarginBottom = 20;

        // Necesario para cargar las imágenes desde URLs externas
        converter.Options.MinPageLoadTime = 2; 

        PdfDocument doc = converter.ConvertHtmlString(htmlContent);
        
        using var stream = new MemoryStream();
        doc.Save(stream);
        doc.Close();

        return stream.ToArray();
    }


    /// <summary>
    /// Devuelve el CSS embebido para los 5 estilos (Moderno es el default).
    /// </summary>
    private string ObtenerCssPorEstilo(string estilo)
    {
        var cssBase = @"
            body { margin: 0; padding: 0; background-color: #f9fafb; }
            .container { width: 100%; max-width: 1000px; margin: 0 auto; padding: 20px; }
            .grid { display: flex; flex-wrap: wrap; gap: 20px; justify-content: center; }
            .card { width: 30%; background: #fff; display: flex; flex-direction: column; overflow: hidden; page-break-inside: avoid; }
            .card img { width: 100%; height: 250px; object-fit: cover; }
            .card-body { padding: 15px; text-align: center; }
            .product-title { margin: 0; font-size: 18px; font-weight: bold; }
            .product-price { margin: 10px 0 0; font-size: 20px; }
            .hero { text-align: center; padding: 60px 20px; color: white; margin-bottom: 40px; page-break-after: avoid; }
            .hero h1 { margin: 0; font-size: 48px; }
            .hero p { margin: 10px 0 0; font-size: 24px; opacity: 0.9; }
            .category-title { border-bottom: 2px solid #eee; padding-bottom: 10px; margin-top: 40px; margin-bottom: 20px; font-size: 28px; }
            .footer { text-align: center; padding: 30px; margin-top: 50px; color: #666; font-size: 14px; page-break-inside: avoid; }
        ";

        // Variaciones de Diseño
        string styleOverrides = estilo.ToLower() switch
        {
            "elegante" => @"
                body { font-family: 'Times New Roman', serif; background-color: #fbfbf9; }
                .hero { background-color: #2c3e50; }
                .card { border: 1px solid #dcdde1; box-shadow: none; border-radius: 0; }
                .product-price { color: #2c3e50; font-family: 'Courier New', monospace; }
                .category-title { color: #2c3e50; font-style: italic; text-align: center; border-bottom: 1px solid #2c3e50; }
            ",
            "minimalista" => @"
                body { font-family: 'Helvetica Neue', Arial, sans-serif; background-color: #ffffff; }
                .hero { background-color: #ffffff; color: #000; border-bottom: 1px solid #000; padding: 40px 20px; }
                .hero p { opacity: 0.6; }
                .card { border: none; box-shadow: none; }
                .card img { filter: grayscale(20%); }
                .product-title { font-weight: 400; font-size: 16px; color: #333; }
                .product-price { font-weight: bold; color: #000; }
                .category-title { border: none; color: #000; font-weight: 300; font-size: 20px; }
            ",
            "vibrante" => @"
                body { font-family: 'Poppins', sans-serif; background-color: #f0fdf4; }
                .hero { background: linear-gradient(135deg, #FF0076, #590FB7); }
                .card { border-radius: 16px; box-shadow: 0 10px 20px rgba(0,0,0,0.1); border: 2px solid transparent; }
                .product-price { color: #FF0076; font-weight: 900; }
                .category-title { color: #590FB7; border-bottom: 3px dashed #FF0076; font-weight: 900; }
            ",
            "clasico" => @"
                body { font-family: 'Georgia', serif; background-color: #f4f1ea; }
                .hero { background-color: #8b5a2b; border-bottom: 5px solid #5c3a21; }
                .card { border: 1px solid #d3c4a9; border-radius: 4px; box-shadow: 2px 2px 5px rgba(0,0,0,0.1); }
                .product-title { color: #5c3a21; }
                .product-price { color: #8b5a2b; font-weight: bold; }
                .category-title { color: #5c3a21; border-bottom: 2px solid #8b5a2b; }
            ",
            "moderno" or _ => @"
                body { font-family: 'Inter', sans-serif; background-color: #f8fafc; }
                .hero { background: linear-gradient(to right, #2563eb, #3b82f6); border-radius: 12px; margin-top: 10px; }
                .card { border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1); border: 1px solid #e2e8f0; }
                .product-title { color: #1e293b; }
                .product-price { color: #2563eb; font-weight: bold; }
                .category-title { color: #0f172a; border-bottom: 2px solid #e2e8f0; }
            "
        };

        return cssBase + styleOverrides;
    }
}
