// ═══════════════════════════════════════════════════════════
// MotorizadoController.cs — Controller for delivery drivers
//
// Handles motorizado authentication, order management, and GPS tracking.
// Motorizados authenticate with their own credentials (email + BCrypt password)
// using the same cookie scheme as negocios but with Role="Motorizado".
//
// Views:
//   /Motorizado       → dashboard with available/active orders
//   /Motorizado/Login → login page
//
// Auth:
//   POST Login  → authenticate motorizado, create cookie
//   POST Logout → sign out
//
// API:
//   GET  GetPedidosDisponibles  → orders waiting for a driver
//   POST AceptarPedido          → take an available order
//   POST ConfirmarRecogida      → confirm food pickup
//   POST MarcarEntregado        → mark order as delivered
//   POST ActualizarUbicacion    → update GPS position
//   GET  GetDatosCliente/{id}   → client address/phone for navigation
//   GET  GetPedidoActivo        → motorizado's current active order
// ═══════════════════════════════════════════════════════════

using System.Security.Claims;
using Gimnasio.Data;
using Gimnasio.Hubs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[Route("Motorizado")]
[Authorize]
public class MotorizadoController : Controller
{
    private readonly IPedidoService _pedidoService;
    private readonly IHubContext<PedidoHub> _hubContext;
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly IDeliveryAdminService _deliveryAdminService;

    public MotorizadoController(
        IPedidoService pedidoService,
        IHubContext<PedidoHub> hubContext,
        IAuthService authService,
        ApplicationDbContext context,
        IDeliveryAdminService deliveryAdminService)
    {
        _pedidoService = pedidoService;
        _hubContext = hubContext;
        _authService = authService;
        _context = context;
        _deliveryAdminService = deliveryAdminService;
    }

    /// <summary>
    /// Extrae el MotorizadoId del claim del usuario autenticado.
    /// Retorna null si el claim no existe o no es un GUID valido.
    /// </summary>
    private Guid? GetMotorizadoId()
        => Guid.TryParse(User.FindFirst("MotorizadoId")?.Value, out var id) ? id : null;

    // ═══════════════════════════════════════════════════════════
    // VISTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Dashboard del motorizado — pedidos disponibles y pedido activo.
    /// </summary>
    [HttpGet("")]
    public IActionResult Index() => View();

    /// <summary>
    /// Pagina de login para motorizados.
    /// </summary>
    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login() => View();

    // ═══════════════════════════════════════════════════════════
    // AUTENTICACION
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Autentica un motorizado con email y password (BCrypt).
    /// Crea una cookie con claims MotorizadoId, Name y Role="Motorizado".
    /// </summary>
    [HttpPost("Login")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginMotorizadoRequest req)
    {
        try
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { success = false, message = "Email y contraseña son obligatorios" });

            // Buscar motorizado por email
            var motorizado = await _context.Motorizados
                .FirstOrDefaultAsync(m => m.Email == req.Email.Trim().ToLower());

            if (motorizado == null)
                return BadRequest(new { success = false, message = "Credenciales incorrectas" });

            if (!motorizado.IsActive)
                return BadRequest(new { success = false, message = "Tu cuenta está desactivada. Contacta al administrador." });

            // Verificar contraseña con BCrypt
            if (!_authService.VerifyPassword(req.Password, motorizado.Password))
                return BadRequest(new { success = false, message = "Credenciales incorrectas" });

            // Si el motorizado está bloqueado, permitir login pero informar el bloqueo
            var bloqueado = motorizado.Bloqueado;

            // Crear claims para la cookie de autenticacion
            var claims = new List<Claim>
            {
                new Claim("MotorizadoId", motorizado.MotorizadoId.ToString()),
                new Claim(ClaimTypes.Name, $"{motorizado.Nombre} {motorizado.Apellido}"),
                new Claim(ClaimTypes.Role, "Motorizado"),
                new Claim(ClaimTypes.Email, motorizado.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
                });

