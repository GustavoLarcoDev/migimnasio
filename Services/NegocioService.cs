using ClosedXML.Excel;
using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class NegocioService : INegocioService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;

    public NegocioService(ApplicationDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<object> GetAllNegociosAsync()
    {
        return await _context.Negocios
            .Select(g => new
            {
                g.NegocioId,
                g.NegocioNombre,
                g.DuenoNegocio,
                g.Telefono,
                g.Email,
                g.IsActive,
                g.EsPrueba,
                g.FechaCreacion,
                TotalClientes = _context.Clientes.Count(c => c.NegocioId == g.NegocioId)
            })
            .OrderByDescending(g => g.FechaCreacion)
            .ToListAsync();
    }

    public async Task<object> GetNegocioAsync(Guid id)
    {
        var negocio = await _context.Negocios.FindAsync(id);
        if (negocio == null) return null;

        return new
        {
            negocio.NegocioId,
            negocio.NegocioNombre,
            negocio.DuenoNegocio,
            negocio.Telefono,
            negocio.Email,
            negocio.Password,
            negocio.IsActive,
            negocio.EsPrueba
        };
    }

    public async Task<(bool success, string message)> CreateNegocioAsync(string nombre, string dueno, string telefono, string email, string password, bool isActive, bool esPrueba)
    {
        if (isActive && esPrueba)
            return (false, "Un negocio no puede ser de pago y de prueba al mismo tiempo");
        if (!isActive && !esPrueba)
            return (false, "Debe seleccionar si el negocio es de pago o de prueba");
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Correo del Negocio es necesario");

        bool existing = await _context.Negocios.AnyAsync(c => c.Email == email);
        if (existing)
            return (false, "Negocio con este email ya existe");
        if (string.IsNullOrWhiteSpace(nombre))
            return (false, "Nombre del Negocio es necesario");
        if (string.IsNullOrWhiteSpace(dueno))
            return (false, "Dueño del Negocio es necesario");
        if (string.IsNullOrWhiteSpace(telefono))
            return (false, "Teléfono es necesario");
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password es necesario");

        var negocio = new Gym
        {
            NegocioId = Guid.NewGuid(),
            NegocioNombre = nombre,
            DuenoNegocio = dueno,
            Telefono = telefono,
            Email = email,
            Password = _authService.HashPassword(password),
            IsActive = isActive,
            EsPrueba = esPrueba,
            FechaCreacion = DateTime.Now,
            FechaDeActualizacion = DateTime.Now,
        };

        _context.Negocios.Add(negocio);
        await _context.SaveChangesAsync();

        return (true, "Negocio creado exitosamente");
    }

    public async Task<(bool success, string message)> EditarNegocioAsync(Gym negocio)
    {
        if (negocio.IsActive && negocio.EsPrueba)
            return (false, "Un negocio no puede ser de pago y de prueba al mismo tiempo");
        if (!negocio.IsActive && !negocio.EsPrueba)
            return (false, "Debe seleccionar si el negocio es de pago o de prueba");

        var existente = await _context.Negocios.FindAsync(negocio.NegocioId);
        if (existente == null)
            return (false, "Negocio no encontrado");

        var existeEmail = await _context.Negocios
            .AnyAsync(g => g.Email == negocio.Email && g.NegocioId != negocio.NegocioId);
        if (existeEmail)
            return (false, "Ya existe otro negocio con ese email");

        existente.NegocioNombre = negocio.NegocioNombre;
        existente.DuenoNegocio = negocio.DuenoNegocio;
        existente.Telefono = negocio.Telefono;
        existente.Email = negocio.Email;
        existente.IsActive = negocio.IsActive;
        existente.EsPrueba = negocio.EsPrueba;
        existente.FechaDeActualizacion = DateTime.Now;

        if (!string.IsNullOrEmpty(negocio.Password))
        {
            existente.Password = _authService.HashPassword(negocio.Password);
        }

        _context.Update(existente);
        await _context.SaveChangesAsync();

        return (true, "Negocio actualizado exitosamente");
    }

    public async Task<(bool success, string message)> EliminarNegocioAsync(Guid id)
    {
        var negocio = await _context.Negocios
            .Include(g => g.Clientes)
            .FirstOrDefaultAsync(g => g.NegocioId == id);

        if (negocio == null)
            return (false, "Negocio no encontrado");

        if (negocio.Clientes.Any())
            return (false, $"No se puede eliminar. El negocio tiene {negocio.Clientes.Count} cliente(s) registrado(s)");

        _context.Negocios.Remove(negocio);
        await _context.SaveChangesAsync();

        return (true, "Negocio eliminado exitosamente");
    }

    public async Task<(bool success, string message, bool? isActive, bool? esPrueba)> CambiarEstadoAsync(Guid id)
    {
        var negocio = await _context.Negocios.FindAsync(id);
        if (negocio == null)
            return (false, "Negocio no encontrado", null, null);

        if (negocio.IsActive)
        {
            negocio.IsActive = false;
            negocio.EsPrueba = true;
        }
        else
        {
            negocio.IsActive = true;
            negocio.EsPrueba = false;
        }

        negocio.FechaDeActualizacion = DateTime.Now;
        _context.Update(negocio);
        await _context.SaveChangesAsync();

        string tipoActual = negocio.IsActive ? "Pago (Activo)" : "Prueba";
        return (true, $"Negocio cambiado a modo {tipoActual} exitosamente", negocio.IsActive, negocio.EsPrueba);
    }

    public async Task<Gym> GetNegocioForImpersonationAsync(Guid id)
    {
        return await _context.Negocios.FindAsync(id);
    }

    public async Task<byte[]> ExportExcelAsync()
    {
        var negocios = await _context.Negocios
            .Include(g => g.Clientes)
            .OrderByDescending(g => g.FechaCreacion)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Negocios");

        worksheet.Cell(1, 1).Value = "Nombre del Negocio";
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
        foreach (var negocio in negocios)
        {
            worksheet.Cell(row, 1).Value = negocio.NegocioNombre;
            worksheet.Cell(row, 2).Value = negocio.DuenoNegocio;
            worksheet.Cell(row, 3).Value = negocio.Email;
            worksheet.Cell(row, 4).Value = negocio.Telefono;
            worksheet.Cell(row, 5).Value = negocio.IsActive ? "Activo" : "Inactivo";
            worksheet.Cell(row, 6).Value = negocio.EsPrueba ? "Sí" : "No";
            worksheet.Cell(row, 7).Value = negocio.Clientes.Count;
            worksheet.Cell(row, 8).Value = negocio.FechaCreacion.ToString("dd/MM/yyyy");
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
