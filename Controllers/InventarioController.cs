using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class InventarioController : Controller
{
    private readonly IInventarioService _inventarioService;
    private readonly IAuthService _authService;

    public InventarioController(IInventarioService inventarioService, IAuthService authService)
    {
        _inventarioService = inventarioService;
        _authService = authService;
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
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
                producto.StockMinimo
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
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

            var (success, message) = await _inventarioService.CrearProductoAsync(model);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
