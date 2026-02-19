// ═══════════════════════════════════════════════════════════
// LogService.cs — Servicio de registros/logs financieros
//
// CONCEPTO: Los Logs son la FUENTE INMUTABLE DE VERDAD FINANCIERA.
// Cada vez que ocurre un movimiento de dinero (cliente paga, se hace
// un ajuste, se registra un gasto), se crea un log. Los logs no se
// borran al eliminar un cliente — el historial financiero persiste.
//
// DOS TIPOS DE LOG:
//   - Automáticos: generados por ClienteService e InventarioService
//     al crear/editar/renovar/eliminar clientes o productos.
//   - Manuales: el dueño del negocio los crea desde el dashboard
//     para registrar ingresos o gastos adicionales.
//
// CONVENCIÓN DE MONTOS:
//   - monto > 0: ingreso (pago de membresía, venta de producto, etc.)
//   - monto < 0: gasto (restock, devolución, gasto manual)
//   - monto = 0: evento sin movimiento de dinero (login, eliminación, etc.)
//
// EXPORTACIÓN EXCEL: Genera un archivo con todos los logs más un resumen
// al final con total ingresos, total gastos y balance neto.
// ═══════════════════════════════════════════════════════════

using ClosedXML.Excel;
using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Implementación del servicio de registros financieros. Maneja la creación
/// automática y manual de logs, consultas, edición, eliminación y exportación Excel.
///
/// Los logs son la fuente principal para calcular ingresos y gastos en
/// <see cref="VentasService"/> y en los resúmenes de <see cref="DailyReportService"/>.
/// </summary>
public class LogService : ILogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LogService> _logger;

    /// <summary>
    /// Constructor: recibe el DbContext y logger por inyección de dependencias.
    /// </summary>
    public LogService(ApplicationDbContext context, ILogger<LogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    // CREACIÓN DE LOGS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un log automático en la base de datos. Es llamado internamente
    /// por <see cref="ClienteService"/> e <see cref="InventarioService"/> después de
    /// cada operación relevante (crear, editar, renovar, eliminar clientes o productos).
    ///
    /// No lanza excepciones — si algo falla, solo imprime en consola.
    /// Esto evita que un fallo al registrar el log interrumpa la operación principal.
    /// </summary>
    /// <param name="negocioId">ID del negocio al que pertenece el log.</param>
    /// <param name="tipo">Tipo de operación: "cliente_creado", "cliente_renovado", "venta_inventario", etc.</param>
    /// <param name="message">Descripción detallada de la acción realizada.</param>
    /// <param name="monto">Monto del movimiento: positivo = ingreso, negativo = gasto, 0 = sin movimiento.</param>
    /// <param name="clienteId">ID del cliente relacionado (opcional, para trazabilidad).</param>
    /// <param name="nombreCliente">Nombre del cliente (se guarda desnormalizado para que no cambie al editar el cliente).</param>
    public async Task CreateLogAsync(Guid negocioId, string tipo, string message, decimal monto = 0, Guid? clienteId = null, string nombreCliente = null)
    {
        try
        {
            var log = new Logs
            {
                Id = Guid.NewGuid(),
                NegocioId = negocioId,
                Message = message,
                Monto = monto,
                Tipo = tipo,
                ClienteId = clienteId,
                // Se guarda el nombre en el momento de la operación para que
                // si el cliente cambia de nombre más adelante, el log conserve el original.
                NombreCliente = nombreCliente,
                Fecha = TimeHelper.Now
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Registrar el error via ILogger (capturado por la infraestructura de logging)
            // sin propagarlo — un fallo en el log no debe interrumpir la operación principal.
            _logger.LogError(ex, "Error al crear log financiero para negocio {NegocioId}", negocioId);
        }
    }

    /// <summary>
    /// Crea un log manual ingresado directamente por el dueño del negocio
    /// desde la pestaña de Logs en el dashboard.
    ///
    /// El tipo se asigna automáticamente según el signo del monto:
    ///   - monto >= 0 → tipo = "ingreso"
    ///   - monto menor a 0  → tipo = "gasto"
    /// </summary>
    /// <param name="negocioId">ID del negocio que crea el log.</param>
    /// <param name="message">Descripción del ingreso o gasto (obligatoria).</param>
    /// <param name="monto">Monto: positivo para ingresos, negativo para gastos.</param>
    public async Task<(bool success, string message)> CrearLogManualAsync(Guid negocioId, string message, decimal monto)
    {
        if (string.IsNullOrWhiteSpace(message))
            return (false, "La descripción es obligatoria");

        var log = new Logs
        {
            Id = Guid.NewGuid(),
            NegocioId = negocioId,
            Message = message,
            Monto = monto,
            // El tipo se determina por el signo del monto, no hay que elegirlo manualmente
            Tipo = monto >= 0 ? "ingreso" : "gasto",
            Fecha = TimeHelper.Now
        };

        _context.Logs.Add(log);
        await _context.SaveChangesAsync();

        return (true, "Log registrado exitosamente");
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los logs del negocio ordenados por fecha descendente
    /// (el más reciente aparece primero). Usado por la pestaña Logs del dashboard.
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyos logs se quieren obtener.</param>
    public async Task<object> GetLogsAsync(Guid negocioId)
    {
        return await _context.Logs
            .Where(l => l.NegocioId == negocioId)
            .OrderByDescending(l => l.Fecha)
            .Select(l => new
            {
                l.Id,
                l.Message,
                l.Monto,
                l.Tipo,
                l.ClienteId,
                l.NombreCliente,
                l.Fecha
            })
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un log individual por su ID. Se usa para pre-rellenar el modal
    /// de edición de log en el dashboard.
    /// </summary>
    /// <param name="id">ID único del log.</param>
    /// <param name="negocioId">ID del negocio (para verificar que el log pertenece al negocio correcto).</param>
    public async Task<object> GetLogAsync(Guid id, Guid negocioId)
    {
        return await _context.Logs
            .FirstOrDefaultAsync(l => l.Id == id && l.NegocioId == negocioId);
    }

    /// <summary>
    /// Obtiene la fecha del log más antiguo del negocio.
    /// Se usa en el frontend para limitar el selector de rango de fechas
    /// al periodo real de datos disponibles.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    public async Task<DateTime?> GetOldestLogDateAsync(Guid negocioId)
    {
        var oldest = await _context.Logs
            .Where(l => l.NegocioId == negocioId)
            .OrderBy(l => l.Fecha)
            .FirstOrDefaultAsync();
        return oldest?.Fecha;
    }

    // ═══════════════════════════════════════════════════════════
    // EDITAR Y ELIMINAR
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Edita la descripción y el monto de un log existente.
    /// El tipo se recalcula automáticamente según el nuevo monto.
    ///
    /// Solo el dueño del negocio puede editar sus propios logs
    /// (validado por el parámetro negocioId).
    /// </summary>
    /// <param name="id">ID del log a editar.</param>
    /// <param name="negocioId">ID del negocio (seguridad: evita editar logs ajenos).</param>
    /// <param name="message">Nueva descripción del log.</param>
    /// <param name="monto">Nuevo monto del movimiento.</param>
    public async Task<(bool success, string message)> EditarLogAsync(Guid id, Guid negocioId, string message, decimal monto)
    {
        var log = await _context.Logs
            .FirstOrDefaultAsync(l => l.Id == id && l.NegocioId == negocioId);

        if (log == null)
            return (false, "Log no encontrado");

        log.Message = message;
        log.Monto = monto;
        // Recalcular tipo según el nuevo monto
        log.Tipo = monto >= 0 ? "ingreso" : "gasto";

        _context.Update(log);
        await _context.SaveChangesAsync();

        return (true, "Log actualizado exitosamente");
    }

    /// <summary>
    /// Elimina un log individual. Solo el dueño puede eliminar sus propios logs.
    ///
    /// Nota: eliminar un log automático (generado por crear/renovar un cliente)
    /// afectará los totales de ingresos mostrados en el dashboard.
    /// </summary>
    /// <param name="id">ID del log a eliminar.</param>
    /// <param name="negocioId">ID del negocio (seguridad).</param>
    public async Task<(bool success, string message)> EliminarLogAsync(Guid id, Guid negocioId)
    {
        var log = await _context.Logs
            .FirstOrDefaultAsync(l => l.Id == id && l.NegocioId == negocioId);

        if (log == null)
            return (false, "Log no encontrado");

        _context.Logs.Remove(log);
        await _context.SaveChangesAsync();

        return (true, "Log eliminado exitosamente");
    }

    /// <summary>
    /// Elimina TODOS los logs de un negocio. Esta acción es irreversible.
    /// Se usa principalmente en escenarios de limpieza de datos o demo.
    ///
    /// Después de esta operación, todos los contadores de ingresos y gastos
    /// del dashboard mostrarán $0 hasta que se registren nuevas operaciones.
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyos logs se eliminarán.</param>
    public async Task<(bool success, string message)> EliminarTodosLogsAsync(Guid negocioId)
    {
        var logs = await _context.Logs.Where(l => l.NegocioId == negocioId).ToListAsync();

        if (!logs.Any())
            return (true, "No hay logs para eliminar");

        _context.Logs.RemoveRange(logs);
        await _context.SaveChangesAsync();

        return (true, $"Se eliminaron {logs.Count} logs exitosamente");
    }

    // ═══════════════════════════════════════════════════════════
    // EXPORTACIÓN EXCEL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Genera un archivo Excel (.xlsx) con todos los logs del negocio.
    ///
    /// Características del archivo:
    ///   - Columnas: Descripción, Monto, Tipo, Cliente, Fecha
    ///   - Montos positivos en verde, negativos en rojo
    ///   - Al final del archivo: resumen con total ingresos, total gastos y balance neto
    ///   - Retorna los bytes del archivo para que el controlador lo devuelva como descarga
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyos logs se exportarán.</param>
    /// <returns>Array de bytes del archivo .xlsx listo para descargar.</returns>
    public async Task<byte[]> ExportLogsExcelAsync(Guid negocioId)
    {
        var logs = await _context.Logs
            .Where(l => l.NegocioId == negocioId)
            .OrderByDescending(l => l.Fecha)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Logs");

        // Encabezados de columna
        worksheet.Cell(1, 1).Value = "Descripción";
        worksheet.Cell(1, 2).Value = "Monto";
        worksheet.Cell(1, 3).Value = "Tipo";
        worksheet.Cell(1, 4).Value = "Cliente";
        worksheet.Cell(1, 5).Value = "Fecha";

        // Estilo de encabezados: fondo azul, texto blanco, negrita
        var headerRange = worksheet.Range(1, 1, 1, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
        headerRange.Style.Font.FontColor = XLColor.White;

        // Llenar filas con datos y aplicar color según tipo de movimiento
        int row = 2;
        decimal totalIngresos = 0;
        decimal totalGastos = 0;

        foreach (var log in logs)
        {
            worksheet.Cell(row, 1).Value = log.Message;
            worksheet.Cell(row, 2).Value = log.Monto;
            worksheet.Cell(row, 3).Value = log.Tipo;
            worksheet.Cell(row, 4).Value = log.NombreCliente ?? "-";
            worksheet.Cell(row, 5).Value = log.Fecha.ToString("dd/MM/yyyy HH:mm");

            if (log.Monto >= 0)
            {
                // Ingreso: monto en verde
                worksheet.Cell(row, 2).Style.Font.FontColor = XLColor.Green;
                totalIngresos += log.Monto;
            }
            else
            {
                // Gasto: monto en rojo
                worksheet.Cell(row, 2).Style.Font.FontColor = XLColor.Red;
                totalGastos += Math.Abs(log.Monto);
            }
            row++;
        }

        // Resumen al final: fila vacía de separación + totales
        row++;
        worksheet.Cell(row, 1).Value = "Total Ingresos:";
        worksheet.Cell(row, 2).Value = totalIngresos;
        worksheet.Cell(row, 2).Style.Font.FontColor = XLColor.Green;
        row++;
        worksheet.Cell(row, 1).Value = "Total Gastos:";
        worksheet.Cell(row, 2).Value = totalGastos;
        worksheet.Cell(row, 2).Style.Font.FontColor = XLColor.Red;
        row++;
        worksheet.Cell(row, 1).Value = "Balance:";
        worksheet.Cell(row, 2).Value = totalIngresos - totalGastos;
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Style.Font.Bold = true;

        // Ajustar el ancho de todas las columnas al contenido
        worksheet.Columns().AdjustToContents();

        // Guardar el archivo en memoria y retornar sus bytes
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
