// ═══════════════════════════════════════════════════════════
// VendedorService.cs — Servicio de gestión de vendedores
// Maneja CRUD de vendedores, login, asignación de negocios,
// estadísticas de rendimiento y gestión de leads comerciales.
// ═══════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los vendedores activos con conteo de negocios asignados
    /// </summary>
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

    /// <summary>
    /// Obtiene un vendedor específico por su ID
    /// </summary>
    public async Task<Vendedor?> GetVendedorAsync(Guid id)
    {
        return await _context.Vendedores.FirstOrDefaultAsync(v => v.VendedorId == id && v.IsActive);
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE VENDEDORES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo vendedor con contraseña hasheada (mínimo 6 caracteres)
    /// </summary>
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
            FechaCreacion = TimeHelper.Now
        };

        _context.Vendedores.Add(vendedor);
        await _context.SaveChangesAsync();

        return (true, $"Vendedor '{nombre} {apellido}' creado exitosamente");
    }

    /// <summary>
    /// Edita un vendedor existente. Si se envía contraseña nueva, se re-hashea.
    /// </summary>
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

    /// <summary>
    /// Elimina un vendedor de forma lógica (IsActive = false)
    /// </summary>
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

    // ═══════════════════════════════════════════════════════════
    // LOGIN Y SESIÓN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Valida credenciales de un vendedor y retorna el objeto si es válido
    /// </summary>
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

    // ═══════════════════════════════════════════════════════════
    // NEGOCIOS ASIGNADOS Y ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene los negocios asignados a un vendedor con estadísticas básicas
    /// </summary>
    public async Task<List<object>> GetNegociosByVendedorAsync(Guid vendedorId)
    {
        var now = TimeHelper.Now;
        return (await _context.Negocios
            .Where(n => n.VendedorId == vendedorId)
            .OrderByDescending(n => n.FechaCreacion)
            .Select(n => new
            {
                n.NegocioId,
                n.NegocioNombre,
                n.DuenoNegocio,
                n.Email,
                n.Telefono,
                n.IsActive,
                n.EsPrueba,
                totalClientes = _context.Clientes.Count(c => c.NegocioId == n.NegocioId),
                fechaCreacion = n.FechaCreacion.ToString("yyyy-MM-dd"),
                diasRestantesSuscripcion = n.FechaExpiracion.HasValue
                    ? (int)(n.FechaExpiracion.Value.Date - now.Date).TotalDays
                    : (int?)null,
                n.FechaExpiracion,
                n.DiasPagados,
                n.PrecioSuscripcion,
                porEmpezar = n.FechaPago.HasValue && n.FechaPago.Value.Date > now.Date
            })
            .ToListAsync())
            .Cast<object>()
            .ToList();
    }

    /// <summary>
    /// Calcula estadísticas del vendedor: negocios totales, activos, en prueba y clientes
    /// </summary>
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

    // ═══════════════════════════════════════════════════════════
    // GESTIÓN DE LEADS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los leads ordenados por estado (no atendidos primero) y fecha
    /// </summary>
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

    /// <summary>
    /// Marca un lead como atendido por un vendedor específico
    /// </summary>
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

    /// <summary>
    /// Obtiene el conteo de leads no atendidos (para badge de notificaciones)
    /// </summary>
    public async Task<int> GetLeadsCountAsync()
    {
        return await _context.LeadsVendedor.CountAsync(l => !l.Atendido);
    }
}
