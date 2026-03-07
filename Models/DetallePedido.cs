// ═══════════════════════════════════════════════════════════
// DetallePedido.cs — Línea de detalle de un pedido de delivery
//
// Cada item que el cliente agregó al carrito se convierte en
// un DetallePedido al confirmar el pedido.
//
// Los campos NombreProducto y PrecioUnitario se desnormalizan
// (copian del Producto al momento de crear) para garantizar que
// el historial sea inmutable: si el restaurante cambia el precio
// o nombre del plato después, los pedidos pasados no se afectan.
//
// El restaurante puede Confirmar o Rechazar cada item antes de
// empezar a preparar. Si un item se rechaza (ej: plato agotado),
// el costo del pedido se recalcula automáticamente.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa un item individual dentro de un pedido de delivery.
/// Es el equivalente a una línea en un ticket de venta.
/// Los datos del producto se desnormalizan al momento de la creación
/// para mantener un snapshot inmutable del precio y nombre.
/// </summary>
public class DetallePedido
{
    /// <summary>
    /// Identificador único del detalle (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla DetallesPedido.
    /// </summary>
    [Key]
    public Guid DetallePedidoId { get; set; }

    /// <summary>
    /// ID del pedido al que pertenece este detalle.
    /// [Required] = todo detalle debe pertenecer a un pedido.
    /// FK configurada con Cascade delete en OnModelCreating.
    /// </summary>
    [Required]
    public Guid PedidoId { get; set; }

    /// <summary>
    /// ID del producto (plato/bebida) del catálogo del restaurante.
    /// [Required] = todo detalle debe referenciar un producto existente.
    /// FK configurada con NoAction delete en OnModelCreating.
    /// </summary>
    [Required]
    public Guid ProductoId { get; set; }

    /// <summary>
    /// Nombre del producto al momento de crear el pedido (snapshot desnormalizado).
    /// Se guarda aquí para que el historial sea inmutable incluso si el restaurante
    /// renombra o elimina el producto después.
    /// [StringLength(200)] = mismo límite que Producto.Nombre.
    /// </summary>
    [StringLength(200)]
    public string NombreProducto { get; set; }

    /// <summary>
    /// Cantidad de unidades de este producto en el pedido.
    /// Siempre >= 1.
    /// </summary>
    public int Cantidad { get; set; }

    /// <summary>
    /// Precio unitario al momento de crear el pedido (snapshot desnormalizado).
    /// Inmutable — no cambia si el restaurante modifica el precio después.
    /// </summary>
    public decimal PrecioUnitario { get; set; }

    /// <summary>
    /// Subtotal = Cantidad * PrecioUnitario.
    /// Se calcula y almacena al crear el pedido para evitar recálculos.
    /// </summary>
    public decimal Subtotal { get; set; }

    // ═══════════════════════════════════════════════════════════
    // CONFIRMACIÓN DEL RESTAURANTE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// El restaurante confirmó que tiene este item disponible.
    /// true = el plato está disponible y se va a preparar.
    /// Se marca individualmente para cada item del pedido.
    /// </summary>
    public bool Confirmado { get; set; } = false;

    /// <summary>
    /// El restaurante rechazó este item (ej: plato agotado).
    /// true = el plato no está disponible, no se incluirá en el pedido.
    /// Cuando un item se rechaza, el CostoComida del pedido se recalcula.
    /// </summary>
    public bool Rechazado { get; set; } = false;

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Pedido al que pertenece este detalle.
    /// Relación muchos-a-uno: muchos detalles → un pedido.
    /// </summary>
    public Pedido Pedido { get; set; }

    /// <summary>
    /// Producto del catálogo del restaurante que referencia este detalle.
    /// Relación muchos-a-uno: muchos detalles → un producto.
    /// </summary>
    public Producto Producto { get; set; }
}
