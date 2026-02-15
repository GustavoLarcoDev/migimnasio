#nullable enable
using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class VendedorService : IVendedorService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;

    public VendedorService(ApplicationDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<List<object>> GetAllVendedoresAsync()
    {
        var vendedores = await _context.Vendedores
            .Where(v => v.IsActive)
            .OrderByDescending(v => v.FechaCreacion)
            .ToListAsync();

        var vendedorIds = vendedores.Select(v => v.VendedorId).ToHashSet();

        var negociosPorVendedor = await _context.Negocios
            .Where(n => n.VendedorId.HasValue && vendedorIds.Contains(n.VendedorId.Value) && n.IsActive && !n.EsPrueba)
            .GroupBy(n => n.VendedorId!.Value)
            .Select(g => new { VendedorId = g.Key, Count = g.Count() })
            .ToListAsync();

        var countMap = negociosPorVendedor.ToDictionary(x => x.VendedorId, x => x.Count);

        return vendedores.Select(v => (object)new
        {
            v.VendedorId,
            v.Nombre,
            v.Apellido,
            nombreCompleto = $"{v.Nombre} {v.Apellido}",
            v.Correo,
            v.Telefono,
            v.IsActive,
            fechaCreacion = v.FechaCreacion.ToString("yyyy-MM-dd"),
            negociosCreados = countMap.GetValueOrDefault(v.VendedorId, 0)
        }).ToList();
    }

    public async Task<Vendedor?> GetVendedorAsync(Guid id)
    {
        return await _context.Vendedores.FirstOrDefaultAsync(v => v.VendedorId == id && v.IsActive);
    }

    public async Task<(bool success, string message)> CrearVendedorAsync(
        string nombre, string apellido, string correo, string telefono, string password)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
            return (false, "Nombre y apellido son obligatorios");

        if (string.IsNullOrWhiteSpace(correo))
            return (false, "El correo es obligatorio");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "La contraseña debe tener al menos 6 caracteres");

        var existeCorreo = await _context.Vendedores.AnyAsync(v => v.Correo == correo && v.IsActive);
        if (existeCorreo)
            return (false, "Ya existe un vendedor con ese correo");

        var vendedor = new Vendedor
        {
            VendedorId = Guid.NewGuid(),
            Nombre = nombre.Trim(),
            Apellido = apellido.Trim(),
            Correo = correo.Trim().ToLower(),
            Telefono = telefono?.Trim() ?? "",
            Password = _authService.HashPassword(password),
            IsActive = true,
            FechaCreacion = DateTime.Now
        };

        _context.Vendedores.Add(vendedor);
        await _context.SaveChangesAsync();

        return (true, $"Vendedor '{nombre} {apellido}' creado exitosamente");
    }

    public async Task<(bool success, string message)> EditarVendedorAsync(
        Guid id, string nombre, string apellido, string correo, string telefono, string? password)
    {
        var vendedor = await _context.Vendedores.FirstOrDefaultAsync(v => v.VendedorId == id && v.IsActive);
        if (vendedor == null)
            return (false, "Vendedor no encontrado");

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
            return (false, "Nombre y apellido son obligatorios");

        var existeCorreo = await _context.Vendedores.AnyAsync(v => v.Correo == correo && v.IsActive && v.VendedorId != id);
        if (existeCorreo)
            return (false, "Ya existe otro vendedor con ese correo");

        vendedor.Nombre = nombre.Trim();
        vendedor.Apellido = apellido.Trim();
        vendedor.Correo = correo.Trim().ToLower();
        vendedor.Telefono = telefono?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(password))
        {
            if (password.Length < 6)
                return (false, "La contraseña debe tener al menos 6 caracteres");
            vendedor.Password = _authService.HashPassword(password);
        }

        _context.Update(vendedor);
        await _context.SaveChangesAsync();

        return (true, $"Vendedor '{nombre} {apellido}' actualizado");
    }

    public async Task<(bool success, string message)> EliminarVendedorAsync(Guid id)
    {
        var vendedor = await _context.Vendedores.FirstOrDefaultAsync(v => v.VendedorId == id);
        if (vendedor == null)
            return (false, "Vendedor no encontrado");

        vendedor.IsActive = false;
        _context.Update(vendedor);
        await _context.SaveChangesAsync();

        return (true, $"Vendedor '{vendedor.Nombre} {vendedor.Apellido}' eliminado");
    }

    public async Task<Vendedor?> LoginVendedorAsync(string correo, string password)
    {
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
            return null;

        var vendedor = await _context.Vendedores
            .FirstOrDefaultAsync(v => v.Correo == correo.Trim().ToLower() && v.IsActive);

        if (vendedor == null)
            return null;

        if (!_authService.VerifyPassword(password, vendedor.Password))
            return null;

        return vendedor;
    }

    public async Task<List<object>> GetNegociosByVendedorAsync(Guid vendedorId)
    {
        var negocios = await _context.Negocios
            .Where(n => n.VendedorId == vendedorId)
            .Include(n => n.Clientes)
            .OrderByDescending(n => n.FechaCreacion)
            .ToListAsync();

        return negocios.Select(n => (object)new
        {
            n.NegocioId,
            n.NegocioNombre,
            n.DuenoNegocio,
            n.Email,
            n.Telefono,
            n.IsActive,
            n.EsPrueba,
            totalClientes = n.Clientes.Count,
            fechaCreacion = n.FechaCreacion.ToString("yyyy-MM-dd"),
            diasRestantes = n.FechaExpiracion.HasValue
                ? (n.FechaExpiracion.Value.Date - DateTime.Now.Date).Days
                : (int?)null,
            n.PrecioSuscripcion
        }).ToList();
    }

    public async Task<object> GetVendedorStatsAsync(Guid vendedorId)
    {
        var negocios = await _context.Negocios
            .Where(n => n.VendedorId == vendedorId)
            .ToListAsync();

        return new
        {
            totalNegocios = negocios.Count,
            activos = negocios.Count(n => n.IsActive && !n.EsPrueba),
            enPrueba = negocios.Count(n => n.EsPrueba),
            totalClientes = await _context.Clientes
                .Where(c => negocios.Select(n => n.NegocioId).Contains(c.NegocioId))
                .CountAsync()
        };
    }

    public async Task<List<object>> GetLeadsAsync()
    {
        var leads = await _context.LeadsVendedor
            .OrderBy(l => l.Atendido)
            .ThenByDescending(l => l.FechaCreacion)
            .ToListAsync();

        return leads.Select(l => (object)new
        {
            l.Id,
            l.Nombre,
            l.NombreNegocio,
            l.Email,
            l.Telefono,
            l.Mensaje,
            l.Atendido,
            l.AtendidoPorNombre,
            fechaCreacion = l.FechaCreacion.ToString("yyyy-MM-dd HH:mm")
        }).ToList();
    }

    public async Task<(bool success, string message)> MarcarLeadAtendidoAsync(Guid leadId, Guid vendedorId, string vendedorNombre)
    {
        var lead = await _context.LeadsVendedor.FirstOrDefaultAsync(l => l.Id == leadId);
        if (lead == null)
            return (false, "Lead no encontrado");

        if (lead.Atendido)
            return (false, $"Este lead ya fue atendido por {lead.AtendidoPorNombre}");

        lead.Atendido = true;
        lead.AtendidoPorId = vendedorId;
        lead.AtendidoPorNombre = vendedorNombre;

        _context.Update(lead);
        await _context.SaveChangesAsync();

        return (true, "Lead marcado como atendido");
    }

    public async Task<int> GetLeadsCountAsync()
    {
        return await _context.LeadsVendedor.CountAsync(l => !l.Atendido);
    }
}
