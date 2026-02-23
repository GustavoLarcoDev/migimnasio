// =====================================================================
// MetodoPagoStatsDto.cs -- DTO para estadisticas de ventas por metodo de pago
//
// Agrupa totales y cantidades de transacciones por metodo de pago
// en tres periodos: hoy, esta semana y este mes.
//
// Se alimenta de dos fuentes:
//   1. OrdenVenta (ventas POS de tienda/restaurante)
//   2. MovimientoInventario tipo "venta" (ventas rapidas individuales)
// =====================================================================

namespace Gimnasio.Models.DTOs;

/// <summary>
/// Estadisticas de ventas agrupadas por un metodo de pago especifico.
/// Incluye montos totales y cantidades de transacciones para hoy, semana y mes.
/// </summary>
public class MetodoPagoStatsDto
{
    /// <summary>
    /// Nombre del metodo de pago (ej: "Efectivo", "Banco Pichincha", "Tarjeta Visa").
    /// </summary>
    public string MetodoPago { get; set; } = "Efectivo";

    /// <summary>
    /// Monto total de ventas realizadas HOY con este metodo de pago.
    /// </summary>
    public decimal TotalHoy { get; set; }

    /// <summary>
    /// Monto total de ventas realizadas ESTA SEMANA con este metodo de pago.
    /// La semana comienza el domingo (DayOfWeek = 0).
    /// </summary>
    public decimal TotalSemana { get; set; }

    /// <summary>
    /// Monto total de ventas realizadas ESTE MES con este metodo de pago.
    /// </summary>
    public decimal TotalMes { get; set; }

    /// <summary>
    /// Cantidad de transacciones realizadas HOY con este metodo de pago.
    /// </summary>
    public int CantidadHoy { get; set; }

    /// <summary>
    /// Cantidad de transacciones realizadas ESTA SEMANA con este metodo de pago.
    /// </summary>
    public int CantidadSemana { get; set; }

    /// <summary>
    /// Cantidad de transacciones realizadas ESTE MES con este metodo de pago.
    /// </summary>
    public int CantidadMes { get; set; }
}
