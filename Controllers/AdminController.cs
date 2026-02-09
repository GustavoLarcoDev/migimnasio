using Gimnasio.Models;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gimnasio.Controllers;

[Route("Gimnasios")]
[Authorize]
public class AdminController : Controller
{
    private readonly IGimnasioService _gimnasioService;
    private readonly IAuthService _authService;

    public AdminController(IGimnasioService gimnasioService, IAuthService authService)
    {
        _gimnasioService = gimnasioService;
        _authService = authService;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        if (!_authService.IsAdmin(User))
        {
            TempData["Error"] = "No tiene permisos para acceder a esta página";
            return RedirectToAction("Login", "Auth");
        }
        return View("~/Views/Gimnasios/Index.cshtml");
    }

    [HttpGet("GetGimnasios")]
    public async Task<IActionResult> GetGimnasios()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var gimnasios = await _gimnasioService.GetAllGimnasiosAsync();
            return Ok(gimnasios);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("GetGimnasio/{id}")]
    public async Task<IActionResult> GetGimnasio(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var gimnasio = await _gimnasioService.GetGimnasioAsync(id);
            if (gimnasio == null)
                return NotFound(new { success = false, message = "Gimnasio no encontrado" });

            return Ok(gimnasio);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("Crear")]
    public IActionResult Crear()
    {
        if (!_authService.IsAdmin(User))
        {
            TempData["Error"] = "No tiene permisos para acceder a esta página";
            return RedirectToAction("Login", "Auth");
        }
        return View("~/Views/Gimnasios/Create.cshtml");
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create(string NombreGimnasio, string duenoGimnasio, string telefono, string EmailGimnasio,
        string passwordGimnasio, bool isActive, bool esPrueba)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message) = await _gimnasioService.CreateGimnasioAsync(
                NombreGimnasio, duenoGimnasio, telefono, EmailGimnasio, passwordGimnasio, isActive, esPrueba);

            if (!success)
                return BadRequest(message);

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("Editar")]
    public async Task<IActionResult> Editar([FromForm] Gym gimnasio)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message) = await _gimnasioService.EditarGimnasioAsync(gimnasio);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("Eliminar")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message) = await _gimnasioService.EliminarGimnasioAsync(id);

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

    [HttpPost("CambiarEstado")]
    public async Task<IActionResult> CambiarEstado(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message, isActive, esPrueba) = await _gimnasioService.CambiarEstadoAsync(id);

            if (!success)
                return NotFound(new { success = false, message });

            return Ok(new { success = true, message, isActive, esPrueba });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("ExportExcel")]
    public async Task<IActionResult> ExportExcel()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var content = await _gimnasioService.ExportExcelAsync();
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Gimnasios_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("Impersonate/{id}")]
    public async Task<IActionResult> Impersonate(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var gimnasio = await _gimnasioService.GetGimnasioForImpersonationAsync(id);
            if (gimnasio == null)
                return NotFound(new { success = false, message = "Gimnasio no encontrado" });

            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, gimnasio.GimnasioNombre),
                new Claim(ClaimTypes.Email, gimnasio.Email ?? ""),
                new Claim("GimnasioId", gimnasio.GimnasioId.ToString()),
                new Claim(ClaimTypes.Role, "Gimnasio"),
                new Claim("AdminImpersonating", "true"),
                new Claim("AdminEmail", adminEmail ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            return RedirectToAction("Dashboard", "Clientes", new { id = gimnasio.GimnasioId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

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
