// ═══ InventarioController.cs — CRUD productos, stock, categorias, POS y recibos ═══

using Gimnasio.Data;
using Gimnasio.Helpers;
using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

public class InventarioController : NegocioBaseController
{
    private readonly IInventarioService _inventarioService;
    private readonly IVentaProductoService _ventaService;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public InventarioController(
        IInventarioService inventarioService,
        IVentaProductoService ventaService,
        IAuthService authService,
        ApplicationDbContext context,
        IEmailService emailService) : base(authService)
    {
        _inventarioService = inventarioService;
        _ventaService = ventaService;
        _context = context;
        _emailService = emailService;
    }

    // ═══ CRUD de productos ═══

    [HttpGet("GetProductos")]
    public Task<IActionResult> GetProductos(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _inventarioService.GetProductosAsync(nId)));

    [HttpGet("GetProducto")]
    public Task<IActionResult> GetProducto(Guid id, Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var producto = await _inventarioService.GetProductoAsync(id, nId);
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
        });

    [HttpPost("CrearProducto")]
    public Task<IActionResult> CrearProducto([FromForm] ProductoCreateDto model)
        => Execute(model.NegocioId, async nId => ServiceResult(await _inventarioService.CrearProductoAsync(model)));

    [HttpPost("EditarProducto")]
    public Task<IActionResult> EditarProducto([FromForm] ProductoCreateDto model)
        => Execute(model.NegocioId, async nId => ServiceResult(await _inventarioService.EditarProductoAsync(model)));

    [HttpPost("EliminarProducto")]
    public Task<IActionResult> EliminarProducto(Guid id, Guid negocioId)
        => Execute(negocioId, async nId => ServiceResult(await _inventarioService.EliminarProductoAsync(id, nId)));

    // ═══ Operaciones de stock ═══

    [HttpPost("VenderProducto")]
    public Task<IActionResult> VenderProducto(Guid productoId, Guid negocioId, int cantidad, string metodoPago = "Efectivo", string numeroConfirmacion = null)
        => Execute(negocioId, async nId => ServiceResult(await _inventarioService.VenderProductoAsync(productoId, nId, cantidad, metodoPago, numeroConfirmacion)));

    [HttpPost("DevolverProducto")]
    public Task<IActionResult> DevolverProducto(Guid productoId, Guid negocioId, int cantidad, string nota)
        => Execute(negocioId, async nId => ServiceResult(await _inventarioService.DevolverProductoAsync(productoId, nId, cantidad, nota)));

    [HttpPost("RestockProducto")]
    public Task<IActionResult> RestockProducto(Guid productoId, Guid negocioId, int cantidad, decimal costoTotal)
        => Execute(negocioId, async nId => ServiceResult(await _inventarioService.RestockAsync(productoId, nId, cantidad, costoTotal)));

    [HttpPost("AjustarStock")]
    public Task<IActionResult> AjustarStock(Guid productoId, Guid negocioId, int stockReal, string nota)
        => Execute(negocioId, async nId => ServiceResult(await _inventarioService.AjustarStockAsync(productoId, nId, stockReal, nota)));

    // ═══ Consultas y reportes ═══

    [HttpGet("GetMovimientos")]
    public Task<IActionResult> GetMovimientos(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _inventarioService.GetMovimientosAsync(nId)));

    [HttpGet("GetInventarioStats")]
    public Task<IActionResult> GetInventarioStats(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _inventarioService.GetInventarioStatsAsync(nId)));

    [HttpGet("ExportInventarioExcel")]
    public Task<IActionResult> ExportInventarioExcel(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var content = await _inventarioService.ExportInventarioExcelAsync(nId);
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Inventario_{TimeHelper.Now:yyyyMMdd}.xlsx");
        });

    // ═══ Categorias e imagenes ═══

    [HttpGet("GetCategorias")]
    public Task<IActionResult> GetCategorias(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _inventarioService.GetCategoriasAsync(nId)));

    [HttpPost("CrearCategoria")]
    public Task<IActionResult> CrearCategoria([FromForm] Guid negocioId, [FromForm] string nombre)
        => Execute(negocioId, async nId => ServiceResult(await _inventarioService.CrearCategoriaAsync(nId, nombre)));

    [HttpPost("EliminarCategoria")]
    public Task<IActionResult> EliminarCategoria([FromForm] Guid id, [FromForm] Guid negocioId)
        => Execute(negocioId, async nId => ServiceResult(await _inventarioService.EliminarCategoriaAsync(id, nId)));

    [HttpPost("CambiarCategoriaProducto")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> CambiarCategoriaProducto([FromBody] CambiarCategoriaRequest req)
        => ExecuteSelf(async nId =>
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.ProductoId == req.ProductoId && p.NegocioId == nId);
            if (producto == null) return NotFound(new { success = false, message = "Producto no encontrado" });
            producto.CategoriaProductoId = req.CategoriaProductoId;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Categoria actualizada" });
        });

    [HttpPost("ReordenarCategorias")]
    public Task<IActionResult> ReordenarCategorias([FromBody] List<Guid> categoriasIds, [FromQuery] Guid negocioId)
        => Execute(negocioId, async nId => ServiceResult(await _inventarioService.ReordenarCategoriasAsync(nId, categoriasIds)));

    [HttpPost("MoverProductoDeCategoria")]
    public Task<IActionResult> MoverProductoDeCategoria([FromForm] Guid productoId, [FromForm] Guid? categoriaId, [FromForm] Guid negocioId)
        => Execute(negocioId, async nId => ServiceResult(await _inventarioService.MoverProductoDeCategoriaAsync(productoId, nId, categoriaId)));

    [HttpPost("SubirImagenProducto")]
    public Task<IActionResult> SubirImagenProducto([FromForm] Guid productoId, [FromForm] Guid negocioId, IFormFile imagen)
        => Execute(negocioId, async nId =>
        {
            var (valid, error, dataUri) = await ImageUploadHelper.ProcessAsync(imagen);
            if (!valid) return BadRequest(new { success = false, message = error });

            var (success, message) = await _inventarioService.CambiarImagenProductoAsync(productoId, nId, dataUri!);
            return success ? Ok(new { success, message, url = dataUri }) : BadRequest(new { success, message });
        });

    // ═══ Punto de Venta (POS) ═══

    [HttpPost("ProcesarVentaPOS")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> ProcesarVentaPOS([FromBody] OrdenVentaCreateRequest request)
        => ExecuteSelf(async nId =>
        {
            if (request == null || request.Items == null || !request.Items.Any())
                return BadRequest(new { success = false, message = "Orden vacía." });

            var res = await _ventaService.RegistrarVentaAsync(
                nId, request.NombreCliente, request.EmailCliente,
                request.Items, request.DescuentoAdicional, request.PorcentajeIva,
                request.TipoOrden, request.MesaId, request.EmpleadoId,
                request.DireccionEntrega, request.MetodoPago,
                request.NumeroConfirmacion, request.CargosExtra);

            if (!res.success)
                return BadRequest(new { success = false, message = res.message });

            return Ok(new { success = true, message = res.message, ordenId = res.ordenId, reciboId = res.reciboId });
        });

    [HttpGet("GetOrdenesVenta")]
    public Task<IActionResult> GetOrdenesVenta(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var ordenes = await _ventaService.GetOrdenesVentaAsync(nId);
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
                o.MetodoPago,
                Items = o.Detalles.Count
            }));
        });

    [HttpGet("GetOrdenVenta")]
    public Task<IActionResult> GetOrdenVenta(Guid ordenId, Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var orden = await _ventaService.GetOrdenVentaAsync(ordenId, nId);
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
        });

    // ═══ Envio de recibos ═══

    [HttpPost("EnviarRecibo")]
    public Task<IActionResult> EnviarRecibo([FromForm] Guid reciboId, [FromForm] Guid negocioId, [FromForm] string destino, [FromForm] string tipo)
        => Execute(negocioId, async nId =>
        {
            if (string.IsNullOrWhiteSpace(destino))
                return BadRequest(new { success = false, message = "Destino requerido" });

            var recibo = await _context.Recibos
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ReciboId == reciboId && r.NegocioId == nId);
            if (recibo == null)
                return NotFound(new { success = false, message = "Recibo no encontrado" });

            if (tipo == "email")
            {
                var enviado = await _emailService.EnviarReciboPorEmailGenericoAsync(
                    destino,
                    $"Tu recibo de compra #{recibo.NumeroRecibo:D6} - {recibo.NegocioNombre}",
                    recibo.ContenidoHtml);
                return Ok(new { success = enviado, message = enviado ? "Recibo enviado por email" : "Error al enviar email" });
            }

            return BadRequest(new { success = false, message = "Tipo de envio no soportado" });
        });

    // ═══ Ventas por categoria y productos mas vendidos ═══

    [HttpGet("GetProductosMasVendidos")]
    public Task<IActionResult> GetProductosMasVendidos(Guid negocioId, string periodo = "mes")
        => Execute(negocioId, async nId =>
        {
            var desde = DateHelper.GetDesde(periodo);
            var datos = await _context.DetallesOrdenVenta
                .Include(d => d.OrdenVenta)
                .Include(d => d.Producto)
                .Where(d => d.OrdenVenta.NegocioId == nId && d.OrdenVenta.FechaCreacion >= desde)
                .GroupBy(d => d.Producto.Nombre)
                .Select(g => new
                {
                    producto = g.Key,
                    unidades = g.Sum(d => d.Cantidad),
                    totalVentas = g.Sum(d => d.Subtotal)
                })
                .OrderByDescending(x => x.unidades)
                .Take(10)
                .ToListAsync();
            return Ok(datos);
        });

    [HttpGet("GetVentasPorCategoria")]
    public Task<IActionResult> GetVentasPorCategoria(Guid negocioId, string periodo = "mes")
        => Execute(negocioId, async nId =>
        {
            var desde = DateHelper.GetDesde(periodo);
            var datos = await _context.DetallesOrdenVenta
                .Include(d => d.OrdenVenta)
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Categoria)
                .Where(d => d.OrdenVenta.NegocioId == nId && d.OrdenVenta.FechaCreacion >= desde)
                .GroupBy(d => d.Producto.Categoria != null ? d.Producto.Categoria.Nombre : "Sin Categoria")
                .Select(g => new
                {
                    categoria = g.Key,
                    totalVentas = g.Sum(d => d.Subtotal),
                    unidades = g.Sum(d => d.Cantidad)
                })
                .OrderByDescending(x => x.totalVentas)
                .ToListAsync();
            return Ok(datos);
        });
}

public class CambiarCategoriaRequest
{
    public Guid ProductoId { get; set; }
    public Guid? CategoriaProductoId { get; set; }
}
