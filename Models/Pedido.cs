// ═══════════════════════════════════════════════════════════
// Pedido.cs — Modelo de orden de delivery
//
// Representa un pedido de comida realizado por un cliente final
// a un restaurante registrado en My-Negocio, entregado por un motorizado.
//
// Estados del pedido (máquina de estados):
//   nuevo       → el cliente creó el pedido, visible para motorizados
//   tomado      → un motorizado aceptó el pedido
//   confirmado  → el restaurante confirmó todos los items disponibles
//   preparando  → el restaurante está cocinando
//   listo       → la comida está lista para recoger
//   recogido    → el motorizado recogió la comida (doble confirmación)
//   en_camino   → el motorizado va hacia el cliente
//   entregado   → el cliente recibió su pedido
//   cancelado   → pedido cancelado (por cliente o restaurante)
//
// Transiciones válidas:
//   nuevo → tomado (motorizado acepta)
//   tomado → confirmado (restaurante confirma items)
//   confirmado → preparando (restaurante empieza a cocinar)
//   preparando → listo (comida lista)
//   listo → recogido (doble confirmación: restaurante + motorizado)
//   recogido → en_camino (automático tras doble confirmación)
//   en_camino → entregado (motorizado confirma entrega)
//   cualquiera (excepto entregado) → cancelado
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa una orden de delivery en el sistema de pedidos de My-Negocio.
/// Contiene toda la información del pedido: cliente, restaurante, motorizado,
/// items, costos, estado actual y timestamps de cada transición.
/// </summary>
public class Pedido
{
    /// <summary>
    /// Identificador único del pedido (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla Pedidos.
    /// </summary>
    [Key]
    public Guid PedidoId { get; set; }

