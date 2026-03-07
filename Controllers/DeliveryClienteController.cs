// ═══════════════════════════════════════════════════════════
// DeliveryClienteController.cs — Public-facing delivery client controller
//
// Endpoints for end-users ordering food delivery.
// No login required — clients are identified by phone number.
//
// Views:
//   /Delivery              → browse nearby restaurants
//   /Delivery/Menu/{id}    → view restaurant menu and build cart
//   /Delivery/Checkout     → enter delivery details and place order
//   /Delivery/Esperando/{id} → waiting for motorizado to accept
//   /Delivery/Tracking/{id}  → real-time order tracking (8 steps)
//
// API:
//   GET  GetRestaurantesCercanos  → restaurants near GPS point
//   GET  GetMenuRestaurante/{id}  → restaurant products for menu display
//   POST CrearPedido              → create new delivery order
//   GET  GetPedidoTracking/{id}   → tracking data for client
//   GET  GetDatosMotorizado/{id}  → motorizado info for client
//   POST CancelarPedido           → client cancels order
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Hubs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[Route("Delivery")]
public class DeliveryClienteController : Controller
{
    private readonly IPedidoService _pedidoService;
    private readonly IHubContext<PedidoHub> _hubContext;
    private readonly ApplicationDbContext _context;

    public DeliveryClienteController(
        IPedidoService pedidoService,
        IHubContext<PedidoHub> hubContext,
        ApplicationDbContext context)
    {
        _pedidoService = pedidoService;
        _hubContext = hubContext;
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // VISTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Pagina principal de delivery — muestra restaurantes cercanos.
    /// </summary>
    [HttpGet("")]
    [AllowAnonymous]
    public IActionResult Index() => View();

    /// <summary>
    /// Menu de un restaurante para que el cliente arme su pedido.
    /// </summary>
    [HttpGet("Menu/{negocioId}")]
    [AllowAnonymous]
    public IActionResult Menu(Guid negocioId)
    {
        ViewBag.NegocioId = negocioId;
        return View();
    }

    /// <summary>
    /// Pagina de checkout — datos de entrega y confirmacion del pedido.
    /// </summary>
    [HttpGet("Checkout")]
    [AllowAnonymous]
    public IActionResult Checkout() => View();

    /// <summary>
    /// Pagina de espera — el pedido fue creado, esperando un motorizado.
    /// </summary>
    [HttpGet("Esperando/{pedidoId}")]
    [AllowAnonymous]
    public IActionResult Esperando(Guid pedidoId)
    {
        ViewBag.PedidoId = pedidoId;
        return View();
    }

    /// <summary>
    /// Pagina de tracking — seguimiento en tiempo real del pedido con 8 pasos.
    /// </summary>
    [HttpGet("Tracking/{pedidoId}")]
    [AllowAnonymous]
    public IActionResult Tracking(Guid pedidoId)
    {
        ViewBag.PedidoId = pedidoId;
        return View();
    }

    // ═══════════════════════════════════════════════════════════
    // API ENDPOINTS (JSON)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Busca restaurantes cercanos a las coordenadas GPS del cliente.
    /// </summary>
    [HttpGet("GetRestaurantesCercanos")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetRestaurantesCercanos(double lat, double lng)
    {
        try
        {
            var result = await _pedidoService.GetRestaurantesCercanosAsync(lat, lng);
            return Ok(result);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al buscar restaurantes" });
        }
    }

    /// <summary>
    /// Obtiene los productos de un restaurante para mostrar en el menu de delivery.
    /// Devuelve solo productos activos con stock > 0, agrupados por categoria.
    /// Endpoint publico — no requiere autenticacion.
    /// </summary>
    [HttpGet("GetMenuRestaurante/{negocioId}")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetMenuRestaurante(Guid negocioId)
    {
        try
        {
            var negocio = await _context.Negocios.AsNoTracking()
                .Where(n => n.NegocioId == negocioId && n.IsActive && !n.NegocioBloqueado)
                .Select(n => new { n.NegocioId, n.NegocioNombre, n.Direccion, n.Telefono, n.LogoUrl })
                .FirstOrDefaultAsync();

            if (negocio == null)
                return NotFound(new { success = false, message = "Restaurante no encontrado" });

            var categorias = await _context.CategoriasProducto.AsNoTracking()
                .Where(c => c.NegocioId == negocioId)
                .OrderBy(c => c.Orden)
                .Select(c => new { c.CategoriaId, c.Nombre, c.Orden })
                .ToListAsync();

            var productos = await _context.Productos.AsNoTracking()
                .Where(p => p.NegocioId == negocioId && p.IsActive && p.Stock > 0)
                .OrderBy(p => p.Nombre)
                .Select(p => new
                {
                    p.ProductoId,
                    p.Nombre,
                    p.PrecioVenta,
                    p.Stock,
                    p.ImagenUrl,
                    p.CategoriaProductoId,
                    p.Receta
                })
                .ToListAsync();

            return Ok(new
            {
                restaurante = negocio,
                categorias,
                productos
            });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al obtener el menu" });
        }
    }

    /// <summary>
    /// Crea un nuevo pedido de delivery.
    /// Notifica a todos los motorizados disponibles via SignalR.
    /// </summary>
    [HttpPost("CrearPedido")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("registro")]
    public async Task<IActionResult> CrearPedido([FromBody] CrearPedidoRequest req)
    {
        try
        {
            if (req == null)
                return BadRequest(new { success = false, message = "Datos del pedido invalidos" });

            var (success, message, pedidoId) = await _pedidoService.CrearPedidoAsync(
                req.NegocioId, req.NombreCliente, req.TelefonoCliente,
                req.DireccionEntrega, req.LatitudCliente, req.LongitudCliente,
                req.CostoEnvio, req.Notas, req.Items);

            if (!success)
                return BadRequest(new { success, message });

            // Notificar a todos los motorizados disponibles que hay un nuevo pedido
            await _hubContext.Clients.Group("motorizados_disponibles")
                .SendAsync("NuevoPedido", new { pedidoId });

            return Ok(new { success = true, message, pedidoId });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al crear el pedido" });
        }
    }

    /// <summary>
    /// Obtiene datos de tracking del pedido para la vista del cliente.
    /// </summary>
    [HttpGet("GetPedidoTracking/{pedidoId}")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetPedidoTracking(Guid pedidoId)
    {
        try
        {
            var data = await _pedidoService.GetPedidoTrackingAsync(pedidoId);
            if (data == null)
                return NotFound(new { success = false, message = "Pedido no encontrado" });

            return Ok(data);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al obtener tracking" });
        }
    }

    /// <summary>
    /// Obtiene datos del motorizado asignado a un pedido.
    /// </summary>
    [HttpGet("GetDatosMotorizado/{pedidoId}")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetDatosMotorizado(Guid pedidoId)
    {
        try
        {
            var data = await _pedidoService.GetDatosMotorizadoAsync(pedidoId);
            if (data == null)
                return NotFound(new { success = false, message = "Motorizado no asignado" });

            return Ok(data);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al obtener datos del motorizado" });
        }
    }

    /// <summary>
    /// El cliente cancela su pedido.
    /// Notifica a todas las partes involucradas via SignalR.
    /// </summary>
    [HttpPost("CancelarPedido")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> CancelarPedido([FromBody] CancelarPedidoRequest req)
    {
        try
        {
            if (req == null)
                return BadRequest(new { success = false, message = "Datos invalidos" });

            var (success, message) = await _pedidoService.CancelarPedidoAsync(
                req.PedidoId, "cliente", req.Razon);

            if (!success)
                return BadRequest(new { success, message });

            // Notificar a todas las partes que el pedido fue cancelado
            await _hubContext.Clients.Group($"pedido_{req.PedidoId}")
                .SendAsync("PedidoCancelado", new { canceladoPor = "cliente", razon = req.Razon });

            return Ok(new { success = true, message });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error al cancelar el pedido" });
        }
    }
}

// ═══════════════════════════════════════════════════════════
// Request DTOs
// ═══════════════════════════════════════════════════════════

public class CrearPedidoRequest
{
    public Guid NegocioId { get; set; }
    public string NombreCliente { get; set; }
    public string TelefonoCliente { get; set; }
    public string DireccionEntrega { get; set; }
    public double LatitudCliente { get; set; }
    public double LongitudCliente { get; set; }
    public decimal CostoEnvio { get; set; }
    public string Notas { get; set; }
    public List<DetallePedidoDto> Items { get; set; }
}

public class CancelarPedidoRequest
{
    public Guid PedidoId { get; set; }
    public string Razon { get; set; }
}
