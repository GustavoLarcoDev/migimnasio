using ClosedXML.Excel;
using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class ClienteService : IClienteService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogService _logService;

    public ClienteService(ApplicationDbContext context, ILogService logService)
    {
        _context = context;
        _logService = logService;
    }

    public async Task<object> GetDashboardStatsAsync(Guid negocioId)
    {
        var clientes = await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
            .ToListAsync();

        var totalClientes = clientes.Count;
        var clientesActivos = clientes.Count(c => c.FechaQueTermina.Date >= DateTime.Now.Date);
        var clientesVencidos = clientes.Count(c => c.FechaQueTermina.Date < DateTime.Now.Date);
        var clientesNuevosHoy = clientes.Count(c => c.FechaDeCreacion.Date == DateTime.Now.Date);
        var clientesNuevosMes = clientes.Count(c => c.FechaDeCreacion.Month == DateTime.Now.Month && c.FechaDeCreacion.Year == DateTime.Now.Year);

        // Ingresos desde Logs (fuente inmutable). Eliminar un cliente no afecta los ingresos.
        var logs = await _context.Logs
            .Where(l => l.NegocioId == negocioId && l.Monto > 0)
            .ToListAsync();

        var ingresosMes = logs
            .Where(l => l.Fecha.Month == DateTime.Now.Month && l.Fecha.Year == DateTime.Now.Year)
            .Sum(l => l.Monto);

        var ingresosHoy = logs
            .Where(l => l.Fecha.Date == DateTime.Now.Date)
            .Sum(l => l.Monto);

        var proximosVencer = clientes
            .Where(c => c.FechaQueTermina.Date >= DateTime.Now.Date && c.FechaQueTermina.Date <= DateTime.Now.AddDays(5).Date)
            .Select(c => new
            {
                c.Nombre,
                c.Apellido,
                NombreCompleto = $"{c.Nombre} {c.Apellido}",
                c.Telefono,
                c.FechaQueTermina,
                DiasRestantes = (c.FechaQueTermina.Date - DateTime.Now.Date).Days
            })
            .OrderBy(c => c.DiasRestantes)
            .ToList();

        return new
        {
            totalClientes,
            clientesActivos,
            clientesVencidos,
            clientesNuevosHoy,
            clientesNuevosMes,
            ingresosMes,
            ingresosHoy,
            proximosVencer
        };
    }

    public async Task<object> GetClientesAsync(Guid negocioId)
    {
        return await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
            .OrderByDescending(c => c.FechaQueTermina)
            .Select(c => new
            {
                c.ClienteId,
                c.Nombre,
                c.Apellido,
                NombreCompleto = $"{c.Nombre} {c.Apellido}",
                c.Email,
                c.Telefono,
                c.Direccion,
                c.Dias,
                c.Precio,
                c.FechaDeCreacion,
                c.FechaQueTermina,
                DiasRestantes = (c.FechaQueTermina.Date - DateTime.Now.Date).Days,
                EstaActivo = c.FechaQueTermina.Date >= DateTime.Now.Date
            })
            .ToListAsync();
    }

    public async Task<Cliente> GetClienteAsync(Guid id, Guid negocioId)
    {
        return await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == id && c.NegocioId == negocioId);
    }

    public async Task<(bool success, string message)> CrearClienteAsync(ClienteCreateDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Nombre) || string.IsNullOrWhiteSpace(model.Apellido))
            return (false, "Nombre y Apellido son obligatorios");
        if (string.IsNullOrWhiteSpace(model.Telefono))
            return (false, "Teléfono es obligatorio");
        if (model.Precio <= 0)
            return (false, "El precio debe ser mayor a 0");

        var fechaInicio = model.FechaInicio ?? DateTime.Now;
        DateTime fechaFin;
        int dias;

        if (model.FechaFin.HasValue)
        {
            fechaFin = model.FechaFin.Value;
            dias = (fechaFin - fechaInicio).Days;
        }
        else if (model.Dias > 0)
        {
            dias = model.Dias;
            fechaFin = fechaInicio.AddDays(dias);
        }
        else
        {
            return (false, "Debe indicar la fecha de finalización o los días");
        }

        if (dias <= 0)
            return (false, "La fecha de finalización debe ser posterior a la fecha de inicio");

        var cliente = new Cliente
        {
            ClienteId = Guid.NewGuid(),
            NegocioId = model.NegocioId,
            Nombre = model.Nombre,
            Apellido = model.Apellido,
            Email = model.Email,
            Telefono = model.Telefono,
            Direccion = model.Direccion,
            Dias = dias,
            Precio = model.Precio,
            EsDiario = model.EsDiario,
            FechaDeCreacion = fechaInicio,
            FechaDeActualizacion = DateTime.Now,
            FechaQueTermina = fechaFin
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
        await _logService.CreateLogAsync(model.NegocioId, "cliente_creado",
            $"Nuevo cliente creado: {nombreCompleto}", model.Precio, cliente.ClienteId, nombreCompleto);

        return (true, "Cliente creado exitosamente");
    }

    public async Task<(bool success, string message)> EditarClienteAsync(ClienteCreateDto model)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == model.ClienteId && c.NegocioId == model.NegocioId);

        if (cliente == null)
            return (false, "Cliente no encontrado");
        if (string.IsNullOrWhiteSpace(model.Nombre) || string.IsNullOrWhiteSpace(model.Apellido))
            return (false, "Nombre y Apellido son obligatorios");
        if (string.IsNullOrWhiteSpace(model.Telefono))
            return (false, "Teléfono es obligatorio");
        if (model.Precio <= 0)
            return (false, "El precio debe ser mayor a 0");

        var fechaInicio = model.FechaInicio ?? cliente.FechaDeCreacion;
        DateTime fechaFin;
        int dias;

        if (model.FechaFin.HasValue)
        {
            fechaFin = model.FechaFin.Value;
            dias = (fechaFin - fechaInicio).Days;
        }
        else if (model.Dias > 0)
        {
            dias = model.Dias;
            fechaFin = fechaInicio.AddDays(dias);
        }
        else
        {
            return (false, "Debe indicar la fecha de finalización o los días");
        }

        if (dias <= 0)
            return (false, "La fecha de finalización debe ser posterior a la fecha de inicio");

        cliente.Nombre = model.Nombre;
        cliente.Apellido = model.Apellido;
        cliente.Email = model.Email;
        cliente.Telefono = model.Telefono;
        cliente.Direccion = model.Direccion;
        cliente.Dias = dias;
        cliente.Precio = model.Precio;
        cliente.EsDiario = model.EsDiario;
        cliente.FechaDeCreacion = fechaInicio;
        cliente.FechaQueTermina = fechaFin;
        cliente.FechaDeActualizacion = DateTime.Now;

        _context.Update(cliente);
        await _context.SaveChangesAsync();

        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
        await _logService.CreateLogAsync(model.NegocioId, "cliente_editado",
            $"Cliente editado: {nombreCompleto}", 0, cliente.ClienteId, nombreCompleto);

        return (true, "Cliente actualizado exitosamente");
    }

    public async Task<(bool success, string message)> EliminarClienteAsync(Guid id, Guid negocioId)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == id && c.NegocioId == negocioId);

        if (cliente == null)
            return (false, "Cliente no encontrado");

        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";

        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync();

        await _logService.CreateLogAsync(negocioId, "cliente_eliminado",
            $"Cliente eliminado: {nombreCompleto}", 0, cliente.ClienteId, nombreCompleto);

        return (true, "Cliente eliminado exitosamente");
    }

    public async Task<(bool success, string message)> RenovarClienteAsync(Guid id, Guid negocioId, DateTime nuevaFechaFin, decimal precio)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == id && c.NegocioId == negocioId);

        if (cliente == null)
            return (false, "Cliente no encontrado");
        if (nuevaFechaFin <= cliente.FechaQueTermina)
            return (false, "La nueva fecha debe ser posterior a la fecha de finalización actual");

        var diasAgregados = (nuevaFechaFin - cliente.FechaQueTermina).Days;
        cliente.FechaQueTermina = nuevaFechaFin;
        cliente.Dias = diasAgregados;
        cliente.Precio = precio;
        cliente.FechaDeActualizacion = DateTime.Now;

        _context.Update(cliente);
        await _context.SaveChangesAsync();

        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
        await _logService.CreateLogAsync(negocioId, "cliente_renovado",
            $"Cliente {nombreCompleto} ha renovado su suscripción, ahora su suscripción termina en {nuevaFechaFin:dd/MM/yyyy}",
            precio, cliente.ClienteId, nombreCompleto);

        return (true, "Membresía renovada exitosamente");
    }

    public async Task<byte[]> ExportClientesExcelAsync(Guid negocioId)
    {
        var clientes = await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Clientes");

        worksheet.Cell(1, 1).Value = "Nombre";
        worksheet.Cell(1, 2).Value = "Apellido";
        worksheet.Cell(1, 3).Value = "Email";
        worksheet.Cell(1, 4).Value = "Teléfono";
        worksheet.Cell(1, 5).Value = "Fecha Inicio";
        worksheet.Cell(1, 6).Value = "Fecha Vencimiento";
        worksheet.Cell(1, 7).Value = "Días";
        worksheet.Cell(1, 8).Value = "Precio";
        worksheet.Cell(1, 9).Value = "Tipo";
        worksheet.Cell(1, 10).Value = "Estado";

        var headerRange = worksheet.Range(1, 1, 1, 10);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var cliente in clientes)
        {
            worksheet.Cell(row, 1).Value = cliente.Nombre;
            worksheet.Cell(row, 2).Value = cliente.Apellido;
            worksheet.Cell(row, 3).Value = cliente.Email;
            worksheet.Cell(row, 4).Value = cliente.Telefono;
            worksheet.Cell(row, 5).Value = cliente.FechaDeCreacion;
            worksheet.Cell(row, 5).Style.DateFormat.Format = "dd/MM/yyyy";
            worksheet.Cell(row, 6).Value = cliente.FechaQueTermina;
            worksheet.Cell(row, 6).Style.DateFormat.Format = "dd/MM/yyyy";
            worksheet.Cell(row, 7).Value = cliente.Dias;
            worksheet.Cell(row, 8).Value = cliente.Precio;
            worksheet.Cell(row, 9).Value = cliente.EsDiario ? "Diario" : "Regular";
            bool activo = cliente.FechaQueTermina.Date >= DateTime.Now.Date;
            worksheet.Cell(row, 10).Value = activo ? "Activo" : "Vencido";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<object> ImportarClientesExcelAsync(Guid negocioId, Stream fileStream)
    {
        var clientesCreados = new List<string>();
        var clientesOmitidos = new List<string>();
        var errores = new List<string>();

        using var memStream = new MemoryStream();
        await fileStream.CopyToAsync(memStream);
        memStream.Position = 0;

        using var workbook = new XLWorkbook(memStream);
        var worksheet = workbook.Worksheet(1);
        var rangeUsed = worksheet.RangeUsed();

        if (rangeUsed == null)
            return new { success = false, message = "El archivo está vacío" };

        // --- Detectar columnas por headers (fila 1) ---
        var headerRow = rangeUsed.Row(1);
        var colMap = new Dictionary<string, int>();
        for (int col = 1; col <= rangeUsed.ColumnCount(); col++)
        {
            var header = headerRow.Cell(col).GetString()?.Trim().ToLowerInvariant() ?? "";
            // Quitar tildes para matching flexible
            header = header.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");

            if (header.Contains("nombre") && !header.Contains("apellido") && !header.Contains("cliente") && !colMap.ContainsKey("nombre"))
                colMap["nombre"] = col;
            else if (header.Contains("apellido"))
                colMap["apellido"] = col;
            else if (header.Contains("nombre") && header.Contains("cliente") || header == "cliente")
                colMap["nombre_completo"] = col;
            else if (header.Contains("email") || header.Contains("correo"))
                colMap["email"] = col;
            else if (header.Contains("telefono") || header.Contains("tel") || header.Contains("celular") || header.Contains("whatsapp"))
                colMap["telefono"] = col;
            else if (header.Contains("direccion") || header.Contains("address"))
                colMap["direccion"] = col;
            else if (header.Contains("inicio") || header.Contains("creacion"))
                colMap["fecha_inicio"] = col;
            else if (header.Contains("vencimiento") || header.Contains("fecha fin") || header.Contains("fin") || header.Contains("termina"))
                colMap["fecha_fin"] = col;
            else if (header.Contains("dia") && !header.Contains("diario"))
                colMap["dias"] = col;
            else if (header.Contains("precio") || header.Contains("monto") || header.Contains("costo"))
                colMap["precio"] = col;
            else if (header == "tipo")
                colMap["tipo"] = col;
        }

        // Fallback: si no detecta headers, asumir formato por posición
        if (!colMap.ContainsKey("nombre") && !colMap.ContainsKey("nombre_completo"))
        {
            colMap["nombre"] = 1;
            colMap["apellido"] = 2;
            if (rangeUsed.ColumnCount() >= 3) colMap["email"] = 3;
            if (rangeUsed.ColumnCount() >= 4) colMap["telefono"] = 4;
            if (rangeUsed.ColumnCount() >= 5) colMap["fecha_inicio"] = 5;
            if (rangeUsed.ColumnCount() >= 6) colMap["fecha_fin"] = 6;
            if (rangeUsed.ColumnCount() >= 7) colMap["dias"] = 7;
            if (rangeUsed.ColumnCount() >= 8) colMap["precio"] = 8;
        }

        var clientesExistentes = await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
            .Select(c => new { c.Nombre, c.Apellido })
            .ToListAsync();

        var rows = rangeUsed.RowsUsed().Skip(1); // Skip header
        int rowNumber = 2;

        foreach (var row in rows)
        {
            try
            {
                // --- Nombre y Apellido ---
                string nombre, apellido;
                if (colMap.ContainsKey("nombre_completo"))
                {
                    var fullName = GetCellString(row.Cell(colMap["nombre_completo"]));
                    if (string.IsNullOrWhiteSpace(fullName))
                    {
                        errores.Add($"Fila {rowNumber}: Nombre es obligatorio");
                        rowNumber++;
                        continue;
                    }
                    var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    nombre = parts[0];
                    apellido = parts.Length > 1 ? parts[1] : "-";
                }
                else
                {
                    nombre = colMap.ContainsKey("nombre") ? GetCellString(row.Cell(colMap["nombre"])) : "";
                    apellido = colMap.ContainsKey("apellido") ? GetCellString(row.Cell(colMap["apellido"])) : "-";
                }

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errores.Add($"Fila {rowNumber}: Nombre es obligatorio");
                    rowNumber++;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(apellido)) apellido = "-";

                // Duplicados
                if (clientesExistentes.Any(c => c.Nombre == nombre && c.Apellido == apellido))
                {
                    clientesOmitidos.Add($"{nombre} {apellido}");
                    rowNumber++;
                    continue;
                }

                // --- Campos opcionales ---
                var email = colMap.ContainsKey("email") ? GetCellString(row.Cell(colMap["email"])) : "";
                var telefono = colMap.ContainsKey("telefono") ? GetCellString(row.Cell(colMap["telefono"])) : "";
                var direccion = colMap.ContainsKey("direccion") ? GetCellString(row.Cell(colMap["direccion"])) : "";

                // --- Fechas ---
                DateTime fechaInicio = DateTime.Now;
                DateTime? fechaFin = null;
                int dias = 30;

                if (colMap.ContainsKey("fecha_inicio"))
                    fechaInicio = TryParseExcelDate(row.Cell(colMap["fecha_inicio"])) ?? DateTime.Now;

                if (colMap.ContainsKey("fecha_fin"))
                    fechaFin = TryParseExcelDate(row.Cell(colMap["fecha_fin"]));

                if (colMap.ContainsKey("dias"))
                {
                    try { dias = (int)row.Cell(colMap["dias"]).GetDouble(); } catch { dias = 30; }
                }

                // Calcular fechas: FechaFin tiene prioridad sobre Días
                if (fechaFin.HasValue && fechaFin.Value > fechaInicio)
                {
                    dias = (fechaFin.Value - fechaInicio).Days;
                }
                else if (dias > 0)
                {
                    fechaFin = fechaInicio.AddDays(dias);
                }
                else
                {
                    dias = 30;
                    fechaFin = fechaInicio.AddDays(30);
                }

                // --- Precio ---
                decimal precio = 0;
                if (colMap.ContainsKey("precio"))
                {
                    try { precio = (decimal)row.Cell(colMap["precio"]).GetDouble(); } catch { precio = 0; }
                }
                if (precio < 0) precio = 0;

                // --- Tipo ---
                bool esDiario = false;
                if (colMap.ContainsKey("tipo"))
                {
                    var tipo = GetCellString(row.Cell(colMap["tipo"]));
                    esDiario = tipo.Equals("diario", StringComparison.OrdinalIgnoreCase) ||
                               tipo.Equals("si", StringComparison.OrdinalIgnoreCase);
                }

                var cliente = new Cliente
                {
                    ClienteId = Guid.NewGuid(),
                    NegocioId = negocioId,
                    Nombre = nombre,
                    Apellido = apellido,
                    Email = email,
                    Telefono = telefono,
                    Direccion = direccion,
                    Dias = dias,
                    Precio = precio,
                    EsDiario = esDiario,
                    FechaDeCreacion = fechaInicio,
                    FechaDeActualizacion = DateTime.Now,
                    FechaQueTermina = fechaFin.Value
                };

                _context.Clientes.Add(cliente);
                clientesCreados.Add($"{nombre} {apellido}");
                clientesExistentes.Add(new { Nombre = nombre, Apellido = apellido });
            }
            catch (Exception ex)
            {
                errores.Add($"Fila {rowNumber}: {ex.Message}");
            }
            rowNumber++;
        }

        await _context.SaveChangesAsync();

        return new
        {
            success = true,
            message = "Importación completada",
            clientesCreados = clientesCreados.Count,
            clientesOmitidos = clientesOmitidos.Count,
            erroresCount = errores.Count,
            detalleCreados = clientesCreados,
            detalleOmitidos = clientesOmitidos,
            detalleErrores = errores
        };
    }

    /// <summary>
    /// Intenta parsear una celda de Excel como fecha. Maneja: DateTime nativo, número serial OLE, y strings con múltiples formatos.
    /// </summary>
    private static DateTime? TryParseExcelDate(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;

        // 1. DateTime nativo de Excel
        try
        {
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();
        }
        catch { }

        // 2. Número serial de Excel (OLE Automation Date)
        try
        {
            if (cell.DataType == XLDataType.Number)
            {
                var num = cell.GetDouble();
                if (num > 1 && num < 100000)
                    return DateTime.FromOADate(num);
            }
        }
        catch { }

        // 3. String con varios formatos
        var str = cell.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(str)) return null;

        string[] formats = {
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
            "yyyy-MM-dd", "yyyy/MM/dd",
            "MM/dd/yyyy", "M/d/yyyy",
            "dd/MM/yyyy HH:mm", "yyyy-MM-dd HH:mm:ss"
        };

        if (DateTime.TryParseExact(str, formats,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var parsed))
            return parsed;

        if (DateTime.TryParse(str, out var fallback))
            return fallback;

        return null;
    }

    /// <summary>
    /// Lee el contenido de una celda como string limpio, manejando tipos numéricos y vacíos.
    /// </summary>
    private static string GetCellString(IXLCell cell)
    {
        if (cell.IsEmpty()) return "";
        try { return cell.GetString()?.Trim() ?? ""; }
        catch { return cell.Value.ToString()?.Trim() ?? ""; }
    }

    public async Task<object> GetClientesDiariosAsync(Guid negocioId)
    {
        return await _context.Clientes
            .Where(c => c.NegocioId == negocioId && c.EsDiario)
            .OrderByDescending(c => c.FechaDeCreacion)
            .Select(c => new
            {
                c.ClienteId,
                c.Nombre,
                c.Apellido,
                NombreCompleto = $"{c.Nombre} {c.Apellido}",
                c.Telefono,
                c.FechaDeCreacion
            })
            .ToListAsync();
    }
}
