using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetNotificacionesAsync(Guid gimnasioId)
    {
        return await _context.Notificaciones
            .Where(n => n.GimnasioId == gimnasioId)
            .OrderByDescending(n => n.FechaCreacion)
            .Select(n => new
            {
                n.Id,
                n.Mensaje,
                n.Tipo,
                n.ClienteId,
                n.NombreCliente,
                n.Leida,
                n.FechaCreacion
            })
            .Take(50)
            .ToListAsync();
    }

    public async Task<int> GetNotificacionesCountAsync(Guid gimnasioId)
    {
        return await _context.Notificaciones
            .CountAsync(n => n.GimnasioId == gimnasioId && !n.Leida);
    }

    public async Task<(bool success, string message)> MarcarLeidaAsync(Guid id, Guid gimnasioId)
    {
        var notificacion = await _context.Notificaciones
            .FirstOrDefaultAsync(n => n.Id == id && n.GimnasioId == gimnasioId);

        if (notificacion == null)
            return (false, "Notificación no encontrada");

        notificacion.Leida = true;
        _context.Update(notificacion);
        await _context.SaveChangesAsync();

        return (true, "Notificación marcada como leída");
    }

    public async Task<(bool success, string message)> MarcarTodasLeidasAsync(Guid gimnasioId)
    {
        var notificaciones = await _context.Notificaciones
            .Where(n => n.GimnasioId == gimnasioId && !n.Leida)
            .ToListAsync();

        foreach (var n in notificaciones)
            n.Leida = true;

        await _context.SaveChangesAsync();

        return (true, $"{notificaciones.Count} notificaciones marcadas como leídas");
    }

    public async Task<(bool success, string message, int count)> GenerarNotificacionesAsync(Guid gimnasioId)
    {
        var hoy = DateTime.Now.Date;
        var en3Dias = hoy.AddDays(3);

        var clientesProximos = await _context.Clientes
            .Where(c => c.GimnasioId == gimnasioId
                && c.FechaQueTermina.Date >= hoy
                && c.FechaQueTermina.Date <= en3Dias)
            .ToListAsync();

        int count = 0;
        foreach (var cliente in clientesProximos)
        {
            // Check if notification already exists for this client today
            var existe = await _context.Notificaciones
                .AnyAsync(n => n.GimnasioId == gimnasioId
                    && n.ClienteId == cliente.ClienteId
                    && n.FechaCreacion.Date == hoy);

            if (existe) continue;

            var diasRestantes = (cliente.FechaQueTermina.Date - hoy).Days;
            var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";

            var notificacion = new Notificacion
            {
                Id = Guid.NewGuid(),
                GimnasioId = gimnasioId,
                Mensaje = diasRestantes == 0
                    ? $"La membresía de {nombreCompleto} vence HOY"
                    : $"La membresía de {nombreCompleto} vence en {diasRestantes} día(s)",
                Tipo = "vencimiento",
                ClienteId = cliente.ClienteId,
                NombreCliente = nombreCompleto,
                Leida = false,
                FechaCreacion = DateTime.Now
            };

            _context.Notificaciones.Add(notificacion);
            count++;
        }

        await _context.SaveChangesAsync();

        return (true, $"Se generaron {count} notificaciones", count);
    }
}