    /// <summary>
    /// ID del restaurante (negocio) al que se le hizo el pedido.
    /// [Required] = todo pedido debe pertenecer a un restaurante.
    /// Se usa para filtrar pedidos por negocio (aislamiento multi-tenant).
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// ID del motorizado asignado al pedido.
    /// Nullable — se asigna cuando un motorizado toma el pedido (estado "tomado").
    /// Antes de eso es null (pedido disponible para cualquier motorizado).
    /// </summary>
    public Guid? MotorizadoId { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DATOS DEL CLIENTE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Nombre del cliente que realiza el pedido.
    /// [Required] = obligatorio para identificar al destinatario.
    /// [StringLength(200)] = límite razonable para nombres.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string NombreCliente { get; set; }

    /// <summary>
    /// Teléfono del cliente para contacto durante la entrega.
    /// [Required] = obligatorio para que el motorizado pueda comunicarse.
    /// [StringLength(20)] = suficiente para números con código de país.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string TelefonoCliente { get; set; }

    /// <summary>
    /// Dirección de entrega en texto libre.
    /// [Required] = obligatorio para que el motorizado sepa dónde entregar.
    /// [StringLength(500)] = suficiente para direcciones completas con referencias.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string DireccionEntrega { get; set; }

    /// <summary>
    /// Latitud GPS de la dirección de entrega del cliente.
    /// Se obtiene del mapa interactivo en el checkout.
    /// </summary>
    public double LatitudCliente { get; set; }

    /// <summary>
    /// Longitud GPS de la dirección de entrega del cliente.
    /// Se obtiene del mapa interactivo en el checkout.
    /// </summary>
    public double LongitudCliente { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DATOS DEL RESTAURANTE (GPS)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Latitud GPS del restaurante.
    /// Se copia del negocio al crear el pedido para que quede como snapshot.
    /// </summary>
    public double LatitudRestaurante { get; set; }

    /// <summary>
    /// Longitud GPS del restaurante.
    /// Se copia del negocio al crear el pedido para que quede como snapshot.
    /// </summary>
    public double LongitudRestaurante { get; set; }

    // ═══════════════════════════════════════════════════════════
    // COSTOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Suma de los precios de todos los items del pedido.
    /// Se calcula automáticamente al crear el pedido.
    /// </summary>
    public decimal CostoComida { get; set; }

    /// <summary>
    /// Costo de envío definido por el cliente (mínimo $1).
    /// Va directamente al motorizado como pago por la entrega.
    /// </summary>
    public decimal CostoEnvio { get; set; }

    /// <summary>
    /// Total del pedido = CostoComida + CostoEnvio.
    /// Se calcula automáticamente al crear el pedido.
    /// </summary>
    public decimal Total { get; set; }

    // ═══════════════════════════════════════════════════════════
    // ESTADO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Estado actual del pedido en la máquina de estados.
    /// Valores: "nuevo", "tomado", "confirmado", "preparando", "listo",
    ///          "recogido", "en_camino", "entregado", "cancelado".
    /// Default "nuevo" — el pedido acaba de crearse.
    /// [StringLength(20)] = suficiente para todos los estados posibles.
    /// </summary>
    [StringLength(20)]
    public string Estado { get; set; } = "nuevo";

    /// <summary>
    /// Notas adicionales del cliente para el restaurante o motorizado.
    /// Ejemplos: "Sin cebolla", "Timbre no funciona, llamar al llegar".
    /// [StringLength(500)] = suficiente para notas detalladas.
    /// </summary>
    [StringLength(500)]
    public string Notas { get; set; }

    // ═══════════════════════════════════════════════════════════
    // CANCELACIÓN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Quién canceló el pedido: "cliente" o "restaurante".
    /// Null si el pedido no ha sido cancelado.
    /// [StringLength(50)] = suficiente para los roles posibles.
    /// </summary>
    [StringLength(50)]
    public string CanceladoPor { get; set; }

    /// <summary>
    /// Razón por la cual se canceló el pedido.
    /// Obligatoria al cancelar para mantener trazabilidad.
    /// [StringLength(500)] = suficiente para explicaciones detalladas.
    /// </summary>
    [StringLength(500)]
    public string RazonCancelacion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DOBLE CONFIRMACIÓN DE RECOGIDA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// El restaurante confirma que entregó la comida al motorizado.
    /// Ambas flags (Restaurante + Motorizado) deben ser true para
    /// que el pedido pase automáticamente a estado "recogido"/"en_camino".
    /// </summary>
    public bool RestauranteConfirmoEntrega { get; set; } = false;

    /// <summary>
    /// El motorizado confirma que recibió la comida del restaurante.
    /// Ambas flags (Restaurante + Motorizado) deben ser true para
    /// que el pedido pase automáticamente a estado "recogido"/"en_camino".
    /// </summary>
    public bool MotorizadoConfirmoRecepcion { get; set; } = false;

    // ═══════════════════════════════════════════════════════════
    // TIMESTAMPS — Registro de cada transición de estado
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Fecha y hora en que el pedido fue creado por el cliente.
    /// Se asigna automáticamente con TimeHelper.Now.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    /// <summary>Fecha y hora en que un motorizado tomó el pedido.</summary>
    public DateTime? FechaTomado { get; set; }

    /// <summary>Fecha y hora en que el restaurante confirmó todos los items.</summary>
    public DateTime? FechaConfirmado { get; set; }

    /// <summary>Fecha y hora en que el restaurante empezó a preparar.</summary>
    public DateTime? FechaPreparando { get; set; }

    /// <summary>Fecha y hora en que la comida quedó lista para recoger.</summary>
    public DateTime? FechaListo { get; set; }

    /// <summary>Fecha y hora en que el motorizado recogió la comida.</summary>
    public DateTime? FechaRecogido { get; set; }

    /// <summary>Fecha y hora en que el pedido fue entregado al cliente.</summary>
    public DateTime? FechaEntregado { get; set; }

    /// <summary>Fecha y hora en que el pedido fue cancelado.</summary>
    public DateTime? FechaCancelado { get; set; }

    // ═══════════════════════════════════════════════════════════
    // CONCURRENCIA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Token de concurrencia optimista para prevenir race conditions.
    /// Crítico para TomarPedido: si dos motorizados intentan tomar el mismo
    /// pedido simultáneamente, el segundo fallará con DbUpdateConcurrencyException.
    /// SQL Server actualiza este campo automáticamente en cada UPDATE.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Restaurante (negocio) al que pertenece este pedido.
    /// Relación muchos-a-uno: muchos pedidos → un restaurante.
    /// </summary>
    public Gym Negocio { get; set; }

    /// <summary>
    /// Motorizado asignado al pedido (null si aún no fue tomado).
    /// Relación muchos-a-uno: muchos pedidos → un motorizado.
    /// </summary>
    public Motorizado Motorizado { get; set; }

    /// <summary>
    /// Items individuales del pedido (líneas de detalle).
    /// Relación uno-a-muchos: un pedido tiene muchos detalles.
    /// </summary>
    public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
}
