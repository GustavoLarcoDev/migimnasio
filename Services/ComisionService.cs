// ═══════════════════════════════════════════════════════════
// ComisionService.cs — Implementación del servicio de comisiones
//
// Gestiona las comisiones que se generan automáticamente cada vez
// que un vendedor crea un negocio con precio >= $15 y >= 30 días.
//
// FÓRMULA DE CÁLCULO:
//   Comisión fija de $5 por negocio (sin importar la duración).
//   Solo se genera UNA comisión por negocio (sin duplicados en renovaciones).
//
// REQUISITOS:
//   precioNegocio >= $15 Y diasContratados >= 30
//
// FLUJO DE ESTADOS:
//   Pagada = false → comisión pendiente, aparece en el panel del admin
//   Pagada = true  → comisión ya transferida al vendedor, pasa al historial
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Implementación concreta de IComisionService.
/// Usa ApplicationDbContext para leer y escribir registros de comisiones en la BD.
/// </summary>
public class ComisionService : IComisionService
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor: recibe el DbContext por inyección de dependencias.
    /// </summary>
    public ComisionService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // GENERACIÓN DE COMISIONES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Genera una comisión para el vendedor si el negocio cumple los requisitos mínimos.
    ///
    /// Requisitos para generar comisión:
    ///   1. precioNegocio >= $15 (filtro de precio mínimo para que sea rentable)
    ///   2. diasContratados >= 30 (mínimo un mes de contrato)
    ///   3. No debe existir ya una comisión para este negocio (previene duplicados en renovaciones)
    ///
    /// Si no cumple los requisitos, el método no hace nada (sin comisión).
    ///
    /// Fórmula: comisión fija de $5 por negocio.
    /// </summary>
    public async Task GenerarComisionNuevaNegocioAsync(
        Guid vendedorId, string vendedorNombre,
        Guid negocioId, string negocioNombre,
        int diasContratados, decimal precioNegocio)
    {
        // ─── Validación de requisitos mínimos ─────────────────────────────
        // No generar comisión si el precio o los días no superan el umbral mínimo.
        // Esto evita generar comisiones por negocios en prueba o a precio simbólico.
        if (precioNegocio < 15m || diasContratados < 30)
            return;

        // ─── Prevenir duplicados ────────────────────────────────────────
        // Solo se genera UNA comisión por negocio. Si el negocio se renueva,
        // no se genera una nueva comisión.
        var yaExiste = await _context.ComisionesVendedor.AsNoTracking()
            .AnyAsync(c => c.NegocioId == negocioId);
        if (yaExiste)
            return;

        // ─── Monto fijo ─────────────────────────────────────────────────
        var montoComision = 5m; // Comisión fija de $5 por negocio

        // ─── Persistir el registro de comisión ───────────────────────────
        var comision = new ComisionVendedor
        {
            Id = Guid.NewGuid(),
            VendedorId = vendedorId,
            NegocioId = negocioId,
            NombreVendedor = vendedorNombre,
            NombreNegocio = negocioNombre,
            MontoComision = montoComision,
            TipoComision = "nueva",
            DiasContratados = diasContratados,
            PrecioNegocio = precioNegocio,
            Pagada = false,
            FechaCreacion = TimeHelper.Now
        };

        _context.ComisionesVendedor.Add(comision);
        await _context.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS PARA EL PANEL DE ADMIN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todas las comisiones pendientes de pago agrupadas por vendedor.
    ///
    /// El resultado es una lista de grupos donde cada elemento contiene:
    ///   - Datos del vendedor (ID, nombre)
    ///   - Total acumulado de comisiones pendientes
    ///   - Lista de comisiones individuales con el detalle de cada negocio
    ///
    /// Solo se incluyen comisiones con Pagada = false.
    /// Los grupos están ordenados por el vendedor con más comisiones primero.
    /// </summary>
    public async Task<object> GetComisionesPendientesAsync()
    {
        // Traer todas las comisiones pendientes en memoria para agrupar con LINQ.
        // En producción el volumen será bajo (cientos, no miles), así que es eficiente.
        var comisiones = await _context.ComisionesVendedor.AsNoTracking()
            .Where(c => !c.Pagada)
            .OrderBy(c => c.NombreVendedor)
            .ThenByDescending(c => c.FechaCreacion)
            .ToListAsync();

        // Cargar datos bancarios de vendedores para incluirlos en la respuesta
        var vendedorIds = comisiones.Select(c => c.VendedorId).Distinct().ToList();
        var vendedores = await _context.Vendedores.AsNoTracking()
            .Where(v => vendedorIds.Contains(v.VendedorId))
            .ToDictionaryAsync(v => v.VendedorId);

        // Agrupar por vendedor y calcular totales con LINQ to Objects
        var agrupado = comisiones
            .GroupBy(c => new { c.VendedorId, c.NombreVendedor })
            .Select(g =>
            {
                vendedores.TryGetValue(g.Key.VendedorId, out var vendedor);
                return new
                {
                    vendedorId = g.Key.VendedorId,
                    nombreVendedor = g.Key.NombreVendedor,
                    totalPendiente = g.Sum(c => c.MontoComision),
                    cantidadPendientes = g.Count(),
                    // Datos bancarios del vendedor para que el admin sepa dónde transferir
                    nombreBanco = vendedor?.NombreBanco ?? "",
                    numeroCedula = vendedor?.NumeroCedula ?? "",
                    numeroCuenta = vendedor?.NumeroCuenta ?? "",
                    comisiones = g.Select(c => new
                    {
                        c.Id,
                        c.NegocioId,
                        c.NombreNegocio,
                        c.MontoComision,
                        c.TipoComision,
                        c.DiasContratados,
                        c.PrecioNegocio,
                        // Formatear fechas en español para mostrar en la UI
                        fechaCreacion = c.FechaCreacion.ToString("dd/MM/yyyy"),
                        fechaCreacionCompleta = c.FechaCreacion.ToString("dd/MM/yyyy HH:mm")
                    }).ToList()
                };
            })
            // Los vendedores con mayor deuda pendiente aparecen primero
            .OrderByDescending(g => g.totalPendiente)
            .ToList();

        return agrupado;
    }

    /// <summary>
    /// Obtiene un resumen de las comisiones de cada vendedor:
    /// cuánto tiene pendiente de cobrar, cuánto ya fue pagado históricamente,
    /// y cuántos negocios ha generado en total.
    ///
    /// Este resumen alimenta las tarjetas de estadísticas del panel de comisiones.
    /// Incluye a TODOS los vendedores que hayan generado al menos una comisión,
    /// sin importar si tienen pendientes o no.
    /// </summary>
    public async Task<object> GetResumenComisionesAsync()
    {
        // Cargar todas las comisiones de una vez para hacer los cálculos en memoria.
        // Es más eficiente que hacer múltiples queries para pendientes y pagadas por separado.
        var todasLasComisiones = await _context.ComisionesVendedor.AsNoTracking()
            .ToListAsync();

        // Agrupar por vendedor y calcular los tres totales que necesita el resumen
        var resumen = todasLasComisiones
            .GroupBy(c => new { c.VendedorId, c.NombreVendedor })
            .Select(g => new
            {
                vendedorId = g.Key.VendedorId,
                nombreVendedor = g.Key.NombreVendedor,
                // Dinero que aún no ha sido transferido al vendedor
                montoPendiente = g.Where(c => !c.Pagada).Sum(c => c.MontoComision),
                // Dinero que ya fue transferido históricamente
                montoPagado = g.Where(c => c.Pagada).Sum(c => c.MontoComision),
                // Ganancia total histórica del vendedor (pagada + pendiente)
                totalGeneral = g.Sum(c => c.MontoComision),
                // Cuántos negocios ha convertido este vendedor
                totalNegocios = g.Count()
            })
            // Ordenar por quienes más han generado (métrica de rendimiento)
            .OrderByDescending(r => r.totalGeneral)
            .ToList();

        return resumen;
    }

    /// <summary>
    /// Paga todas las comisiones pendientes de un vendedor específico.
    ///
    /// Operación atómica: todas las comisiones pendientes del vendedor se marcan
    /// como pagadas en el mismo SaveChangesAsync, garantizando consistencia.
    ///
    /// Retorna el total pagado y el detalle de los negocios involucrados para
    /// que el admin pueda confirmar el desglose antes de transferir el dinero.
    ///
    /// Falla si el vendedor no tiene comisiones pendientes (nada que pagar).
    /// </summary>
    public async Task<(bool success, string message, decimal totalPagado, List<object> detalleNegocios)> PagarComisionesVendedorAsync(Guid vendedorId)
    {
        // Cargar todas las comisiones pendientes del vendedor
        var pendientes = await _context.ComisionesVendedor
            .Where(c => c.VendedorId == vendedorId && !c.Pagada)
            .ToListAsync();

        // Si no hay pendientes, no hay nada que pagar
        if (!pendientes.Any())
            return (false, "Este vendedor no tiene comisiones pendientes de pago", 0m, new List<object>());

        var fechaPago = TimeHelper.Now;
        var totalPagado = pendientes.Sum(c => c.MontoComision);

        // Preparar el detalle ANTES de marcar como pagado (mismo datos, más cómodo)
        var detalleNegocios = pendientes.Select(c => (object)new
        {
            c.NombreNegocio,
            c.MontoComision,
            c.DiasContratados,
            c.PrecioNegocio,
            c.TipoComision,
            fechaCreacion = c.FechaCreacion.ToString("dd/MM/yyyy")
        }).ToList();

        // Marcar todas como pagadas en una sola operación (eficiente: no hace roundtrip por cada una)
        foreach (var comision in pendientes)
        {
            comision.Pagada = true;
            comision.FechaPago = fechaPago;
        }

        // Persistir todos los cambios en una sola transacción
        await _context.SaveChangesAsync();

        return (
            true,
            $"Se pagaron {pendientes.Count} comisiones por un total de ${totalPagado:F2}",
            totalPagado,
            detalleNegocios
        );
    }

    /// <summary>
    /// Obtiene el historial completo de comisiones ya pagadas (Pagada = true).
    ///
    /// Ordenado por FechaPago descendente para que los pagos más recientes
    /// aparezcan primero en la tabla del panel de admin.
    ///
    /// Cada fila muestra: fecha de pago, vendedor, negocio y monto,
    /// permitiendo reconstruir el historial contable de pagos a vendedores.
    /// </summary>
    public async Task<object> GetHistorialComisionesAsync()
    {
        return await _context.ComisionesVendedor.AsNoTracking()
            .Where(c => c.Pagada)
            .OrderByDescending(c => c.FechaPago)
            .Select(c => new
            {
                c.Id,
                c.VendedorId,
                c.NombreVendedor,
                c.NegocioId,
                c.NombreNegocio,
                c.MontoComision,
                c.TipoComision,
                c.DiasContratados,
                c.PrecioNegocio,
                fechaPago = c.FechaPago.HasValue
                    ? c.FechaPago.Value.ToString("dd/MM/yyyy HH:mm")
                    : "",
                fechaCreacion = c.FechaCreacion.ToString("dd/MM/yyyy")
            })
            .ToListAsync();
    }
}
