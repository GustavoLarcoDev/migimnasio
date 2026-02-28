// ═══ CitasController.cs — Citas, pagos y clientes artesanal ═══

using Gimnasio.Data;
using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

public class CitasController : NegocioBaseController
{
    private readonly ICitaService _citaService;
    private readonly IClienteService _clienteService;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IReciboService _reciboService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<CitasController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public CitasController(
        ICitaService citaService,
        IClienteService clienteService,
        IAuthService authService,
        ApplicationDbContext context,
        IEmailService emailService,
        IReciboService reciboService,
        IWhatsAppService whatsAppService,
        ILogger<CitasController> logger,
        IServiceScopeFactory scopeFactory) : base(authService)
    {
        _citaService = citaService;
        _clienteService = clienteService;
        _context = context;
        _reciboService = reciboService;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    // ═══ Consultas ═══

    [HttpGet("GetCitasCalendario")]
    public Task<IActionResult> GetCitasCalendario(Guid negocioId, DateTime start, DateTime end)
        => Execute(negocioId, async nId => Ok(await _citaService.GetCitasCalendarioAsync(nId, start, end)));

    [HttpGet("GetCita")]
    public Task<IActionResult> GetCita(Guid citaId, Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var cita = await _citaService.GetCitaAsync(citaId, nId);
            if (cita == null) return NotFound(new { success = false, message = "Cita no encontrada" });
            return Ok(cita);
        });

    [HttpGet("GetSlotsDisponibles")]
    public Task<IActionResult> GetSlotsDisponibles(Guid negocioId, Guid empleadoId, DateTime fecha, int duracionMinutos = 30)
        => Execute(negocioId, async nId => Ok(await _citaService.GetSlotsDisponiblesAsync(nId, empleadoId, fecha, duracionMinutos)));

