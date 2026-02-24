// ═══════════════════════════════════════════════════════════
// ReciboService.cs — Servicio de gestion de recibos digitales
//
// RESPONSABILIDADES:
//   - Generar numeros secuenciales de recibo por negocio
//   - Almacenar recibos con su contenido HTML completo
//   - Consultar recibos por negocio (multi-tenant) o por admin (NegocioId = null)
//   - Buscar recibos por numero secuencial
//   - Exportar recibos a Excel filtrados por ano y mes
//   - Eliminar recibos antiguos (limpieza de datos)
//
// NUMERACION:
//   Cada negocio tiene su propia secuencia independiente de numeros.
//   El numero se formatea con 6 digitos (ej: 000001, 000042).
//   Los recibos del admin (comisiones, pagos SaaS) tienen NegocioId = null.
//
// CONTENIDO HTML:
//   El campo ContenidoHtml almacena el recibo completo tal como se genera
//   en el momento del pago. Esto permite re-enviar o reimprimir el recibo
//   exactamente como se vio la primera vez, sin recalcular nada.
// ═══════════════════════════════════════════════════════════

#nullable enable

using ClosedXML.Excel;
using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Servicio de gestion de recibos digitales. Cada recibo guarda el HTML completo
/// para poder re-enviarlo o reimprimirlo sin recalcular. Multi-tenant por NegocioId.
/// </summary>
public class ReciboService : IReciboService
{
    private readonly ApplicationDbContext _context;

    public ReciboService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> ObtenerSiguienteNumeroAsync(Guid? negocioId)
    {
        var max = await _context.Recibos
            .Where(r => r.NegocioId == negocioId)
            .MaxAsync(r => (int?)r.NumeroRecibo) ?? 0;

        return (max + 1).ToString("D6");
    }

    public async Task CrearReciboAsync(Guid? negocioId, string numeroRecibo, string tipoRecibo,
        string destinatarioEmail, string destinatarioNombre, string negocioNombre,
        string concepto, decimal monto, string contenidoHtml, string metodoPago = "Efectivo")
    {
        var recibo = new Recibo
        {
            ReciboId = Guid.NewGuid(),
            NumeroRecibo = int.Parse(numeroRecibo),
            NegocioId = negocioId,
            TipoRecibo = tipoRecibo,
            DestinatarioEmail = destinatarioEmail,
            DestinatarioNombre = destinatarioNombre,
            NegocioNombre = negocioNombre,
            Concepto = concepto,
            Monto = monto,
            MetodoPago = metodoPago ?? "Efectivo",
            ContenidoHtml = contenidoHtml,
            FechaCreacion = TimeHelper.Now
        };

        _context.Recibos.Add(recibo);
        await _context.SaveChangesAsync();
    }

    public async Task<List<object>> GetRecibosAsync(Guid negocioId)
    {
        return await _context.Recibos
            .Where(r => r.NegocioId == negocioId)
            .OrderByDescending(r => r.FechaCreacion)
            .Select(r => (object)new
            {
                r.ReciboId,
                NumeroRecibo = r.NumeroRecibo.ToString("D6"),
                r.TipoRecibo,
                r.DestinatarioNombre,
                r.DestinatarioEmail,
                r.Concepto,
                r.Monto,
                r.FechaCreacion
            })
            .ToListAsync();
    }

    public async Task<object?> GetReciboAsync(Guid reciboId, Guid negocioId)
    {
        return await _context.Recibos
            .Where(r => r.ReciboId == reciboId && r.NegocioId == negocioId)
            .Select(r => new
            {
                r.ReciboId,
                NumeroRecibo = r.NumeroRecibo.ToString("D6"),
                r.TipoRecibo,
                r.DestinatarioNombre,
                r.DestinatarioEmail,
                r.NegocioNombre,
                r.Concepto,
                r.Monto,
                r.ContenidoHtml,
                r.FechaCreacion
            })
            .FirstOrDefaultAsync();
    }

    public async Task<object?> BuscarPorNumeroAsync(int numero, Guid negocioId)
    {
        return await _context.Recibos
            .Where(r => r.NumeroRecibo == numero && r.NegocioId == negocioId)
            .Select(r => new
            {
                r.ReciboId,
                NumeroRecibo = r.NumeroRecibo.ToString("D6"),
                r.TipoRecibo,
                r.DestinatarioNombre,
                r.DestinatarioEmail,
                r.NegocioNombre,
                r.Concepto,
                r.Monto,
                r.ContenidoHtml,
                r.FechaCreacion
            })
            .FirstOrDefaultAsync();
    }

