// ═══════════════════════════════════════════════════════════
// AuthController.cs — Controlador de autenticación
// Maneja login/logout usando Cookie Authentication.
// Soporta dos roles: Admin y Negocio.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gimnasio.Controllers;

[Route("Negocios")]
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogService _logService;
    private readonly INegocioService _negocioService;

    public AuthController(IAuthService authService, ILogService logService, INegocioService negocioService)
    {
        _authService = authService;
        _logService = logService;
        _negocioService = negocioService;
    }

    // ═══════════════════════════════════════════════════════════
    // LOGIN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Muestra la vista de login. Si el usuario ya está autenticado,
    /// redirige al panel admin o al dashboard del negocio según su rol.
    /// </summary>
    [HttpGet("Login")]
    public IActionResult Login()
    {
        if (User.Identity.IsAuthenticated)
        {
            if (_authService.IsAdmin(User))
                return RedirectToAction("Index", "Admin");

            if (User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Vendedor"))
                return RedirectToAction("VendedorDashboard", "Vendedor");

            var negocioId = _authService.GetNegocioId(User);
            if (negocioId.HasValue)
                return RedirectToAction("Dashboard", "Clientes", new { id = negocioId.Value });
        }
        return View("~/Views/Negocios/Login.cshtml");
    }

    /// <summary>
    /// Procesa el formulario de login. Valida credenciales contra:
    /// 1. AdminSettings (admin del sistema)
    /// 2. Tabla Negocios (por email o teléfono)
    /// Si la suscripción expiró, muestra mensaje especial (EXPIRED).
    /// </summary>
    [HttpPost("Login")]
    public async Task<IActionResult> Login(string email, string password)
    {
        var (success, role, negocio, vendedorId, vendedorNombre, error) = await _authService.LoginAsync(email, password);

        if (!success)
        {
            if (error == "EXPIRED")
            {
                ViewBag.Expired = true;
            }
            else
            {
                ViewBag.Error = error;
            }
            return View("~/Views/Negocios/Login.cshtml");
        }

        // Login como Admin
        if (role == "Admin")
        {
            var adminClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Administrador"),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var adminIdentity = new ClaimsIdentity(adminClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(adminIdentity),
                new AuthenticationProperties { IsPersistent = true });

            return RedirectToAction("Index", "Admin");
        }

        // Login como Vendedor
        if (role == "Vendedor" && vendedorId.HasValue)
        {
            var vendedorClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, vendedorNombre ?? email),
                new Claim(ClaimTypes.Email, email),
                new Claim("VendedorId", vendedorId.Value.ToString()),
                new Claim(ClaimTypes.Role, "Vendedor")
            };

            var vendedorIdentity = new ClaimsIdentity(vendedorClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(vendedorIdentity),
                new AuthenticationProperties { IsPersistent = true });

            await _negocioService.RegistrarAdminLogAsync(
                "VendedorLogin",
                $"Vendedor {vendedorNombre} inició sesión",
                null);

            return RedirectToAction("VendedorDashboard", "Vendedor");
        }

        // Login como Negocio
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, negocio.NegocioNombre),
            new Claim(ClaimTypes.Email, negocio.Email),
            new Claim("NegocioId", negocio.NegocioId.ToString()),
            new Claim(ClaimTypes.Role, "Negocio")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties { IsPersistent = true });

        await _logService.CreateLogAsync(negocio.NegocioId, "sesion_inicio",
            $"{negocio.DuenoNegocio} inició sesión en {negocio.NegocioNombre}");

        return RedirectToAction("Dashboard", "Clientes", new { id = negocio.NegocioId });
    }

    // ═══════════════════════════════════════════════════════════
    // LOGOUT
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Cierra la sesión del usuario. Si es un negocio, registra
    /// el cierre de sesión en los logs antes de cerrar.
    /// </summary>
    [HttpGet("Logout")]
    public async Task<IActionResult> Logout()
    {
        var negocioId = _authService.GetNegocioId(User);
        var userName = User.Identity?.Name ?? "Usuario";

        if (negocioId.HasValue)
        {
            await _logService.CreateLogAsync(negocioId.Value, "sesion_cierre",
                $"{userName} cerró sesión");
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
