// ═══════════════════════════════════════════════════════════
// Producto.cs — Modelo de producto del inventario de un negocio
//
// Representa un artículo físico que el negocio vende o usa en sus servicios.
// Ejemplos: suplementos en un gym, shampoo en una barbería, esmaltes en un spa.
//
// El sistema lleva el control de inventario (Stock) y genera alertas
// cuando el stock baja del umbral mínimo (StockMinimo).
//
// Cada cambio en el Stock genera un registro en MovimientoInventario para
// mantener un historial auditable de entradas y salidas.
//
// El soft delete (IsActive = false) permite "eliminar" lógicamente el producto
// sin perder el historial de movimientos asociados.
//
// Concurrencia: el campo RowVersion ([Timestamp]) previene race conditions
// cuando dos usuarios intentan modificar el stock simultáneamente.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa un producto físico en el inventario de un negocio.
/// Puede ser un producto de venta al público o un insumo usado en los servicios.
///
/// El inventario se controla mediante el campo Stock y se audita con MovimientoInventario.
/// Las alertas de stock mínimo se activan cuando Stock &lt;= StockMinimo.
/// </summary>
public class Producto
{
    /// <summary>
    /// Identificador único del producto (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla Productos.
    /// </summary>
    [Key]
    public Guid ProductoId { get; set; }

    /// <summary>
    /// ID del negocio al que pertenece este producto.
    /// [Required] = todo producto debe pertenecer a un negocio.
    /// Se usa para filtrar productos por negocio (aislamiento multi-tenant).
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre del producto tal como aparece en el inventario y al venderlo.
    /// Ejemplo: "Proteína Whey Chocolate 1kg", "Shampoo para barba", "Esmalte rojo #15".
    /// [Required] = campo obligatorio para identificar el producto.
    /// [StringLength(200)] = límite suficiente para nombres descriptivos.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Nombre { get; set; }

    /// <summary>
    /// Precio al que el negocio vende el producto a sus clientes (en moneda local).
    /// [Required] = obligatorio; todo producto debe tener precio de venta.
    /// Ejemplo: 450.00 = $450 MXN por unidad.
    /// El margen de ganancia se calcula como: PrecioVenta - CostoCompra.
    /// </summary>
    [Required]
    public decimal PrecioVenta { get; set; }

    /// <summary>
    /// Precio que le costó al negocio comprar o adquirir el producto (en moneda local).
    /// [Required] = obligatorio para calcular rentabilidad y reportes de utilidades.
    /// Ejemplo: 300.00 = $300 MXN de costo, vendiendo a $450 = $150 de ganancia por unidad.
    /// Este valor es interno del negocio y no se muestra al cliente.
    /// </summary>
    [Required]
    public decimal CostoCompra { get; set; }

    /// <summary>
    /// Número de unidades disponibles actualmente en el inventario físico.
    /// Se actualiza automáticamente con cada MovimientoInventario.
    /// Cuando Stock llega a 0, el producto no puede venderse.
    /// Cuando Stock &lt;= StockMinimo, el sistema puede generar una alerta al negocio.
    /// Valor por defecto: 0 (sin stock al registrar el producto).
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Umbral mínimo de stock que activa una alerta de reabastecimiento.
    /// Cuando Stock &lt;= StockMinimo, el sistema avisa al negocio que debe pedir más.
    /// Ejemplo: StockMinimo = 5 significa "avísame cuando queden 5 unidades o menos".
    /// El negocio puede ajustar este valor según cuánto tiempo le toma reabastecer.
    /// Valor por defecto: 5 unidades.
    /// </summary>
    public int StockMinimo { get; set; } = 5;

    /// <summary>
    /// Indica si el producto está activo y disponible para venta e inventario.
    /// true  = producto activo (aparece en el inventario y puede venderse).
    /// false = producto dado de baja (soft delete; no aparece para nuevas ventas,
    ///         pero su historial de movimientos se conserva intacto).
    /// El dueño puede desactivar productos descontinuados sin perder el historial.
    /// Valor por defecto: true.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Fecha y hora en que el producto fue agregado al inventario del negocio.
    /// Se asigna automáticamente con TimeHelper.Now al crear el registro.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    /// <summary>
    /// Fecha y hora de la última modificación del producto (precio, nombre, stock mínimo, etc.).
    /// Se actualiza con TimeHelper.Now cada vez que el dueño edita el producto.
    /// </summary>
    public DateTime FechaDeActualizacion { get; set; } = TimeHelper.Now;

    /// <summary>
    /// Imagen del producto como data URI Base64 (data:image/jpeg;base64,...).
    /// Se almacena directamente en la BD para funcionar en cualquier entorno
    /// de producción sin depender del filesystem (contenedores, Azure, etc.).
    /// Sin [StringLength] = nvarchar(max) en SQL Server.
    /// </summary>
    public string ImagenUrl { get; set; }

    /// <summary>
    /// Receta o lista de ingredientes del producto (uso principal en restaurantes).
    /// Opcional — colapsable en el modal de crear/editar producto.
    /// </summary>
    [StringLength(4000)]
    public string Receta { get; set; }

    /// <summary>
    /// ID de la categoría a la que pertenece este producto (agrupación visual en tab).
    /// Opcional (nullable) para negocios antiguos o productos sin catalogar.
    /// </summary>
    public Guid? CategoriaProductoId { get; set; }

    /// <summary>
    /// Categoría padre de este producto si fuera asignado.
    /// </summary>
    public CategoriaProducto Categoria { get; set; }

    /// <summary>
    /// Token de concurrencia optimista para prevenir race conditions en operaciones de stock.
    /// [Timestamp] = EF Core lo usa automáticamente para detectar conflictos de concurrencia.
    ///
    /// Problema que resuelve: si dos usuarios intentan vender el mismo producto
    /// al mismo tiempo, el segundo UPDATE fallará porque el RowVersion cambió
    /// desde que el segundo usuario leyó el registro. Esto evita "overselling"
    /// (vender más de lo que hay en inventario).
    ///
    /// SQL Server actualiza este campo automáticamente en cada UPDATE.
    /// El código de la aplicación no necesita manejarlo directamente.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; }
}
