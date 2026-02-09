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

    public async Task CreateLogAsync(Guid gimnasioId, string tipo, string message, decimal monto = 0, Guid? clienteId = null, string nombreCliente = null)
    {
        try
        {
            var log = new Logs
            {
                Id = Guid.NewGuid(),
                GimnasioId = gimnasioId,
                Message = message,
                Monto = monto,
                Tipo = tipo,
                ClienteId = clienteId,
                NombreCliente = nombreCliente,
                Fecha = DateTime.Now
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating log: {ex.Message}");
        }
    }

    public async Task<object> GetLogsAsync(Guid gimnasioId)
    {
        return await _context.Logs
            .Where(l => l.GimnasioId == gimnasioId)
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

    public async Task<object> GetLogAsync(Guid id, Guid gimnasioId)
    {
        return await _context.Logs
            .FirstOrDefaultAsync(l => l.Id == id && l.GimnasioId == gimnasioId);
    }

    public async Task<(bool success, string message)> CrearLogManualAsync(Guid gimnasioId, string message, decimal monto)
    {
        if (string.IsNullOrWhiteSpace(message))
            return (false, "La descripción es obligatoria");

        var log = new Logs
        {
            Id = Guid.NewGuid(),
            GimnasioId = gimnasioId,
            Message = message,
            Monto = monto,
            Tipo = monto >= 0 ? "ingreso" : "gasto",
            Fecha = DateTime.Now
        };

        _context.Logs.Add(log);
        await _context.SaveChangesAsync();

        return (true, "Log registrado exitosamente");
    }

    public async Task<(bool success, string message)> EditarLogAsync(Guid id, Guid gimnasioId, string message, decimal monto)
    {
        var log = await _context.Logs
            .FirstOrDefaultAsync(l => l.Id == id && l.GimnasioId == gimnasioId);

        if (log == null)
            return (false, "Log no encontrado");

        log.Message = message;
        log.Monto = monto;
        log.Tipo = monto >= 0 ? "ingreso" : "gasto";

        _context.Update(log);
        await _context.SaveChangesAsync();

        return (true, "Log actualizado exitosamente");
    }

    public async Task<(bool success, string message)> EliminarLogAsync(Guid id, Guid gimnasioId)
    {
        var log = await _context.Logs
            .FirstOrDefaultAsync(l => l.Id == id && l.GimnasioId == gimnasioId);

        if (log == null)
            return (false, "Log no encontrado");

        _context.Logs.Remove(log);
        await _context.SaveChangesAsync();

        return (true, "Log eliminado exitosamente");
    }

    public async Task<(bool success, string message)> EliminarTodosLogsAsync(Guid gimnasioId)
    {
        var logs = await _context.Logs.Where(l => l.GimnasioId == gimnasioId).ToListAsync();

        if (!logs.Any())
            return (true, "No hay logs para eliminar");

        _context.Logs.RemoveRange(logs);
        await _context.SaveChangesAsync();

        return (true, $"Se eliminaron {logs.Count} logs exitosamente");
    }

    public async Task<byte[]> ExportLogsExcelAsync(Guid gimnasioId)
    {
        var logs = await _context.Logs
            .Where(l => l.GimnasioId == gimnasioId)
            .OrderByDescending(l => l.Fecha)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Logs");

        worksheet.Cell(1, 1).Value = "Descripción";
        worksheet.Cell(1, 2).Value = "Monto";
        worksheet.Cell(1, 3).Value = "Tipo";
        worksheet.Cell(1, 4).Value = "Cliente";
        worksheet.Cell(1, 5).Value = "Fecha";

        var headerRange = worksheet.Range(1, 1, 1, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
        headerRange.Style.Font.FontColor = XLColor.White;

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

    public async Task<DateTime?> GetOldestLogDateAsync(Guid gimnasioId)
    {
        var oldest = await _context.Logs
            .Where(l => l.GimnasioId == gimnasioId)
            .OrderBy(l => l.Fecha)
            .FirstOrDefaultAsync();
        return oldest?.Fecha;
    }
}
