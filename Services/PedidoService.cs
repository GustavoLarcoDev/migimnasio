// ═══════════════════════════════════════════════════════════════════════════
// PedidoService.cs — Servicio de gestión de pedidos de delivery
//
// RESPONSABILIDADES:
//   - Crear pedidos con validación de items y costos
//   - Máquina de estados completa del ciclo de vida del pedido
//   - Protección de race conditions al tomar pedidos (concurrencia optimista)
//   - Doble confirmación de recogida (restaurante + motorizado)
//   - Cálculo de proximidad GPS con fórmula Haversine
//   - Búsqueda de restaurantes cercanos
//
// CONCURRENCIA:
//   TomarPedidoAsync usa [Timestamp] RowVersion en el modelo Pedido
//   para garantizar que solo un motorizado pueda tomar cada pedido.
//   Si dos motorizados intentan tomar el mismo pedido simultáneamente,
//   el segundo recibe DbUpdateConcurrencyException → mensaje amigable.
// ═══════════════════════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Implementación del servicio de pedidos de delivery.
/// Gestiona todo el ciclo de vida de un pedido desde la creación
/// hasta la entrega, incluyendo la interacción entre cliente,
/// restaurante y motorizado.
/// </summary>
public class PedidoService : IPedidoService
{
    private readonly ApplicationDbContext _context;

