using Gimnasio.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gimnasio.Controllers;

[Route("Gimnasios")]
public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("Login")]
    public IActionResult Login()
    {
        if (User.Identity.IsAuthenticated)
        {
            if (_authService.IsAdmin(User))
                return RedirectToAction("Index", "Admin");

            var gimnasioId = _authService.GetGimnasioId(User);
            if (gimnasioId.HasValue)
                return RedirectToAction("Dashboard", "Clientes", new { id = gimnasioId.Value });
        }
        return View("~/Views/Gimnasios/Login.cshtml");
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(string email, string password)
    {
        var (success, role, gimnasio, error) = await _authService.LoginAsync(email, password);

        if (!success)
        {
            ViewBag.Error = error;
            return View("~/Views/Gimnasios/Login.cshtml");
        }

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

        // Gimnasio login
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, gimnasio.GimnasioNombre),
            new Claim(ClaimTypes.Email, gimnasio.Email),
            new Claim("GimnasioId", gimnasio.GimnasioId.ToString()),
            new Claim(ClaimTypes.Role, "Gimnasio")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties { IsPersistent = true });

        return RedirectToAction("Dashboard", "Clientes", new { id = gimnasio.GimnasioId });
    }

    [HttpGet("Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
