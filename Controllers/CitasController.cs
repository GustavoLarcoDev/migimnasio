// ═══════════════════════════════════════════════════════════
// CitasController.cs — Controlador de citas/reservaciones del modelo artesanal
// Gestión de citas, calendario, slots disponibles y pagos
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class CitasController : Controller
{
    private readonly ICitaService _citaService;
    private readonly IClienteService _clienteService;
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;

    public CitasController(
        ICitaService citaService,
        IClienteService clienteService,
        IAuthService authService,
        ApplicationDbContext context)
    {
        _citaService = citaService;
        _clienteService = clienteService;
        _authService = authService;
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // VISTA PRINCIPAL
    // ═══════════════════════════════════════════════════════════

    [HttpGet("{id}/DashboardArtesanal")]
    public async Task<IActionResult> DashboardArtesanal(Guid id)
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

            return View("~/Views/Negocios/DashboardArtesanal.cshtml", negocio);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ENDPOINTS DE CONSULTA
    // ═══════════════════════════════════════════════════════════

    [HttpGet("GetCitasCalendario")]
    public async Task<IActionResult> GetCitasCalendario(Guid negocioId, DateTime start, DateTime end)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var citas = await _citaService.GetCitasCalendarioAsync(negocioId, start, end);
            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetCita")]
    public async Task<IActionResult> GetCita(Guid citaId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var cita = await _citaService.GetCitaAsync(citaId, negocioId);
            if (cita == null)
                return NotFound(new { success = false, message = "Cita no encontrada" });

            return Ok(cita);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetSlotsDisponibles")]
    public async Task<IActionResult> GetSlotsDisponibles(Guid negocioId, Guid empleadoId, DateTime fecha, int duracionMinutos = 30)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var slots = await _citaService.GetSlotsDisponiblesAsync(negocioId, empleadoId, fecha, duracionMinutos);
            return Ok(slots);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetCitasDashboardStats")]
    public async Task<IActionResult> GetCitasDashboardStats(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var stats = await _citaService.GetDashboardStatsAsync(negocioId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetHistorialCliente")]
    public async Task<IActionResult> GetHistorialCliente(Guid clienteId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var historial = await _citaService.GetHistorialClienteAsync(clienteId, negocioId);
            return Ok(historial);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE CITAS
    // ═══════════════════════════════════════════════════════════

    [HttpPost("CrearCitaRapida")]
    public async Task<IActionResult> CrearCitaRapida([FromForm] CitaQuickCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message, citaId) = await _citaService.CrearCitaRapidaAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message, citaId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("BuscarClientes")]
    public async Task<IActionResult> BuscarClientes(Guid negocioId, string q)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var clientes = await _clienteService.BuscarClientesAsync(negocioId, q);
            return Ok(clientes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("CrearCita")]
    public async Task<IActionResult> CrearCita([FromForm] CitaCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _citaService.CrearCitaAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("MoverCita")]
    public async Task<IActionResult> MoverCita([FromForm] CitaMoveDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _citaService.MoverCitaAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("CambiarEstadoCita")]
    public async Task<IActionResult> CambiarEstadoCita(Guid citaId, Guid negocioId, string nuevoEstado, string motivoCancelacion)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _citaService.CambiarEstadoCitaAsync(citaId, negocioId, nuevoEstado, motivoCancelacion);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // PAGOS
    // ═══════════════════════════════════════════════════════════

    [HttpPost("RegistrarPagoCita")]
    public async Task<IActionResult> RegistrarPagoCita([FromForm] PagoCitaDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _citaService.RegistrarPagoAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetPagoCita")]
    public async Task<IActionResult> GetPagoCita(Guid citaId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var pago = await _citaService.GetPagoCitaAsync(citaId, negocioId);
            return Ok(pago);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CLIENTES ARTESANAL
    // ═══════════════════════════════════════════════════════════

    [HttpPost("CrearClienteArtesanal")]
    public async Task<IActionResult> CrearClienteArtesanal([FromForm] ClienteArtesanalCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _clienteService.CrearClienteArtesanalAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("EditarClienteArtesanal")]
    public async Task<IActionResult> EditarClienteArtesanal([FromForm] ClienteArtesanalCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _clienteService.EditarClienteArtesanalAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetClientesArtesanal")]
    public async Task<IActionResult> GetClientesArtesanal(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var clientes = await _clienteService.GetClientesArtesanalAsync(negocioId);
            return Ok(clientes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