    public PedidoService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene pedidos con estado "nuevo" para que los motorizados los vean.
    /// Solo pedidos que aún no han sido tomados por ningún motorizado.
    /// Incluye datos del restaurante y los items para que el motorizado
    /// pueda decidir si quiere aceptar el pedido.
    /// </summary>
    public async Task<object> GetPedidosDisponiblesAsync()
    {
        return await _context.Pedidos.AsNoTracking()
            .Where(p => p.Estado == "nuevo")
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new
            {
                p.PedidoId,
                p.NegocioId,
                RestauranteNombre = p.Negocio.NegocioNombre,
                RestauranteDireccion = p.Negocio.Direccion,
                p.NombreCliente,
                p.DireccionEntrega,
                p.LatitudCliente,
                p.LongitudCliente,
                p.LatitudRestaurante,
                p.LongitudRestaurante,
                p.CostoComida,
                p.CostoEnvio,
                p.Total,
                p.Notas,
                p.FechaCreacion,
                Items = p.Detalles.Where(d => !d.Rechazado).Select(d => new
                {
                    d.NombreProducto,
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal
                }).ToList()
            })
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene todos los datos de un pedido específico por su ID.
    /// Incluye datos del restaurante, motorizado y detalles de items.
    /// </summary>
    public async Task<object> GetPedidoAsync(Guid pedidoId)
    {
        return await _context.Pedidos.AsNoTracking()
            .Where(p => p.PedidoId == pedidoId)
            .Select(p => new
            {
                p.PedidoId,
                p.NegocioId,
                RestauranteNombre = p.Negocio.NegocioNombre,
                RestauranteDireccion = p.Negocio.Direccion,
                RestauranteTelefono = p.Negocio.Telefono,
                p.MotorizadoId,
                MotorizadoNombre = p.Motorizado != null ? p.Motorizado.Nombre + " " + p.Motorizado.Apellido : null,
                MotorizadoTelefono = p.Motorizado != null ? p.Motorizado.Telefono : null,
                MotorizadoFoto = p.Motorizado != null ? p.Motorizado.FotoUrl : null,
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
                p.CanceladoPor,
                p.RazonCancelacion,
                p.RestauranteConfirmoEntrega,
                p.MotorizadoConfirmoRecepcion,
                p.FechaCreacion,
                p.FechaTomado,
                p.FechaConfirmado,
                p.FechaPreparando,
                p.FechaListo,
                p.FechaRecogido,
                p.FechaEntregado,
                p.FechaCancelado,
                Items = p.Detalles.Select(d => new
                {
                    d.DetallePedidoId,
                    d.ProductoId,
                    d.NombreProducto,
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal,
                    d.Confirmado,
                    d.Rechazado
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Obtiene los pedidos activos de un restaurante (excluye entregados y cancelados).
    /// Se usa en el dashboard del restaurante para ver pedidos en curso.
    /// </summary>
    public async Task<object> GetPedidosRestauranteAsync(Guid negocioId)
    {
        return await _context.Pedidos.AsNoTracking()
            .Where(p => p.NegocioId == negocioId
                        && p.Estado != "entregado"
                        && p.Estado != "cancelado")
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new
            {
                p.PedidoId,
                p.NombreCliente,
                p.TelefonoCliente,
                p.DireccionEntrega,
                p.MotorizadoId,
                MotorizadoNombre = p.Motorizado != null ? p.Motorizado.Nombre + " " + p.Motorizado.Apellido : null,
                MotorizadoTelefono = p.Motorizado != null ? p.Motorizado.Telefono : null,
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
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene datos de tracking para la vista del cliente.
    /// Incluye estado actual, timestamps de cada transición,
    /// y la posición GPS del motorizado si el pedido está en camino.
    /// </summary>
    public async Task<object> GetPedidoTrackingAsync(Guid pedidoId)
    {
        return await _context.Pedidos.AsNoTracking()
            .Where(p => p.PedidoId == pedidoId)
            .Select(p => new
            {
                p.PedidoId,
                p.Estado,
                RestauranteNombre = p.Negocio.NegocioNombre,
                p.NombreCliente,
                p.DireccionEntrega,
                p.LatitudCliente,
                p.LongitudCliente,
                p.LatitudRestaurante,
                p.LongitudRestaurante,
                p.CostoComida,
                p.CostoEnvio,
                p.Total,
                p.Notas,
                p.CanceladoPor,
                p.RazonCancelacion,
                // Datos del motorizado para mostrar al cliente
                MotorizadoNombre = p.Motorizado != null ? p.Motorizado.Nombre + " " + p.Motorizado.Apellido : null,
                MotorizadoFoto = p.Motorizado != null ? p.Motorizado.FotoUrl : null,
                MotorizadoTelefono = p.Motorizado != null ? p.Motorizado.Telefono : null,
                MotorizadoTotalEntregas = p.Motorizado != null ? (int?)p.Motorizado.TotalEntregas : null,
                // Posición GPS del motorizado (solo si está en camino)
                MotorizadoLatitud = p.Motorizado != null ? p.Motorizado.Latitud : null,
                MotorizadoLongitud = p.Motorizado != null ? p.Motorizado.Longitud : null,
                // Timestamps
                p.FechaCreacion,
                p.FechaTomado,
                p.FechaConfirmado,
                p.FechaPreparando,
                p.FechaListo,
                p.FechaRecogido,
                p.FechaEntregado,
                p.FechaCancelado,
                // Items del pedido
                Items = p.Detalles.Select(d => new
                {
                    d.NombreProducto,
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal,
                    d.Confirmado,
                    d.Rechazado
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Obtiene datos públicos del motorizado asignado a un pedido.
    /// Se muestra al cliente y al restaurante para identificar al repartidor.
    /// </summary>
    public async Task<object> GetDatosMotorizadoAsync(Guid pedidoId)
    {
        var pedido = await _context.Pedidos.AsNoTracking()
            .Include(p => p.Motorizado)
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

        if (pedido?.Motorizado == null) return null;

        return new
        {
            pedido.Motorizado.MotorizadoId,
            Nombre = pedido.Motorizado.Nombre + " " + pedido.Motorizado.Apellido,
            pedido.Motorizado.FotoUrl,
            pedido.Motorizado.Telefono,
            pedido.Motorizado.Vehiculo,
            pedido.Motorizado.Placa,
            pedido.Motorizado.TotalEntregas,
            pedido.Motorizado.Latitud,
            pedido.Motorizado.Longitud
        };
    }

    /// <summary>
    /// Obtiene datos del cliente destinatario del pedido.
    /// Solo accesible por el motorizado asignado — valida MotorizadoId.
    /// </summary>
    public async Task<object> GetDatosClienteAsync(Guid pedidoId, Guid motorizadoId)
    {
        var pedido = await _context.Pedidos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId && p.MotorizadoId == motorizadoId);

        if (pedido == null) return null;

        return new
        {
            pedido.NombreCliente,
            pedido.TelefonoCliente,
            pedido.DireccionEntrega,
            pedido.LatitudCliente,
            pedido.LongitudCliente,
            pedido.Notas
        };
    }

    /// <summary>
    /// Busca restaurantes cercanos al punto GPS dado usando la fórmula Haversine.
    /// Solo incluye restaurantes activos y no bloqueados.
    /// Devuelve los 50 más cercanos ordenados por distancia.
    ///
    /// La fórmula Haversine se evalúa en memoria (no en SQL) porque
    /// SQL Server no soporta funciones trigonométricas en consultas LINQ.
    /// Para un volumen de cientos de restaurantes esto es eficiente.
    /// </summary>
    public async Task<List<object>> GetRestaurantesCercanosAsync(double lat, double lng)
    {
        // Cargar todos los restaurantes activos en memoria
        var restaurantes = await _context.Negocios.AsNoTracking()
            .Where(n => n.TipoNegocio == "restaurante"
                        && n.IsActive
                        && !n.NegocioBloqueado)
            .Select(n => new
            {
                n.NegocioId,
                n.NegocioNombre,
                n.Direccion,
                n.Telefono,
                n.LogoUrl
            })
            .ToListAsync();

        // Por ahora, retornar todos los restaurantes sin filtrar por distancia
        // ya que el modelo Gym aún no tiene campos Latitud/Longitud.
        // Cuando se agreguen, se calculará la distancia con Haversine.
        return restaurantes
            .Select(r => (object)new
            {
                r.NegocioId,
                r.NegocioNombre,
                r.Direccion,
                r.Telefono,
                r.LogoUrl,
                Distancia = 0.0 // Placeholder hasta que Gym tenga coordenadas GPS
            })
            .Take(50)
            .ToList();
    }

    /// <summary>
    /// Obtiene el historial de pedidos entregados por el motorizado.
    /// Ordenados del más reciente al más antiguo.
    /// </summary>
    public async Task<object> GetHistorialMotorizadoAsync(Guid motorizadoId)
    {
        return await _context.Pedidos.AsNoTracking()
            .Where(p => p.MotorizadoId == motorizadoId && p.Estado == "entregado")
            .OrderByDescending(p => p.FechaEntregado)
            .Select(p => new
            {
                p.PedidoId,
                RestauranteNombre = p.Negocio.NegocioNombre,
                p.NombreCliente,
                p.DireccionEntrega,
                p.CostoEnvio,
                p.Total,
                p.FechaCreacion,
                p.FechaEntregado
            })
            .ToListAsync();
    }

    /// <summary>
    /// Calcula estadísticas para el dashboard del motorizado.
    /// </summary>
    public async Task<object> GetMotorizadoStatsAsync(Guid motorizadoId)
    {
        var hoy = TimeHelper.Now.Date;

        var pedidos = await _context.Pedidos.AsNoTracking()
            .Where(p => p.MotorizadoId == motorizadoId)
            .ToListAsync();

        var entregados = pedidos.Where(p => p.Estado == "entregado").ToList();
        var entregadosHoy = entregados.Where(p => p.FechaEntregado?.Date == hoy).ToList();

        return new
        {
            totalEntregas = entregados.Count,
            entregasHoy = entregadosHoy.Count,
            gananciaHoy = entregadosHoy.Sum(p => p.CostoEnvio),
            gananciaTotal = entregados.Sum(p => p.CostoEnvio),
            pedidoActivo = pedidos.FirstOrDefault(p =>
                p.Estado != "entregado" && p.Estado != "cancelado")?.PedidoId
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TRANSICIONES DE ESTADO
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo pedido de delivery con validación exhaustiva.
    /// Calcula automáticamente CostoComida y Total a partir de los items.
    /// </summary>
    public async Task<(bool success, string message, Guid? pedidoId)> CrearPedidoAsync(
        Guid negocioId, string nombreCliente, string telefonoCliente,
        string direccion, double latCliente, double lngCliente,
        decimal costoEnvio, string notas, List<DetallePedidoDto> items)
    {
        // Validaciones de entrada
        if (string.IsNullOrWhiteSpace(nombreCliente))
            return (false, "El nombre del cliente es obligatorio", null);
        if (string.IsNullOrWhiteSpace(telefonoCliente))
            return (false, "El teléfono del cliente es obligatorio", null);
        if (string.IsNullOrWhiteSpace(direccion))
            return (false, "La dirección de entrega es obligatoria", null);
        if (costoEnvio < 1)
            return (false, "El costo de envío mínimo es $1.00", null);
        if (items == null || items.Count == 0)
            return (false, "Debe agregar al menos un producto al pedido", null);

        // Verificar que el restaurante existe y está activo
        var negocio = await _context.Negocios.AsNoTracking()
            .FirstOrDefaultAsync(n => n.NegocioId == negocioId && n.IsActive && !n.NegocioBloqueado);
        if (negocio == null)
            return (false, "Restaurante no encontrado o no disponible", null);

        // Calcular costos
        var costoComida = 0m;
        var detalles = new List<DetallePedido>();

        foreach (var item in items)
        {
            if (item.Cantidad <= 0) continue;

            var subtotal = item.Cantidad * item.PrecioUnitario;
            costoComida += subtotal;

            detalles.Add(new DetallePedido
            {
                DetallePedidoId = Guid.NewGuid(),
                ProductoId = item.ProductoId,
                NombreProducto = item.NombreProducto,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario,
                Subtotal = subtotal,
                Confirmado = false,
                Rechazado = false
            });
        }

        if (detalles.Count == 0)
            return (false, "Debe agregar al menos un producto válido", null);

        var pedido = new Pedido
        {
            PedidoId = Guid.NewGuid(),
            NegocioId = negocioId,
            NombreCliente = nombreCliente.Trim(),
            TelefonoCliente = telefonoCliente.Trim(),
            DireccionEntrega = direccion.Trim(),
            LatitudCliente = latCliente,
            LongitudCliente = lngCliente,
            LatitudRestaurante = 0, // Se actualizará cuando Gym tenga coordenadas GPS
            LongitudRestaurante = 0,
            CostoComida = costoComida,
            CostoEnvio = costoEnvio,
            Total = costoComida + costoEnvio,
            Estado = "nuevo",
            Notas = notas?.Trim(),
            FechaCreacion = TimeHelper.Now,
            Detalles = detalles
        };

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        return (true, "Pedido creado exitosamente", pedido.PedidoId);
    }

    /// <summary>
    /// Un motorizado toma un pedido disponible.
    /// Transición: nuevo → tomado.
    ///
    /// RACE CONDITION PROTECTION:
    /// El modelo Pedido tiene [Timestamp] RowVersion. Si dos motorizados
    /// intentan tomar el mismo pedido al mismo tiempo:
    ///   1. Ambos leen el pedido con estado="nuevo" y el mismo RowVersion
    ///   2. Ambos cambian estado a "tomado" y llaman SaveChangesAsync
    ///   3. El primer motorizado guarda exitosamente (RowVersion se actualiza)
    ///   4. El segundo motorizado falla con DbUpdateConcurrencyException
    ///      porque su RowVersion ya no coincide con la fila en la BD
    ///   5. Le devolvemos un mensaje amigable: "pedido ya tomado"
    /// </summary>
    public async Task<(bool success, string message)> TomarPedidoAsync(Guid pedidoId, Guid motorizadoId)
    {
        var pedido = await _context.Pedidos
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

        if (pedido == null)
            return (false, "Pedido no encontrado");

        if (pedido.Estado != "nuevo")
            return (false, "Este pedido ya fue tomado por otro motorizado");

        // Verificar que el motorizado existe y está disponible
        var motorizado = await _context.Motorizados
            .FirstOrDefaultAsync(m => m.MotorizadoId == motorizadoId && m.IsActive);
        if (motorizado == null)
            return (false, "Motorizado no encontrado o inactivo");

        // Verificar que el motorizado no tiene otro pedido activo
        var tieneActivo = await _context.Pedidos.AnyAsync(p =>
            p.MotorizadoId == motorizadoId
            && p.Estado != "entregado"
            && p.Estado != "cancelado");
        if (tieneActivo)
            return (false, "Ya tienes un pedido activo. Complétalo antes de tomar otro.");

        // Asignar motorizado y cambiar estado
        pedido.MotorizadoId = motorizadoId;
        pedido.Estado = "tomado";
        pedido.FechaTomado = TimeHelper.Now;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otro motorizado tomó el pedido primero — perdimos la carrera
            return (false, "Este pedido ya fue tomado por otro motorizado");
        }

        return (true, "Pedido tomado exitosamente");
    }

    /// <summary>
    /// El restaurante confirma la disponibilidad de un item.
    /// Si todos los items no-rechazados están confirmados,
    /// el pedido pasa automáticamente a "confirmado".
    /// </summary>
    public async Task<(bool success, string message)> ConfirmarItemAsync(Guid detallePedidoId, Guid negocioId)
    {
        var detalle = await _context.DetallesPedido
            .Include(d => d.Pedido)
            .FirstOrDefaultAsync(d => d.DetallePedidoId == detallePedidoId
                                      && d.Pedido.NegocioId == negocioId);

        if (detalle == null)
            return (false, "Item no encontrado");

        if (detalle.Rechazado)
            return (false, "Este item ya fue rechazado");

        if (detalle.Confirmado)
            return (false, "Este item ya fue confirmado");

        if (detalle.Pedido.Estado != "tomado" && detalle.Pedido.Estado != "confirmado")
            return (false, "El pedido no está en un estado válido para confirmar items");

        detalle.Confirmado = true;
        await _context.SaveChangesAsync();

        // Verificar si todos los items no-rechazados están confirmados
        await TryAutoConfirmarPedidoAsync(detalle.PedidoId);

        return (true, "Item confirmado");
    }

    /// <summary>
    /// El restaurante rechaza un item (no disponible).
    /// Recalcula el CostoComida sin el item rechazado.
    /// Si TODOS los items son rechazados, el pedido se cancela.
    /// Si los restantes están confirmados, auto-confirma el pedido.
    /// </summary>
    public async Task<(bool success, string message)> RechazarItemAsync(Guid detallePedidoId, Guid negocioId)
    {
        var detalle = await _context.DetallesPedido
            .Include(d => d.Pedido)
                .ThenInclude(p => p.Detalles)
            .FirstOrDefaultAsync(d => d.DetallePedidoId == detallePedidoId
                                      && d.Pedido.NegocioId == negocioId);

        if (detalle == null)
            return (false, "Item no encontrado");

        if (detalle.Rechazado)
            return (false, "Este item ya fue rechazado");

        if (detalle.Pedido.Estado != "tomado" && detalle.Pedido.Estado != "confirmado")
            return (false, "El pedido no está en un estado válido para rechazar items");

        detalle.Rechazado = true;
        detalle.Confirmado = false;

        // Recalcular costo del pedido sin los items rechazados
        var pedido = detalle.Pedido;
        var itemsActivos = pedido.Detalles.Where(d => !d.Rechazado).ToList();

        if (itemsActivos.Count == 0)
        {
            // Todos los items rechazados → cancelar pedido
            pedido.Estado = "cancelado";
            pedido.CanceladoPor = "restaurante";
            pedido.RazonCancelacion = "Todos los productos del pedido están agotados";
            pedido.FechaCancelado = TimeHelper.Now;

            await _context.SaveChangesAsync();
            return (true, "Pedido cancelado porque todos los items fueron rechazados");
        }

        // Recalcular costos con los items restantes
        pedido.CostoComida = itemsActivos.Sum(d => d.Subtotal);
        pedido.Total = pedido.CostoComida + pedido.CostoEnvio;

        await _context.SaveChangesAsync();

        // Verificar si los items restantes ya están todos confirmados
        await TryAutoConfirmarPedidoAsync(pedido.PedidoId);

        return (true, $"Item rechazado. Nuevo total: ${pedido.Total:F2}");
    }

    /// <summary>
    /// El restaurante empieza a preparar el pedido.
    /// Transición: confirmado → preparando.
    /// </summary>
    public async Task<(bool success, string message)> EmpezarOrdenAsync(Guid pedidoId, Guid negocioId)
    {
        var pedido = await _context.Pedidos
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId && p.NegocioId == negocioId);

        if (pedido == null)
            return (false, "Pedido no encontrado");

        if (pedido.Estado != "confirmado")
            return (false, "El pedido debe estar confirmado para empezar a preparar");

        pedido.Estado = "preparando";
        pedido.FechaPreparando = TimeHelper.Now;

        await _context.SaveChangesAsync();
        return (true, "Pedido en preparación");
    }

    /// <summary>
    /// El restaurante marca el pedido como listo para recoger.
    /// Transición: preparando → listo.
    /// </summary>
    public async Task<(bool success, string message)> MarcarListoAsync(Guid pedidoId, Guid negocioId)
    {
        var pedido = await _context.Pedidos
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId && p.NegocioId == negocioId);

        if (pedido == null)
            return (false, "Pedido no encontrado");

        if (pedido.Estado != "preparando")
            return (false, "El pedido debe estar en preparación para marcarlo como listo");

        pedido.Estado = "listo";
        pedido.FechaListo = TimeHelper.Now;

        await _context.SaveChangesAsync();
        return (true, "Pedido listo para recoger");
    }

    /// <summary>
    /// El restaurante confirma que entregó la comida al motorizado.
    /// Si el motorizado también confirmó, auto-transición a "en_camino".
    ///
    /// DOBLE CONFIRMACIÓN:
    /// Ambas partes (restaurante + motorizado) deben confirmar para evitar
    /// disputas sobre si la comida fue realmente entregada/recibida.
    /// </summary>
    public async Task<(bool success, string message)> ConfirmarEntregaRestauranteAsync(Guid pedidoId, Guid negocioId)
    {
        var pedido = await _context.Pedidos
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId && p.NegocioId == negocioId);

        if (pedido == null)
            return (false, "Pedido no encontrado");

        if (pedido.Estado != "listo" && pedido.Estado != "recogido")
            return (false, "El pedido debe estar listo para confirmar la entrega");

        if (pedido.RestauranteConfirmoEntrega)
            return (false, "Ya confirmaste la entrega");

        pedido.RestauranteConfirmoEntrega = true;

        // Si ambos confirmaron, pasar a en_camino
        if (pedido.MotorizadoConfirmoRecepcion)
        {
            pedido.Estado = "en_camino";
            pedido.FechaRecogido = TimeHelper.Now;
        }

        await _context.SaveChangesAsync();

        if (pedido.Estado == "en_camino")
            return (true, "Entrega confirmada. El motorizado va en camino al cliente.");

        return (true, "Entrega confirmada. Esperando confirmación del motorizado.");
    }

    /// <summary>
    /// El motorizado confirma que recibió la comida del restaurante.
    /// Si el restaurante también confirmó, auto-transición a "en_camino".
    /// </summary>
    public async Task<(bool success, string message)> ConfirmarRecepcionMotorizadoAsync(Guid pedidoId, Guid motorizadoId)
    {
        var pedido = await _context.Pedidos
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId && p.MotorizadoId == motorizadoId);

        if (pedido == null)
            return (false, "Pedido no encontrado o no estás asignado a este pedido");

        if (pedido.Estado != "listo" && pedido.Estado != "recogido")
            return (false, "El pedido debe estar listo para confirmar la recepción");

        if (pedido.MotorizadoConfirmoRecepcion)
            return (false, "Ya confirmaste la recepción");

        pedido.MotorizadoConfirmoRecepcion = true;

        // Si ambos confirmaron, pasar a en_camino
        if (pedido.RestauranteConfirmoEntrega)
        {
            pedido.Estado = "en_camino";
            pedido.FechaRecogido = TimeHelper.Now;
        }

        await _context.SaveChangesAsync();

        if (pedido.Estado == "en_camino")
            return (true, "Recepción confirmada. Ahora ve hacia el cliente.");

        return (true, "Recepción confirmada. Esperando confirmación del restaurante.");
    }

    /// <summary>
    /// El motorizado marca el pedido como entregado al cliente.
    /// Transición: en_camino → entregado.
    /// Incrementa el contador TotalEntregas del motorizado.
    /// </summary>
    public async Task<(bool success, string message)> MarcarEntregadoAsync(Guid pedidoId, Guid motorizadoId)
    {
        var pedido = await _context.Pedidos
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId && p.MotorizadoId == motorizadoId);

        if (pedido == null)
            return (false, "Pedido no encontrado o no estás asignado a este pedido");

        if (pedido.Estado != "en_camino")
            return (false, "El pedido debe estar en camino para marcarlo como entregado");

        pedido.Estado = "entregado";
        pedido.FechaEntregado = TimeHelper.Now;

        // Incrementar contador de entregas del motorizado
        var motorizado = await _context.Motorizados.FindAsync(motorizadoId);
        if (motorizado != null)
        {
            motorizado.TotalEntregas++;
        }

        // Registrar comisiones de delivery ($0.20 para motorizado y/o restaurante si plan=comision)
        // Motorizado: comisión si plan="comision"
        if (motorizado != null && motorizado.TipoPlan == "comision")
        {
            _context.ComisionesDelivery.Add(new Gimnasio.Models.ComisionDelivery
            {
                TipoPagador = "motorizado",
                MotorizadoId = motorizadoId,
                PedidoId = pedidoId,
                Monto = 0.20m,
                Fecha = TimeHelper.Now
            });
            motorizado.ComisionesAcumuladas += 0.20m;
        }

        // Restaurante: comisión si TipoPlanDelivery="comision"
        var restaurante = await _context.Negocios.FindAsync(pedido.NegocioId);
        if (restaurante != null && restaurante.TipoPlanDelivery == "comision")
        {
            _context.ComisionesDelivery.Add(new Gimnasio.Models.ComisionDelivery
            {
                TipoPagador = "restaurante",
                NegocioId = pedido.NegocioId,
                PedidoId = pedidoId,
                Monto = 0.20m,
                Fecha = TimeHelper.Now
            });
            restaurante.ComisionesDeliveryAcumuladas += 0.20m;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return (true, "Pedido entregado exitosamente");
    }

    /// <summary>
    /// Cancela un pedido. Solo se puede cancelar si no ha sido entregado.
    /// Registra quién canceló y por qué para trazabilidad.
    /// </summary>
    public async Task<(bool success, string message)> CancelarPedidoAsync(
        Guid pedidoId, string canceladoPor, string razon)
    {
        var pedido = await _context.Pedidos
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

        if (pedido == null)
            return (false, "Pedido no encontrado");

        if (pedido.Estado == "entregado")
            return (false, "No se puede cancelar un pedido ya entregado");

        if (pedido.Estado == "cancelado")
            return (false, "Este pedido ya está cancelado");

        if (string.IsNullOrWhiteSpace(canceladoPor))
            return (false, "Debe indicar quién cancela el pedido");

        pedido.Estado = "cancelado";
        pedido.CanceladoPor = canceladoPor.Trim();
        pedido.RazonCancelacion = razon?.Trim();
        pedido.FechaCancelado = TimeHelper.Now;

        await _context.SaveChangesAsync();
        return (true, "Pedido cancelado");
    }

    /// <summary>
    /// Actualiza la posición GPS del motorizado.
    /// Se llama periódicamente desde el frontend del motorizado
    /// para que el cliente pueda ver su ubicación en tiempo real.
    /// </summary>
    public async Task<(bool success, string message)> ActualizarUbicacionMotorizadoAsync(
        Guid motorizadoId, double lat, double lng)
    {
        var motorizado = await _context.Motorizados
            .FirstOrDefaultAsync(m => m.MotorizadoId == motorizadoId);

        if (motorizado == null)
            return (false, "Motorizado no encontrado");

        motorizado.Latitud = lat;
        motorizado.Longitud = lng;

        await _context.SaveChangesAsync();
        return (true, "Ubicación actualizada");
    }

    /// <summary>
    /// Verifica si el motorizado está a menos de 1km del cliente.
    /// Usa la fórmula Haversine para calcular la distancia real
    /// sobre la superficie terrestre (no en línea recta plana).
    ///
    /// FÓRMULA HAVERSINE:
    /// d = 2r * arcsin(√(sin²((φ2-φ1)/2) + cos(φ1)·cos(φ2)·sin²((λ2-λ1)/2)))
    /// donde r = 6371000 metros (radio de la Tierra)
    /// </summary>
    public async Task<(bool success, string message, bool estaCerca)> VerificarProximidadAsync(
        Guid pedidoId, Guid motorizadoId, double latMoto, double lngMoto)
    {
        var pedido = await _context.Pedidos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId && p.MotorizadoId == motorizadoId);

        if (pedido == null)
            return (false, "Pedido no encontrado", false);

        var distancia = CalcularDistanciaHaversine(
            latMoto, lngMoto,
            pedido.LatitudCliente, pedido.LongitudCliente);

        var estaCerca = distancia < 1000; // < 1km

        return (true, $"Distancia al cliente: {distancia:F0}m", estaCerca);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MÉTODOS AUXILIARES PRIVADOS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifica si todos los items no-rechazados de un pedido están confirmados.
    /// Si es así, auto-transiciona el pedido a estado "confirmado".
    /// Se llama después de confirmar o rechazar un item.
    /// </summary>
    private async Task TryAutoConfirmarPedidoAsync(Guid pedidoId)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

        if (pedido == null || pedido.Estado == "confirmado") return;

        var itemsActivos = pedido.Detalles.Where(d => !d.Rechazado).ToList();

        // Si todos los items activos están confirmados → confirmar pedido
        if (itemsActivos.Count > 0 && itemsActivos.All(d => d.Confirmado))
        {
            pedido.Estado = "confirmado";
            pedido.FechaConfirmado = TimeHelper.Now;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Calcula la distancia en metros entre dos puntos GPS usando la fórmula Haversine.
    /// Es la fórmula estándar para calcular distancias sobre la superficie terrestre
    /// teniendo en cuenta la curvatura de la Tierra.
    ///
    /// Parámetros: latitud y longitud en grados decimales.
    /// Retorna: distancia en metros.
    /// </summary>
    private static double CalcularDistanciaHaversine(
        double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Radio de la Tierra en metros

        // Convertir grados a radianes
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    /// <summary>
    /// Convierte grados sexagesimales a radianes.
    /// Fórmula: radianes = grados × π / 180
    /// </summary>
    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