    public async Task<byte[]> ExportRecibosExcelAsync(Guid negocioId, int anio, int mes)
    {
        var recibos = await _context.Recibos
            .Where(r => r.NegocioId == negocioId
                && r.FechaCreacion.Year == anio
                && r.FechaCreacion.Month == mes)
            .OrderBy(r => r.NumeroRecibo)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Recibos");

        ws.Cell(1, 1).Value = "#Recibo";
        ws.Cell(1, 2).Value = "Tipo";
        ws.Cell(1, 3).Value = "Destinatario";
        ws.Cell(1, 4).Value = "Email";
        ws.Cell(1, 5).Value = "Concepto";
        ws.Cell(1, 6).Value = "Monto";
        ws.Cell(1, 7).Value = "Fecha";

        var header = ws.Range(1, 1, 1, 7);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#ff6b35");
        header.Style.Font.FontColor = XLColor.White;

        int row = 2;
        decimal total = 0;
        foreach (var r in recibos)
        {
            ws.Cell(row, 1).Value = r.NumeroRecibo.ToString("D6");
            ws.Cell(row, 2).Value = r.TipoRecibo;
            ws.Cell(row, 3).Value = r.DestinatarioNombre;
            ws.Cell(row, 4).Value = r.DestinatarioEmail;
            ws.Cell(row, 5).Value = r.Concepto;
            ws.Cell(row, 6).Value = r.Monto;
            ws.Cell(row, 7).Value = r.FechaCreacion.ToString("dd/MM/yyyy HH:mm");
            total += r.Monto;
            row++;
        }

        ws.Cell(row, 5).Value = "TOTAL";
        ws.Cell(row, 5).Style.Font.Bold = true;
        ws.Cell(row, 6).Value = total;
        ws.Cell(row, 6).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<DateTime?> GetFechaReciboMasAntiguoAsync(Guid negocioId)
    {
        return await _context.Recibos
            .Where(r => r.NegocioId == negocioId)
            .MinAsync(r => (DateTime?)r.FechaCreacion);
    }

    public async Task<int> EliminarRecibosAntiguosAsync(Guid negocioId, DateTime anteriorA)
    {
        var recibos = await _context.Recibos
            .Where(r => r.NegocioId == negocioId && r.FechaCreacion < anteriorA)
            .ToListAsync();

        _context.Recibos.RemoveRange(recibos);
        await _context.SaveChangesAsync();
        return recibos.Count;
    }

    public async Task<List<object>> GetRecibosAdminAsync()
    {
        return await _context.Recibos
            .Where(r => r.NegocioId == null)
            .OrderByDescending(r => r.FechaCreacion)
            .Select(r => (object)new
            {
                r.ReciboId,
                NumeroRecibo = r.NumeroRecibo.ToString("D6"),
                r.TipoRecibo,
                r.DestinatarioNombre,
                r.DestinatarioEmail,
                r.Concepto,
                r.Monto,
                r.FechaCreacion
            })
            .ToListAsync();
    }

    public async Task<object?> GetReciboAdminAsync(Guid reciboId)
    {
        return await _context.Recibos
            .Where(r => r.ReciboId == reciboId && r.NegocioId == null)
            .Select(r => new
            {
                r.ReciboId,
                NumeroRecibo = r.NumeroRecibo.ToString("D6"),
                r.TipoRecibo,
                r.DestinatarioNombre,
                r.DestinatarioEmail,
                r.NegocioNombre,
                r.Concepto,
                r.Monto,
                r.ContenidoHtml,
                r.FechaCreacion
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<object>> GetAdminPaymentStatsAsync()
    {
        var now = TimeHelper.Now;
        var startOfDay = now.Date;
        var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var stats = await _context.Recibos.AsNoTracking()
            .Where(r => r.NegocioId == null && r.FechaCreacion >= startOfMonth)
            .GroupBy(r => r.MetodoPago ?? "Efectivo")
            .Select(g => new
            {
                MetodoPago = g.Key,
                TotalHoy = g.Where(r => r.FechaCreacion >= startOfDay).Sum(r => r.Monto),
                TotalSemana = g.Where(r => r.FechaCreacion >= startOfWeek).Sum(r => r.Monto),
                TotalMes = g.Sum(r => r.Monto),
                CantidadMes = g.Count()
            })
            .ToListAsync();

        // Asegurar que Efectivo siempre aparezca
        if (!stats.Any(s => s.MetodoPago == "Efectivo"))
        {
            stats.Add(new { MetodoPago = "Efectivo", TotalHoy = 0m, TotalSemana = 0m, TotalMes = 0m, CantidadMes = 0 });
        }

        return stats.OrderByDescending(s => s.TotalMes).Cast<object>().ToList();
    }
}
