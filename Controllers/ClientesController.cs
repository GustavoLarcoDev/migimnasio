// ═══ ClientesController.cs — Dashboard, clientes, membresias, recibos, perfil ═══

using Gimnasio.Helpers;
using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;
using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

public class ClientesController : NegocioBaseController
{
    private readonly IClienteService _clienteService;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IReciboService _reciboService;
    private readonly IWhatsAppService _whatsAppService;

    public ClientesController(
        IClienteService clienteService,
        IAuthService authService,
        ApplicationDbContext context,
        IEmailService emailService,
        IReciboService reciboService,
        IWhatsAppService whatsAppService) : base(authService)
    {
        _clienteService = clienteService;
        _context = context;
        _emailService = emailService;
        _reciboService = reciboService;
        _whatsAppService = whatsAppService;
    }

    // ═══ Dashboard ═══

    [HttpGet("{id}/Dashboard")]
    public Task<IActionResult> Dashboard(Guid id)
        => Execute(id, async nId =>
        {
            var negocio = await _context.Negocios
                .Include(g => g.Clientes)
                .FirstOrDefaultAsync(g => g.NegocioId == nId);

            if (negocio == null) return NotFound();

            if (negocio.TipoNegocio == "artesanal")
                return View("~/Views/Negocios/DashboardArtesanal.cshtml", negocio);
            if (negocio.TipoNegocio == "tienda")
                return View("~/Views/Negocios/DashboardTienda.cshtml", negocio);
            if (negocio.TipoNegocio == "restaurante")
                return View("~/Views/Negocios/DashboardRestaurante.cshtml", negocio);

            return View("~/Views/Negocios/Dashboard.cshtml", negocio);
        });

    // ═══ Consultas ═══

