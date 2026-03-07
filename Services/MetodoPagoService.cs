// ═══════════════════════════════════════════════════════════════════════════
// MetodoPagoService.cs — Implementación del servicio de métodos de pago
//
// RESPONSABILIDADES:
//   - CRUD de métodos de pago (alta, consulta, edición, baja lógica)
//   - Gestión de imagen QR en Base64
//   - Reordenamiento de métodos por drag & drop
//   - Gestión de método predeterminado (solo uno activo a la vez)
//
// MULTI-TENANT:
//   Todas las consultas filtran por NegocioId para garantizar que un negocio
//   nunca pueda acceder o modificar los métodos de pago de otro negocio.
//
// ELIMINACIÓN LÓGICA:
//   Los métodos de pago se marcan como IsActive = false en lugar de borrarse
//   físicamente. Esto preserva la referencia histórica en pagos pasados.
//   No se permite eliminar el último método activo del negocio.
// ═══════════════════════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Helpers;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gimnasio.Services;

/// <summary>
/// Servicio de gestión de métodos de pago de un negocio.
/// Maneja el ciclo de vida completo: desde la creación hasta la baja lógica,
/// incluyendo la imagen QR, el orden de visualización y el método predeterminado.
/// </summary>
public class MetodoPagoService : IMetodoPagoService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MetodoPagoService> _logger;

    /// <summary>
    /// El constructor recibe los servicios por inyección de dependencia.
    /// <paramref name="context"/> es el acceso a la base de datos (EF Core).
    /// <paramref name="logger"/> registra errores para diagnóstico en producción.
    /// </summary>
    public MetodoPagoService(ApplicationDbContext context, ILogger<MetodoPagoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Convierte Guid.Empty a null para que el admin (sin negocio) almacene NULL en la BD
    /// en vez de 00000000-0000-0000-0000-000000000000 (que rompería el FK).
    /// Los negocios reales pasan su Guid normal, que se mantiene sin cambio.
    /// </summary>
    private Guid? NormalizeNegocioId(Guid negocioId) => negocioId == Guid.Empty ? null : negocioId;

    // ═══════════════════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los métodos de pago activos del negocio.
    /// Se usa AsNoTracking porque es una consulta de solo lectura (no se van a
    /// modificar las entidades) — esto ahorra memoria y mejora rendimiento.
    /// El orden respeta el campo Orden configurado por el usuario via drag & drop.
    /// </summary>
    public async Task<List<MetodoPago>> GetMetodosPagoAsync(Guid negocioId)
    {
        var nId = NormalizeNegocioId(negocioId);
        return await _context.MetodosPago.AsNoTracking()
            .Where(m => m.NegocioId == nId && m.IsActive)
            .OrderBy(m => m.Orden)
            .ThenBy(m => m.FechaCreacion)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un método de pago específico por su ID.
    /// El filtro doble (MetodoPagoId + NegocioId + IsActive) garantiza que:
    ///   1. El método pertenezca al negocio del usuario autenticado (seguridad)
    ///   2. No se devuelvan métodos eliminados lógicamente
    /// </summary>
    public async Task<MetodoPago> GetMetodoPagoAsync(Guid negocioId, Guid metodoPagoId)
    {
        var nId = NormalizeNegocioId(negocioId);
        return await _context.MetodosPago.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MetodoPagoId == metodoPagoId
                                   && m.NegocioId == nId
                                   && m.IsActive);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CRUD
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo método de pago con validación exhaustiva.
    /// Las validaciones se hacen en el servicio (no en el modelo) para que
    /// los mensajes de error lleguen exactamente como están escritos aquí.
    ///
    /// Si el nuevo método se marca como predeterminado, primero se limpia el flag
    /// de todos los demás métodos activos del negocio (solo uno puede ser default).
    ///
    /// El orden se asigna automáticamente al final (maxOrden + 1) si no se
    /// especifica, para que aparezca como el último en la lista.
    /// </summary>
    public async Task<(bool success, string message, Guid? metodoPagoId)> CrearMetodoPagoAsync(Guid negocioId, MetodoPagoCreateDto dto)
    {
        try
        {
            // Validación de negocio — el nombre es obligatorio
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return (false, "El nombre del método de pago es obligatorio", null);

            var nId = NormalizeNegocioId(negocioId);

            // Si se marca como predeterminado, limpiar los demás primero
            if (dto.EsPredeterminado)
            {
                await ClearPredeterminadoAsync(negocioId);
            }

            // Obtener el orden máximo actual para asignar el siguiente si no se especificó
            var currentMaxOrder = await _context.MetodosPago
                .Where(m => m.NegocioId == nId && m.IsActive)
                .MaxAsync(m => (int?)m.Orden) ?? 0;

            var metodoPago = new MetodoPago
            {
                MetodoPagoId    = Guid.NewGuid(),
                NegocioId       = nId,
                Nombre          = dto.Nombre.Trim(),
                NumeroCuenta    = dto.NumeroCuenta?.Trim(),
                Cedula          = dto.Cedula?.Trim(),
                NombreTitular   = dto.NombreTitular?.Trim(),
                Instrucciones   = dto.Instrucciones?.Trim(),
                EsPredeterminado = dto.EsPredeterminado,
                IsActive        = true,
                Orden           = dto.Orden > 0 ? dto.Orden : currentMaxOrder + 1,
                FechaCreacion   = TimeHelper.Now
            };

            _context.MetodosPago.Add(metodoPago);
            await _context.SaveChangesAsync();

            return (true, "Método de pago creado exitosamente", metodoPago.MetodoPagoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear método de pago para negocio {NegocioId}", negocioId);
            return (false, "Error al crear el método de pago", null);
        }
    }

    /// <summary>
    /// Edita un método de pago existente.
    /// Busca por MetodoPagoId + NegocioId para seguridad multi-tenant.
    /// Si se establece como predeterminado, primero limpia el flag de los demás.
    /// </summary>
    public async Task<(bool success, string message)> EditarMetodoPagoAsync(Guid negocioId, MetodoPagoCreateDto dto)
    {
        try
        {
            var nId = NormalizeNegocioId(negocioId);
            var metodoPago = await _context.MetodosPago
                .FirstOrDefaultAsync(m => m.MetodoPagoId == dto.MetodoPagoId
                                       && m.NegocioId == nId
                                       && m.IsActive);

            if (metodoPago == null)
                return (false, "Método de pago no encontrado");

            // Si se marca como predeterminado y antes no lo era, limpiar los demás
            if (dto.EsPredeterminado && !metodoPago.EsPredeterminado)
            {
                await ClearPredeterminadoAsync(negocioId);
            }

            // Validación de negocio — el nombre es obligatorio
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return (false, "El nombre del método de pago es obligatorio");

            // Actualizar campos desde el DTO
            metodoPago.Nombre           = dto.Nombre.Trim();
            metodoPago.NumeroCuenta     = dto.NumeroCuenta?.Trim();
            metodoPago.Cedula           = dto.Cedula?.Trim();
            metodoPago.NombreTitular    = dto.NombreTitular?.Trim();
            metodoPago.Instrucciones    = dto.Instrucciones?.Trim();
            metodoPago.EsPredeterminado = dto.EsPredeterminado;

            _context.Update(metodoPago);
            await _context.SaveChangesAsync();

            return (true, "Método de pago actualizado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar método de pago {MetodoPagoId} para negocio {NegocioId}",
                dto.MetodoPagoId, negocioId);
            return (false, "Error al actualizar el método de pago");
        }
    }

    /// <summary>
    /// Elimina un método de pago de forma LÓGICA estableciendo IsActive = false.
    ///
    /// Por qué no se elimina físicamente (DELETE):
    ///   - Los pagos históricos pueden referenciar este método
    ///   - Borrar el método rompería la trazabilidad de pagos pasados
    ///
    /// Restricción: no se permite eliminar el último método activo del negocio
    /// porque siempre debe existir al menos una forma de pago disponible.
    ///
    /// Si el método eliminado era el predeterminado, se asigna el flag al
    /// primer método activo restante para que el negocio no quede sin default.
    /// </summary>
    public async Task<(bool success, string message)> EliminarMetodoPagoAsync(Guid negocioId, Guid metodoPagoId)
    {
        try
        {
            var nId = NormalizeNegocioId(negocioId);
            var metodoPago = await _context.MetodosPago
                .FirstOrDefaultAsync(m => m.MetodoPagoId == metodoPagoId
                                       && m.NegocioId == nId
                                       && m.IsActive);

            if (metodoPago == null)
                return (false, "Método de pago no encontrado");

            // Contar cuántos métodos activos tiene el negocio
            var totalActivos = await _context.MetodosPago
                .CountAsync(m => m.NegocioId == nId && m.IsActive);

            // No permitir eliminar el último método activo
            if (totalActivos <= 1)
                return (false, "No se puede eliminar el único método de pago activo. Debe existir al menos uno.");

            var eraPredeterminado = metodoPago.EsPredeterminado;

            // Baja lógica: el registro queda en la base de datos pero invisible en listados
            metodoPago.IsActive = false;
            metodoPago.EsPredeterminado = false;
            _context.Update(metodoPago);

            // Si era el predeterminado, asignar el flag al primer método activo restante
            // Todo se guarda en un único SaveChangesAsync para garantizar atomicidad
            if (eraPredeterminado)
            {
                var primerActivo = await _context.MetodosPago
                    .Where(m => m.NegocioId == nId && m.IsActive && m.MetodoPagoId != metodoPagoId)
                    .OrderBy(m => m.Orden)
                    .FirstOrDefaultAsync();

                if (primerActivo != null)
                {
                    primerActivo.EsPredeterminado = true;
                    _context.Update(primerActivo);
                }
            }

            await _context.SaveChangesAsync();

            return (true, "Método de pago eliminado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar método de pago {MetodoPagoId} para negocio {NegocioId}",
                metodoPagoId, negocioId);
            return (false, "Error al eliminar el método de pago");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IMAGEN QR
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sube una imagen QR codificada en Base64 al método de pago.
    /// La imagen se almacena directamente en la base de datos como string Base64,
    /// siguiendo el mismo patrón usado en el resto de la plataforma para
    /// imágenes de negocios (evita dependencia de almacenamiento externo).
    /// </summary>
    public async Task<(bool success, string message)> SubirImagenQRAsync(Guid negocioId, Guid metodoPagoId, string base64Image)
    {
        try
        {
            var nId = NormalizeNegocioId(negocioId);
            var metodoPago = await _context.MetodosPago
                .FirstOrDefaultAsync(m => m.MetodoPagoId == metodoPagoId
                                       && m.NegocioId == nId
                                       && m.IsActive);

            if (metodoPago == null)
                return (false, "Método de pago no encontrado");

            metodoPago.ImagenQR = base64Image;

            _context.Update(metodoPago);
            await _context.SaveChangesAsync();

            return (true, "Imagen QR actualizada exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir imagen QR para método de pago {MetodoPagoId} del negocio {NegocioId}",
                metodoPagoId, negocioId);
            return (false, "Error al subir la imagen QR");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ORDENAMIENTO Y PREDETERMINADO
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reordena los métodos de pago según el orden de IDs recibido desde el frontend
    /// (resultado de un drag &amp; drop). La posición en la lista = nuevo valor de Orden.
    ///
    /// Patrón idéntico al usado en ReordenarCategoriasAsync de InventarioService:
    /// recibe la lista de IDs en el nuevo orden y asigna Orden = posición + 1.
    /// </summary>
    public async Task<(bool success, string message)> ReordenarMetodosPagoAsync(Guid negocioId, List<Guid> orderedIds)
    {
        try
        {
            if (orderedIds == null || orderedIds.Count == 0)
                return (false, "La lista de métodos de pago está vacía");

            var nId = NormalizeNegocioId(negocioId);
            // Cargar todos los métodos activos que están en la lista de IDs
            var metodos = await _context.MetodosPago
                .Where(m => m.NegocioId == nId
                          && m.IsActive
                          && orderedIds.Contains(m.MetodoPagoId))
                .ToListAsync();

            // Asignar el nuevo orden basado en la posición en la lista
            for (int i = 0; i < orderedIds.Count; i++)
            {
                var id = orderedIds[i];
                var metodo = metodos.FirstOrDefault(m => m.MetodoPagoId == id);
                if (metodo != null)
                    metodo.Orden = i + 1;
            }

            await _context.SaveChangesAsync();

            return (true, "Métodos de pago reordenados exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reordenar métodos de pago para negocio {NegocioId}", negocioId);
            return (false, "Error al reordenar los métodos de pago");
        }
    }

    /// <summary>
    /// Establece un método de pago como predeterminado.
    /// Primero limpia el flag de todos los demás métodos activos del negocio,
    /// luego activa el flag en el método seleccionado.
    /// Solo un método puede ser predeterminado a la vez.
    /// </summary>
    public async Task<(bool success, string message)> SetPredeterminadoAsync(Guid negocioId, Guid metodoPagoId)
    {
        try
        {
            var nId = NormalizeNegocioId(negocioId);
            var metodoPago = await _context.MetodosPago
                .FirstOrDefaultAsync(m => m.MetodoPagoId == metodoPagoId
                                       && m.NegocioId == nId
                                       && m.IsActive);

            if (metodoPago == null)
                return (false, "Método de pago no encontrado");

            // Limpiar el flag predeterminado de todos los demás
            await ClearPredeterminadoAsync(negocioId);

            // Establecer este como predeterminado
            metodoPago.EsPredeterminado = true;

            _context.Update(metodoPago);
            await _context.SaveChangesAsync();

            return (true, $"{metodoPago.Nombre} establecido como método de pago predeterminado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al establecer predeterminado el método de pago {MetodoPagoId} del negocio {NegocioId}",
                metodoPagoId, negocioId);
            return (false, "Error al establecer el método de pago predeterminado");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene estadísticas de ventas agrupadas por método de pago.
    ///
    /// Combina dos fuentes de datos para evitar doble conteo:
    ///   1. OrdenVenta: ventas POS (tienda/restaurante) — tienen un total a nivel de orden
    ///   2. MovimientoInventario tipo "venta": ventas rápidas individuales (membresias)
    ///      que NO crean una OrdenVenta (las de tipo "venta_tienda" se excluyen porque
    ///      ya están reflejadas en OrdenVenta).
    ///
    /// Periodos:
    ///   - Hoy: desde las 00:00 de hoy
    ///   - Semana: desde el domingo de esta semana
    ///   - Mes: desde el primer día del mes actual
    ///
    /// Siempre incluye "Efectivo" aunque no haya ventas con ese método.
    /// </summary>
    public async Task<List<MetodoPagoStatsDto>> GetPaymentStatsByMethodAsync(Guid negocioId)
    {
        try
        {
            var now = TimeHelper.Now;
            var startOfDay = now.Date;
            var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek); // Domingo = inicio de semana
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            // ── Fuente 1: OrdenVenta (ventas POS) ────────────────────────────────
            // Filtramos desde inicio de mes y luego subdividimos hoy/semana en memoria
            var ordenVentaStats = await _context.OrdenesVenta.AsNoTracking()
                .Where(o => o.NegocioId == negocioId && o.FechaCreacion >= startOfMonth)
                .GroupBy(o => o.MetodoPago ?? "Efectivo")
                .Select(g => new
                {
                    MetodoPago = g.Key,
                    TotalHoy = g.Where(o => o.FechaCreacion >= startOfDay).Sum(o => o.Total),
                    TotalSemana = g.Where(o => o.FechaCreacion >= startOfWeek).Sum(o => o.Total),
                    TotalMes = g.Sum(o => o.Total),
                    CantidadHoy = g.Count(o => o.FechaCreacion >= startOfDay),
                    CantidadSemana = g.Count(o => o.FechaCreacion >= startOfWeek),
                    CantidadMes = g.Count()
                })
                .ToListAsync();

            // ── Fuente 2: MovimientoInventario tipo "venta" (ventas rápidas) ─────
            // Excluimos "venta_tienda" porque esas ventas ya están contadas en OrdenVenta.
            // Solo tomamos tipo "venta" que son ventas rápidas sin orden POS.
            var movimientoStats = await _context.MovimientosInventario.AsNoTracking()
                .Where(m => m.NegocioId == negocioId
                          && m.Tipo == "venta"
                          && m.Fecha >= startOfMonth
                          && m.MetodoPago != null)
                .GroupBy(m => m.MetodoPago!)
                .Select(g => new
                {
                    MetodoPago = g.Key,
                    TotalHoy = g.Where(m => m.Fecha >= startOfDay).Sum(m => m.Total),
                    TotalSemana = g.Where(m => m.Fecha >= startOfWeek).Sum(m => m.Total),
                    TotalMes = g.Sum(m => m.Total),
                    CantidadHoy = g.Count(m => m.Fecha >= startOfDay),
                    CantidadSemana = g.Count(m => m.Fecha >= startOfWeek),
                    CantidadMes = g.Count()
                })
                .ToListAsync();

            // ── Fuente 3: PagoCita (pagos de citas de negocios artesanales) ──────
            // Los negocios artesanales registran cobros en PagoCita al completar citas.
            // Esta tabla tiene NegocioId directo, por lo que no se necesita JOIN.
            // Se usa FechaCreacion (fecha de registro del pago) como referencia temporal.
            var pagoCitaStats = await _context.PagosCita.AsNoTracking()
                .Where(p => p.NegocioId == negocioId && p.FechaCreacion >= startOfMonth)
                .GroupBy(p => p.MetodoPago ?? "Efectivo")
                .Select(g => new
                {
                    MetodoPago = g.Key,
                    TotalHoy = g.Where(p => p.FechaCreacion >= startOfDay).Sum(p => p.Total),
                    TotalSemana = g.Where(p => p.FechaCreacion >= startOfWeek).Sum(p => p.Total),
                    TotalMes = g.Sum(p => p.Total),
                    CantidadHoy = g.Count(p => p.FechaCreacion >= startOfDay),
                    CantidadSemana = g.Count(p => p.FechaCreacion >= startOfWeek),
                    CantidadMes = g.Count()
                })
                .ToListAsync();

            // ── Fuente 4: Logs de membresías (cliente_creado, cliente_renovado) ──
            // Los negocios de membresías registran pagos en la tabla Logs con
            // tipo "cliente_creado" o "cliente_renovado" y MetodoPago no nulo.
            var logMembresiaStats = await _context.Logs.AsNoTracking()
                .Where(l => l.NegocioId == negocioId
                          && l.MetodoPago != null
                          && l.Monto > 0
                          && l.Fecha >= startOfMonth
                          && (l.Tipo == "cliente_creado" || l.Tipo == "cliente_renovado"))
                .GroupBy(l => l.MetodoPago!)
                .Select(g => new
                {
                    MetodoPago = g.Key,
                    TotalHoy = g.Where(l => l.Fecha >= startOfDay).Sum(l => l.Monto),
                    TotalSemana = g.Where(l => l.Fecha >= startOfWeek).Sum(l => l.Monto),
                    TotalMes = g.Sum(l => l.Monto),
                    CantidadHoy = g.Count(l => l.Fecha >= startOfDay),
                    CantidadSemana = g.Count(l => l.Fecha >= startOfWeek),
                    CantidadMes = g.Count()
                })
                .ToListAsync();

            // ── Merge: combinar todas las fuentes sin doble conteo ──────────────
            var allStats = new Dictionary<string, MetodoPagoStatsDto>();

            // Agregar stats de OrdenVenta
            foreach (var s in ordenVentaStats)
            {
                allStats[s.MetodoPago] = new MetodoPagoStatsDto
                {
                    MetodoPago = s.MetodoPago,
                    TotalHoy = s.TotalHoy,
                    TotalSemana = s.TotalSemana,
                    TotalMes = s.TotalMes,
                    CantidadHoy = s.CantidadHoy,
                    CantidadSemana = s.CantidadSemana,
                    CantidadMes = s.CantidadMes
                };
            }

            // Sumar stats de MovimientoInventario (ventas rápidas sin orden POS)
            foreach (var s in movimientoStats)
            {
                if (allStats.TryGetValue(s.MetodoPago, out var existing))
                {
                    // Sumar al método existente
                    existing.TotalHoy += s.TotalHoy;
                    existing.TotalSemana += s.TotalSemana;
                    existing.TotalMes += s.TotalMes;
                    existing.CantidadHoy += s.CantidadHoy;
                    existing.CantidadSemana += s.CantidadSemana;
                    existing.CantidadMes += s.CantidadMes;
                }
                else
                {
                    // Nuevo método de pago solo de ventas rápidas
                    allStats[s.MetodoPago] = new MetodoPagoStatsDto
                    {
                        MetodoPago = s.MetodoPago,
                        TotalHoy = s.TotalHoy,
                        TotalSemana = s.TotalSemana,
                        TotalMes = s.TotalMes,
                        CantidadHoy = s.CantidadHoy,
                        CantidadSemana = s.CantidadSemana,
                        CantidadMes = s.CantidadMes
                    };
                }
            }

            // Sumar stats de PagoCita (citas completadas en negocios artesanales)
            foreach (var s in pagoCitaStats)
            {
                if (allStats.TryGetValue(s.MetodoPago, out var existing))
                {
                    // El método ya existe (puede haberse registrado también en otras fuentes)
                    existing.TotalHoy += s.TotalHoy;
                    existing.TotalSemana += s.TotalSemana;
                    existing.TotalMes += s.TotalMes;
                    existing.CantidadHoy += s.CantidadHoy;
                    existing.CantidadSemana += s.CantidadSemana;
                    existing.CantidadMes += s.CantidadMes;
                }
                else
                {
                    // Método de pago exclusivo de citas artesanales
                    allStats[s.MetodoPago] = new MetodoPagoStatsDto
                    {
                        MetodoPago = s.MetodoPago,
                        TotalHoy = s.TotalHoy,
                        TotalSemana = s.TotalSemana,
                        TotalMes = s.TotalMes,
                        CantidadHoy = s.CantidadHoy,
                        CantidadSemana = s.CantidadSemana,
                        CantidadMes = s.CantidadMes
                    };
                }
            }

            // Sumar stats de Logs de membresías (cliente_creado + cliente_renovado)
            foreach (var s in logMembresiaStats)
            {
                if (allStats.TryGetValue(s.MetodoPago, out var existing))
                {
                    existing.TotalHoy += s.TotalHoy;
                    existing.TotalSemana += s.TotalSemana;
                    existing.TotalMes += s.TotalMes;
                    existing.CantidadHoy += s.CantidadHoy;
                    existing.CantidadSemana += s.CantidadSemana;
                    existing.CantidadMes += s.CantidadMes;
                }
                else
                {
                    allStats[s.MetodoPago] = new MetodoPagoStatsDto
                    {
                        MetodoPago = s.MetodoPago,
                        TotalHoy = s.TotalHoy,
                        TotalSemana = s.TotalSemana,
                        TotalMes = s.TotalMes,
                        CantidadHoy = s.CantidadHoy,
                        CantidadSemana = s.CantidadSemana,
                        CantidadMes = s.CantidadMes
                    };
                }
            }

            // Siempre incluir "Efectivo" aunque no haya ventas con ese método
            if (!allStats.ContainsKey("Efectivo"))
                allStats["Efectivo"] = new MetodoPagoStatsDto { MetodoPago = "Efectivo" };

            return allStats.Values.OrderByDescending(s => s.TotalMes).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de métodos de pago para negocio {NegocioId}", negocioId);
            return new List<MetodoPagoStatsDto>
            {
                new() { MetodoPago = "Efectivo" }
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MÉTODOS PRIVADOS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Limpia el flag EsPredeterminado de todos los métodos de pago activos
    /// de un negocio. Se llama antes de asignar un nuevo predeterminado
    /// para garantizar que solo uno tenga el flag activo a la vez.
    /// </summary>
    private async Task ClearPredeterminadoAsync(Guid negocioId)
    {
        var nId = NormalizeNegocioId(negocioId);
        var metodosConFlag = await _context.MetodosPago
            .Where(m => m.NegocioId == nId && m.IsActive && m.EsPredeterminado)
            .ToListAsync();

        foreach (var metodo in metodosConFlag)
        {
            metodo.EsPredeterminado = false;
        }

        if (metodosConFlag.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }
}
