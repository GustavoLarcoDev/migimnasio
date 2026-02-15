// ═══════════════════════════════════════════════════════════
// NotificationService.cs — Servicio de notificaciones automáticas
// Genera alertas cuando la membresía de un cliente está próxima
// a vencer (3 días). Evita duplicados por día.
// Se ejecuta al cargar el dashboard del negocio.
// ═══════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las últimas 50 notificaciones ordenadas por fecha descendente
    /// </summary>
    public async Task<object> GetNotificacionesAsync(Guid negocioId)
    {
        return await _context.Notificaciones
            .Where(n => n.NegocioId == negocioId)
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

    /// <summary>
    /// Cuenta las notificaciones no leídas para mostrar en el badge
    /// </summary>
    public async Task<int> GetNotificacionesCountAsync(Guid negocioId)
    {
        return await _context.Notificaciones
            .CountAsync(n => n.NegocioId == negocioId && !n.Leida);
    }

    // ═══════════════════════════════════════════════════════════
    // MARCAR COMO LEÍDA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Marca una notificación individual como leída
    /// </summary>
    public async Task<(bool success, string message)> MarcarLeidaAsync(Guid id, Guid negocioId)
    {
        var notificacion = await _context.Notificaciones
            .FirstOrDefaultAsync(n => n.Id == id && n.NegocioId == negocioId);

        if (notificacion == null)
            return (false, "Notificación no encontrada");

        notificacion.Leida = true;
        _context.Update(notificacion);
        await _context.SaveChangesAsync();

        return (true, "Notificación marcada como leída");
    }

    /// <summary>
    /// Marca todas las notificaciones no leídas como leídas
    /// </summary>
    public async Task<(bool success, string message)> MarcarTodasLeidasAsync(Guid negocioId)
    {
        var notificaciones = await _context.Notificaciones
            .Where(n => n.NegocioId == negocioId && !n.Leida)
            .ToListAsync();

        foreach (var n in notificaciones)
            n.Leida = true;

        await _context.SaveChangesAsync();

        return (true, $"{notificaciones.Count} notificaciones marcadas como leídas");
    }

    // ═══════════════════════════════════════════════════════════
    // GENERACIÓN AUTOMÁTICA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Genera notificaciones para membresías que vencen en los próximos 3 días.
    /// Verifica si ya existe una notificación para cada cliente en el día actual
    /// para evitar duplicados.
    /// </summary>
    public async Task<(bool success, string message, int count)> GenerarNotificacionesAsync(Guid negocioId)
    {
        var hoy = TimeHelper.Now.Date;
        var en3Dias = hoy.AddDays(3);

        // Buscar clientes cuya membresía vence entre hoy y dentro de 3 días
        var clientesProximos = await _context.Clientes
            .Where(c => c.NegocioId == negocioId
                && c.FechaQueTermina.Date >= hoy
                && c.FechaQueTermina.Date <= en3Dias)
            .ToListAsync();

        int count = 0;
        foreach (var cliente in clientesProximos)
        {
            // Verificar si ya existe una notificación para este cliente hoy
            var existe = await _context.Notificaciones
                .AnyAsync(n => n.NegocioId == negocioId
                    && n.ClienteId == cliente.ClienteId
                    && n.FechaCreacion.Date == hoy);

            if (existe) continue;

            var diasRestantes = (cliente.FechaQueTermina.Date - hoy).Days;
            var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";

            var notificacion = new Notificacion
            {
                Id = Guid.NewGuid(),
                NegocioId = negocioId,
                Mensaje = diasRestantes == 0
                    ? $"La membresía de {nombreCompleto} vence HOY"
                    : $"La membresía de {nombreCompleto} vence en {diasRestantes} día(s)",
                Tipo = "vencimiento",
                ClienteId = cliente.ClienteId,
                NombreCliente = nombreCompleto,
                Leida = false,
                FechaCreacion = TimeHelper.Now
            };

            _context.Notificaciones.Add(notificacion);
            count++;
        }

        await _context.SaveChangesAsync();

        return (true, $"Se generaron {count} notificaciones", count);
    }
}
