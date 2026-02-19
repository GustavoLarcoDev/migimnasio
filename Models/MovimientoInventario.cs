// ═══════════════════════════════════════════════════════════
// MovimientoInventario.cs — Registro de movimientos de stock de un producto
//
// Cada cambio en el inventario de un producto genera un MovimientoInventario.
// Es el historial inmutable de todas las entradas y salidas de stock.
//
// Tipos de movimiento:
//   "venta"      = se vendió stock a un cliente (reduce stock).
//   "devolucion" = un cliente devolvió un producto (aumenta stock).
//   "restock"    = el negocio recibió nueva mercancía (aumenta stock).
//   "ajuste"     = corrección manual de stock por conteo físico (puede subir o bajar).
//
// Por qué guardamos StockAnterior y StockNuevo:
//   Para tener un historial auditable. Si alguien ve que el stock cambió de 10 a 5,
//   puede buscar en esta tabla qué movimiento causó la diferencia.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Registro inmutable de un movimiento de inventario (entrada o salida de stock).
/// Se crea automáticamente cada vez que el stock de un Producto cambia.
///
/// Sirve para auditar cambios de inventario, calcular el valor de ventas
/// y detectar discrepancias entre el sistema y el conteo físico.
/// </summary>
public class MovimientoInventario
{
    /// <summary>
    /// Identificador único del movimiento (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla MovimientosInventario.
    /// </summary>
    [Key]
    public Guid MovimientoId { get; set; }

    /// <summary>
    /// ID del negocio al que pertenece este movimiento de inventario.
    /// [Required] = todo movimiento debe pertenecer a un negocio.
    /// Se usa para filtrar movimientos por negocio (aislamiento multi-tenant).
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// ID del producto cuyo stock fue modificado.
    /// [Required] = todo movimiento afecta a un producto específico.
    /// Relación muchos-a-uno: un producto puede tener muchos movimientos.
    /// </summary>
    [Required]
    public Guid ProductoId { get; set; }

    /// <summary>
    /// Nombre del producto al momento del movimiento (denormalizado).
    /// Se copia desde Producto.Nombre para que el historial sea consistente
    /// aunque el nombre del producto cambie en el futuro.
    /// [Required] = siempre debe quedar registrado el nombre.
    /// [MaxLength(200)] = igual al límite del nombre del producto.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string NombreProducto { get; set; }

    /// <summary>
    /// Categoría del movimiento de inventario.
    /// Valores válidos:
    ///   "venta"      = el producto fue vendido a un cliente (descuenta del stock).
    ///   "devolucion" = el cliente devolvió el producto (suma al stock).
    ///   "restock"    = llegó nueva mercancía al negocio (suma al stock).
    ///   "ajuste"     = corrección manual tras conteo físico (puede sumar o restar).
    /// [Required] = el tipo es obligatorio para saber qué pasó.
    /// [MaxLength(50)] = suficiente para los valores enumerados.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Tipo { get; set; }

    /// <summary>
    /// Número de unidades involucradas en el movimiento.
    /// Siempre es un valor positivo (sin importar si el movimiento sube o baja el stock).
    /// El sistema calcula si suma o resta según el Tipo de movimiento.
    /// Ejemplo: una venta de 3 unidades → Cantidad = 3, StockNuevo = StockAnterior - 3.
    /// </summary>
    public int Cantidad { get; set; }

    /// <summary>
    /// Precio por unidad al momento de la transacción (en moneda local).
    /// Se copia desde Producto.PrecioVenta al momento del movimiento.
    /// Si el precio cambia en el futuro, este campo conserva el precio histórico.
    /// Es 0 para ajustes de inventario que no involucran una transacción monetaria.
    /// </summary>
    public decimal PrecioUnitario { get; set; }

    /// <summary>
    /// Monto total de la transacción (Cantidad x PrecioUnitario).
    /// Ejemplo: 3 unidades a $50 c/u → Total = $150.
    /// Puede ser 0 para ajustes de inventario sin valor monetario asociado.
    /// Se usa para reportes de ingresos y valor de ventas por producto.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Cantidad de unidades en stock ANTES de este movimiento.
    /// Se guarda para poder auditar cualquier cambio y reconstruir el historial.
    /// Ejemplo: si había 10 unidades y se vendieron 3, StockAnterior = 10.
    /// </summary>
    public int StockAnterior { get; set; }

    /// <summary>
    /// Cantidad de unidades en stock DESPUÉS de este movimiento.
    /// Debe coincidir con Producto.Stock tras aplicar el movimiento.
    /// Ejemplo: si había 10 unidades y se vendieron 3, StockNuevo = 7.
    /// Se guarda para verificar consistencia y detectar discrepancias.
    /// </summary>
    public int StockNuevo { get; set; }

    /// <summary>
    /// Texto explicativo opcional sobre el motivo del movimiento.
    /// Para ajustes y devoluciones se recomienda (o requiere) escribir una razón.
    /// Ejemplos: "Conteo físico reveló diferencia", "Producto dañado devuelto por cliente".
    /// [MaxLength(500)] = suficiente para una explicación detallada.
    /// </summary>
    [MaxLength(500)]
    public string Nota { get; set; }

    /// <summary>
    /// Fecha y hora en que se realizó el movimiento de inventario.
    /// Se asigna automáticamente con TimeHelper.Now al crear el registro.
    /// No se modifica después (inmutabilidad del historial de inventario).
    /// Se usa para filtrar movimientos por fecha en reportes de inventario.
    /// </summary>
    public DateTime Fecha { get; set; } = TimeHelper.Now;
}
