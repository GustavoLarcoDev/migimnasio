// ═══════════════════════════════════════════════════════════
// MesaService.cs — Gestión de mesas de restaurante
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class MesaService : IMesaService
{
    private readonly ApplicationDbContext _context;

    public MesaService(ApplicationDbContext context) => _context = context;

    public async Task<object> GetMesasAsync(Guid negocioId)
    {
        return await _context.Mesas
            .Where(m => m.NegocioId == negocioId && m.IsActive)
            .OrderBy(m => m.Numero)
            .Select(m => new
            {
                m.MesaId,
                m.Numero,
                m.Nombre,
                m.Capacidad,
                m.Estado,
                m.FechaCreacion
            })
            .ToListAsync();
    }

    public async Task<Mesa> GetMesaAsync(Guid mesaId, Guid negocioId)
    {
        return await _context.Mesas
            .FirstOrDefaultAsync(m => m.MesaId == mesaId && m.NegocioId == negocioId && m.IsActive);
    }

    public async Task<(bool success, string message)> CrearMesaAsync(
        Guid negocioId, string nombre, int numero, int capacidad)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return (false, "El nombre de la mesa es obligatorio");

        // Validar unicidad del número de mesa dentro del negocio
        var duplicado = await _context.Mesas.AnyAsync(m => m.NegocioId == negocioId && m.Numero == numero && m.IsActive);
        if (duplicado) return (false, "Ya existe una mesa con ese número");

        if (capacidad <= 0) capacidad = 4;

        var mesa = new Mesa
        {
            MesaId = Guid.NewGuid(),
            NegocioId = negocioId,
            Nombre = nombre.Trim(),
            Numero = numero,
            Capacidad = capacidad,
            Estado = "libre",
            IsActive = true,
            FechaCreacion = TimeHelper.Now
        };

        _context.Mesas.Add(mesa);
        await _context.SaveChangesAsync();
        return (true, "Mesa creada exitosamente");
    }

    public async Task<(bool success, string message)> EditarMesaAsync(
        Guid mesaId, Guid negocioId, string nombre, int numero, int capacidad)
    {
        var mesa = await _context.Mesas
            .FirstOrDefaultAsync(m => m.MesaId == mesaId && m.NegocioId == negocioId && m.IsActive);
        if (mesa == null) return (false, "Mesa no encontrada");

        if (string.IsNullOrWhiteSpace(nombre))
            return (false, "El nombre de la mesa es obligatorio");

        // Validar unicidad del número de mesa dentro del negocio (excluyendo la mesa actual)
        var duplicado = await _context.Mesas.AnyAsync(m => m.NegocioId == negocioId && m.Numero == numero && m.MesaId != mesaId && m.IsActive);
        if (duplicado) return (false, "Ya existe una mesa con ese número");

        mesa.Nombre = nombre.Trim();
        mesa.Numero = numero;
        mesa.Capacidad = capacidad > 0 ? capacidad : 4;

        await _context.SaveChangesAsync();
        return (true, "Mesa actualizada exitosamente");
    }

    public async Task<(bool success, string message)> CambiarEstadoMesaAsync(
        Guid mesaId, Guid negocioId, string estado)
    {
        var mesa = await _context.Mesas
            .FirstOrDefaultAsync(m => m.MesaId == mesaId && m.NegocioId == negocioId && m.IsActive);
        if (mesa == null) return (false, "Mesa no encontrada");

        var estadosValidos = new[] { "libre", "ocupada", "reservada" };
        if (!estadosValidos.Contains(estado))
            return (false, "Estado no valido. Usar: libre, ocupada, reservada");

        mesa.Estado = estado;
        await _context.SaveChangesAsync();
        return (true, $"Mesa {mesa.Nombre} ahora esta {estado}");
    }

    public async Task<(bool success, string message)> EliminarMesaAsync(Guid mesaId, Guid negocioId)
    {
        var mesa = await _context.Mesas
            .FirstOrDefaultAsync(m => m.MesaId == mesaId && m.NegocioId == negocioId && m.IsActive);
        if (mesa == null) return (false, "Mesa no encontrada");

        mesa.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, "Mesa eliminada exitosamente");
    }
}
