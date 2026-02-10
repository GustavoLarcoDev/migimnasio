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

    public AdminController(INegocioService negocioService, IAuthService authService)
    {
        _negocioService = negocioService;
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
        return View("~/Views/Negocios/Index.cshtml");
    }

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

    [HttpGet("Crear")]
    public IActionResult Crear()
    {
        if (!_authService.IsAdmin(User))
        {
            TempData["Error"] = "No tiene permisos para acceder a esta página";
            return RedirectToAction("Login", "Auth");
        }
        return View("~/Views/Negocios/Create.cshtml");
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create(string NombreNegocio, string duenoNegocio, string telefono, string EmailNegocio,
        string passwordNegocio, bool isActive, bool esPrueba)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message) = await _negocioService.CreateNegocioAsync(
                NombreNegocio, duenoNegocio, telefono, EmailNegocio, passwordNegocio, isActive, esPrueba);

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
    public async Task<IActionResult> Editar([FromForm] Gym negocio)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message) = await _negocioService.EditarNegocioAsync(negocio);

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

            var (success, message) = await _negocioService.EliminarNegocioAsync(id);

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

            var (success, message, isActive, esPrueba) = await _negocioService.CambiarEstadoAsync(id);

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

            var content = await _negocioService.ExportExcelAsync();
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Negocios_{DateTime.Now:yyyyMMdd}.xlsx");
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

            var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
            if (negocio == null)
                return NotFound(new { success = false, message = "Negocio no encontrado" });

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
