// ═══════════════════════════════════════════════════════════════════════════════
// ProductoCreateDto.cs
//
// ESTE DTO: recibe los datos del formulario "Agregar / Editar Producto" del
// módulo de inventario del dashboard (InventarioService / InventarioController).
//
// FLUJO: El dueño del negocio abre el formulario de producto → llena los campos
// → se envía por AJAX/POST → el controlador recibe este DTO → InventarioService
// crea o actualiza el producto en la tabla Productos de la BD.
//
// MODO DUAL (crear / editar):
// ProductoId = Guid.Empty → INSERT de producto nuevo.
// ProductoId con valor   → UPDATE del producto existente.
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para crear o editar un producto del inventario del negocio.
/// Captura nombre, precio de venta, costo de compra, stock actual y stock mínimo.
/// El margen de ganancia (PrecioVenta - CostoCompra) se calcula en el servicio
/// o en los reportes; no se envía desde el formulario.
/// Cuando ProductoId viene vacío (Guid.Empty), se crea un producto nuevo.
/// Cuando ProductoId tiene valor, se actualiza el producto existente.
/// </summary>
public class ProductoCreateDto
{
    /// <summary>
    /// Identificador único del producto.
    /// Guid.Empty = crear producto nuevo.
    /// Guid con valor = editar el producto cuyo Id coincida en la base de datos.
    /// </summary>
    public Guid ProductoId { get; set; }

    /// <summary>
    /// Id del negocio dueño de este producto. Obligatorio.
    /// Se envía como campo oculto en el formulario para asociar el producto
    /// al negocio del usuario logueado y evitar que un negocio acceda al
    /// inventario de otro.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre del producto. Obligatorio, máximo 200 caracteres.
    /// Se muestra en la tabla de inventario, en las ventas y en los reportes.
    /// Ejemplos: "Proteína Whey 1kg", "Guantes de Box Talla M".
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Nombre { get; set; }

    /// <summary>
    /// Precio al que el negocio vende el producto al cliente. Obligatorio.
    /// [Range(0.01, double.MaxValue)] garantiza que el precio de venta sea positivo;
    /// un precio de cero o negativo causaría reportes de ingresos incorrectos.
    /// Se usa para calcular el total de ventas y el margen de ganancia.
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal PrecioVenta { get; set; }

    /// <summary>
    /// Precio que el negocio pagó para adquirir el producto. Obligatorio.
    /// [Range(0.01, double.MaxValue)] garantiza que el costo sea positivo.
    /// Se usa para calcular el margen de ganancia: Margen = PrecioVenta - CostoCompra.
    /// Permite al dueño del negocio ver la rentabilidad de cada producto.
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal CostoCompra { get; set; }

    /// <summary>
    /// Cantidad disponible actualmente en el inventario. Obligatorio, mínimo 0.
    /// [Range(0, int.MaxValue)] permite stock de cero (producto agotado) pero no negativo.
    /// El sistema descuenta una unidad de Stock cada vez que se registra una venta.
    /// Se muestra con alerta visual cuando el stock está por debajo del StockMinimo.
    /// </summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    /// <summary>
    /// Cantidad mínima de stock antes de que el sistema emita una alerta de reabastecimiento.
    /// Opcional; si no se especifica, el valor por defecto es 5.
    /// [Range(0, int.MaxValue)] no permite valores negativos.
    /// Ejemplo: si StockMinimo = 5 y Stock cae a 4, el producto se muestra en rojo
    /// en la tabla de inventario para avisar al dueño que debe reabastecer.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int StockMinimo { get; set; } = 5;

    /// <summary>
    /// Categoría a la que pertenece el producto. Opcional.
    /// Null o Guid.Empty = Sin categoría.
    /// </summary>
    public Guid? CategoriaProductoId { get; set; }
}
