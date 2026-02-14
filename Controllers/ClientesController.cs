using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class ClientesController : Controller
{
    private readonly IClienteService _clienteService;
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;

    public ClientesController(IClienteService clienteService, IAuthService authService, ApplicationDbContext context)
    {
        _clienteService = clienteService;
        _authService = authService;
        _context = context;
    }

    [HttpGet("{id}/Dashboard")]
    public async Task<IActionResult> Dashboard(Guid id)
    {
        try
        {
            var negocioId = _authService.GetNegocioId(User);
            if (!negocioId.HasValue || negocioId.Value != id)
                return Forbid();

            var negocio = await _context.Negocios
                .Include(g => g.Clientes)
                .FirstOrDefaultAsync(g => g.NegocioId == id);

            if (negocio == null)
                return NotFound();

            return View("~/Views/Negocios/Dashboard.cshtml", negocio);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetDashboardStats")]
    public async Task<IActionResult> GetDashboardStats(Guid negocioId)
    {
        try
        {
            var stats = await _clienteService.GetDashboardStatsAsync(negocioId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetClientes")]
    public async Task<IActionResult> GetClientes(Guid negocioId)
    {
        try
        {
            var clientes = await _clienteService.GetClientesAsync(negocioId);
            return Ok(clientes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetCliente")]
    public async Task<IActionResult> GetCliente(Guid id, Guid negocioId)
    {
        try
        {
            var cliente = await _clienteService.GetClienteAsync(id, negocioId);
            if (cliente == null)
                return NotFound(new { success = false, message = "Cliente no encontrado" });

            return Ok(cliente);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("CrearCliente")]
    public async Task<IActionResult> CrearCliente([FromForm] ClienteCreateDto model)
    {
        try
        {
            var negocioId = _authService.GetNegocioId(User);
            if (!negocioId.HasValue || model.NegocioId != negocioId.Value)
                return Forbid();

            var (success, message) = await _clienteService.CrearClienteAsync(model);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("EditarCliente")]
    public async Task<IActionResult> EditarCliente([FromForm] ClienteCreateDto model)
    {
        try
        {
            var negocioId = _authService.GetNegocioId(User);
            if (!negocioId.HasValue || model.NegocioId != negocioId.Value)
                return Forbid();

            var (success, message) = await _clienteService.EditarClienteAsync(model);

            if (!success)
            {
                if (message.Contains("no encontrado"))
                    return NotFound(new { success = false, message });
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("EliminarCliente")]
    public async Task<IActionResult> EliminarCliente(Guid id, Guid negocioId)
    {
        try
        {
            var (success, message) = await _clienteService.EliminarClienteAsync(id, negocioId);

            if (!success)
                return NotFound(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("RenovarCliente")]
    public async Task<IActionResult> RenovarCliente(Guid id, Guid negocioId, DateTime nuevaFechaFin, decimal precio)
    {
        try
        {
            var (success, message) = await _clienteService.RenovarClienteAsync(id, negocioId, nuevaFechaFin, precio);

            if (!success)
            {
                if (message.Contains("no encontrado"))
                    return NotFound(new { success = false, message });
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("ExportClientesExcel")]
    public async Task<IActionResult> ExportClientesExcel(Guid negocioId)
    {
        try
        {
            var content = await _clienteService.ExportClientesExcelAsync(negocioId);
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Clientes_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("ImportarClientesExcel")]
    public async Task<IActionResult> ImportarClientesExcel(Guid negocioId, IFormFile file)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No se ha proporcionado ningún archivo" });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest(new { success = false, message = "El archivo debe ser un Excel (.xlsx o .xls)" });

            using var stream = file.OpenReadStream();
            var result = await _clienteService.ImportarClientesExcelAsync(negocioId, stream);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetSuscripcionStatus")]
    public async Task<IActionResult> GetSuscripcionStatus(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var negocio = await _context.Negocios.FindAsync(negocioId);
            if (negocio == null)
                return NotFound();

            int? diasRestantes = negocio.FechaExpiracion.HasValue
                ? (int)(negocio.FechaExpiracion.Value.Date - DateTime.Now.Date).TotalDays
                : null;

            return Ok(new
            {
                diasRestantes,
                fechaExpiracion = negocio.FechaExpiracion,
                diasPagados = negocio.DiasPagados,
                precioSuscripcion = negocio.PrecioSuscripcion
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetClientesDiarios")]
    public async Task<IActionResult> GetClientesDiarios(Guid negocioId)
    {
        try
        {
            var clientes = await _clienteService.GetClientesDiariosAsync(negocioId);
            return Ok(clientes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
