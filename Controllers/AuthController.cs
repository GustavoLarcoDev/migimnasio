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

            var negocioId = _authService.GetNegocioId(User);
            if (negocioId.HasValue)
                return RedirectToAction("Dashboard", "Clientes", new { id = negocioId.Value });
        }
        return View("~/Views/Negocios/Login.cshtml");
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(string email, string password)
    {
        var (success, role, negocio, error) = await _authService.LoginAsync(email, password);

        if (!success)
        {
            ViewBag.Error = error;
            return View("~/Views/Negocios/Login.cshtml");
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

        // Negocio login
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

        return RedirectToAction("Dashboard", "Clientes", new { id = negocio.NegocioId });
    }

    [HttpGet("Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
