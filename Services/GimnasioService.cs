using ClosedXML.Excel;
using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class GimnasioService : IGimnasioService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;

    public GimnasioService(ApplicationDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<object> GetAllGimnasiosAsync()
    {
        return await _context.Gimnasios
            .Select(g => new
            {
                g.GimnasioId,
                g.GimnasioNombre,
                g.DuenoGimnasio,
                g.Telefono,
                g.Email,
                g.IsActive,
                g.EsPrueba,
                g.FechaCreacion,
                TotalClientes = _context.Clientes.Count(c => c.GimnasioId == g.GimnasioId)
            })
            .OrderByDescending(g => g.FechaCreacion)
            .ToListAsync();
    }

    public async Task<object> GetGimnasioAsync(Guid id)
    {
        var gimnasio = await _context.Gimnasios.FindAsync(id);
        if (gimnasio == null) return null;

        return new
        {
            gimnasio.GimnasioId,
            gimnasio.GimnasioNombre,
            gimnasio.DuenoGimnasio,
            gimnasio.Telefono,
            gimnasio.Email,
            gimnasio.Password,
            gimnasio.IsActive,
            gimnasio.EsPrueba
        };
    }

    public async Task<(bool success, string message)> CreateGimnasioAsync(string nombre, string dueno, string telefono, string email, string password, bool isActive, bool esPrueba)
    {
        if (isActive && esPrueba)
            return (false, "Un gimnasio no puede ser de pago y de prueba al mismo tiempo");
        if (!isActive && !esPrueba)
            return (false, "Debe seleccionar si el gimnasio es de pago o de prueba");
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Correo del Gimnasio es necesario");

        bool existing = await _context.Gimnasios.AnyAsync(c => c.Email == email);
        if (existing)
            return (false, "Gimnasio con este email ya existe");
        if (string.IsNullOrWhiteSpace(nombre))
            return (false, "Nombre del Gimnasio es necesario");
        if (string.IsNullOrWhiteSpace(dueno))
            return (false, "Dueño Gimnasio es necesario");
        if (string.IsNullOrWhiteSpace(telefono))
            return (false, "Teléfono es necesario");
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password es necesario");

        var gimnasio = new Gym
        {
            GimnasioId = Guid.NewGuid(),
            GimnasioNombre = nombre,
            DuenoGimnasio = dueno,
            Telefono = telefono,
            Email = email,
            Password = _authService.HashPassword(password),
            IsActive = isActive,
            EsPrueba = esPrueba,
            FechaCreacion = DateTime.Now,
            FechaDeActualizacion = DateTime.Now,
        };

        _context.Gimnasios.Add(gimnasio);
        await _context.SaveChangesAsync();

        return (true, "Gimnasio creado exitosamente");
    }

    public async Task<(bool success, string message)> EditarGimnasioAsync(Gym gimnasio)
    {
        if (gimnasio.IsActive && gimnasio.EsPrueba)
            return (false, "Un gimnasio no puede ser de pago y de prueba al mismo tiempo");
        if (!gimnasio.IsActive && !gimnasio.EsPrueba)
            return (false, "Debe seleccionar si el gimnasio es de pago o de prueba");

        var existente = await _context.Gimnasios.FindAsync(gimnasio.GimnasioId);
        if (existente == null)
            return (false, "Gimnasio no encontrado");

        var existeEmail = await _context.Gimnasios
            .AnyAsync(g => g.Email == gimnasio.Email && g.GimnasioId != gimnasio.GimnasioId);
        if (existeEmail)
            return (false, "Ya existe otro gimnasio con ese email");

        existente.GimnasioNombre = gimnasio.GimnasioNombre;
        existente.DuenoGimnasio = gimnasio.DuenoGimnasio;
        existente.Telefono = gimnasio.Telefono;
        existente.Email = gimnasio.Email;
        existente.IsActive = gimnasio.IsActive;
        existente.EsPrueba = gimnasio.EsPrueba;
        existente.FechaDeActualizacion = DateTime.Now;

        if (!string.IsNullOrEmpty(gimnasio.Password))
        {
            existente.Password = _authService.HashPassword(gimnasio.Password);
        }

        _context.Update(existente);
        await _context.SaveChangesAsync();

        return (true, "Gimnasio actualizado exitosamente");
    }

    public async Task<(bool success, string message)> EliminarGimnasioAsync(Guid id)
    {
        var gimnasio = await _context.Gimnasios
            .Include(g => g.Clientes)
            .FirstOrDefaultAsync(g => g.GimnasioId == id);

        if (gimnasio == null)
            return (false, "Gimnasio no encontrado");

        if (gimnasio.Clientes.Any())
            return (false, $"No se puede eliminar. El gimnasio tiene {gimnasio.Clientes.Count} cliente(s) registrado(s)");

        _context.Gimnasios.Remove(gimnasio);
        await _context.SaveChangesAsync();

        return (true, "Gimnasio eliminado exitosamente");
    }

    public async Task<(bool success, string message, bool? isActive, bool? esPrueba)> CambiarEstadoAsync(Guid id)
    {
        var gimnasio = await _context.Gimnasios.FindAsync(id);
        if (gimnasio == null)
            return (false, "Gimnasio no encontrado", null, null);

        if (gimnasio.IsActive)
        {
            gimnasio.IsActive = false;
            gimnasio.EsPrueba = true;
        }
        else
        {
            gimnasio.IsActive = true;
            gimnasio.EsPrueba = false;
        }

        gimnasio.FechaDeActualizacion = DateTime.Now;
        _context.Update(gimnasio);
        await _context.SaveChangesAsync();

        string tipoActual = gimnasio.IsActive ? "Pago (Activo)" : "Prueba";
        return (true, $"Gimnasio cambiado a modo {tipoActual} exitosamente", gimnasio.IsActive, gimnasio.EsPrueba);
    }

    public async Task<byte[]> ExportExcelAsync()
    {
        var gimnasios = await _context.Gimnasios
            .Include(g => g.Clientes)
            .OrderByDescending(g => g.FechaCreacion)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Gimnasios");

        worksheet.Cell(1, 1).Value = "Nombre del Gimnasio";
        worksheet.Cell(1, 2).Value = "Dueño";
        worksheet.Cell(1, 3).Value = "Email";
        worksheet.Cell(1, 4).Value = "Teléfono";
        worksheet.Cell(1, 5).Value = "Estado";
        worksheet.Cell(1, 6).Value = "Es Prueba";
        worksheet.Cell(1, 7).Value = "Total Clientes";
        worksheet.Cell(1, 8).Value = "Fecha Creación";

        var headerRange = worksheet.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var gimnasio in gimnasios)
        {
            worksheet.Cell(row, 1).Value = gimnasio.GimnasioNombre;
            worksheet.Cell(row, 2).Value = gimnasio.DuenoGimnasio;
            worksheet.Cell(row, 3).Value = gimnasio.Email;
            worksheet.Cell(row, 4).Value = gimnasio.Telefono;
            worksheet.Cell(row, 5).Value = gimnasio.IsActive ? "Activo" : "Inactivo";
            worksheet.Cell(row, 6).Value = gimnasio.EsPrueba ? "Sí" : "No";
            worksheet.Cell(row, 7).Value = gimnasio.Clientes.Count;
            worksheet.Cell(row, 8).Value = gimnasio.FechaCreacion.ToString("dd/MM/yyyy");
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
