// ═══════════════════════════════════════════════════════════
// AdminController.cs — Controlador de administración de negocios
// Maneja el panel admin: CRUD de negocios, estadísticas,
// exportación Excel, impersonación y logs de actividad
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class AdminController : Controller
{
    private readonly INegocioService _negocioService;
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;

    public AdminController(INegocioService negocioService, IAuthService authService, IEmailService emailService)
    {
        _negocioService = negocioService;
        _authService = authService;
        _emailService = emailService;
    }

    // ═══════════════════════════════════════════════════════════
    // VISTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Muestra la vista principal del panel de administración con la lista de negocios
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        if (!_authService.IsAdmin(User))
        {
            TempData["Error"] = "No tiene permisos para acceder a esta página";
            return RedirectToAction("Login", "Auth");
        }
        return View("~/Views/Negocios/Index.cshtml");
    }

    // ═══════════════════════════════════════════════════════════
    // ENDPOINTS DE CONSULTA (GET)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los negocios con sus estadísticas (AJAX)
    /// </summary>
    [HttpGet("GetNegocios")]
    public async Task<IActionResult> GetNegocios()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var negocios = await _negocioService.GetAllNegociosAsync();
            return Ok(negocios);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene los datos de un negocio específico por su ID
    /// </summary>
    [HttpGet("GetNegocio/{id}")]
    public async Task<IActionResult> GetNegocio(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var negocio = await _negocioService.GetNegocioAsync(id);
            if (negocio == null)
                return NotFound(new { success = false, message = "Negocio no encontrado" });

            return Ok(negocio);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene estadísticas generales del dashboard admin:
    /// total de negocios, activos, en prueba, MRR e ingresos por mes
    /// </summary>
    [HttpGet("GetAdminDashboardStats")]
    public async Task<IActionResult> GetAdminDashboardStats()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var stats = await _negocioService.GetAdminDashboardStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene el historial de acciones realizadas por el admin (últimas 200)
    /// </summary>
    [HttpGet("GetAdminLogs")]
    public async Task<IActionResult> GetAdminLogs()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var logs = await _negocioService.GetAdminLogsAsync();
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene datos financieros para la pestaña de Ventas del panel admin (AJAX)
    /// </summary>
    [HttpGet("GetVentasAdmin")]
    public async Task<IActionResult> GetVentasAdmin()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var ventas = await _negocioService.GetVentasAdminAsync();
            return Ok(ventas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE NEGOCIOS (POST)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo negocio con datos de suscripción opcionales.
    /// Registra la acción en el log de admin.
    /// </summary>
    [HttpPost("Create")]
    public async Task<IActionResult> Create(string NombreNegocio, string duenoNegocio, string telefono, string EmailNegocio,
        string passwordNegocio, bool isActive, bool esPrueba,
        DateTime? fechaPago, DateTime? fechaExpiracion, decimal? precioSuscripcion, int? diasPagados,
        string tipoNegocio = "membresias")
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message) = await _negocioService.CreateNegocioAsync(
                NombreNegocio, duenoNegocio, telefono, EmailNegocio, passwordNegocio, isActive, esPrueba,
                fechaPago, fechaExpiracion, precioSuscripcion, diasPagados, tipoNegocio: tipoNegocio);

            if (!success)
                return BadRequest(message);

            await _negocioService.RegistrarAdminLogAsync(
                "Crear",
                $"Negocio '{NombreNegocio}' creado ({(esPrueba ? "Prueba" : "Pago")})",
                NombreNegocio);

            _ = _emailService.EnviarBienvenidaNegocioAsync(EmailNegocio, NombreNegocio, duenoNegocio, null);

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Edita un negocio existente. Si se envía contraseña, se re-hashea.
    /// Registra la acción en el log de admin.
    /// </summary>
    [HttpPost("Editar")]
    public async Task<IActionResult> Editar([FromForm] Gym negocio)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message) = await _negocioService.EditarNegocioAsync(negocio);

            if (!success)
                return BadRequest(new { success = false, message });

            await _negocioService.RegistrarAdminLogAsync(
                "Editar",
                $"Negocio '{negocio.NegocioNombre}' editado",
                negocio.NegocioNombre);

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un negocio si no tiene clientes registrados.
    /// Registra la acción en el log de admin.
    /// </summary>
    [HttpPost("Eliminar")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            // Obtener nombre antes de eliminar para el log
            var negocioData = await _negocioService.GetNegocioAsync(id);
            var (success, message) = await _negocioService.EliminarNegocioAsync(id);

            if (!success)
            {
                if (message.Contains("no encontrado"))
                    return NotFound(new { success = false, message });
                return BadRequest(new { success = false, message });
            }

            await _negocioService.RegistrarAdminLogAsync(
                "Eliminar",
                $"Negocio eliminado (ID: {id})",
                null);

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Alterna el estado de un negocio entre Pago (Activo) y Prueba.
    /// Registra la acción en el log de admin.
    /// </summary>
    [HttpPost("CambiarEstado")]
    public async Task<IActionResult> CambiarEstado(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message, isActive, esPrueba) = await _negocioService.CambiarEstadoAsync(id);

            if (!success)
                return NotFound(new { success = false, message });

            var estado = isActive == true ? "Pago (Activo)" : "Prueba";
            await _negocioService.RegistrarAdminLogAsync(
                "CambiarEstado",
                $"Estado cambiado a '{estado}' (ID: {id})",
                null);

            return Ok(new { success = true, message, isActive, esPrueba });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // EXPORTACIÓN EXCEL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Exporta todos los negocios a un archivo Excel (.xlsx)
    /// </summary>
    [HttpGet("ExportExcel")]
    public async Task<IActionResult> ExportExcel()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var content = await _negocioService.ExportExcelAsync();
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Negocios_{TimeHelper.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // IMPERSONACIÓN — El admin puede iniciar sesión como cualquier negocio
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Permite al admin iniciar sesión como un negocio específico.
    /// Crea un nuevo ClaimsPrincipal con el claim "AdminImpersonating" = true,
    /// para poder regresar al panel admin después.
    /// </summary>
    [HttpPost("Impersonate/{id}")]
    public async Task<IActionResult> Impersonate(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
            if (negocio == null)
                return NotFound(new { success = false, message = "Negocio no encontrado" });

            await _negocioService.RegistrarAdminLogAsync(
                "Impersonar",
                $"Impersonando negocio '{negocio.NegocioNombre}'",
                negocio.NegocioNombre);

            // Guardar el email del admin para poder restaurar la sesión después
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, negocio.NegocioNombre),
                new Claim(ClaimTypes.Email, negocio.Email ?? ""),
                new Claim("NegocioId", negocio.NegocioId.ToString()),
                new Claim(ClaimTypes.Role, "Negocio"),
                new Claim("AdminImpersonating", "true"),
                new Claim("AdminEmail", adminEmail ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            return RedirectToAction("Dashboard", "Clientes", new { id = negocio.NegocioId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Finaliza la impersonación y restaura la sesión del admin.
    /// Lee el claim "AdminEmail" para recrear los claims del admin.
    /// </summary>
    [HttpPost("StopImpersonation")]
    [AllowAnonymous]
    public async Task<IActionResult> StopImpersonation()
    {
        try
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == "AdminEmail")?.Value;
            var isImpersonating = User.Claims.Any(c => c.Type == "AdminImpersonating" && c.Value == "true");

            if (!isImpersonating || string.IsNullOrEmpty(adminEmail))
                return RedirectToAction("Login", "Auth");

            // Restaurar claims de admin
            var adminClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Administrador"),
                new Claim(ClaimTypes.Email, adminEmail),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(adminClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            return RedirectToAction("Index", "Admin");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
