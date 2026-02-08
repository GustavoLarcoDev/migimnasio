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

    public async Task<object> GetDashboardStatsAsync(Guid gimnasioId)
    {
        var clientes = await _context.Clientes
            .Where(c => c.GimnasioId == gimnasioId)
            .ToListAsync();

        var totalClientes = clientes.Count;
        var clientesActivos = clientes.Count(c => c.FechaQueTermina.Date >= DateTime.Now.Date);
        var clientesVencidos = clientes.Count(c => c.FechaQueTermina.Date < DateTime.Now.Date);
        var clientesNuevosHoy = clientes.Count(c => c.FechaDeCreacion.Date == DateTime.Now.Date);
        var clientesNuevosMes = clientes.Count(c => c.FechaDeCreacion.Month == DateTime.Now.Month && c.FechaDeCreacion.Year == DateTime.Now.Year);

        var ingresosMes = clientes
            .Where(c => c.FechaDeActualizacion.Month == DateTime.Now.Month && c.FechaDeActualizacion.Year == DateTime.Now.Year)
            .Sum(c => c.Precio);

        var ingresosHoy = clientes
            .Where(c => c.FechaDeActualizacion.Date == DateTime.Now.Date)
            .Sum(c => c.Precio);

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

    public async Task<object> GetClientesAsync(Guid gimnasioId)
    {
        return await _context.Clientes
            .Where(c => c.GimnasioId == gimnasioId)
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

    public async Task<Cliente> GetClienteAsync(Guid id, Guid gimnasioId)
    {
        return await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == id && c.GimnasioId == gimnasioId);
    }

    public async Task<(bool success, string message)> CrearClienteAsync(ClienteCreateDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Nombre) || string.IsNullOrWhiteSpace(model.Apellido))
            return (false, "Nombre y Apellido son obligatorios");
        if (string.IsNullOrWhiteSpace(model.Telefono))
            return (false, "Teléfono es obligatorio");
        if (model.Dias <= 0)
            return (false, "Los días deben ser mayor a 0");
        if (model.Precio <= 0)
            return (false, "El precio debe ser mayor a 0");

        var cliente = new Cliente
        {
            ClienteId = Guid.NewGuid(),
            GimnasioId = model.GimnasioId,
            Nombre = model.Nombre,
            Apellido = model.Apellido,
            Email = model.Email,
            Telefono = model.Telefono,
            Direccion = model.Direccion,
            Dias = model.Dias,
            Precio = model.Precio,
            EsDiario = model.EsDiario,
            FechaDeCreacion = DateTime.Now,
            FechaDeActualizacion = DateTime.Now,
            FechaQueTermina = DateTime.Now.AddDays(model.Dias)
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
        await _logService.CreateLogAsync(model.GimnasioId, "cliente_creado",
            $"Nuevo cliente creado: {nombreCompleto}", model.Precio, cliente.ClienteId, nombreCompleto);

        return (true, "Cliente creado exitosamente");
    }

    public async Task<(bool success, string message)> EditarClienteAsync(ClienteCreateDto model)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == model.ClienteId && c.GimnasioId == model.GimnasioId);

        if (cliente == null)
            return (false, "Cliente no encontrado");
        if (string.IsNullOrWhiteSpace(model.Nombre) || string.IsNullOrWhiteSpace(model.Apellido))
            return (false, "Nombre y Apellido son obligatorios");
        if (string.IsNullOrWhiteSpace(model.Telefono))
            return (false, "Teléfono es obligatorio");
        if (model.Dias <= 0)
            return (false, "Los días deben ser mayor a 0");
        if (model.Precio <= 0)
            return (false, "El precio debe ser mayor a 0");

        cliente.Nombre = model.Nombre;
        cliente.Apellido = model.Apellido;
        cliente.Email = model.Email;
        cliente.Telefono = model.Telefono;
        cliente.Direccion = model.Direccion;
        cliente.Dias = model.Dias;
        cliente.Precio = model.Precio;
        cliente.EsDiario = model.EsDiario;
        cliente.FechaDeActualizacion = DateTime.Now;

        _context.Update(cliente);
        await _context.SaveChangesAsync();

        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
        await _logService.CreateLogAsync(model.GimnasioId, "cliente_editado",
            $"Cliente editado: {nombreCompleto}", 0, cliente.ClienteId, nombreCompleto);

        return (true, "Cliente actualizado exitosamente");
    }

    public async Task<(bool success, string message)> EliminarClienteAsync(Guid id, Guid gimnasioId)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == id && c.GimnasioId == gimnasioId);

        if (cliente == null)
            return (false, "Cliente no encontrado");

        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";

        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync();

        await _logService.CreateLogAsync(gimnasioId, "cliente_eliminado",
            $"Cliente eliminado: {nombreCompleto}", 0, cliente.ClienteId, nombreCompleto);

        return (true, "Cliente eliminado exitosamente");
    }

    public async Task<(bool success, string message)> RenovarClienteAsync(Guid id, Guid gimnasioId, int dias, decimal precio)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == id && c.GimnasioId == gimnasioId);

        if (cliente == null)
            return (false, "Cliente no encontrado");
        if (dias <= 0)
            return (false, "Días debe ser mayor a 0");

        if (cliente.FechaQueTermina < DateTime.Now)
            cliente.FechaQueTermina = DateTime.Now.AddDays(dias);
        else
            cliente.FechaQueTermina = cliente.FechaQueTermina.AddDays(dias);

        cliente.Dias = dias;
        cliente.Precio = precio;
        cliente.FechaDeActualizacion = DateTime.Now;

        _context.Update(cliente);
        await _context.SaveChangesAsync();

        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
        await _logService.CreateLogAsync(gimnasioId, "cliente_renovado",
            $"Renovación: {nombreCompleto} ({dias} días)", precio, cliente.ClienteId, nombreCompleto);

        return (true, "Membresía renovada exitosamente");
    }

    public async Task<byte[]> ExportClientesExcelAsync(Guid gimnasioId)
    {
        var clientes = await _context.Clientes
            .Where(c => c.GimnasioId == gimnasioId)
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
        worksheet.Cell(1, 7).Value = "Estado";
        worksheet.Cell(1, 8).Value = "Tipo";
        worksheet.Cell(1, 9).Value = "Último Precio";

        var headerRange = worksheet.Range(1, 1, 1, 9);
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
            worksheet.Cell(row, 5).Value = cliente.FechaDeCreacion.ToString("dd/MM/yyyy");
            worksheet.Cell(row, 6).Value = cliente.FechaQueTermina.ToString("dd/MM/yyyy");
            bool activo = cliente.FechaQueTermina.Date >= DateTime.Now.Date;
            worksheet.Cell(row, 7).Value = activo ? "Activo" : "Vencido";
            worksheet.Cell(row, 8).Value = cliente.EsDiario ? "Diario" : "Regular";
            worksheet.Cell(row, 9).Value = cliente.Precio;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<object> ImportarClientesExcelAsync(Guid gimnasioId, Stream fileStream)
    {
        var clientesCreados = new List<string>();
        var clientesOmitidos = new List<string>();
        var errores = new List<string>();

        using var memStream = new MemoryStream();
        await fileStream.CopyToAsync(memStream);
        memStream.Position = 0;

        using var workbook = new XLWorkbook(memStream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1);

        if (rows == null)
            return new { success = false, message = "El archivo está vacío" };

        var clientesExistentes = await _context.Clientes
            .Where(c => c.GimnasioId == gimnasioId)
            .Select(c => new { c.Nombre, c.Apellido })
            .ToListAsync();

        int rowNumber = 2;
        foreach (var row in rows)
        {
            try
            {
                var nombre = row.Cell(1).GetValue<string>()?.Trim();
                var apellido = row.Cell(2).GetValue<string>()?.Trim();

                if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
                {
                    errores.Add($"Fila {rowNumber}: Nombre y Apellido son obligatorios");
                    rowNumber++;
                    continue;
                }

                var existeDuplicado = clientesExistentes.Any(c =>
                    c.Nombre == nombre && c.Apellido == apellido);

                if (existeDuplicado)
                {
                    clientesOmitidos.Add($"{nombre} {apellido}");
                    rowNumber++;
                    continue;
                }

                var email = row.Cell(3).GetValue<string>()?.Trim();
                var telefono = row.Cell(4).GetValue<string>()?.Trim();
                var direccion = row.Cell(5).GetValue<string>()?.Trim();

                int dias = 30;
                decimal precio = 0;
                try { dias = row.Cell(6).GetValue<int>(); } catch { }
                try { precio = row.Cell(7).GetValue<decimal>(); } catch { }
                if (dias <= 0) dias = 30;

                var cliente = new Cliente
                {
                    ClienteId = Guid.NewGuid(),
                    GimnasioId = gimnasioId,
                    Nombre = nombre,
                    Apellido = apellido,
                    Email = email,
                    Telefono = telefono ?? "",
                    Direccion = direccion,
                    Dias = dias,
                    Precio = precio,
                    EsDiario = false,
                    FechaDeCreacion = DateTime.Now,
                    FechaDeActualizacion = DateTime.Now,
                    FechaQueTermina = DateTime.Now.AddDays(dias)
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

    public async Task<object> GetClientesDiariosAsync(Guid gimnasioId)
    {
        return await _context.Clientes
            .Where(c => c.GimnasioId == gimnasioId && c.EsDiario)
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