    [HttpGet("GetCitasDashboardStats")]
    public Task<IActionResult> GetCitasDashboardStats(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _citaService.GetDashboardStatsAsync(nId)));

    [HttpGet("GetHistorialCliente")]
    public Task<IActionResult> GetHistorialCliente(Guid clienteId, Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _citaService.GetHistorialClienteAsync(clienteId, nId)));

    // ═══ CRUD de citas ═══

    [HttpPost("CrearCitaRapida")]
    public Task<IActionResult> CrearCitaRapida([FromForm] CitaQuickCreateDto dto)
        => Execute(dto.NegocioId, async nId =>
        {
            var (success, message, citaId) = await _citaService.CrearCitaRapidaAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            if (citaId.HasValue)
            {
                var cita = await _context.Citas.FindAsync(citaId.Value);
                if (cita != null)
                {
                    var cliente = await _context.Clientes.FindAsync(cita.ClienteId);
                    var negocio = await _context.Negocios.FindAsync(cita.NegocioId);

                    if (cliente != null && negocio != null)
                    {
                        var clienteEmail = cliente.Email;
                        var clienteTelefono = cliente.Telefono;
                        var citaNombreCliente = cita.NombreCliente;
                        var negNombre = negocio.NegocioNombre;
                        var svcNombre = cita.NombreServicio;
                        var empNombre = cita.NombreEmpleado;
                        var fechaInicio = cita.FechaHoraInicio;
                        var duracion = cita.DuracionMinutos;
                        var precio = cita.PrecioServicio;
                        var negEmail = negocio.Email;
                        var negTelefono = negocio.Telefono;
                        var scopeFactory = _scopeFactory;

                        if (!string.IsNullOrWhiteSpace(clienteEmail))
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    using var scope = scopeFactory.CreateScope();
                                    var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                                    await emailSvc.EnviarConfirmacionReservaAsync(
                                        clienteEmail, citaNombreCliente, negNombre,
                                        svcNombre, empNombre, fechaInicio,
                                        duracion, precio, negEmail, negTelefono);
                                }
                                catch { }
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(clienteTelefono))
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    using var scope = scopeFactory.CreateScope();
                                    var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                                    await whatsApp.EnviarConfirmacionReservaWhatsAppAsync(
                                        clienteTelefono, citaNombreCliente, negNombre,
                                        svcNombre, empNombre, fechaInicio,
                                        precio, negTelefono);
                                }
                                catch { }
                            });
                        }
                    }
                }
            }

            return Ok(new { success, message, citaId });
        });

    [HttpGet("BuscarClientes")]
    public Task<IActionResult> BuscarClientes(Guid negocioId, string q)
        => Execute(negocioId, async nId => Ok(await _clienteService.BuscarClientesAsync(nId, q)));

    [HttpPost("CrearCita")]
    public Task<IActionResult> CrearCita([FromForm] CitaCreateDto dto)
        => Execute(dto.NegocioId, async nId =>
        {
            var (success, message) = await _citaService.CrearCitaAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.ClienteId == dto.ClienteId && c.NegocioId == nId);
            var negocio = await _context.Negocios.FindAsync(nId);

            if (cliente != null && negocio != null)
            {
                var cita = await _context.Citas
                    .Where(c => c.ClienteId == dto.ClienteId
                        && c.NegocioId == nId
                        && c.EmpleadoId == dto.EmpleadoId
                        && c.FechaHoraInicio == dto.FechaHoraInicio)
                    .OrderByDescending(c => c.FechaCreacion)
                    .FirstOrDefaultAsync();

                if (cita != null)
                {
                    var clienteEmail = cliente.Email;
                    var clienteTelefono = cliente.Telefono;
                    var citaNombreCliente = cita.NombreCliente;
                    var negNombre = negocio.NegocioNombre;
                    var svcNombre = cita.NombreServicio;
                    var empNombre = cita.NombreEmpleado;
                    var fechaInicio = cita.FechaHoraInicio;
                    var duracion = cita.DuracionMinutos;
                    var precio = cita.PrecioServicio;
                    var negEmail = negocio.Email;
                    var negTelefono = negocio.Telefono;
                    var scopeFactory = _scopeFactory;

                    if (!string.IsNullOrWhiteSpace(clienteEmail))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var scope = scopeFactory.CreateScope();
                                var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                                await emailSvc.EnviarConfirmacionReservaAsync(
                                    clienteEmail, citaNombreCliente, negNombre,
                                    svcNombre, empNombre, fechaInicio,
                                    duracion, precio, negEmail, negTelefono);
                            }
                            catch { }
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(clienteTelefono))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var scope = scopeFactory.CreateScope();
                                var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                                await whatsApp.EnviarConfirmacionReservaWhatsAppAsync(
                                    clienteTelefono, citaNombreCliente, negNombre,
                                    svcNombre, empNombre, fechaInicio,
                                    precio, negTelefono);
                            }
                            catch { }
                        });
                    }
                }
            }

            return Ok(new { success, message });
        });

    [HttpPost("MoverCita")]
    public Task<IActionResult> MoverCita([FromForm] CitaMoveDto dto)
        => Execute(dto.NegocioId, async nId => ServiceResult(await _citaService.MoverCitaAsync(dto)));

    [HttpPost("CambiarEstadoCita")]
    public Task<IActionResult> CambiarEstadoCita(Guid citaId, Guid negocioId, string nuevoEstado, string motivoCancelacion)
        => Execute(negocioId, async nId =>
        {
            var (success, message) = await _citaService.CambiarEstadoCitaAsync(citaId, nId, nuevoEstado, motivoCancelacion);
            if (!success)
                return BadRequest(new { success, message });

            if (nuevoEstado == "confirmada" || nuevoEstado == "cancelada")
            {
                var cita = await _context.Citas.FindAsync(citaId);
                if (cita != null)
                {
                    var cliente = await _context.Clientes.FindAsync(cita.ClienteId);
                    var negocio = await _context.Negocios.FindAsync(cita.NegocioId);
                    var fechaHora = cita.FechaHoraInicio.ToString("dd/MM/yyyy hh:mm tt",
                        System.Globalization.CultureInfo.InvariantCulture);

                    if (negocio != null)
                    {
                        var scopeFactory = _scopeFactory;
                        if (nuevoEstado == "confirmada")
                        {
                            var empleado = await _context.Empleados.FindAsync(cita.EmpleadoId);
                            if (empleado != null && !string.IsNullOrWhiteSpace(empleado.Telefono))
                            {
                                var empNombre = cita.NombreEmpleado;
                                var cliNombre = cita.NombreCliente;
                                var negNombre = negocio.NegocioNombre;
                                var svcNombre = cita.NombreServicio;
                                var empTelefono = empleado.Telefono;
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        using var scope = scopeFactory.CreateScope();
                                        var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                                        await whatsApp.EnviarCitaConfirmadaEmpleadoAsync(
                                            empTelefono, empNombre, cliNombre,
                                            negNombre, svcNombre, fechaHora);
                                    }
                                    catch { }
                                });
                            }
                        }
                        else if (nuevoEstado == "cancelada")
                        {
                            if (cliente != null && !string.IsNullOrWhiteSpace(cliente.Telefono))
                            {
                                var cliTelefono = cliente.Telefono;
                                var cliNombre = cita.NombreCliente;
                                var negNombre = negocio.NegocioNombre;
                                var svcNombre = cita.NombreServicio;
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        using var scope = scopeFactory.CreateScope();
                                        var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                                        await whatsApp.EnviarCitaCanceladaClienteAsync(
                                            cliTelefono, cliNombre, negNombre,
                                            svcNombre, fechaHora);
                                    }
                                    catch { }
                                });
                            }
                        }
                    }
                }
            }

            return Ok(new { success, message });
        });

    // ═══ Pagos ═══

    [HttpPost("RegistrarPagoCita")]
    public Task<IActionResult> RegistrarPagoCita([FromForm] PagoCitaDto dto)
        => Execute(dto.NegocioId, async nId =>
        {
            var (success, message) = await _citaService.RegistrarPagoAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            var cita = await _context.Citas
                .FirstOrDefaultAsync(c => c.CitaId == dto.CitaId && c.NegocioId == nId);

            if (cita != null)
            {
                var cliente = await _context.Clientes.FindAsync(cita.ClienteId);
                var negocio = await _context.Negocios.FindAsync(cita.NegocioId);
                var pago = await _context.PagosCita
                    .FirstOrDefaultAsync(p => p.CitaId == dto.CitaId);

                if (negocio != null && pago != null)
                {
                    try
                    {
                        var numRecibo = await _reciboService.ObtenerSiguienteNumeroAsync(nId);
                        var concepto = $"Servicio: {cita.NombreServicio}";
                        var clienteEmail = cliente?.Email ?? "";
                        var clienteNombre = cita.NombreCliente ?? "Cliente";

                        var (enviado, html) = await _emailService.EnviarReciboCitaCompletadaAsync(
                            clienteEmail, clienteNombre, negocio.NegocioNombre,
                            cita.NombreServicio, cita.NombreEmpleado,
                            pago.MontoServicio, pago.MontoExtra, pago.Propina, pago.Total,
                            negocio.Email, negocio.Telefono, numRecibo,
                            metodoPago: pago.MetodoPago ?? "Efectivo", logoUrl: negocio.LogoUrl);

                        await _reciboService.CrearReciboAsync(nId, numRecibo, "pago_cita",
                            clienteEmail, clienteNombre, negocio.NegocioNombre, concepto, pago.Total, html);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al generar recibo para cita {CitaId}", dto.CitaId);
                    }
                }
            }

            return Ok(new { success, message });
        });

    [HttpGet("GetPagoCita")]
    public Task<IActionResult> GetPagoCita(Guid citaId, Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _citaService.GetPagoCitaAsync(citaId, nId)));

    // ═══ Clientes artesanal ═══

    [HttpPost("CrearClienteArtesanal")]
    public Task<IActionResult> CrearClienteArtesanal([FromForm] ClienteArtesanalCreateDto dto)
        => Execute(dto.NegocioId, async nId => ServiceResult(await _clienteService.CrearClienteArtesanalAsync(dto)));

    [HttpPost("EditarClienteArtesanal")]
    public Task<IActionResult> EditarClienteArtesanal([FromForm] ClienteArtesanalCreateDto dto)
        => Execute(dto.NegocioId, async nId => ServiceResult(await _clienteService.EditarClienteArtesanalAsync(dto)));

    [HttpGet("GetClientesArtesanal")]
    public Task<IActionResult> GetClientesArtesanal(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _clienteService.GetClientesArtesanalAsync(nId)));
}
