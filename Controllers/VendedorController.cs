#nullable enable
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
public class VendedorController : Controller
{
    private readonly IVendedorService _vendedorService;
    private readonly IAuthService _authService;
    private readonly INegocioService _negocioService;

    public VendedorController(IVendedorService vendedorService, IAuthService authService, INegocioService negocioService)
    {
        _vendedorService = vendedorService;
        _authService = authService;
        _negocioService = negocioService;
    }

    // ═══════════════════════════════════════════════════════════
    // ADMIN: CRUD DE VENDEDORES
    // ═══════════════════════════════════════════════════════════

    [HttpGet("GetVendedores")]
    public async Task<IActionResult> GetVendedores()
    {
        if (!_authService.IsAdmin(User))
            return Forbid();

        var vendedores = await _vendedorService.GetAllVendedoresAsync();
        return Ok(vendedores);
    }

    [HttpGet("GetVendedor")]
    public async Task<IActionResult> GetVendedor(Guid id)
    {
        if (!_authService.IsAdmin(User))
            return Forbid();

        var vendedor = await _vendedorService.GetVendedorAsync(id);
        if (vendedor == null)
            return NotFound(new { success = false, message = "Vendedor no encontrado" });

        return Ok(new
        {
            vendedor.VendedorId,
            vendedor.Nombre,
            vendedor.Apellido,
            vendedor.Correo,
            vendedor.Telefono
        });
    }

    [HttpPost("CrearVendedor")]
    public async Task<IActionResult> CrearVendedor(string nombre, string apellido, string correo, string telefono, string password)
    {
        if (!_authService.IsAdmin(User))
            return Forbid();

        var (success, message) = await _vendedorService.CrearVendedorAsync(nombre, apellido, correo, telefono, password);

        if (!success)
            return BadRequest(new { success = false, message });

        await _negocioService.RegistrarAdminLogAsync("CrearVendedor", $"Vendedor '{nombre} {apellido}' creado", null);

        return Ok(new { success = true, message });
    }

    [HttpPost("EditarVendedor")]
    public async Task<IActionResult> EditarVendedor(Guid id, string nombre, string apellido, string correo, string telefono, string? password)
    {
        if (!_authService.IsAdmin(User))
            return Forbid();

        var (success, message) = await _vendedorService.EditarVendedorAsync(id, nombre, apellido, correo, telefono, password);

        if (!success)
            return BadRequest(new { success = false, message });

        await _negocioService.RegistrarAdminLogAsync("EditarVendedor", $"Vendedor '{nombre} {apellido}' editado", null);

        return Ok(new { success = true, message });
    }

    [HttpPost("EliminarVendedor")]
    public async Task<IActionResult> EliminarVendedor(Guid id)
    {
        if (!_authService.IsAdmin(User))
            return Forbid();

        var (success, message) = await _vendedorService.EliminarVendedorAsync(id);

        if (!success)
            return BadRequest(new { success = false, message });

        await _negocioService.RegistrarAdminLogAsync("EliminarVendedor", message, null);

        return Ok(new { success = true, message });
    }

    // ═══════════════════════════════════════════════════════════
    // ADMIN: IMPERSONAR VENDEDOR
    // ═══════════════════════════════════════════════════════════

    [HttpPost("AdminImpersonateVendedor/{id}")]
    public async Task<IActionResult> AdminImpersonateVendedor(Guid id)
    {
        if (!_authService.IsAdmin(User))
            return Forbid();

        var vendedor = await _vendedorService.GetVendedorAsync(id);
        if (vendedor == null)
            return NotFound(new { success = false, message = "Vendedor no encontrado" });

        var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, $"{vendedor.Nombre} {vendedor.Apellido}"),
            new Claim(ClaimTypes.Email, vendedor.Correo),
            new Claim("VendedorId", vendedor.VendedorId.ToString()),
            new Claim(ClaimTypes.Role, "Vendedor"),
            new Claim("AdminImpersonating", "true"),
            new Claim("AdminEmail", adminEmail ?? "")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        await _negocioService.RegistrarAdminLogAsync(
            "AdminImpersonarVendedor",
            $"Admin impersonó al vendedor '{vendedor.Nombre} {vendedor.Apellido}'",
            null);

