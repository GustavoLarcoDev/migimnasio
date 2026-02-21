// ═══════════════════════════════════════════════════════════════════════════════
// RestauranteController.cs — Mesas, Menus y endpoints publicos del restaurante
//
// Rutas publicas bajo /Restaurante (menus compartibles sin login).
// Rutas CRUD bajo /Negocios (consistente con el resto de controladores).
// ═══════════════════════════════════════════════════════════════════════════════

using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

// ═══════════════════════════════════════════════════════════════════════════════
// PARTE 1 — Endpoints publicos del menu (AllowAnonymous)
// ═══════════════════════════════════════════════════════════════════════════════

[Route("Restaurante")]
[Authorize]
public class RestauranteController : Controller
{
    private readonly IAuthService _authService;
    private readonly IMenuRestauranteService _menuService;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsappService;

    public RestauranteController(
        IAuthService authService,
        IMenuRestauranteService menuService,
        IEmailService emailService,
        IWhatsAppService whatsappService)
    {
        _authService = authService;
        _menuService = menuService;
        _emailService = emailService;
        _whatsappService = whatsappService;
    }

    /// <summary>
    /// Vista publica del menu HTML (compartible por link sin autenticacion).
    /// </summary>
    [HttpGet("VerMenu")]
    [AllowAnonymous]
    public async Task<IActionResult> VerMenu(Guid negocioId, Guid menuId)
    {
        var html = await _menuService.GenerarHtmlMenuAsync(menuId, negocioId);
        if (html == null)
        {
            Response.StatusCode = 404;
            return Content("<h1>Menu no encontrado</h1>", "text/html");
        }
        return Content(html, "text/html");
    }

    /// <summary>
    /// Descarga/imprime el menu con window.print() inyectado.
    /// </summary>
    [HttpGet("DescargarMenu")]
    public async Task<IActionResult> DescargarMenu(Guid menuId)
    {
        var nId = _authService.GetNegocioId(User);
        if (nId == null) return Forbid();

        var html = await _menuService.GenerarHtmlMenuAsync(menuId, nId.Value);
        if (html == null) return NotFound();

        html = html.Replace("</body>", "<script>window.onload=function(){setTimeout(function(){window.print();},500);}</script></body>");
        return Content(html, "text/html");
    }

    /// <summary>
    /// Envia el link del menu por email o WhatsApp.
    /// </summary>
    [HttpPost("EnviarMenu")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarMenu([FromForm] Guid menuId, [FromForm] string destino, [FromForm] string tipo)
    {
        var nId = _authService.GetNegocioId(User);
        if (nId == null) return Forbid();

        var companyName = User.Identity?.Name ?? "Mi Restaurante";

        if (string.IsNullOrWhiteSpace(destino) || string.IsNullOrWhiteSpace(tipo))
            return Json(new { success = false, message = "Datos de destino invalidos." });

        var host = Request.Host.Value;
        var scheme = Request.Scheme;
        var menuUrl = $"{scheme}://{host}/Restaurante/VerMenu?negocioId={nId.Value}&menuId={menuId}";

        if (tipo == "email")
        {
            var htmlBody = $@"<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
                <div style='background:linear-gradient(135deg,#dc3545,#fd7e14);padding:30px;border-radius:12px;text-align:center;'>
                    <h1 style='color:white;margin:0;font-size:24px;'>{System.Net.WebUtility.HtmlEncode(companyName)}</h1>
                    <p style='color:rgba(255,255,255,0.9);margin:8px 0 0;'>Te comparte su menu</p>
                </div>
                <div style='padding:30px;text-align:center;'>
                    <p style='color:#333;font-size:16px;'>Haz clic en el boton para ver el menu completo:</p>
                    <a href='{menuUrl}' style='display:inline-block;padding:14px 40px;background:#dc3545;color:white;text-decoration:none;border-radius:8px;font-weight:bold;font-size:16px;margin:16px 0;'>Ver Menu</a>
                </div>
                <p style='color:#999;font-size:12px;text-align:center;'>Enviado a traves de My-Negocio</p>
            </div>";

            var enviado = await _emailService.EnviarReciboPorEmailGenericoAsync(destino, $"Menu de {companyName}", htmlBody);
            return Json(new { success = enviado, message = enviado ? "Menu enviado por correo." : "Error al enviar el correo." });
        }
        else if (tipo == "whatsapp")
        {
            var enviado = await _whatsappService.EnviarLinkCatalogoTiendaAsync(destino, companyName, menuUrl);
            return Json(new { success = enviado, message = enviado ? "Link del menu enviado por WhatsApp." : "Error al enviar el WhatsApp." });
        }

        return Json(new { success = false, message = "Metodo de envio no soportado." });
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// PARTE 2 — CRUD de Mesas y Menus (bajo ruta /Negocios)
// ═══════════════════════════════════════════════════════════════════════════════

[Route("Negocios")]
[Authorize]
public class RestauranteCrudController : Controller
{
    private readonly IAuthService _authService;
    private readonly IMesaService _mesaService;
    private readonly IMenuRestauranteService _menuService;

    public RestauranteCrudController(
        IAuthService authService,
        IMesaService mesaService,
        IMenuRestauranteService menuService)
    {
        _authService = authService;
        _mesaService = mesaService;
        _menuService = menuService;
    }

    // ── Mesas ──────────────────────────────────────────────────

    [HttpGet("GetMesas")]
    public async Task<IActionResult> GetMesas(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value) return Forbid();

            var mesas = await _mesaService.GetMesasAsync(negocioId);
            return Ok(mesas);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("GetMesa")]
    public async Task<IActionResult> GetMesa(Guid mesaId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue) return Forbid();

            var mesa = await _mesaService.GetMesaAsync(mesaId, nId.Value);
            if (mesa == null) return NotFound(new { success = false, message = "Mesa no encontrada" });
            return Ok(mesa);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("CrearMesa")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CrearMesa([FromBody] MesaRequest req)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue) return Forbid();

            var (success, message) = await _mesaService.CrearMesaAsync(nId.Value, req.Nombre, req.Numero, req.Capacidad);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("EditarMesa")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> EditarMesa([FromBody] MesaRequest req)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue) return Forbid();

