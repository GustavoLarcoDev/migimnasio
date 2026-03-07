// ═══════════════════════════════════════════════════════════
// PedidoHub.cs — SignalR hub for real-time delivery tracking
//
// Manages WebSocket connections for delivery order updates.
// All parties (client, restaurant, motorizado) join groups
// to receive push notifications about order state changes.
//
// Groups:
//   pedido_{pedidoId}         — all parties watching a specific order
//   restaurante_{negocioId}   — restaurant listening for new orders
//   motorizado_{motorizadoId} — specific motorizado receiving updates
//   motorizados_disponibles   — all available motorizados
//   cliente_{telefonoCliente} — client tracking (identified by phone)
// ═══════════════════════════════════════════════════════════

using System.Security.Claims;
using Gimnasio.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Hubs;

/// <summary>
/// Hub de SignalR para el sistema de delivery en tiempo real.
/// Los clientes, restaurantes y motorizados se conectan a este hub
/// para recibir actualizaciones instantáneas sobre el estado de los pedidos,
/// ubicación GPS del motorizado, y notificaciones push.
///
/// SEGURIDAD: Las conexiones autenticadas (restaurante, motorizado) requieren
/// que el caller sea dueño del recurso al que quiere unirse.
/// Las conexiones de clientes (pedido, cliente) validan que el pedido exista.
/// </summary>
public class PedidoHub : Hub
{
    private readonly IServiceProvider _sp;

    public PedidoHub(IServiceProvider sp) => _sp = sp;

    /// <summary>
    /// Unirse al grupo de un pedido específico.
    /// Valida que el pedidoId sea un GUID válido y que el pedido exista.
    /// </summary>
    public async Task JoinPedidoGroup(string pedidoId)
    {
        if (!Guid.TryParse(pedidoId, out var pid)) return;
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var exists = await db.Pedidos.AnyAsync(p => p.PedidoId == pid);
        if (!exists) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"pedido_{pedidoId}");
    }

    public async Task LeavePedidoGroup(string pedidoId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"pedido_{pedidoId}");

    /// <summary>
    /// El restaurante se une a su grupo. Requiere autenticación y que el
    /// NegocioId del claim coincida con el negocioId solicitado.
    /// </summary>
    public async Task JoinRestauranteGroup(string negocioId)
    {
        var userNegocioId = Context.User?.FindFirst("NegocioId")?.Value;
        if (string.IsNullOrEmpty(userNegocioId) || userNegocioId != negocioId) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"restaurante_{negocioId}");
    }

    /// <summary>
    /// Motorizado se une a su grupo. Requiere autenticación y que el
    /// MotorizadoId del claim coincida.
    /// </summary>
    public async Task JoinMotorizadoGroup(string motorizadoId)
    {
        var userMotoId = Context.User?.FindFirst("MotorizadoId")?.Value;
        if (string.IsNullOrEmpty(userMotoId) || userMotoId != motorizadoId) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"motorizado_{motorizadoId}");
    }

    /// <summary>
    /// Motorizado disponible se une al pool global. Requiere claim MotorizadoId.
    /// </summary>
    public async Task JoinMotorizadosDisponibles()
    {
        var userMotoId = Context.User?.FindFirst("MotorizadoId")?.Value;
        if (string.IsNullOrEmpty(userMotoId)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, "motorizados_disponibles");
    }

    public async Task LeaveMotorizadosDisponibles()
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "motorizados_disponibles");

    /// <summary>
    /// Cliente se une a su grupo. Valida que el key no esté vacío.
    /// </summary>
    public async Task JoinClienteGroup(string clienteKey)
    {
        if (string.IsNullOrWhiteSpace(clienteKey) || clienteKey.Length > 20) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"cliente_{clienteKey}");
    }
}