    [HttpGet("GetDashboardStats")]
    public Task<IActionResult> GetDashboardStats(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _clienteService.GetDashboardStatsAsync(nId)));

    [HttpGet("GetClientes")]
    public Task<IActionResult> GetClientes(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _clienteService.GetClientesAsync(nId)));

    [HttpGet("GetCliente")]
    public Task<IActionResult> GetCliente(Guid id, Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var cliente = await _clienteService.GetClienteAsync(id, nId);
            if (cliente == null)
                return NotFound(new { success = false, message = "Cliente no encontrado" });
            return Ok(cliente);
        });

    [HttpGet("GetSuscripcionStatus")]
    public Task<IActionResult> GetSuscripcionStatus(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var negocio = await _context.Negocios.FindAsync(nId);
            if (negocio == null) return NotFound();

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
        });

    [HttpGet("GetClientesDiarios")]
    public Task<IActionResult> GetClientesDiarios(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _clienteService.GetClientesDiariosAsync(nId)));

    // ═══ CRUD de clientes ═══

    [HttpPost("CrearCliente")]
    public Task<IActionResult> CrearCliente([FromForm] ClienteCreateDto model)
        => Execute(model.NegocioId, async nId =>
        {
            var (success, message) = await _clienteService.CrearClienteAsync(model);
            if (!success)
                return BadRequest(new { success = false, message });

            try
            {
                var negocio = await _context.Negocios.FindAsync(nId);
                if (negocio != null)
                {
                    var concepto = $"Membresía x{model.Dias} días";
                    var nombreCompleto = $"{model.Nombre} {model.Apellido}";
                    var numRecibo = await _reciboService.ObtenerSiguienteNumeroAsync(nId);
                    var metodo = model.MetodoPago ?? "Efectivo";
                    var email = model.Email?.Trim();

                    string html = "";
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        var (enviado, htmlEmail) = await _emailService.EnviarReciboPagoClienteAsync(
                            email, nombreCompleto, negocio.NegocioNombre,
                            concepto, model.Precio, model.Dias,
                            negocio.Email, negocio.Telefono, numRecibo,
                            metodoPago: metodo, logoUrl: negocio.LogoUrl);
                        html = htmlEmail;
                    }
                    await _reciboService.CrearReciboAsync(nId, numRecibo, "pago_cliente",
                        email ?? "", nombreCompleto, negocio.NegocioNombre, concepto, model.Precio, html, metodo);
                }
            }
            catch { }

            return Ok(new { success = true, message });
        });

    [HttpPost("EditarCliente")]
    public Task<IActionResult> EditarCliente([FromForm] ClienteCreateDto model)
        => Execute(model.NegocioId, async nId =>
        {
            var (success, message) = await _clienteService.EditarClienteAsync(model);
            if (!success)
            {
                if (message.Contains("no encontrado"))
                    return NotFound(new { success = false, message });
                return BadRequest(new { success = false, message });
            }
            return Ok(new { success = true, message });
        });

    [HttpPost("EliminarCliente")]
    public Task<IActionResult> EliminarCliente(Guid id, Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var (success, message) = await _clienteService.EliminarClienteAsync(id, nId);
            if (!success)
                return NotFound(new { success = false, message });
            return Ok(new { success = true, message });
        });

    // ═══ Renovacion de membresias ═══

    [HttpPost("RenovarCliente")]
    public Task<IActionResult> RenovarCliente(Guid id, Guid negocioId, DateTime nuevaFechaFin, decimal precio, string metodoPago = "Efectivo")
        => Execute(negocioId, async nId =>
        {
            var (success, message) = await _clienteService.RenovarClienteAsync(id, nId, nuevaFechaFin, precio, metodoPago);
            if (!success)
            {
                if (message.Contains("no encontrado"))
                    return NotFound(new { success = false, message });
                return BadRequest(new { success = false, message });
            }

            try
            {
                var cliente = await _context.Clientes.FindAsync(id);
                var negocio = await _context.Negocios.FindAsync(nId);
                if (cliente != null && negocio != null)
                {
                    var dias = (int)(nuevaFechaFin.Date - TimeHelper.Now.Date).TotalDays;
                    if (dias < 1) dias = 1;
                    var concepto = $"Renovación membresía x{dias} días";
                    var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
                    var numRecibo = await _reciboService.ObtenerSiguienteNumeroAsync(nId);
                    var email = cliente.Email?.Trim();

                    string html = "";
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        var (enviado, htmlEmail) = await _emailService.EnviarReciboPagoClienteAsync(
                            email, nombreCompleto, negocio.NegocioNombre,
                            concepto, precio, dias, negocio.Email, negocio.Telefono, numRecibo,
                            metodoPago: metodoPago, logoUrl: negocio.LogoUrl);
                        html = htmlEmail;
                    }
                    await _reciboService.CrearReciboAsync(nId, numRecibo, "pago_cliente",
                        email ?? "", nombreCompleto, negocio.NegocioNombre, concepto, precio, html, metodoPago);
                }
            }
            catch { }

            return Ok(new { success = true, message });
        });

    // ═══ Clientes diarios ═══

    [HttpPost("LimpiarClientesDiarios")]
    public Task<IActionResult> LimpiarClientesDiarios(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var limpiados = await _clienteService.LimpiarClientesDiariosAsync(nId);
            return Ok(new { success = true, limpiados });
        });

    // ═══ Excel import/export ═══

    [HttpGet("ExportClientesExcel")]
    public Task<IActionResult> ExportClientesExcel(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var content = await _clienteService.ExportClientesExcelAsync(nId);
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Clientes_{TimeHelper.Now:yyyyMMdd}.xlsx");
        });

    [HttpPost("ImportarClientesExcel")]
    public Task<IActionResult> ImportarClientesExcel(Guid negocioId, IFormFile file)
        => Execute(negocioId, async nId =>
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No se ha proporcionado ningún archivo" });

            const long maxBytes = 10 * 1024 * 1024;
            if (file.Length > maxBytes)
                return BadRequest(new { success = false, message = "El archivo no puede superar 10 MB" });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest(new { success = false, message = "El archivo debe ser un Excel (.xlsx o .xls)" });

            using var stream = file.OpenReadStream();
            var result = await _clienteService.ImportarClientesExcelAsync(nId, stream);
            return Ok(result);
        });

    // ═══ Recibos ═══

    [HttpGet("GetRecibos")]
    public Task<IActionResult> GetRecibos(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _reciboService.GetRecibosAsync(nId)));

    [HttpGet("GetRecibo")]
    public Task<IActionResult> GetRecibo(Guid reciboId, Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var recibo = await _reciboService.GetReciboAsync(reciboId, nId);
            if (recibo == null)
                return NotFound(new { success = false, message = "Recibo no encontrado" });
            return Ok(recibo);
        });

    [HttpGet("BuscarRecibo")]
    public Task<IActionResult> BuscarRecibo(int numero, Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _reciboService.BuscarPorNumeroAsync(numero, nId)));

    [HttpGet("ExportRecibosExcel")]
    public Task<IActionResult> ExportRecibosExcel(Guid negocioId, int anio, int mes)
        => Execute(negocioId, async nId =>
        {
            var content = await _reciboService.ExportRecibosExcelAsync(nId, anio, mes);
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Recibos_{anio}_{mes:D2}.xlsx");
        });

    [HttpGet("GetFechaReciboMasAntiguo")]
    public Task<IActionResult> GetFechaReciboMasAntiguo(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var fecha = await _reciboService.GetFechaReciboMasAntiguoAsync(nId);
            return Ok(new { fecha });
        });

    [HttpPost("EliminarRecibosAntiguos")]
    public Task<IActionResult> EliminarRecibosAntiguos(Guid negocioId, DateTime anteriorA)
        => Execute(negocioId, async nId =>
        {
            var eliminados = await _reciboService.EliminarRecibosAntiguosAsync(nId, anteriorA);
            return Ok(new { success = true, eliminados });
        });

    // ═══ Promociones masivas WhatsApp ═══

    [HttpGet("ContarDestinatariosPromocion")]
    public Task<IActionResult> ContarDestinatariosPromocion(Guid negocioId, string filtro = "todos")
        => Execute(negocioId, async nId =>
        {
            var hoy = TimeHelper.Now.Date;
            var query = _context.Clientes
                .Where(c => c.NegocioId == nId && !c.EsDiario && !string.IsNullOrEmpty(c.Telefono));

            if (filtro == "activos")
                query = query.Where(c => c.FechaQueTermina.Date >= hoy);
            else if (filtro == "vencidos")
                query = query.Where(c => c.FechaQueTermina.Date < hoy);

            var count = await query.CountAsync();
            return Ok(new { count });
        });

    [HttpPost("EnviarPromocionMasiva")]
    public Task<IActionResult> EnviarPromocionMasiva(Guid negocioId, string mensaje, string filtro = "todos")
        => Execute(negocioId, async nId =>
        {
            if (string.IsNullOrWhiteSpace(mensaje))
                return BadRequest(new { success = false, message = "El mensaje no puede estar vacío" });

            var negocio = await _context.Negocios.FindAsync(nId);
            if (negocio == null)
                return NotFound(new { success = false, message = "Negocio no encontrado" });

            var hoy = TimeHelper.Now.Date;
            var query = _context.Clientes
                .Where(c => c.NegocioId == nId && !c.EsDiario && !string.IsNullOrEmpty(c.Telefono));

            if (filtro == "activos")
                query = query.Where(c => c.FechaQueTermina.Date >= hoy);
            else if (filtro == "vencidos")
                query = query.Where(c => c.FechaQueTermina.Date < hoy);

            var clientes = await query
                .Select(c => new { c.Telefono, c.Nombre, c.Apellido })
                .Take(200)
                .ToListAsync();

            if (clientes.Count == 0)
                return BadRequest(new { success = false, message = "No hay clientes con teléfono para enviar" });

            var disclaimer = "";
            if (!string.IsNullOrWhiteSpace(negocio.Telefono))
            {
                var numLimpio = negocio.Telefono.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                if (numLimpio.StartsWith("0") && numLimpio.Length == 10)
                    numLimpio = "593" + numLimpio.Substring(1);
                disclaimer = $"\n\n_Para comunicarte con *{negocio.NegocioNombre}*, escríbeles aquí:_ https://wa.me/{numLimpio}";
            }
            else
            {
                disclaimer = $"\n\n_Si tiene alguna duda, comuníquese directamente con *{negocio.NegocioNombre}*._";
            }

            var negocioNombre = negocio.NegocioNombre;
            var telefonos = clientes.Select(c => c.Telefono).ToList();
            _ = Task.Run(async () =>
            {
                foreach (var telefono in telefonos)
                {
                    try
                    {
                        var mensajeCompleto = $"*{negocioNombre}*\n\n{mensaje}{disclaimer}";
                        await _whatsAppService.EnviarMensajeTextoAsync(telefono, mensajeCompleto);
                    }
                    catch { }
                    await Task.Delay(1000);
                }
            });

            return Ok(new { success = true, message = $"Enviando promoción a {clientes.Count} clientes...", total = clientes.Count });
        });

    // ═══ Perfil del negocio ═══

    [HttpGet("GetPerfilNegocio")]
    public Task<IActionResult> GetPerfilNegocio(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var negocio = await _context.Negocios.FindAsync(nId);
            if (negocio == null) return NotFound();

            var now = TimeHelper.Now;
            int? diasRestantes = negocio.FechaExpiracion.HasValue
                ? (int)(negocio.FechaExpiracion.Value.Date - now.Date).TotalDays
                : null;
            var diasSuscrito = (int)(now.Date - negocio.FechaCreacion.Date).TotalDays;

            return Ok(new
            {
                logoUrl = negocio.LogoUrl,
                nombre = negocio.NegocioNombre,
                email = negocio.Email,
                telefono = negocio.Telefono,
                direccion = negocio.Direccion,
                tipoNegocio = negocio.TipoNegocio,
                diasRestantes,
                diasSuscrito,
                fechaRegistro = negocio.FechaCreacion,
                esPrueba = negocio.EsPrueba
            });
        });

    [HttpPost("SubirLogoNegocio")]
    public Task<IActionResult> SubirLogoNegocio([FromForm] Guid negocioId, IFormFile imagen)
        => Execute(negocioId, async nId =>
        {
            var (valid, error, dataUri) = await ImageUploadHelper.ProcessAsync(imagen);
            if (!valid) return BadRequest(new { success = false, message = error });

            var negocio = await _context.Negocios.FindAsync(nId);
            if (negocio == null) return NotFound();

            negocio.LogoUrl = dataUri;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Logo actualizado", url = dataUri });
        });

    [HttpPost("CambiarPassword")]
    public Task<IActionResult> CambiarPassword(Guid negocioId, string passwordActual, string passwordNueva)
        => Execute(negocioId, async nId =>
        {
            if (string.IsNullOrWhiteSpace(passwordNueva) || passwordNueva.Length < 6)
                return BadRequest(new { success = false, message = "La nueva contraseña debe tener al menos 6 caracteres." });

            var negocio = await _context.Negocios.FindAsync(nId);
            if (negocio == null) return NotFound();

            bool passwordValida = negocio.Password.StartsWith("$2")
                ? AuthService.VerifyPassword(passwordActual, negocio.Password)
                : passwordActual == negocio.Password;

            if (!passwordValida)
                return BadRequest(new { success = false, message = "La contraseña actual es incorrecta." });

            negocio.Password = AuthService.HashPassword(passwordNueva);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Contraseña actualizada correctamente." });
        });

    [HttpPost("ActualizarPerfilNegocio")]
    public Task<IActionResult> ActualizarPerfilNegocio(Guid negocioId, string email = null, string telefono = null, string direccion = null)
        => Execute(negocioId, async nId =>
        {
            var negocio = await _context.Negocios.FindAsync(nId);
            if (negocio == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(email))
                negocio.Email = email.Trim();
            if (telefono != null)
                negocio.Telefono = telefono.Trim();
            if (direccion != null)
                negocio.Direccion = direccion.Trim();

            negocio.FechaDeActualizacion = TimeHelper.Now;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Perfil actualizado correctamente." });
        });

    // ═══ Terminos y condiciones ═══

    [HttpPost("AceptarTerminosNegocio")]
    public Task<IActionResult> AceptarTerminosNegocio()
        => ExecuteSelf(async nId =>
        {
            var negocio = await _context.Negocios.FindAsync(nId);
            if (negocio == null) return NotFound();

            negocio.AceptoTerminos = true;
            negocio.FechaAceptoTerminos = TimeHelper.Now;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Términos aceptados exitosamente" });
        });
}
