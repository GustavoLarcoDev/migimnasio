// ═══ RestauranteController.cs — Menus publicos + CRUD mesas/menus/reservas ═══

using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

// ═══ PARTE 1 — Endpoints publicos del menu (ruta /Restaurante) ═══

[Route("Restaurante")]
[Authorize]
public class RestauranteController : Controller
{
    private readonly IAuthService _authService;
    private readonly IMenuRestauranteService _menuService;
    private readonly IEmailService _emailService;

    public RestauranteController(
        IAuthService authService,
        IMenuRestauranteService menuService,
        IEmailService emailService)
    {
        _authService = authService;
        _menuService = menuService;
        _emailService = emailService;
    }

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

        return Json(new { success = false, message = "Metodo de envio no soportado." });
    }
}

// ═══ PARTE 2 — CRUD mesas, menus y reservas (ruta /Negocios) ═══

public class RestauranteCrudController : NegocioBaseController
{
    private readonly IMesaService _mesaService;
    private readonly IMenuRestauranteService _menuService;
    private readonly IReservaService _reservaService;

    public RestauranteCrudController(
        IAuthService authService,
        IMesaService mesaService,
        IMenuRestauranteService menuService,
        IReservaService reservaService) : base(authService)
    {
        _mesaService = mesaService;
        _menuService = menuService;
        _reservaService = reservaService;
    }

    // ═══ Mesas ═══

    [HttpGet("GetMesas")]
    public Task<IActionResult> GetMesas(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _mesaService.GetMesasAsync(nId)));

    [HttpGet("GetMesa")]
    public Task<IActionResult> GetMesa(Guid mesaId)
        => ExecuteSelf(async nId =>
        {
            var mesa = await _mesaService.GetMesaAsync(mesaId, nId);
            if (mesa == null) return NotFound(new { success = false, message = "Mesa no encontrada" });
            return Ok(mesa);
        });

    [HttpPost("CrearMesa")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> CrearMesa([FromBody] MesaRequest req)
        => ExecuteSelf(async nId => ServiceResult(await _mesaService.CrearMesaAsync(nId, req.Nombre, req.Numero, req.Capacidad)));

    [HttpPost("EditarMesa")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> EditarMesa([FromBody] MesaRequest req)
        => ExecuteSelf(async nId => ServiceResult(await _mesaService.EditarMesaAsync(req.MesaId, nId, req.Nombre, req.Numero, req.Capacidad)));

    [HttpPost("CambiarEstadoMesa")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> CambiarEstadoMesa([FromBody] CambiarEstadoMesaRequest req)
        => ExecuteSelf(async nId => ServiceResult(await _mesaService.CambiarEstadoMesaAsync(req.MesaId, nId, req.Estado)));

    [HttpPost("EliminarMesa")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> EliminarMesa([FromBody] EliminarMesaRequest req)
        => ExecuteSelf(async nId => ServiceResult(await _mesaService.EliminarMesaAsync(req.MesaId, nId)));

    // ═══ Menus ═══

    [HttpGet("GetMenus")]
    public Task<IActionResult> GetMenus(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _menuService.GetMenusAsync(nId)));

    [HttpGet("GetMenu")]
    public Task<IActionResult> GetMenu(Guid menuId)
        => ExecuteSelf(async nId =>
        {
            var menu = await _menuService.GetMenuAsync(menuId, nId);
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
        });

    [HttpPost("CrearMenu")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> CrearMenu([FromBody] MenuRestauranteDto dto)
        => ExecuteSelf(async nId =>
        {
            dto.NegocioId = nId;
            return ServiceResult(await _menuService.CrearMenuAsync(dto));
        });

    [HttpPost("EditarMenu")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> EditarMenu([FromBody] MenuRestauranteDto dto)
        => ExecuteSelf(async nId =>
        {
            dto.NegocioId = nId;
            return ServiceResult(await _menuService.EditarMenuAsync(dto));
        });

    [HttpPost("CambiarDisponibilidadMenu")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> CambiarDisponibilidadMenu([FromBody] EliminarMenuRequest req)
        => ExecuteSelf(async nId =>
        {
            var (success, message, disponible) = await _menuService.CambiarDisponibilidadMenuAsync(req.MenuId, nId);
            return success ? Ok(new { success, message, disponible }) : BadRequest(new { success, message });
        });

    [HttpPost("EliminarMenu")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> EliminarMenu([FromBody] EliminarMenuRequest req)
        => ExecuteSelf(async nId => ServiceResult(await _menuService.EliminarMenuAsync(req.MenuId, nId)));

    [HttpGet("PreviewMenu")]
    public Task<IActionResult> PreviewMenu(Guid menuId)
        => ExecuteSelf(async nId =>
        {
            var html = await _menuService.GenerarHtmlMenuAsync(menuId, nId);
            if (html == null) return NotFound();
            return Content(html, "text/html");
        });

    // ═══ Reservas ═══

    [HttpGet("GetReservas")]
    public Task<IActionResult> GetReservas(Guid negocioId, DateTime? fecha = null)
        => Execute(negocioId, async nId => Ok(await _reservaService.GetReservasAsync(nId, fecha)));

    [HttpGet("GetReservasHoy")]
    public Task<IActionResult> GetReservasHoy(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _reservaService.GetReservasHoyAsync(nId)));

    [HttpPost("CrearReserva")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> CrearReserva([FromBody] ReservaRequest req)
        => ExecuteSelf(async nId => ServiceResult(await _reservaService.CrearReservaAsync(
            nId, req.NombreCliente, req.Telefono,
            req.FechaHoraReserva, req.CantidadPersonas, req.MesaId, req.Notas)));

    [HttpPost("EditarReserva")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> EditarReserva([FromBody] ReservaRequest req)
        => ExecuteSelf(async nId => ServiceResult(await _reservaService.EditarReservaAsync(
            req.ReservaId, nId, req.NombreCliente, req.Telefono,
            req.FechaHoraReserva, req.CantidadPersonas, req.MesaId, req.Notas)));

    [HttpPost("CambiarEstadoReserva")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> CambiarEstadoReserva([FromBody] CambiarEstadoReservaRequest req)
        => ExecuteSelf(async nId => ServiceResult(await _reservaService.CambiarEstadoReservaAsync(req.ReservaId, nId, req.Estado)));

    [HttpPost("CancelarReserva")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> CancelarReserva([FromBody] CambiarEstadoReservaRequest req)
        => ExecuteSelf(async nId => ServiceResult(await _reservaService.CancelarReservaAsync(req.ReservaId, nId)));
}

// ═══ Request DTOs ═══

public class ReservaRequest
{
    public Guid ReservaId { get; set; }
    public string NombreCliente { get; set; }
    public string Telefono { get; set; }
    public DateTime FechaHoraReserva { get; set; }
    public int CantidadPersonas { get; set; } = 2;
    public Guid? MesaId { get; set; }
    public string Notas { get; set; }
}

public class CambiarEstadoReservaRequest
{
    public Guid ReservaId { get; set; }
    public string Estado { get; set; }
}

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
