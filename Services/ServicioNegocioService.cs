// ═══════════════════════════════════════════════════════════
// ServicioNegocioService.cs — Servicio de gestión de servicios del negocio
// Maneja CRUD de servicios ofrecidos (cortes, combos, etc.)
// con validación de precios, duración y eliminación lógica.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class ServicioNegocioService : IServicioNegocioService
{
    private readonly ApplicationDbContext _context;

    public ServicioNegocioService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los servicios activos del negocio
    /// </summary>
    public async Task<object> GetServiciosAsync(Guid negocioId)
    {
        return await _context.ServiciosNegocio
            .Where(s => s.NegocioId == negocioId && s.IsActive)
            .OrderBy(s => s.Nombre)
            .Select(s => new
            {
                s.ServicioId,
                s.Nombre,
                s.Descripcion,
                s.ItemsIncluidos,
                s.DuracionMinutos,
                s.Precio,
                s.EsCombo,
                s.FechaCreacion
            })
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un servicio específico por su ID
    /// </summary>
    public async Task<ServicioNegocio> GetServicioAsync(Guid servicioId, Guid negocioId)
    {
        return await _context.ServiciosNegocio
            .FirstOrDefaultAsync(s => s.ServicioId == servicioId && s.NegocioId == negocioId && s.IsActive);
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE SERVICIOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo servicio con validación de nombre, precio y duración
    /// </summary>
    public async Task<(bool success, string message)> CrearServicioAsync(ServicioCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return (false, "El nombre del servicio es obligatorio");
        if (dto.Precio <= 0)
            return (false, "El precio debe ser mayor a 0");
        if (dto.DuracionMinutos <= 0)
            return (false, "La duración debe ser mayor a 0 minutos");

        var servicio = new ServicioNegocio
        {
            ServicioId = Guid.NewGuid(),
            NegocioId = dto.NegocioId,
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            ItemsIncluidos = dto.ItemsIncluidos,
            DuracionMinutos = dto.DuracionMinutos,
            Precio = dto.Precio,
            EsCombo = dto.EsCombo,
            IsActive = true,
            FechaCreacion = TimeHelper.Now,
            FechaDeActualizacion = TimeHelper.Now
        };

        _context.ServiciosNegocio.Add(servicio);
        await _context.SaveChangesAsync();

        return (true, "Servicio creado exitosamente");
    }

    /// <summary>
    /// Edita un servicio existente con validación de campos obligatorios
    /// </summary>
    public async Task<(bool success, string message)> EditarServicioAsync(ServicioCreateDto dto)
    {
        var servicio = await _context.ServiciosNegocio
            .FirstOrDefaultAsync(s => s.ServicioId == dto.ServicioId && s.NegocioId == dto.NegocioId && s.IsActive);

        if (servicio == null)
            return (false, "Servicio no encontrado");
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return (false, "El nombre del servicio es obligatorio");
        if (dto.Precio <= 0)
            return (false, "El precio debe ser mayor a 0");
        if (dto.DuracionMinutos <= 0)
            return (false, "La duración debe ser mayor a 0 minutos");

        servicio.Nombre = dto.Nombre;
        servicio.Descripcion = dto.Descripcion;
        servicio.ItemsIncluidos = dto.ItemsIncluidos;
        servicio.DuracionMinutos = dto.DuracionMinutos;
        servicio.Precio = dto.Precio;
        servicio.EsCombo = dto.EsCombo;
        servicio.FechaDeActualizacion = TimeHelper.Now;

        await _context.SaveChangesAsync();

        return (true, "Servicio actualizado exitosamente");
    }

    /// <summary>
    /// Elimina un servicio de forma lógica (IsActive = false)
    /// </summary>
    public async Task<(bool success, string message)> EliminarServicioAsync(Guid servicioId, Guid negocioId)
    {
        var servicio = await _context.ServiciosNegocio
            .FirstOrDefaultAsync(s => s.ServicioId == servicioId && s.NegocioId == negocioId && s.IsActive);

        if (servicio == null)
            return (false, "Servicio no encontrado");

        servicio.IsActive = false;
        servicio.FechaDeActualizacion = TimeHelper.Now;
        await _context.SaveChangesAsync();

        return (true, "Servicio eliminado exitosamente");
    }
}
