// ═══════════════════════════════════════════════════════════
// ReservaService.cs — Gestión de reservas de restaurante
//
// Maneja creación, edición, cancelación y listado de reservas.
// Cuando se crea una reserva con mesa asignada, la mesa se
// marca automáticamente como "reservada".
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class ReservaService : IReservaService
{
    private readonly ApplicationDbContext _context;

    public ReservaService(ApplicationDbContext context) => _context = context;

    public async Task<object> GetReservasAsync(Guid negocioId, DateTime? fecha = null)
    {
        var query = _context.Reservas
            .Where(r => r.NegocioId == negocioId && r.IsActive);

        if (fecha.HasValue)
            query = query.Where(r => r.FechaHoraReserva.Date == fecha.Value.Date);

        return await query
            .OrderBy(r => r.FechaHoraReserva)
            .Select(r => new
            {
                r.ReservaId,
                r.NombreCliente,
                r.Telefono,
                r.FechaHoraReserva,
                r.CantidadPersonas,
                r.MesaId,
                MesaNombre = r.Mesa != null ? r.Mesa.Nombre : null,
                MesaNumero = r.Mesa != null ? (int?)r.Mesa.Numero : null,
                r.Notas,
                r.Estado,
                r.FechaCreacion
            })
            .ToListAsync();
    }

    public async Task<object> GetReservasHoyAsync(Guid negocioId)
    {
        var hoy = TimeHelper.Now.Date;
        return await GetReservasAsync(negocioId, hoy);
    }

    public async Task<Reserva> GetReservaAsync(Guid reservaId, Guid negocioId)
    {
        return await _context.Reservas
            .Include(r => r.Mesa)
            .FirstOrDefaultAsync(r => r.ReservaId == reservaId && r.NegocioId == negocioId && r.IsActive);
    }

    public async Task<(bool success, string message)> CrearReservaAsync(
        Guid negocioId, string nombreCliente, string telefono,
        DateTime fechaHoraReserva, int cantidadPersonas, Guid? mesaId, string notas)
    {
        if (string.IsNullOrWhiteSpace(nombreCliente))
            return (false, "El nombre del cliente es obligatorio");

        if (cantidadPersonas <= 0) cantidadPersonas = 2;

        // Validar que la mesa existe y pertenece al negocio
        if (mesaId.HasValue && mesaId != Guid.Empty)
        {
            var mesa = await _context.Mesas
                .FirstOrDefaultAsync(m => m.MesaId == mesaId && m.NegocioId == negocioId && m.IsActive);
            if (mesa == null) return (false, "Mesa no encontrada");

            // Verificar que la mesa no tiene otra reserva activa en la misma fecha/hora (±2 horas)
            var conflicto = await _context.Reservas.AnyAsync(r =>
                r.MesaId == mesaId && r.IsActive && r.Estado == "confirmada"
                && r.FechaHoraReserva > fechaHoraReserva.AddHours(-2)
                && r.FechaHoraReserva < fechaHoraReserva.AddHours(2));
            if (conflicto)
                return (false, "La mesa ya tiene una reserva en ese horario");

            // Marcar mesa como reservada si la reserva es para hoy
            if (fechaHoraReserva.Date == TimeHelper.Now.Date)
                mesa.Estado = "reservada";
        }

        var reserva = new Reserva
        {
            ReservaId = Guid.NewGuid(),
            NegocioId = negocioId,
            MesaId = mesaId == Guid.Empty ? null : mesaId,
            NombreCliente = nombreCliente.Trim(),
            Telefono = telefono?.Trim(),
            FechaHoraReserva = fechaHoraReserva,
            CantidadPersonas = cantidadPersonas,
            Notas = notas?.Trim(),
            Estado = "confirmada",
            IsActive = true,
            FechaCreacion = TimeHelper.Now
        };

        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();
        return (true, "Reserva creada exitosamente");
    }

    public async Task<(bool success, string message)> EditarReservaAsync(
        Guid reservaId, Guid negocioId, string nombreCliente, string telefono,
        DateTime fechaHoraReserva, int cantidadPersonas, Guid? mesaId, string notas)
    {
        var reserva = await _context.Reservas
            .FirstOrDefaultAsync(r => r.ReservaId == reservaId && r.NegocioId == negocioId && r.IsActive);
        if (reserva == null) return (false, "Reserva no encontrada");

        if (string.IsNullOrWhiteSpace(nombreCliente))
            return (false, "El nombre del cliente es obligatorio");

        // Si cambió la mesa, liberar la anterior y reservar la nueva
        if (reserva.MesaId != mesaId)
        {
            if (reserva.MesaId.HasValue)
            {
                var mesaAnterior = await _context.Mesas.FindAsync(reserva.MesaId);
                if (mesaAnterior != null && mesaAnterior.Estado == "reservada")
                    mesaAnterior.Estado = "libre";
            }

            if (mesaId.HasValue && mesaId != Guid.Empty)
            {
                var mesaNueva = await _context.Mesas
                    .FirstOrDefaultAsync(m => m.MesaId == mesaId && m.NegocioId == negocioId && m.IsActive);
                if (mesaNueva == null) return (false, "Mesa no encontrada");
                if (fechaHoraReserva.Date == TimeHelper.Now.Date)
                    mesaNueva.Estado = "reservada";
            }
        }

        reserva.NombreCliente = nombreCliente.Trim();
        reserva.Telefono = telefono?.Trim();
        reserva.FechaHoraReserva = fechaHoraReserva;
        reserva.CantidadPersonas = cantidadPersonas > 0 ? cantidadPersonas : 2;
        reserva.MesaId = mesaId == Guid.Empty ? null : mesaId;
        reserva.Notas = notas?.Trim();

        await _context.SaveChangesAsync();
        return (true, "Reserva actualizada exitosamente");
    }

    public async Task<(bool success, string message)> CambiarEstadoReservaAsync(
        Guid reservaId, Guid negocioId, string estado)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Mesa)
            .FirstOrDefaultAsync(r => r.ReservaId == reservaId && r.NegocioId == negocioId && r.IsActive);
        if (reserva == null) return (false, "Reserva no encontrada");

        var estadosValidos = new[] { "confirmada", "completada", "cancelada", "no_presentado" };
        if (!estadosValidos.Contains(estado))
            return (false, "Estado no válido");

        reserva.Estado = estado;

        // Si se completa o cancela, liberar la mesa
        if (estado is "completada" or "cancelada" or "no_presentado" && reserva.Mesa != null)
        {
            if (reserva.Mesa.Estado == "reservada")
                reserva.Mesa.Estado = "libre";
        }

        await _context.SaveChangesAsync();
        return (true, $"Reserva marcada como {estado}");
    }

    public async Task<(bool success, string message)> CancelarReservaAsync(Guid reservaId, Guid negocioId)
    {
        return await CambiarEstadoReservaAsync(reservaId, negocioId, "cancelada");
    }
}
