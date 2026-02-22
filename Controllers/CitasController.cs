// ═══════════════════════════════════════════════════════════
// CitasController.cs — Controlador de citas del modelo artesanal
//
// Responsabilidad: exponer los endpoints HTTP para todo lo
// relacionado con citas (reservaciones). Este controlador NO
// contiene lógica de negocio — delega en ICitaService e
// IClienteService. Su única responsabilidad es:
//   1. Verificar que el usuario autenticado pertenece al negocio
//      que está intentando modificar (seguridad multi-tenant).
//   2. Llamar al servicio correspondiente.
//   3. Devolver la respuesta HTTP correcta.
//
// Ruta base: /Negocios  (todos los endpoints son AJAX)
// Requiere: cookie de autenticación [Authorize]
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class CitasController : Controller
{
    // ── Servicios inyectados por el contenedor de dependencias ──
    // Se reciben en el constructor y se guardan como readonly para
    // asegurarse de que no se reasignan accidentalmente en otra parte.

    private readonly ICitaService _citaService;
    private readonly IClienteService _clienteService;
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IReciboService _reciboService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<CitasController> _logger;

    public CitasController(
        ICitaService citaService,
        IClienteService clienteService,
        IAuthService authService,
        ApplicationDbContext context,
        IEmailService emailService,
        IReciboService reciboService,
        IWhatsAppService whatsAppService,
        ILogger<CitasController> logger)
    {
        _citaService = citaService;
        _clienteService = clienteService;
        _authService = authService;
        _context = context;
        _reciboService = reciboService;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    // ENDPOINTS DE CONSULTA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Devuelve las citas de un rango de fechas formateadas para FullCalendar.
    /// FullCalendar (la librería del frontend) llama este endpoint cada vez que
    /// el usuario navega entre semanas o meses en el calendario, enviando los
    /// parámetros "start" y "end" automáticamente.
    /// </summary>
    /// <param name="negocioId">Negocio al que pertenecen las citas.</param>
    /// <param name="start">Inicio del rango visible en el calendario.</param>
    /// <param name="end">Fin del rango visible en el calendario.</param>
    /// <returns>Array JSON con eventos en formato FullCalendar.</returns>
    [HttpGet("GetCitasCalendario")]
    public async Task<IActionResult> GetCitasCalendario(Guid negocioId, DateTime start, DateTime end)
    {
        try
        {
            // Seguridad: el negocioId del query string debe coincidir con el del usuario logueado.
            // Esto previene que un negocio consulte citas de otro negocio.
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var citas = await _citaService.GetCitasCalendarioAsync(negocioId, start, end);
            return Ok(citas);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Devuelve el detalle completo de una cita individual, incluyendo datos de pago.
    /// Se usa cuando el usuario hace clic en un evento del calendario para ver
    /// el panel lateral con toda la información de esa cita.
    /// </summary>
    /// <param name="citaId">ID único de la cita a consultar.</param>
    /// <param name="negocioId">ID del negocio (para verificar pertenencia).</param>
    /// <returns>Objeto JSON con todos los campos de la cita y su pago asociado.</returns>
    [HttpGet("GetCita")]
    public async Task<IActionResult> GetCita(Guid citaId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var cita = await _citaService.GetCitaAsync(citaId, negocioId);

            // Si la cita no existe o pertenece a otro negocio, el servicio devuelve null.
            if (cita == null)
                return NotFound(new { success = false, message = "Cita no encontrada" });

            return Ok(cita);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Calcula y devuelve los bloques de tiempo disponibles (slots) para un empleado
    /// en una fecha determinada. Este es el corazón del sistema de reservaciones:
    /// el frontend lo llama cada vez que el usuario selecciona un empleado y una fecha
    /// para que el usuario solo pueda elegir horarios realmente libres.
    ///
    /// La lógica de slots funciona así:
    ///   1. Se obtiene el horario semanal del empleado (ej. Lunes 9:00-17:00).
    ///   2. Se verifica si ese día específico tiene una excepción de horario
    ///      (día libre, horario reducido por vacaciones, etc.).
    ///   3. Se listan las citas ya existentes para esa fecha y empleado.
    ///   4. Se generan intervalos de "duracionMinutos" minutos dentro del horario
    ///      disponible, excluyendo los intervalos ya ocupados por otras citas.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <param name="empleadoId">ID del empleado cuya agenda se consulta.</param>
    /// <param name="fecha">Día para el que se calculan los slots.</param>
    /// <param name="duracionMinutos">Duración del servicio en minutos (default 30).</param>
    /// <returns>Array JSON de slots con hora inicio, hora fin y si está disponible.</returns>
    [HttpGet("GetSlotsDisponibles")]
    public async Task<IActionResult> GetSlotsDisponibles(Guid negocioId, Guid empleadoId, DateTime fecha, int duracionMinutos = 30)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var slots = await _citaService.GetSlotsDisponiblesAsync(negocioId, empleadoId, fecha, duracionMinutos);
            return Ok(slots);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Devuelve las estadísticas del día actual para las tarjetas del dashboard:
    /// citas programadas, ingresos del día, citas completadas y canceladas.
    /// El frontend llama este endpoint al cargar la página para mostrar los KPIs.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <returns>Objeto JSON con conteos e importes del día.</returns>
    [HttpGet("GetCitasDashboardStats")]
    public async Task<IActionResult> GetCitasDashboardStats(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var stats = await _citaService.GetDashboardStatsAsync(negocioId);
            return Ok(stats);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Devuelve el historial de las últimas 50 citas de un cliente específico.
    /// Se usa en el panel lateral de detalle de cliente para mostrar su historial
    /// de visitas, servicios contratados y montos pagados.
    /// </summary>
    /// <param name="clienteId">ID del cliente cuyo historial se consulta.</param>
    /// <param name="negocioId">ID del negocio (para verificar pertenencia del cliente).</param>
    /// <returns>Array JSON con las últimas 50 citas del cliente, ordenadas por fecha desc.</returns>
    [HttpGet("GetHistorialCliente")]
    public async Task<IActionResult> GetHistorialCliente(Guid clienteId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var historial = await _citaService.GetHistorialClienteAsync(clienteId, negocioId);
            return Ok(historial);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE CITAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea una cita rápida en un solo paso. Es la acción principal del modal
    /// "Nueva cita" del calendario.
    ///
    /// La diferencia con CrearCita es que este endpoint permite crear un cliente
    /// nuevo "en línea" sin salir del flujo de reservación:
    ///   - Si el DTO incluye ClienteId → usa el cliente existente.
    ///   - Si ClienteId es null → crea un cliente nuevo con Nombre/Apellido/Telefono
    ///     y luego crea la cita con ese nuevo cliente.
    ///
    /// Devuelve el citaId del registro creado para que el frontend pueda
    /// resaltar inmediatamente el nuevo evento en el calendario.
    /// </summary>
    /// <param name="dto">
    ///   Datos de la cita (negocioId, empleadoId, servicioId, fecha/hora inicio)
    ///   y datos opcionales del cliente nuevo (nombre, apellido, teléfono, email).
    /// </param>
    /// <returns>{ success, message, citaId } — citaId permite al frontend actualizar el calendario.</returns>
    [HttpPost("CrearCitaRapida")]
    public async Task<IActionResult> CrearCitaRapida([FromForm] CitaQuickCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            // El servicio devuelve una tupla (success, message, citaId).
            // Si success es false, devolvemos 400 con el mensaje de error del servicio
            // (ej. "El empleado no trabaja en ese horario", "Ese slot ya está ocupado").
            var (success, message, citaId) = await _citaService.CrearCitaRapidaAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            // Enviar confirmación de reserva por email si la cita se creó exitosamente.
            // Se obtiene el email del cliente y los datos del negocio para el email.
            // Se ejecuta en segundo plano para no bloquear la respuesta HTTP al usuario.
            // IMPORTANTE: Los datos se cargan ANTES del Task.Run porque _context es scoped
            // al request HTTP y estará disposed cuando el Task.Run se ejecute.
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
                        if (!string.IsNullOrWhiteSpace(clienteEmail))
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await _emailService.EnviarConfirmacionReservaAsync(
                                        clienteEmail, cita.NombreCliente, negocio.NegocioNombre,
                                        cita.NombreServicio, cita.NombreEmpleado, cita.FechaHoraInicio,
                                        cita.DuracionMinutos, cita.PrecioServicio,
                                        negocio.Email, negocio.Telefono);
                                }
                                catch { }
                            });
                        }

                        // Enviar confirmación de reserva por WhatsApp al cliente
                        if (!string.IsNullOrWhiteSpace(cliente.Telefono))
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await _whatsAppService.EnviarConfirmacionReservaWhatsAppAsync(
                                        cliente.Telefono, cita.NombreCliente, negocio.NegocioNombre,
                                        cita.NombreServicio, cita.NombreEmpleado, cita.FechaHoraInicio,
                                        cita.PrecioServicio);
                                }
                                catch { }
                            });
                        }
                    }
                }
            }

            return Ok(new { success, message, citaId });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Busca clientes del negocio por nombre, apellido o teléfono.
    /// Se usa en el autocompletado (select2/typeahead) del modal de nueva cita
    /// para que el usuario encuentre rápidamente un cliente existente.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <param name="q">Texto de búsqueda (mínimo 2 caracteres en el frontend).</param>
    /// <returns>Array JSON de clientes que coinciden con la búsqueda.</returns>
    [HttpGet("BuscarClientes")]
    public async Task<IActionResult> BuscarClientes(Guid negocioId, string q)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var clientes = await _clienteService.BuscarClientesAsync(negocioId, q);
            return Ok(clientes);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crea una cita estándar con un cliente ya existente en el sistema.
    /// A diferencia de CrearCitaRapida, este flujo requiere que el cliente
    /// ya exista — no permite crear el cliente inline.
    ///
    /// El servicio valida internamente:
    ///   - Que el empleado trabaje en el horario seleccionado.
    ///   - Que no exista ya otra cita en ese slot (doble-booking).
    ///   - Que el servicio pertenezca al negocio.
    /// </summary>
    /// <param name="dto">
    ///   negocioId, clienteId, empleadoId, servicioId y FechaHoraInicio.
    ///   La hora fin se calcula automáticamente según la duración del servicio.
    /// </param>
    [HttpPost("CrearCita")]
    public async Task<IActionResult> CrearCita([FromForm] CitaCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _citaService.CrearCitaAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            // Enviar confirmación de reserva por email al cliente.
            // Se ejecuta en segundo plano para no bloquear la respuesta HTTP al usuario.
            // IMPORTANTE: Los datos se cargan ANTES del Task.Run porque _context es scoped
            // al request HTTP y estará disposed cuando el Task.Run se ejecute.
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.ClienteId == dto.ClienteId && c.NegocioId == dto.NegocioId);
            var negocio = await _context.Negocios.FindAsync(dto.NegocioId);

            if (cliente != null && negocio != null)
            {
                // Buscar la cita recién creada para obtener todos los datos desnormalizados
                var cita = await _context.Citas
                    .Where(c => c.ClienteId == dto.ClienteId
                        && c.NegocioId == dto.NegocioId
                        && c.EmpleadoId == dto.EmpleadoId
                        && c.FechaHoraInicio == dto.FechaHoraInicio)
                    .OrderByDescending(c => c.FechaCreacion)
                    .FirstOrDefaultAsync();

                if (cita != null)
                {
                    var clienteEmail = cliente.Email;
                    if (!string.IsNullOrWhiteSpace(clienteEmail))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _emailService.EnviarConfirmacionReservaAsync(
                                    clienteEmail, cita.NombreCliente, negocio.NegocioNombre,
                                    cita.NombreServicio, cita.NombreEmpleado, cita.FechaHoraInicio,
                                    cita.DuracionMinutos, cita.PrecioServicio,
                                    negocio.Email, negocio.Telefono);
                            }
                            catch { }
                        });
                    }

                    // Enviar confirmación de reserva por WhatsApp al cliente
                    if (!string.IsNullOrWhiteSpace(cliente.Telefono))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _whatsAppService.EnviarConfirmacionReservaWhatsAppAsync(
                                    cliente.Telefono, cita.NombreCliente, negocio.NegocioNombre,
                                    cita.NombreServicio, cita.NombreEmpleado, cita.FechaHoraInicio,
                                    cita.PrecioServicio);
                            }
                            catch { }
                        });
                    }
                }
            }

            return Ok(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Mueve una cita existente a una nueva fecha/hora.
    /// Se invoca cuando el usuario arrastra un evento en el calendario (drag & drop
    /// de FullCalendar). FullCalendar envía la nueva fecha/hora después del drag.
    ///
    /// El servicio re-valida la disponibilidad del empleado en el nuevo slot
    /// antes de confirmar el cambio, por lo que si el destino ya está ocupado
    /// se devuelve un error y el calendario revierte el movimiento visualmente.
    /// </summary>
    /// <param name="dto">citaId, negocioId y NuevaFechaHoraInicio.</param>
    [HttpPost("MoverCita")]
    public async Task<IActionResult> MoverCita([FromForm] CitaMoveDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _citaService.MoverCitaAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Cambia el estado de una cita. Los estados posibles típicamente son:
    ///   Programada → Confirmada → Completada
    ///                           → Cancelada (requiere motivoCancelacion)
    ///                           → NoShow (el cliente no se presentó)
    ///
    /// El servicio valida que la transición de estado sea permitida
    /// (ej. no se puede "completar" una cita que ya fue cancelada).
    /// El motivo de cancelación solo es obligatorio cuando el nuevo estado es Cancelada.
    /// </summary>
    /// <param name="citaId">ID de la cita a modificar.</param>
    /// <param name="negocioId">ID del negocio (verificación de pertenencia).</param>
    /// <param name="nuevoEstado">Estado destino (string que el servicio valida).</param>
    /// <param name="motivoCancelacion">Requerido solo si nuevoEstado == "Cancelada".</param>
    [HttpPost("CambiarEstadoCita")]
    public async Task<IActionResult> CambiarEstadoCita(Guid citaId, Guid negocioId, string nuevoEstado, string motivoCancelacion)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _citaService.CambiarEstadoCitaAsync(citaId, negocioId, nuevoEstado, motivoCancelacion);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // PAGOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Registra el pago de una cita una vez que fue completada.
    /// Se invoca desde el modal de pago del panel lateral del calendario.
    ///
    /// El DTO PagoCitaDto incluye: monto, método de pago (efectivo/tarjeta/transferencia)
    /// y si es un pago de regalo (voucher). El servicio crea un registro PagoCita
    /// asociado a la cita y marca la cita como pagada.
    ///
    /// Nota: Una cita solo puede tener un registro de pago. Si ya tiene pago,
    /// el servicio devuelve error para evitar cobros duplicados.
    /// </summary>
    /// <param name="dto">Datos del pago: citaId, negocioId, monto, metodoPago, esRegalo.</param>
    [HttpPost("RegistrarPagoCita")]
    public async Task<IActionResult> RegistrarPagoCita([FromForm] PagoCitaDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _citaService.RegistrarPagoAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            // Enviar recibo de servicio completado y guardarlo en BD
            var cita = await _context.Citas
                .FirstOrDefaultAsync(c => c.CitaId == dto.CitaId && c.NegocioId == dto.NegocioId);

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
                        var numRecibo = await _reciboService.ObtenerSiguienteNumeroAsync(dto.NegocioId);
                        var concepto = $"Servicio: {cita.NombreServicio}";
                        var clienteEmail = cliente?.Email ?? "";
                        var clienteNombre = cita.NombreCliente ?? "Cliente";

                        // Generar HTML del recibo y enviar email si hay correo del cliente
                        var (enviado, html) = await _emailService.EnviarReciboCitaCompletadaAsync(
                            clienteEmail, clienteNombre, negocio.NegocioNombre,
                            cita.NombreServicio, cita.NombreEmpleado,
                            pago.MontoServicio, pago.MontoExtra, pago.Propina, pago.Total,
                            negocio.Email, negocio.Telefono, numRecibo);

                        // Siempre almacenar el recibo en BD (aunque el email falle)
                        await _reciboService.CrearReciboAsync(dto.NegocioId, numRecibo, "pago_cita",
                            clienteEmail, clienteNombre, negocio.NegocioNombre, concepto, pago.Total, html);

                        // Enviar recibo de servicio completado por WhatsApp al cliente
                        var clienteTelefono = cliente?.Telefono;
                        if (!string.IsNullOrWhiteSpace(clienteTelefono))
                        {
                            _ = Task.Run(async () =>
                            {
                                try { await _whatsAppService.EnviarReciboCitaCompletadaWhatsAppAsync(clienteTelefono, clienteNombre, negocio.NegocioNombre, cita.NombreServicio, pago.Total, numRecibo); }
                                catch { }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al generar recibo para cita {CitaId}", dto.CitaId);
                    }
                }
            }

            return Ok(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el detalle del pago asociado a una cita.
    /// Se usa al abrir el detalle de una cita ya pagada para mostrar
    /// el comprobante: monto, método, fecha de pago y si fue regalo.
    /// Devuelve null (no error) si la cita aún no tiene pago registrado,
    /// y el frontend interpreta null como "pendiente de pago".
    /// </summary>
    /// <param name="citaId">ID de la cita.</param>
    /// <param name="negocioId">ID del negocio (verificación de pertenencia).</param>
    [HttpGet("GetPagoCita")]
    public async Task<IActionResult> GetPagoCita(Guid citaId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var pago = await _citaService.GetPagoCitaAsync(citaId, negocioId);
            // Devolvemos Ok aunque pago sea null: el frontend distingue entre
            // "pago no encontrado" (null) y "error del servidor" (500).
            return Ok(pago);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CLIENTES ARTESANAL
    //
    // El "modelo artesanal" es el tipo de negocio orientado a citas
    // (peluquerías, spas, estudios de tatuajes, etc.) en contraste
    // con el "modelo gimnasio" que usa membresías mensuales.
    // Los clientes artesanales son más simples: solo nombre, apellido
    // y teléfono — sin membresía ni fecha de vencimiento.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un cliente nuevo para el modelo artesanal.
    /// Se diferencia de los clientes de membresía en que no tiene
    /// plan, precio mensual ni fecha de vencimiento — solo datos básicos
    /// de contacto para poder agendar citas.
    /// </summary>
    /// <param name="dto">Nombre, apellido, teléfono, email y negocioId del nuevo cliente.</param>
    [HttpPost("CrearClienteArtesanal")]
    public async Task<IActionResult> CrearClienteArtesanal([FromForm] ClienteArtesanalCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _clienteService.CrearClienteArtesanalAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualiza los datos de contacto de un cliente artesanal existente.
    /// Reutiliza el mismo DTO de creación (ClienteArtesanalCreateDto) porque
    /// los campos editables son idénticos; el servicio distingue entre crear
    /// y editar por la presencia del campo ClienteId dentro del DTO.
    /// </summary>
    /// <param name="dto">Datos actualizados del cliente, incluyendo su ClienteId.</param>
    [HttpPost("EditarClienteArtesanal")]
    public async Task<IActionResult> EditarClienteArtesanal([FromForm] ClienteArtesanalCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _clienteService.EditarClienteArtesanalAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Devuelve la lista completa de clientes artesanales del negocio.
    /// Se usa para poblar la tabla de clientes en el dashboard artesanal
    /// y para el autocompletado al crear una cita nueva.
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyos clientes se listan.</param>
    /// <returns>Array JSON de clientes ordenados por nombre.</returns>
    [HttpGet("GetClientesArtesanal")]
    public async Task<IActionResult> GetClientesArtesanal(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var clientes = await _clienteService.GetClientesArtesanalAsync(negocioId);
            return Ok(clientes);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }
}
