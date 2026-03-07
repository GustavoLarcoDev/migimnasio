using Gimnasio.Models;

namespace Gimnasio.Services;

/// <summary>
/// DTO para crear los items de un pedido de delivery.
/// Se usa al crear un pedido para pasar la lista de productos seleccionados.
/// </summary>
public class DetallePedidoDto
{
    public Guid ProductoId { get; set; }
    public string NombreProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

/// <summary>
/// Servicio de gestión de pedidos de delivery.
/// Maneja la máquina de estados completa del pedido: desde la creación
/// por el cliente hasta la entrega por el motorizado.
/// </summary>
public interface IPedidoService
{
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene pedidos con estado "nuevo" disponibles para que un motorizado los tome.
    /// </summary>
    Task<object> GetPedidosDisponiblesAsync();

    /// <summary>
    /// Obtiene los datos completos de un pedido específico.
    /// </summary>
    Task<object> GetPedidoAsync(Guid pedidoId);

    /// <summary>
    /// Obtiene los pedidos activos (no entregados/cancelados) de un restaurante.
    /// </summary>
    Task<object> GetPedidosRestauranteAsync(Guid negocioId);

    /// <summary>
    /// Obtiene datos de tracking del pedido para la vista del cliente.
    /// Incluye estado, timestamps, y ubicación del motorizado si está en camino.
    /// </summary>
    Task<object> GetPedidoTrackingAsync(Guid pedidoId);

    /// <summary>
    /// Obtiene nombre, foto y estadísticas del motorizado asignado a un pedido.
    /// Se muestra al cliente y al restaurante.
    /// </summary>
    Task<object> GetDatosMotorizadoAsync(Guid pedidoId);

    /// <summary>
    /// Obtiene dirección y teléfono del cliente para el motorizado.
    /// Solo accesible por el motorizado asignado al pedido.
    /// </summary>
    Task<object> GetDatosClienteAsync(Guid pedidoId, Guid motorizadoId);

    /// <summary>
    /// Busca restaurantes cercanos a las coordenadas dadas.
    /// Calcula distancia con fórmula Haversine, ordena por cercanía.
    /// Devuelve top 50 restaurantes activos y no bloqueados.
    /// </summary>
    Task<List<object>> GetRestaurantesCercanosAsync(double lat, double lng);

    /// <summary>
    /// Obtiene el historial de pedidos completados de un motorizado.
    /// </summary>
    Task<object> GetHistorialMotorizadoAsync(Guid motorizadoId);

    /// <summary>
    /// Obtiene estadísticas del motorizado para su dashboard.
    /// </summary>
    Task<object> GetMotorizadoStatsAsync(Guid motorizadoId);

    // ═══════════════════════════════════════════════════════════
    // TRANSICIONES DE ESTADO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo pedido de delivery.
    /// Retorna el ID del pedido creado para redirigir al tracking.
    /// </summary>
    Task<(bool success, string message, Guid? pedidoId)> CrearPedidoAsync(
        Guid negocioId, string nombreCliente, string telefonoCliente,
        string direccion, double latCliente, double lngCliente,
        decimal costoEnvio, string notas, List<DetallePedidoDto> items);

    /// <summary>
    /// Un motorizado toma un pedido disponible (nuevo → tomado).
    /// Usa concurrencia optimista para evitar race conditions:
    /// solo el primer motorizado que guarde gana el pedido.
    /// </summary>
    Task<(bool success, string message)> TomarPedidoAsync(Guid pedidoId, Guid motorizadoId);

    /// <summary>
    /// El restaurante confirma que un item específico está disponible.
    /// Si todos los items no-rechazados están confirmados, el pedido
    /// pasa automáticamente a estado "confirmado".
    /// </summary>
    Task<(bool success, string message)> ConfirmarItemAsync(Guid detallePedidoId, Guid negocioId);

    /// <summary>
    /// El restaurante rechaza un item (plato no disponible).
    /// Se recalcula el CostoComida y Total del pedido sin el item rechazado.
    /// Si todos los items fueron rechazados, el pedido se cancela.
    /// Si los restantes están confirmados, el pedido pasa a "confirmado".
    /// </summary>
    Task<(bool success, string message)> RechazarItemAsync(Guid detallePedidoId, Guid negocioId);

    /// <summary>
    /// El restaurante empieza a preparar el pedido (confirmado → preparando).
    /// </summary>
    Task<(bool success, string message)> EmpezarOrdenAsync(Guid pedidoId, Guid negocioId);

    /// <summary>
    /// El restaurante marca el pedido como listo para recoger (preparando → listo).
    /// </summary>
    Task<(bool success, string message)> MarcarListoAsync(Guid pedidoId, Guid negocioId);

    /// <summary>
    /// El restaurante confirma que entregó la comida al motorizado.
    /// Si el motorizado también confirmó la recepción, el pedido pasa a "en_camino".
    /// </summary>
    Task<(bool success, string message)> ConfirmarEntregaRestauranteAsync(Guid pedidoId, Guid negocioId);

    /// <summary>
    /// El motorizado confirma que recibió la comida del restaurante.
    /// Si el restaurante también confirmó la entrega, el pedido pasa a "en_camino".
    /// </summary>
    Task<(bool success, string message)> ConfirmarRecepcionMotorizadoAsync(Guid pedidoId, Guid motorizadoId);

    /// <summary>
    /// El motorizado marca el pedido como entregado al cliente (en_camino → entregado).
    /// Incrementa el contador TotalEntregas del motorizado.
    /// </summary>
    Task<(bool success, string message)> MarcarEntregadoAsync(Guid pedidoId, Guid motorizadoId);

    /// <summary>
    /// Cancela un pedido. Solo se puede cancelar si no ha sido entregado.
    /// </summary>
    Task<(bool success, string message)> CancelarPedidoAsync(Guid pedidoId, string canceladoPor, string razon);

    /// <summary>
    /// Actualiza la posición GPS del motorizado en la base de datos.
    /// Se llama periódicamente desde el frontend del motorizado.
    /// </summary>
    Task<(bool success, string message)> ActualizarUbicacionMotorizadoAsync(Guid motorizadoId, double lat, double lng);

    /// <summary>
    /// Verifica si el motorizado está cerca del cliente (< 1km).
    /// Usa la fórmula Haversine para calcular distancia en metros.
    /// </summary>
    Task<(bool success, string message, bool estaCerca)> VerificarProximidadAsync(
        Guid pedidoId, Guid motorizadoId, double latMoto, double lngMoto);
}