        return RedirectToAction("VendedorDashboard");
    }

    [HttpPost("AdminStopImpersonateVendedor")]
    [AllowAnonymous]
    public async Task<IActionResult> AdminStopImpersonateVendedor()
    {
        var adminEmail = User.Claims.FirstOrDefault(c => c.Type == "AdminEmail")?.Value;
        var isAdminImpersonating = User.Claims.Any(c => c.Type == "AdminImpersonating" && c.Value == "true");

        if (!isAdminImpersonating || string.IsNullOrEmpty(adminEmail))
            return RedirectToAction("Login", "Auth");

        var vendedorNombre = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

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

        await _negocioService.RegistrarAdminLogAsync(
            "AdminDejarImpersonarVendedor",
            $"Admin dejó de impersonar al vendedor '{vendedorNombre}'",
            null);

        return RedirectToAction("Index", "Admin");
    }

    // ═══════════════════════════════════════════════════════════
    // VENDEDOR: SU PROPIO DASHBOARD
    // ═══════════════════════════════════════════════════════════

    [HttpGet("VendedorDashboard")]
    public IActionResult VendedorDashboard()
    {
        if (!IsVendedor())
            return RedirectToAction("Login", "Auth");

        return View("~/Views/Vendedor/Dashboard.cshtml");
    }

    [HttpGet("GetVendedorNegocios")]
    public async Task<IActionResult> GetVendedorNegocios()
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var negocios = await _vendedorService.GetNegociosByVendedorAsync(vendedorId.Value);
        return Ok(negocios);
    }

    [HttpGet("GetVendedorStats")]
    public async Task<IActionResult> GetVendedorStats()
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var stats = await _vendedorService.GetVendedorStatsAsync(vendedorId.Value);
        return Ok(stats);
    }

    /// <summary>
    /// Vendedor crea un negocio (se marca con su VendedorId)
    /// </summary>
    [HttpPost("VendedorCrearNegocio")]
    public async Task<IActionResult> VendedorCrearNegocio(
        string NombreNegocio, string duenoNegocio, string telefono, string EmailNegocio,
        string passwordNegocio, bool esPrueba,
        DateTime? fechaPago, DateTime? fechaExpiracion, decimal? precioSuscripcion, int? diasPagados)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        bool isActive = !esPrueba;

        var (success, message) = await _negocioService.CreateNegocioAsync(
            NombreNegocio, duenoNegocio, telefono, EmailNegocio, passwordNegocio,
            isActive, esPrueba, fechaPago, fechaExpiracion, precioSuscripcion, diasPagados,
            vendedorId.Value);

        if (!success)
            return BadRequest(new { success = false, message });

        var vendedorNombre = User.Identity?.Name ?? "Vendedor";
        await _negocioService.RegistrarAdminLogAsync(
            "VendedorCrearNegocio",
            $"Vendedor {vendedorNombre} creó el negocio '{NombreNegocio}'",
            NombreNegocio);

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Vendedor impersona un negocio que él creó
    /// </summary>
    [HttpPost("VendedorImpersonate/{id}")]
    public async Task<IActionResult> VendedorImpersonate(Guid id)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
        if (negocio == null)
            return NotFound(new { success = false, message = "Negocio no encontrado" });

        if (negocio.VendedorId != vendedorId.Value)
            return Forbid();

        var vendedorCorreo = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var vendedorNombre = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        var isAdminImpersonating = User.Claims.Any(c => c.Type == "AdminImpersonating" && c.Value == "true");
        var adminEmail = User.Claims.FirstOrDefault(c => c.Type == "AdminEmail")?.Value;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, negocio.NegocioNombre),
            new Claim(ClaimTypes.Email, negocio.Email ?? ""),
            new Claim("NegocioId", negocio.NegocioId.ToString()),
            new Claim(ClaimTypes.Role, "Negocio"),
            new Claim("VendedorImpersonating", "true"),
            new Claim("VendedorId", vendedorId.Value.ToString()),
            new Claim("VendedorEmail", vendedorCorreo ?? ""),
            new Claim("VendedorNombre", vendedorNombre ?? "")
        };

        if (isAdminImpersonating && !string.IsNullOrEmpty(adminEmail))
        {
            claims.Add(new Claim("AdminImpersonating", "true"));
            claims.Add(new Claim("AdminEmail", adminEmail));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        await _negocioService.RegistrarAdminLogAsync(
            "VendedorImpersonar",
            $"Vendedor {vendedorNombre} impersonó el negocio '{negocio.NegocioNombre}'",
            negocio.NegocioNombre);

        return RedirectToAction("Dashboard", "Clientes", new { id = negocio.NegocioId });
    }

    /// <summary>
    /// Vendedor deja de impersonar y vuelve a su dashboard
    /// </summary>
    [HttpPost("VendedorStopImpersonation")]
    [AllowAnonymous]
    public async Task<IActionResult> VendedorStopImpersonation()
    {
        var vendedorId = User.Claims.FirstOrDefault(c => c.Type == "VendedorId")?.Value;
        var vendedorEmail = User.Claims.FirstOrDefault(c => c.Type == "VendedorEmail")?.Value;
        var vendedorNombre = User.Claims.FirstOrDefault(c => c.Type == "VendedorNombre")?.Value;
        var isImpersonating = User.Claims.Any(c => c.Type == "VendedorImpersonating" && c.Value == "true");

        if (!isImpersonating || string.IsNullOrEmpty(vendedorId))
            return RedirectToAction("Login", "Auth");

        // Obtener nombre del negocio que estaba impersonando
        var negocioNombre = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        // Carry forward admin claims if admin was impersonating this vendedor
        var isAdminImpersonating = User.Claims.Any(c => c.Type == "AdminImpersonating" && c.Value == "true");
        var adminEmail = User.Claims.FirstOrDefault(c => c.Type == "AdminEmail")?.Value;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, vendedorNombre ?? "Vendedor"),
            new Claim(ClaimTypes.Email, vendedorEmail ?? ""),
            new Claim("VendedorId", vendedorId),
            new Claim(ClaimTypes.Role, "Vendedor")
        };

        if (isAdminImpersonating && !string.IsNullOrEmpty(adminEmail))
        {
            claims.Add(new Claim("AdminImpersonating", "true"));
            claims.Add(new Claim("AdminEmail", adminEmail));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        await _negocioService.RegistrarAdminLogAsync(
            "VendedorDejarImpersonar",
            $"Vendedor {vendedorNombre} dejó de impersonar '{negocioNombre}'",
            negocioNombre);

        return RedirectToAction("VendedorDashboard");
    }

    // ═══════════════════════════════════════════════════════════
    // VENDEDOR: CRUD SOBRE SUS NEGOCIOS
    // ═══════════════════════════════════════════════════════════

    [HttpGet("GetVendedorNegocio/{id}")]
    public async Task<IActionResult> GetVendedorNegocio(Guid id)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
        if (negocio == null)
            return NotFound(new { success = false, message = "Negocio no encontrado" });

        if (negocio.VendedorId != vendedorId.Value)
            return Forbid();

        return Ok(new
        {
            negocio.NegocioId,
            negocio.NegocioNombre,
            negocio.DuenoNegocio,
            negocio.Telefono,
            negocio.Email,
            negocio.IsActive,
            negocio.EsPrueba,
            negocio.DiasPagados,
            negocio.PrecioSuscripcion,
            negocio.FechaPago,
            negocio.FechaExpiracion
        });
    }

    [HttpPost("VendedorEditarNegocio")]
    public async Task<IActionResult> VendedorEditarNegocio([FromForm] Gym negocio)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var existing = await _negocioService.GetNegocioForImpersonationAsync(negocio.NegocioId);
        if (existing == null)
            return NotFound(new { success = false, message = "Negocio no encontrado" });

        if (existing.VendedorId != vendedorId.Value)
            return Forbid();

        var (success, message) = await _negocioService.EditarNegocioAsync(negocio);

        if (!success)
            return BadRequest(new { success = false, message });

        var vendedorNombre = User.Identity?.Name ?? "Vendedor";
        await _negocioService.RegistrarAdminLogAsync(
            "VendedorEditarNegocio",
            $"Vendedor {vendedorNombre} editó el negocio '{negocio.NegocioNombre}'",
            negocio.NegocioNombre);

        return Ok(new { success = true, message });
    }

    [HttpPost("VendedorEliminarNegocio")]
    public async Task<IActionResult> VendedorEliminarNegocio(Guid id)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
        if (negocio == null)
            return NotFound(new { success = false, message = "Negocio no encontrado" });

        if (negocio.VendedorId != vendedorId.Value)
            return Forbid();

        var negocioNombre = negocio.NegocioNombre;
        var (success, message) = await _negocioService.EliminarNegocioAsync(id);

        if (!success)
        {
            if (message.Contains("no encontrado"))
                return NotFound(new { success = false, message });
            return BadRequest(new { success = false, message });
        }

        var vendedorNombre = User.Identity?.Name ?? "Vendedor";
        await _negocioService.RegistrarAdminLogAsync(
            "VendedorEliminarNegocio",
            $"Vendedor {vendedorNombre} eliminó el negocio '{negocioNombre}'",
            negocioNombre);

        return Ok(new { success = true, message });
    }

    [HttpPost("VendedorCambiarEstado")]
    public async Task<IActionResult> VendedorCambiarEstado(Guid id)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
        if (negocio == null)
            return NotFound(new { success = false, message = "Negocio no encontrado" });

        if (negocio.VendedorId != vendedorId.Value)
            return Forbid();

        var (success, message, isActive, esPrueba) = await _negocioService.CambiarEstadoAsync(id);

        if (!success)
            return NotFound(new { success = false, message });

        var estado = isActive == true ? "Pago (Activo)" : "Prueba";
        var vendedorNombre = User.Identity?.Name ?? "Vendedor";
        await _negocioService.RegistrarAdminLogAsync(
            "VendedorCambiarEstado",
            $"Vendedor {vendedorNombre} cambió estado de '{negocio.NegocioNombre}' a {estado}",
            negocio.NegocioNombre);

        return Ok(new { success = true, message, isActive, esPrueba });
    }

    // ═══════════════════════════════════════════════════════════
    // VENDEDOR: LEADS (INTERESADOS DE LANDING PAGE)
    // ═══════════════════════════════════════════════════════════

    [HttpGet("GetVendedorLeads")]
    public async Task<IActionResult> GetVendedorLeads()
    {
        if (!IsVendedor())
            return Forbid();

        var leads = await _vendedorService.GetLeadsAsync();
        return Ok(leads);
    }

    [HttpPost("MarcarLeadAtendido")]
    public async Task<IActionResult> MarcarLeadAtendido(Guid id)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var vendedorNombre = User.Identity?.Name ?? "Vendedor";
        var (success, message) = await _vendedorService.MarcarLeadAtendidoAsync(id, vendedorId.Value, vendedorNombre);

        if (!success)
            return BadRequest(new { success = false, message });

        return Ok(new { success = true, message });
    }

    [HttpGet("GetVendedorLeadsCount")]
    public async Task<IActionResult> GetVendedorLeadsCount()
    {
        if (!IsVendedor())
            return Forbid();

        var count = await _vendedorService.GetLeadsCountAsync();
        return Ok(new { count });
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════

    private bool IsVendedor()
    {
        return User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Vendedor");
    }

    private Guid? GetVendedorId()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "VendedorId");
        if (claim != null && Guid.TryParse(claim.Value, out var id))
            return id;
        return null;
    }
}
