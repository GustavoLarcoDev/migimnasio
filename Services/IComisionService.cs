// ═══════════════════════════════════════════════════════════
// IComisionService.cs — Contrato del servicio de comisiones de vendedores
//
// Define las operaciones para gestionar las comisiones que se generan
// automáticamente cuando un vendedor crea un negocio que cumple los
// requisitos mínimos ($15+ de precio, 30+ días contratados).
//
// REGLAS DE COMISIÓN:
//   - Solo se genera si precioNegocio >= $15 Y diasContratados >= 30
//   - Comisión fija de $5 por negocio (sin importar la duración)
//   - Solo UNA comisión por negocio (sin duplicados en renovaciones)
//
// FLUJO DE PAGO:
//   - La comisión nace con Pagada = false (pendiente)
//   - El admin ve las comisiones pendientes por vendedor
//   - Al transferir el dinero, marca las comisiones como Pagada = true
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Services;

public interface IComisionService
{
    /// <summary>
    /// Genera una comisión para el vendedor si el negocio cumple los requisitos mínimos.
    /// Solo se genera si precioNegocio >= 15 Y diasContratados >= 30.
    /// Comisión fija de $5 por negocio. Solo una comisión por negocio (sin duplicados).
    /// </summary>
    /// <param name="vendedorId">ID del vendedor que creó el negocio.</param>
    /// <param name="vendedorNombre">Nombre del vendedor (denormalizado para historial).</param>
    /// <param name="negocioId">ID del negocio creado.</param>
    /// <param name="negocioNombre">Nombre del negocio (denormalizado para historial).</param>
    /// <param name="diasContratados">Cantidad de días contratados (mínimo 30 para generar comisión).</param>
    /// <param name="precioNegocio">Precio de suscripción del negocio (mínimo $15 para generar comisión).</param>
    Task GenerarComisionNuevaNegocioAsync(Guid vendedorId, string vendedorNombre, Guid negocioId, string negocioNombre, int diasContratados, decimal precioNegocio);

    /// <summary>
    /// Obtiene todas las comisiones pendientes de pago (Pagada = false),
    /// agrupadas por vendedor con el total acumulado de cada uno.
    /// Usado por el admin para saber a quién le debe pagar y cuánto.
    /// </summary>
    /// <returns>Lista de grupos por vendedor con sus comisiones pendientes y totales.</returns>
    Task<object> GetComisionesPendientesAsync();

    /// <summary>
    /// Obtiene un resumen de comisiones por vendedor: cuánto tiene pendiente,
    /// cuánto ya fue pagado y cuántos negocios ha generado en total.
    /// Usado para las tarjetas de resumen del panel de comisiones del admin.
    /// </summary>
    /// <returns>Lista de vendedores con sus métricas de comisiones.</returns>
    Task<object> GetResumenComisionesAsync();

    /// <summary>
    /// Paga todas las comisiones pendientes de un vendedor específico:
    /// marca todas sus comisiones con Pagada = true y FechaPago = ahora.
    /// Retorna el total pagado y el detalle de los negocios involucrados.
    /// </summary>
    /// <param name="vendedorId">ID del vendedor a quien se le pagan las comisiones.</param>
    /// <returns>
    /// Tupla con:
    ///   - success: true si hubo comisiones pendientes y se pagaron correctamente.
    ///   - message: descripción del resultado de la operación.
    ///   - totalPagado: suma de todas las comisiones que se marcaron como pagadas.
    ///   - detalleNegocios: lista de negocios cuyas comisiones se pagaron en esta operación.
    /// </returns>
    Task<(bool success, string message, decimal totalPagado, List<object> detalleNegocios)> PagarComisionesVendedorAsync(Guid vendedorId);

    /// <summary>
    /// Obtiene el historial de todas las comisiones ya pagadas (Pagada = true),
    /// ordenadas por FechaPago descendente (las más recientes primero).
    /// Sirve como registro contable de los pagos realizados a vendedores.
    /// </summary>
    /// <returns>Lista de comisiones pagadas con fecha, vendedor y monto.</returns>
    Task<object> GetHistorialComisionesAsync();
}
