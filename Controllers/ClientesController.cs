// ═══════════════════════════════════════════════════════════
// ClientesController.cs — Controlador de gestión de clientes
// Maneja CRUD de clientes, renovación de membresías,
// importación/exportación Excel y estadísticas del dashboard
// ═══════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════
    // VISTA PRINCIPAL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Muestra el dashboard principal del negocio con todas las pestañas:
    /// clientes, logs, ventas y notificaciones.
    /// Valida que el usuario autenticado sea dueño de este negocio.
    /// </summary>
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
        catch
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ENDPOINTS DE CONSULTA (GET)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene estadísticas del dashboard: clientes activos/vencidos,
    /// ingresos del mes/hoy y clientes próximos a vencer (5 días)
    /// </summary>
    [HttpGet("GetDashboardStats")]
    public async Task<IActionResult> GetDashboardStats(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var stats = await _clienteService.GetDashboardStatsAsync(negocioId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene la lista completa de clientes del negocio con días restantes y estado
    /// </summary>
    [HttpGet("GetClientes")]
    public async Task<IActionResult> GetClientes(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var clientes = await _clienteService.GetClientesAsync(negocioId);
            return Ok(clientes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene los datos de un cliente específico para edición
    /// </summary>
    [HttpGet("GetCliente")]
    public async Task<IActionResult> GetCliente(Guid id, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

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

    /// <summary>
    /// Obtiene el estado de suscripción del negocio: días restantes,
    /// fecha de expiración y precio
    /// </summary>
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

            // Calcular días restantes de suscripción
            var now = TimeHelper.Now;
            int? diasRestantes = negocio.FechaExpiracion.HasValue
                ? (int)(negocio.FechaExpiracion.Value.Date - now.Date).TotalDays
                : null;
            bool porEmpezar = negocio.FechaPago.HasValue && negocio.FechaPago.Value.Date > now.Date;

            return Ok(new
            {
                diasRestantes,
                fechaExpiracion = negocio.FechaExpiracion,
                fechaPago = negocio.FechaPago,
                diasPagados = negocio.DiasPagados,
                precioSuscripcion = negocio.PrecioSuscripcion,
                porEmpezar
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene solo los clientes marcados como "diario" (pago por día)
    /// </summary>
    [HttpGet("GetClientesDiarios")]
    public async Task<IActionResult> GetClientesDiarios(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var clientes = await _clienteService.GetClientesDiariosAsync(negocioId);
            return Ok(clientes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE CLIENTES (POST)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo cliente. Valida propiedad del negocio.
    /// Registra automáticamente un log con el monto pagado.
    /// </summary>
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

    /// <summary>
    /// Edita un cliente existente. Registra los cambios detallados en el log.
    /// </summary>
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

    /// <summary>
    /// Elimina un cliente y registra la acción en los logs
    /// </summary>
    [HttpPost("EliminarCliente")]
    public async Task<IActionResult> EliminarCliente(Guid id, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

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

    /// <summary>
    /// Renueva la membresía de un cliente extendiendo su fecha de finalización.
    /// Registra un log con el nuevo pago.
    /// </summary>
    [HttpPost("RenovarCliente")]
    public async Task<IActionResult> RenovarCliente(Guid id, Guid negocioId, DateTime nuevaFechaFin, decimal precio)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

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

    // ═══════════════════════════════════════════════════════════
    // NOTIFICACIÓN MASIVA A CLIENTES DIARIOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Marca todos los clientes diarios como EsDiario = false (limpia la lista).
    /// Se llama después de que el dueño ya envió los mensajes via wa.me links.
    /// </summary>
    [HttpPost("LimpiarClientesDiarios")]
    public async Task<IActionResult> LimpiarClientesDiarios(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var limpiados = await _clienteService.LimpiarClientesDiariosAsync(negocioId);
            return Ok(new { success = true, limpiados });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // IMPORTACIÓN / EXPORTACIÓN EXCEL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Exporta todos los clientes del negocio a un archivo Excel (.xlsx)
    /// </summary>
    [HttpGet("ExportClientesExcel")]
    public async Task<IActionResult> ExportClientesExcel(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var content = await _clienteService.ExportClientesExcelAsync(negocioId);
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Clientes_{TimeHelper.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Importa clientes desde un archivo Excel (.xlsx / .xls).
    /// Detecta columnas automáticamente por headers y omite duplicados.
    /// </summary>
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

            // Validar extensión del archivo
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
}
