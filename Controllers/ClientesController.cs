using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[Route("Gimnasios")]
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
            var gimnasioId = _authService.GetGimnasioId(User);
            if (!gimnasioId.HasValue || gimnasioId.Value != id)
                return Forbid();

            var gimnasio = await _context.Gimnasios
                .Include(g => g.Clientes)
                .FirstOrDefaultAsync(g => g.GimnasioId == id);

            if (gimnasio == null)
                return NotFound();

            return View("~/Views/Gimnasios/Dashboard.cshtml", gimnasio);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetDashboardStats")]
    public async Task<IActionResult> GetDashboardStats(Guid gimnasioId)
    {
        try
        {
            var stats = await _clienteService.GetDashboardStatsAsync(gimnasioId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetClientes")]
    public async Task<IActionResult> GetClientes(Guid gimnasioId)
    {
        try
        {
            var clientes = await _clienteService.GetClientesAsync(gimnasioId);
            return Ok(clientes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetCliente")]
    public async Task<IActionResult> GetCliente(Guid id, Guid gimnasioId)
    {
        try
        {
            var cliente = await _clienteService.GetClienteAsync(id, gimnasioId);
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
            var gimnasioId = _authService.GetGimnasioId(User);
            if (!gimnasioId.HasValue || model.GimnasioId != gimnasioId.Value)
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
            var gimnasioId = _authService.GetGimnasioId(User);
            if (!gimnasioId.HasValue || model.GimnasioId != gimnasioId.Value)
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
    public async Task<IActionResult> EliminarCliente(Guid id, Guid gimnasioId)
    {
        try
        {
            var (success, message) = await _clienteService.EliminarClienteAsync(id, gimnasioId);

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
    public async Task<IActionResult> RenovarCliente(Guid id, Guid gimnasioId, int dias, decimal precio)
    {
        try
        {
            var (success, message) = await _clienteService.RenovarClienteAsync(id, gimnasioId, dias, precio);

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
    public async Task<IActionResult> ExportClientesExcel(Guid gimnasioId)
    {
        try
        {
            var content = await _clienteService.ExportClientesExcelAsync(gimnasioId);
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
    public async Task<IActionResult> ImportarClientesExcel(Guid gimnasioId, IFormFile file)
    {
        try
        {
            var gymId = _authService.GetGimnasioId(User);
            if (!gymId.HasValue || gimnasioId != gymId.Value)
                return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No se ha proporcionado ningún archivo" });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest(new { success = false, message = "El archivo debe ser un Excel (.xlsx o .xls)" });

            using var stream = file.OpenReadStream();
            var result = await _clienteService.ImportarClientesExcelAsync(gimnasioId, stream);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetClientesDiarios")]
    public async Task<IActionResult> GetClientesDiarios(Guid gimnasioId)
    {
        try
        {
            var clientes = await _clienteService.GetClientesDiariosAsync(gimnasioId);
            return Ok(clientes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
