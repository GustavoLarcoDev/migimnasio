using Gimnasio.Data;
using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class InventarioController : Controller
{
    private readonly IInventarioService _inventarioService;
    private readonly IVentaProductoService _ventaService;
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsAppService;

    public InventarioController(
        IInventarioService inventarioService,
        IVentaProductoService ventaService,
        IAuthService authService,
        ApplicationDbContext context,
        IEmailService emailService,
        IWhatsAppService whatsAppService)
    {
        _inventarioService = inventarioService;
        _ventaService = ventaService;
        _authService = authService;
        _context = context;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
    }

    [HttpGet("GetProductos")]
    public async Task<IActionResult> GetProductos(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var productos = await _inventarioService.GetProductosAsync(negocioId);
            return Ok(productos);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("GetProducto")]
    public async Task<IActionResult> GetProducto(Guid id, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var producto = await _inventarioService.GetProductoAsync(id, negocioId);
            if (producto == null) return NotFound();

            return Ok(new
            {
                producto.ProductoId,
                producto.Nombre,
                producto.PrecioVenta,
                producto.CostoCompra,
                producto.Stock,
                producto.StockMinimo,
                producto.CategoriaProductoId
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("CrearProducto")]
    public async Task<IActionResult> CrearProducto([FromForm] ProductoCreateDto model)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || model.NegocioId != nId.Value)
                return Forbid();

            var (success, message, dataId) = await _inventarioService.CrearProductoAsync(model);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message, dataId });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("EditarProducto")]
    public async Task<IActionResult> EditarProducto([FromForm] ProductoCreateDto model)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || model.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _inventarioService.EditarProductoAsync(model);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("EliminarProducto")]
    public async Task<IActionResult> EliminarProducto(Guid id, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _inventarioService.EliminarProductoAsync(id, negocioId);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("VenderProducto")]
    public async Task<IActionResult> VenderProducto(Guid productoId, Guid negocioId, int cantidad)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _inventarioService.VenderProductoAsync(productoId, negocioId, cantidad);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("DevolverProducto")]
    public async Task<IActionResult> DevolverProducto(Guid productoId, Guid negocioId, int cantidad, string nota)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _inventarioService.DevolverProductoAsync(productoId, negocioId, cantidad, nota);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("RestockProducto")]
    public async Task<IActionResult> RestockProducto(Guid productoId, Guid negocioId, int cantidad, decimal costoTotal)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _inventarioService.RestockAsync(productoId, negocioId, cantidad, costoTotal);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("AjustarStock")]
    public async Task<IActionResult> AjustarStock(Guid productoId, Guid negocioId, int stockReal, string nota)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _inventarioService.AjustarStockAsync(productoId, negocioId, stockReal, nota);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("GetMovimientos")]
    public async Task<IActionResult> GetMovimientos(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var movimientos = await _inventarioService.GetMovimientosAsync(negocioId);
            return Ok(movimientos);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("GetInventarioStats")]
    public async Task<IActionResult> GetInventarioStats(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var stats = await _inventarioService.GetInventarioStatsAsync(negocioId);
            return Ok(stats);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("ExportInventarioExcel")]
    public async Task<IActionResult> ExportInventarioExcel(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var content = await _inventarioService.ExportInventarioExcelAsync(negocioId);
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Inventario_{TimeHelper.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // TIENDA / CATEGORÍAS É IMÁGENES
    // ═══════════════════════════════════════════════════════════

    [HttpGet("GetCategorias")]
    public async Task<IActionResult> GetCategorias(Guid negocioId)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();

        var categorias = await _inventarioService.GetCategoriasAsync(negocioId);
        return Ok(categorias);
    }

    [HttpPost("CrearCategoria")]
    public async Task<IActionResult> CrearCategoria([FromForm] Guid negocioId, [FromForm] string nombre)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();

        var (success, message) = await _inventarioService.CrearCategoriaAsync(negocioId, nombre);
        return success ? Ok(new { success, message }) : BadRequest(new { success, message });
    }

    [HttpPost("EliminarCategoria")]
    public async Task<IActionResult> EliminarCategoria([FromForm] Guid id, [FromForm] Guid negocioId)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();

        var (success, message) = await _inventarioService.EliminarCategoriaAsync(id, negocioId);
        return success ? Ok(new { success, message }) : BadRequest(new { success, message });
    }

    [HttpPost("ReordenarCategorias")]
    public async Task<IActionResult> ReordenarCategorias([FromBody] List<Guid> categoriasIds, [FromQuery] Guid negocioId)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();

        var (success, message) = await _inventarioService.ReordenarCategoriasAsync(negocioId, categoriasIds);
        return success ? Ok(new { success, message }) : BadRequest(new { success, message });
    }

    [HttpPost("MoverProductoDeCategoria")]
    public async Task<IActionResult> MoverProductoDeCategoria([FromForm] Guid productoId, [FromForm] Guid? categoriaId, [FromForm] Guid negocioId)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();

        var (success, message) = await _inventarioService.MoverProductoDeCategoriaAsync(productoId, negocioId, categoriaId);
        return success ? Ok(new { success, message }) : BadRequest(new { success, message });
    }

    [HttpPost("SubirImagenProducto")]
    public async Task<IActionResult> SubirImagenProducto([FromForm] Guid productoId, [FromForm] Guid negocioId, IFormFile imagen)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();

        if (imagen == null || imagen.Length == 0)
            return BadRequest(new { success = false, message = "No se ha proporcionado ninguna imagen." });

        if (imagen.Length > 2 * 1024 * 1024)
            return BadRequest(new { success = false, message = "La imagen no debe superar los 2MB." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { success = false, message = "Formato de imagen no permitido." });

        var folderRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "productos", negocioId.ToString());
        if (!Directory.Exists(folderRoot)) Directory.CreateDirectory(folderRoot);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(folderRoot, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imagen.CopyToAsync(stream);
        }

        var urlImage = $"/uploads/productos/{negocioId}/{fileName}";

        var (success, message) = await _inventarioService.CambiarImagenProductoAsync(productoId, negocioId, urlImage);

        return success ? Ok(new { success, message, url = urlImage }) : BadRequest(new { success, message });
    }

    // ═══════════════════════════════════════════════════════════
    // POS — Punto de Venta (Tienda)
    // ═══════════════════════════════════════════════════════════

    [HttpPost("ProcesarVentaPOS")]
    public async Task<IActionResult> ProcesarVentaPOS([FromBody] OrdenVentaCreateRequest request)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (nId == null) return Unauthorized();

            if (request == null || request.Items == null || !request.Items.Any())
                return Json(new { success = false, message = "Orden vacía." });

            var res = await _ventaService.RegistrarVentaAsync(
                nId.Value,
                request.NombreCliente,
                request.EmailCliente,
                request.Items,
                request.DescuentoAdicional,
                request.PorcentajeIva
            );

            return Json(new { success = res.success, message = res.message, ordenId = res.ordenId, reciboId = res.reciboId });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("GetOrdenesVenta")]
    public async Task<IActionResult> GetOrdenesVenta(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value) return Forbid();

            var ordenes = await _ventaService.GetOrdenesVentaAsync(negocioId);
            return Ok(ordenes.Select(o => new
            {
                o.OrdenVentaId,
                o.NumeroOrden,
                o.NombreCliente,
                o.Subtotal,
                o.PorcentajeIva,
                o.MontoIva,
                o.Total,
                o.FechaCreacion,
                o.ReciboId,
                Items = o.Detalles.Count
            }));
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("GetOrdenVenta")]
    public async Task<IActionResult> GetOrdenVenta(Guid ordenId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value) return Forbid();

            var orden = await _ventaService.GetOrdenVentaAsync(ordenId, negocioId);
            if (orden == null) return NotFound();

            return Ok(new
            {
                orden.OrdenVentaId,
                orden.NumeroOrden,
                orden.NombreCliente,
                orden.Subtotal,
                orden.PorcentajeIva,
                orden.MontoIva,
                orden.Total,
                orden.FechaCreacion,
                orden.ReciboId,
                Detalles = orden.Detalles.Select(d => new
                {
                    d.DetalleId,
                    d.ProductoId,
                    NombreProducto = d.Producto?.Nombre ?? "Producto eliminado",
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal
                })
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("EnviarRecibo")]
    public async Task<IActionResult> EnviarRecibo([FromForm] Guid reciboId, [FromForm] Guid negocioId, [FromForm] string destino, [FromForm] string tipo)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();
        if (string.IsNullOrWhiteSpace(destino)) return Json(new { success = false, message = "Destino requerido" });

        var recibo = await _context.Recibos
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReciboId == reciboId && r.NegocioId == negocioId);
        if (recibo == null) return Json(new { success = false, message = "Recibo no encontrado" });

        if (tipo == "email")
        {
            var enviado = await _emailService.EnviarReciboPorEmailGenericoAsync(
                destino,
                $"Tu recibo de compra #{recibo.NumeroRecibo:D6} - {recibo.NegocioNombre}",
                recibo.ContenidoHtml);
            return Json(new { success = enviado, message = enviado ? "Recibo enviado por email" : "Error al enviar email" });
        }
        else if (tipo == "whatsapp")
        {
            var msg = $"Hola! Aqui esta tu recibo de compra #{recibo.NumeroRecibo:D6} de {recibo.NegocioNombre} por ${recibo.Monto:F2}. Gracias por tu compra!";
            var enviado = await _whatsAppService.EnviarMensajeTextoAsync(destino, msg);
            return Json(new { success = enviado, message = enviado ? "Recibo enviado por WhatsApp" : "Error al enviar WhatsApp" });
        }

        return Json(new { success = false, message = "Tipo de envio no soportado" });
    }
}
