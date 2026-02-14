using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class SugerenciaService : ISugerenciaService
{
    private readonly ApplicationDbContext _context;

    public SugerenciaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool success, string message)> CrearSugerenciaAsync(Guid negocioId, string negocioNombre, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return (false, "El mensaje es obligatorio");

        if (mensaje.Length > 1000)
            return (false, "El mensaje no puede exceder 1000 caracteres");

        var sugerencia = new Sugerencia
        {
            Id = Guid.NewGuid(),
            NegocioId = negocioId,
            NegocioNombre = negocioNombre,
            Mensaje = mensaje.Trim(),
            FechaCreacion = DateTime.Now
        };

        _context.Sugerencias.Add(sugerencia);
        await _context.SaveChangesAsync();

        return (true, "Sugerencia enviada exitosamente. Gracias por tu feedback.");
    }

    public async Task<object> GetSugerenciasAsync()
    {
        return await _context.Sugerencias
            .OrderByDescending(s => s.FechaCreacion)
            .Select(s => new
            {
                s.Id,
                s.NegocioId,
                s.NegocioNombre,
                s.Mensaje,
                s.FechaCreacion,
                s.Leida
            })
            .ToListAsync();
    }

    public async Task<(bool success, string message)> MarcarLeidaAsync(Guid id)
    {
        var sugerencia = await _context.Sugerencias.FindAsync(id);
        if (sugerencia == null)
            return (false, "Sugerencia no encontrada");

        sugerencia.Leida = true;
        await _context.SaveChangesAsync();

        return (true, "Marcada como leída");
    }
}
