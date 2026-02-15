// ═══════════════════════════════════════════════════════════
// LogService.cs — Servicio de registros/logs financieros
// Los logs son la fuente inmutable de verdad financiera.
// Cada acción sobre clientes (crear, editar, renovar, eliminar)
// genera un log automático. También permite logs manuales.
// ═══════════════════════════════════════════════════════════

using ClosedXML.Excel;
using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class LogService : ILogService
{
    private readonly ApplicationDbContext _context;

    public LogService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // CREACIÓN DE LOGS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un log automático. Llamado internamente por ClienteService
    /// y AuthController al crear/editar/renovar/eliminar clientes o al iniciar sesión.
    /// </summary>
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
                NombreCliente = nombreCliente,
                Fecha = TimeHelper.Now
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating log: {ex.Message}");
        }
    }

    /// <summary>
    /// Crea un log manual de ingreso o gasto desde el dashboard.
    /// El tipo se asigna automáticamente: monto >= 0 = "ingreso", monto menor a 0 = "gasto"
    /// </summary>
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
    /// Obtiene todos los logs del negocio, más recientes primero
    /// </summary>
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
    /// Obtiene un log individual por su ID
    /// </summary>
    public async Task<object> GetLogAsync(Guid id, Guid negocioId)
    {
        return await _context.Logs
            .FirstOrDefaultAsync(l => l.Id == id && l.NegocioId == negocioId);
    }

    /// <summary>
    /// Obtiene la fecha del log más antiguo del negocio
    /// </summary>
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
    /// Edita la descripción y monto de un log existente
    /// </summary>
    public async Task<(bool success, string message)> EditarLogAsync(Guid id, Guid negocioId, string message, decimal monto)
    {
        var log = await _context.Logs
            .FirstOrDefaultAsync(l => l.Id == id && l.NegocioId == negocioId);

        if (log == null)
            return (false, "Log no encontrado");

        log.Message = message;
        log.Monto = monto;
        log.Tipo = monto >= 0 ? "ingreso" : "gasto";

        _context.Update(log);
        await _context.SaveChangesAsync();

        return (true, "Log actualizado exitosamente");
    }

    /// <summary>
    /// Elimina un log individual
    /// </summary>
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
    /// Elimina todos los logs de un negocio
    /// </summary>
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
    /// Genera un archivo Excel con todos los logs e incluye al final
    /// un resumen con total de ingresos, gastos y balance neto
    /// </summary>
    public async Task<byte[]> ExportLogsExcelAsync(Guid negocioId)
    {
        var logs = await _context.Logs
            .Where(l => l.NegocioId == negocioId)
            .OrderByDescending(l => l.Fecha)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Logs");

        // Encabezados
        worksheet.Cell(1, 1).Value = "Descripción";
        worksheet.Cell(1, 2).Value = "Monto";
        worksheet.Cell(1, 3).Value = "Tipo";
        worksheet.Cell(1, 4).Value = "Cliente";
        worksheet.Cell(1, 5).Value = "Fecha";

        var headerRange = worksheet.Range(1, 1, 1, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
        headerRange.Style.Font.FontColor = XLColor.White;

        // Datos con colores según tipo (verde = ingreso, rojo = gasto)
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
                worksheet.Cell(row, 2).Style.Font.FontColor = XLColor.Green;
                totalIngresos += log.Monto;
            }
            else
            {
                worksheet.Cell(row, 2).Style.Font.FontColor = XLColor.Red;
                totalGastos += Math.Abs(log.Monto);
            }
            row++;
        }

        // Resumen al final del archivo
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

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