            return Ok(new
            {
                success = true,
                message = "Login exitoso",
                motorizadoId = motorizado.MotorizadoId,
                nombre = $"{motorizado.Nombre} {motorizado.Apellido}",
                blocked = bloqueado,
                comisionesPendientes = motorizado.ComisionesAcumuladas,
                tipoPlan = motorizado.TipoPlan
            });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Cierra la sesion del motorizado.
    /// </summary>
    [HttpPost("Logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // ═══════════════════════════════════════════════════════════
    // API ENDPOINTS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene pedidos con estado "nuevo" disponibles para tomar.
    /// </summary>
    [HttpGet("GetPedidosDisponibles")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetPedidosDisponibles()
    {
        try
        {
            var data = await _pedidoService.GetPedidosDisponiblesAsync();
            return Ok(data);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al obtener pedidos" });
        }
    }

    /// <summary>
    /// El motorizado acepta un pedido disponible.
    /// Notifica al cliente, restaurante y otros motorizados via SignalR.
    /// </summary>
    [HttpPost("AceptarPedido")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AceptarPedido([FromBody] AceptarPedidoRequest req)
    {
        try
        {
            var motorizadoId = GetMotorizadoId();
            if (!motorizadoId.HasValue)
                return Unauthorized(new { success = false, message = "No autenticado como motorizado" });

            if (req == null)
                return BadRequest(new { success = false, message = "Datos invalidos" });

            var (success, message) = await _pedidoService.TomarPedidoAsync(req.PedidoId, motorizadoId.Value);

            if (!success)
                return BadRequest(new { success, message });

            var motorizadoNombre = User.Identity?.Name ?? "Motorizado";

            // Notificar al grupo del pedido que fue tomado
            await _hubContext.Clients.Group($"pedido_{req.PedidoId}")
                .SendAsync("PedidoTomado", new { motorizadoNombre });

            // Obtener datos del pedido para notificar al restaurante
            var pedidoData = await _pedidoService.GetPedidoAsync(req.PedidoId);

            // Notificar al restaurante que tiene un nuevo pedido por confirmar
            // Necesitamos extraer NegocioId del pedido — lo obtenemos del servicio
            var pedido = await _context.Pedidos.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PedidoId == req.PedidoId);

            if (pedido != null)
            {
                await _hubContext.Clients.Group($"restaurante_{pedido.NegocioId}")
                    .SendAsync("OrdenRecibida", new { pedidoId = req.PedidoId });
            }

            // Notificar a otros motorizados que este pedido ya fue tomado
            await _hubContext.Clients.Group("motorizados_disponibles")
                .SendAsync("PedidoYaTomado", new { pedidoId = req.PedidoId });

            return Ok(new { success = true, message });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al aceptar el pedido" });
        }
    }

    /// <summary>
    /// El motorizado confirma que recogio la comida del restaurante.
    /// Si ambas partes confirmaron, el pedido pasa a "en_camino".
    /// </summary>
    [HttpPost("ConfirmarRecogida")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ConfirmarRecogida([FromBody] PedidoIdRequest req)
    {
        try
        {
            var motorizadoId = GetMotorizadoId();
            if (!motorizadoId.HasValue)
                return Unauthorized(new { success = false, message = "No autenticado como motorizado" });

            if (req == null)
                return BadRequest(new { success = false, message = "Datos invalidos" });

            var (success, message) = await _pedidoService.ConfirmarRecepcionMotorizadoAsync(
                req.PedidoId, motorizadoId.Value);

            if (!success)
                return BadRequest(new { success, message });

            // Check if both parties confirmed (message contains "en camino" or we re-check)
            var pedido = await _context.Pedidos.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PedidoId == req.PedidoId);

            if (pedido?.Estado == "en_camino")
            {
                await _hubContext.Clients.Group($"pedido_{req.PedidoId}")
                    .SendAsync("ComidaRecogida", new { pedidoId = req.PedidoId });
            }

            return Ok(new { success = true, message });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al confirmar recogida" });
        }
    }

    /// <summary>
    /// El motorizado marca el pedido como entregado al cliente.
    /// </summary>
    [HttpPost("MarcarEntregado")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MarcarEntregado([FromBody] PedidoIdRequest req)
    {
        try
        {
            var motorizadoId = GetMotorizadoId();
            if (!motorizadoId.HasValue)
                return Unauthorized(new { success = false, message = "No autenticado como motorizado" });

            if (req == null)
                return BadRequest(new { success = false, message = "Datos invalidos" });

            var (success, message) = await _pedidoService.MarcarEntregadoAsync(
                req.PedidoId, motorizadoId.Value);

            if (!success)
                return BadRequest(new { success, message });

            // Obtener datos del pedido para enviar resumen final
            var pedido = await _context.Pedidos.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PedidoId == req.PedidoId);

            // Notificar a todas las partes que el pedido fue entregado
            await _hubContext.Clients.Group($"pedido_{req.PedidoId}")
                .SendAsync("PedidoEntregado", new
                {
                    costoEnvio = pedido?.CostoEnvio ?? 0,
                    costoComida = pedido?.CostoComida ?? 0,
                    total = pedido?.Total ?? 0
                });

            return Ok(new { success = true, message });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al marcar como entregado" });
        }
    }

    /// <summary>
    /// Actualiza la posicion GPS del motorizado y verifica proximidad al cliente.
    /// Se llama periodicamente desde el frontend del motorizado.
    /// </summary>
    [HttpPost("ActualizarUbicacion")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ActualizarUbicacion([FromBody] UbicacionRequest req)
    {
        try
        {
            var motorizadoId = GetMotorizadoId();
            if (!motorizadoId.HasValue)
                return Unauthorized(new { success = false, message = "No autenticado como motorizado" });

            if (req == null)
                return BadRequest(new { success = false, message = "Datos invalidos" });

            // Actualizar GPS en la base de datos
            var (success, message) = await _pedidoService.ActualizarUbicacionMotorizadoAsync(
                motorizadoId.Value, req.Latitud, req.Longitud);

            if (!success)
                return BadRequest(new { success, message });

            // Buscar si el motorizado tiene un pedido activo en_camino
            var pedidoActivo = await _context.Pedidos.AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.MotorizadoId == motorizadoId.Value
                    && p.Estado == "en_camino");

            if (pedidoActivo != null)
            {
                // Notificar ubicacion al grupo del pedido
                await _hubContext.Clients.Group($"pedido_{pedidoActivo.PedidoId}")
                    .SendAsync("UbicacionMotorizado", new
                    {
                        lat = req.Latitud,
                        lng = req.Longitud
                    });

                // Verificar proximidad al cliente
                var (proxSuccess, proxMessage, estaCerca) = await _pedidoService.VerificarProximidadAsync(
                    pedidoActivo.PedidoId, motorizadoId.Value, req.Latitud, req.Longitud);

                if (proxSuccess && estaCerca)
                {
                    await _hubContext.Clients.Group($"pedido_{pedidoActivo.PedidoId}")
                        .SendAsync("PedidoCerca", new { pedidoId = pedidoActivo.PedidoId });
                }
            }

            return Ok(new { success = true, message });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al actualizar ubicacion" });
        }
    }

    /// <summary>
    /// Obtiene datos del cliente (direccion, telefono) para el motorizado.
    /// Solo accesible por el motorizado asignado al pedido.
    /// </summary>
    [HttpGet("GetDatosCliente/{pedidoId}")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetDatosCliente(Guid pedidoId)
    {
        try
        {
            var motorizadoId = GetMotorizadoId();
            if (!motorizadoId.HasValue)
                return Unauthorized(new { success = false, message = "No autenticado como motorizado" });

            var data = await _pedidoService.GetDatosClienteAsync(pedidoId, motorizadoId.Value);
            if (data == null)
                return NotFound(new { success = false, message = "Pedido no encontrado o no estas asignado" });

            return Ok(data);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al obtener datos del cliente" });
        }
    }

    /// <summary>
    /// Obtiene el pedido activo actual del motorizado (si tiene uno).
    /// </summary>
    [HttpGet("GetPedidoActivo")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetPedidoActivo()
    {
        try
        {
            var motorizadoId = GetMotorizadoId();
            if (!motorizadoId.HasValue)
                return Unauthorized(new { success = false, message = "No autenticado como motorizado" });

            var pedido = await _context.Pedidos.AsNoTracking()
                .Include(p => p.Negocio)
                .Include(p => p.Detalles)
                .Where(p => p.MotorizadoId == motorizadoId.Value
                            && p.Estado != "entregado"
                            && p.Estado != "cancelado")
                .Select(p => new
                {
                    p.PedidoId,
                    p.NegocioId,
                    RestauranteNombre = p.Negocio.NegocioNombre,
                    RestauranteDireccion = p.Negocio.Direccion,
                    RestauranteTelefono = p.Negocio.Telefono,
                    p.NombreCliente,
                    p.TelefonoCliente,
                    p.DireccionEntrega,
                    p.LatitudCliente,
                    p.LongitudCliente,
                    p.LatitudRestaurante,
                    p.LongitudRestaurante,
                    p.CostoComida,
                    p.CostoEnvio,
                    p.Total,
                    p.Estado,
                    p.Notas,
                    p.RestauranteConfirmoEntrega,
                    p.MotorizadoConfirmoRecepcion,
                    p.FechaCreacion,
                    p.FechaTomado,
                    Items = p.Detalles.Select(d => new
                    {
                        d.DetallePedidoId,
                        d.NombreProducto,
                        d.Cantidad,
                        d.PrecioUnitario,
                        d.Subtotal,
                        d.Confirmado,
                        d.Rechazado
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (pedido == null)
                return Ok(new { hayPedidoActivo = false });

            return Ok(new { hayPedidoActivo = true, pedido });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al obtener pedido activo" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // SUSCRIPCION & PAGOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Estado de suscripción del motorizado: plan, deuda, bloqueo, fechas.
    /// </summary>
    [HttpGet("GetEstadoSuscripcion")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetEstadoSuscripcion()
    {
        try
        {
            var motorizadoId = GetMotorizadoId();
            if (!motorizadoId.HasValue)
                return Unauthorized(new { success = false, message = "No autenticado" });

            var data = await _deliveryAdminService.GetEstadoSuscripcionMotorizadoAsync(motorizadoId.Value);
            if (data == null)
                return NotFound(new { success = false, message = "Motorizado no encontrado" });

            return Ok(data);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al obtener estado de suscripción" });
        }
    }

    /// <summary>
    /// Métodos de pago del admin (para que el motorizado sepa dónde pagar).
    /// Admin methods have NegocioId = null in MetodoPago table.
    /// </summary>
    [HttpGet("GetMetodosPagoAdmin")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetMetodosPagoAdmin()
    {
        try
        {
            var metodos = await _context.MetodosPago
                .Where(m => m.NegocioId == null && m.IsActive)
                .OrderByDescending(m => m.EsPredeterminado)
                .ThenBy(m => m.Orden)
                .Select(m => new
                {
                    m.MetodoPagoId,
                    m.Nombre,
                    m.NumeroCuenta,
                    m.NombreTitular,
                    m.Cedula,
                    m.Instrucciones,
                    m.ImagenQR,
                    m.EsPredeterminado
                })
                .ToListAsync();
            return Ok(metodos);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al obtener métodos de pago" });
        }
    }

    /// <summary>
    /// El motorizado envía confirmación de pago (crea PagoDelivery pendiente).
    /// </summary>
    [HttpPost("EnviarConfirmacionPago")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> EnviarConfirmacionPago([FromBody] ConfirmacionPagoRequest req)
    {
        try
        {
            var motorizadoId = GetMotorizadoId();
            if (!motorizadoId.HasValue)
                return Unauthorized(new { success = false, message = "No autenticado" });

            if (req == null || string.IsNullOrWhiteSpace(req.NumConfirmacion))
                return BadRequest(new { success = false, message = "Número de confirmación es obligatorio" });

            var (success, message) = await _deliveryAdminService.EnviarConfirmacionPagoAsync(
                "motorizado", motorizadoId.Value, req.TipoPago ?? "comision", req.Monto, req.NumConfirmacion);

            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al enviar confirmación de pago" });
        }
    }

    /// <summary>
    /// Historial de pagos del motorizado.
    /// </summary>
    [HttpGet("GetHistorialPagos")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetHistorialPagos()
    {
        try
        {
            var motorizadoId = GetMotorizadoId();
            if (!motorizadoId.HasValue)
                return Unauthorized(new { success = false, message = "No autenticado" });

            var data = await _deliveryAdminService.GetHistorialPagosMotorizadoAsync(motorizadoId.Value);
            return Ok(data);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al obtener historial de pagos" });
        }
    }
}

// ═══════════════════════════════════════════════════════════
// Request DTOs
// ═══════════════════════════════════════════════════════════

public class LoginMotorizadoRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class AceptarPedidoRequest
{
    public Guid PedidoId { get; set; }
}

public class PedidoIdRequest
{
    public Guid PedidoId { get; set; }
}

public class UbicacionRequest
{
    public double Latitud { get; set; }
    public double Longitud { get; set; }
}

public class ConfirmacionPagoRequest
{
    public string? TipoPago { get; set; }
    public decimal Monto { get; set; }
    public string NumConfirmacion { get; set; }
}
