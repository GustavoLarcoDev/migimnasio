// ═══════════════════════════════════════════════════════════════════════════════
// InventarioController.cs — Controlador de inventario, categorias, POS y recibos
//
// Maneja tres grandes bloques funcionales:
//   1. CRUD de productos y operaciones de stock (compartido por membresias y tienda)
//   2. Categorias e imagenes de productos (exclusivo del modelo Tienda)
//   3. Punto de Venta (POS) y envio de recibos (exclusivo del modelo Tienda)
//
// Todos los endpoints validan multi-tenancy comparando el NegocioId del
// claim de sesion con el NegocioId del request para evitar acceso cruzado.
// ═══════════════════════════════════════════════════════════════════════════════

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
    // ── Dependencias inyectadas ──────────────────────────────────────────────
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

    // ═══════════════════════════════════════════════════════════════════════════
    // SECCION 1 — CRUD DE PRODUCTOS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene la lista completa de productos activos del negocio.
    /// Devuelve campos calculados como margen unitario y alerta de stock bajo.
    /// </summary>
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

    /// <summary>
    /// Obtiene los datos de un producto especifico para pre-cargar el formulario de edicion.
    /// </summary>
    [HttpGet("GetProducto")]
    public async Task<IActionResult> GetProducto(Guid id, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var producto = await _inventarioService.GetProductoAsync(id, negocioId);
            if (producto == null) return NotFound(new { success = false, message = "Producto no encontrado" });

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

    /// <summary>
    /// Crea un nuevo producto en el inventario del negocio.
    /// Las validaciones de negocio se delegan al servicio, no se hacen aqui.
    /// </summary>
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

    /// <summary>
    /// Edita los metadatos de un producto (nombre, precios, stock minimo).
    /// No modifica el stock actual — eso se hace via los endpoints de movimiento.
    /// </summary>
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

    /// <summary>
    /// Elimina un producto de forma logica (IsActive = false).
    /// Los movimientos historicos se mantienen para trazabilidad.
    /// </summary>
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

    // ═══════════════════════════════════════════════════════════════════════════
    // SECCION 2 — OPERACIONES DE STOCK (Venta, Devolucion, Restock, Ajuste)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registra la venta de unidades de un producto. Reduce el stock y genera log de ingreso.
    /// </summary>
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

    /// <summary>
    /// Registra la devolucion de productos. Incrementa el stock y registra gasto (devolucion de dinero).
    /// </summary>
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

    /// <summary>
    /// Registra el reabastecimiento de stock por compra a proveedor. Incrementa stock y registra gasto.
    /// </summary>
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

    /// <summary>
    /// Ajusta el stock del sistema para que coincida con el conteo fisico real.
    /// Financieramente neutro (no es ingreso ni gasto).
    /// </summary>
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

    // ═══════════════════════════════════════════════════════════════════════════
    // SECCION 3 — CONSULTAS Y REPORTES DE INVENTARIO
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene el historial completo de movimientos de inventario (ventas, devoluciones, restocks, ajustes).
    /// </summary>
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

    /// <summary>
    /// Obtiene estadisticas del inventario para los widgets del dashboard
    /// (total productos, stock bajo, ventas del dia, valor total del inventario).
    /// </summary>
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

    /// <summary>
    /// Exporta el inventario completo a un archivo Excel (.xlsx) con dos hojas:
    /// "Productos" (catalogo actual) y "Movimientos" (historial de operaciones).
    /// </summary>
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

    // ═══════════════════════════════════════════════════════════════════════════
    // SECCION 4 — CATEGORIAS E IMAGENES (exclusivo modelo Tienda)
    //
    // Las categorias permiten agrupar productos en pestanas (tabs) dentro del
    // catalogo y del POS. El orden es configurable via drag & drop.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todas las categorias del negocio con sus productos activos incluidos.
    /// </summary>
    [HttpGet("GetCategorias")]
    public async Task<IActionResult> GetCategorias(Guid negocioId)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();

        var categorias = await _inventarioService.GetCategoriasAsync(negocioId);
        return Ok(categorias);
    }

    /// <summary>
    /// Crea una nueva categoria de productos. Se asigna al final del orden existente.
    /// </summary>
    [HttpPost("CrearCategoria")]
    public async Task<IActionResult> CrearCategoria([FromForm] Guid negocioId, [FromForm] string nombre)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();

        var (success, message) = await _inventarioService.CrearCategoriaAsync(negocioId, nombre);
        return success ? Ok(new { success, message }) : BadRequest(new { success, message });
    }

    /// <summary>
    /// Elimina una categoria. Los productos que pertenecian a ella quedan sin categoria (null).
    /// </summary>
    [HttpPost("EliminarCategoria")]
    public async Task<IActionResult> EliminarCategoria([FromForm] Guid id, [FromForm] Guid negocioId)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();

        var (success, message) = await _inventarioService.EliminarCategoriaAsync(id, negocioId);
        return success ? Ok(new { success, message }) : BadRequest(new { success, message });
    }

    /// <summary>
    /// Reordena las categorias segun el orden de IDs recibido (drag & drop del frontend).
    /// </summary>
    [HttpPost("ReordenarCategorias")]
    public async Task<IActionResult> ReordenarCategorias([FromBody] List<Guid> categoriasIds, [FromQuery] Guid negocioId)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();

        var (success, message) = await _inventarioService.ReordenarCategoriasAsync(negocioId, categoriasIds);
        return success ? Ok(new { success, message }) : BadRequest(new { success, message });
    }

    /// <summary>
    /// Mueve un producto de una categoria a otra (o lo deja sin categoria si categoriaId es null).
    /// </summary>
    [HttpPost("MoverProductoDeCategoria")]
    public async Task<IActionResult> MoverProductoDeCategoria([FromForm] Guid productoId, [FromForm] Guid? categoriaId, [FromForm] Guid negocioId)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();

        var (success, message) = await _inventarioService.MoverProductoDeCategoriaAsync(productoId, negocioId, categoriaId);
        return success ? Ok(new { success, message }) : BadRequest(new { success, message });
    }

    /// <summary>
    /// Sube una imagen para un producto. Validaciones:
    ///   - Tamanio maximo: 2 MB
    ///   - Formatos permitidos: .jpg, .jpeg, .png, .webp
    /// La imagen se guarda en wwwroot/uploads/productos/{negocioId}/{guid}.ext
    /// </summary>
    [HttpPost("SubirImagenProducto")]
    public async Task<IActionResult> SubirImagenProducto([FromForm] Guid productoId, [FromForm] Guid negocioId, IFormFile imagen)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();

        // Validar que se haya adjuntado un archivo
        if (imagen == null || imagen.Length == 0)
            return BadRequest(new { success = false, message = "No se ha proporcionado ninguna imagen." });

        // Limitar tamanio a 2 MB para evitar consumo excesivo de almacenamiento
        if (imagen.Length > 2 * 1024 * 1024)
            return BadRequest(new { success = false, message = "La imagen no debe superar los 2MB." });

        // Validar extension del archivo para prevenir subida de archivos maliciosos
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { success = false, message = "Formato de imagen no permitido." });

        // Crear directorio si no existe y guardar con nombre unico (GUID) para evitar colisiones
        var folderRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "productos", negocioId.ToString());
        if (!Directory.Exists(folderRoot)) Directory.CreateDirectory(folderRoot);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(folderRoot, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imagen.CopyToAsync(stream);
        }

        // Guardar la URL relativa en la BD para servir la imagen desde wwwroot
        var urlImage = $"/uploads/productos/{negocioId}/{fileName}";

        var (success, message) = await _inventarioService.CambiarImagenProductoAsync(productoId, negocioId, urlImage);

        return success ? Ok(new { success, message, url = urlImage }) : BadRequest(new { success, message });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SECCION 5 — PUNTO DE VENTA (POS) — exclusivo modelo Tienda
    //
    // El POS permite procesar ventas con multiples productos en una sola orden,
    // calcular IVA y descuentos, y generar recibos automaticamente.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Procesa una venta completa desde el POS. Flujo:
    ///   1. Valida que la orden tenga al menos un item
    ///   2. Descuenta stock de cada producto vendido
    ///   3. Calcula subtotal, IVA y total con descuento
    ///   4. Genera un recibo HTML profesional
    ///   5. Registra un log financiero de la venta
    /// Todo dentro de una transaccion para garantizar consistencia.
    /// </summary>
    [HttpPost("ProcesarVentaPOS")]
    [IgnoreAntiforgeryToken]
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
                request.PorcentajeIva,
                request.TipoOrden,
                request.MesaId,
                request.EmpleadoId,
                request.DireccionEntrega
            );

            return Json(new { success = res.success, message = res.message, ordenId = res.ordenId, reciboId = res.reciboId });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el listado de todas las ordenes de venta del negocio (historial POS).
    /// </summary>
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
                o.TipoOrden,
                Items = o.Detalles.Count
            }));
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el detalle completo de una orden de venta especifica, incluyendo
    /// todos los productos vendidos con cantidad, precio unitario y subtotal.
    /// </summary>
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

    // ═══════════════════════════════════════════════════════════════════════════
    // SECCION 6 — ENVIO DE RECIBOS (email y WhatsApp)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Envia un recibo existente al cliente por email o WhatsApp.
    /// El recibo ya fue generado y guardado en BD durante la venta POS;
    /// aqui solo se recupera y se envia al destino indicado.
    /// </summary>
    [HttpPost("EnviarRecibo")]
    public async Task<IActionResult> EnviarRecibo([FromForm] Guid reciboId, [FromForm] Guid negocioId, [FromForm] string destino, [FromForm] string tipo)
    {
        var nId = _authService.GetNegocioId(User);
        if (!nId.HasValue || negocioId != nId.Value) return Forbid();
        if (string.IsNullOrWhiteSpace(destino)) return Json(new { success = false, message = "Destino requerido" });

        // Buscar el recibo en la BD con filtro de NegocioId para seguridad multi-tenant
        var recibo = await _context.Recibos
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReciboId == reciboId && r.NegocioId == negocioId);
        if (recibo == null) return Json(new { success = false, message = "Recibo no encontrado" });

        if (tipo == "email")
        {
            // Enviar el HTML completo del recibo como cuerpo del correo
            var enviado = await _emailService.EnviarReciboPorEmailGenericoAsync(
                destino,
                $"Tu recibo de compra #{recibo.NumeroRecibo:D6} - {recibo.NegocioNombre}",
                recibo.ContenidoHtml);
            return Json(new { success = enviado, message = enviado ? "Recibo enviado por email" : "Error al enviar email" });
        }
        else if (tipo == "whatsapp")
        {
            // Por WhatsApp solo se envia un resumen de texto (no soporta HTML)
            var msg = $"Hola! Aqui esta tu recibo de compra #{recibo.NumeroRecibo:D6} de {recibo.NegocioNombre} por ${recibo.Monto:F2}. Gracias por tu compra!";
            var enviado = await _whatsAppService.EnviarMensajeTextoAsync(destino, msg);
            return Json(new { success = enviado, message = enviado ? "Recibo enviado por WhatsApp" : "Error al enviar WhatsApp" });
        }

        return Json(new { success = false, message = "Tipo de envio no soportado" });
    }
}