            var (success, message) = await _mesaService.EditarMesaAsync(req.MesaId, nId.Value, req.Nombre, req.Numero, req.Capacidad);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("CambiarEstadoMesa")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CambiarEstadoMesa([FromBody] CambiarEstadoMesaRequest req)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue) return Forbid();

            var (success, message) = await _mesaService.CambiarEstadoMesaAsync(req.MesaId, nId.Value, req.Estado);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("EliminarMesa")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> EliminarMesa([FromBody] EliminarMesaRequest req)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue) return Forbid();

            var (success, message) = await _mesaService.EliminarMesaAsync(req.MesaId, nId.Value);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ── Menus ──────────────────────────────────────────────────

    [HttpGet("GetMenus")]
    public async Task<IActionResult> GetMenus(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value) return Forbid();

            var menus = await _menuService.GetMenusAsync(negocioId);
            return Ok(menus);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("GetMenu")]
    public async Task<IActionResult> GetMenu(Guid menuId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue) return Forbid();

            var menu = await _menuService.GetMenuAsync(menuId, nId.Value);
            if (menu == null) return NotFound(new { success = false, message = "Menu no encontrado" });
            return Ok(new
            {
                menu.MenuId,
                menu.Nombre,
                menu.TipoMenu,
                menu.PrecioFijo,
                menu.ItemsJson,
                menu.Estilo,
                menu.FechaCreacion
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("CrearMenu")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CrearMenu([FromBody] MenuRestauranteDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue) return Forbid();
            dto.NegocioId = nId.Value;

            var (success, message) = await _menuService.CrearMenuAsync(dto);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("EditarMenu")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> EditarMenu([FromBody] MenuRestauranteDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue) return Forbid();
            dto.NegocioId = nId.Value;

            var (success, message) = await _menuService.EditarMenuAsync(dto);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("EliminarMenu")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> EliminarMenu([FromBody] EliminarMenuRequest req)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue) return Forbid();

            var (success, message) = await _menuService.EliminarMenuAsync(req.MenuId, nId.Value);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("PreviewMenu")]
    public async Task<IActionResult> PreviewMenu(Guid menuId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue) return Forbid();

            var html = await _menuService.GenerarHtmlMenuAsync(menuId, nId.Value);
            if (html == null) return NotFound();
            return Content(html, "text/html");
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }
}

// ── Request DTOs ──────────────────────────────────────────────────

public class MesaRequest
{
    public Guid MesaId { get; set; }
    public string Nombre { get; set; }
    public int Numero { get; set; }
    public int Capacidad { get; set; } = 4;
}

public class CambiarEstadoMesaRequest
{
    public Guid MesaId { get; set; }
    public string Estado { get; set; }
}

public class EliminarMesaRequest
{
    public Guid MesaId { get; set; }
}

public class EliminarMenuRequest
{
    public Guid MenuId { get; set; }
}
