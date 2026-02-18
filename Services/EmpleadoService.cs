// ═══════════════════════════════════════════════════════════
// EmpleadoService.cs — Servicio de gestión de empleados
// Maneja CRUD de empleados, horarios semanales, excepciones
// de horario y consulta de disponibilidad por fecha.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class EmpleadoService : IEmpleadoService
{
    private readonly ApplicationDbContext _context;

    public EmpleadoService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los empleados activos del negocio
    /// </summary>
    public async Task<object> GetEmpleadosAsync(Guid negocioId)
    {
        return await _context.Empleados
            .Where(e => e.NegocioId == negocioId && e.IsActive)
            .OrderBy(e => e.Nombre)
            .Select(e => new
            {
                e.EmpleadoId,
                e.Nombre,
                e.Apellido,
                NombreCompleto = e.Nombre + " " + e.Apellido,
                e.Telefono,
                e.Especialidad,
                e.FechaCreacion
            })
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un empleado específico por su ID
    /// </summary>
    public async Task<Empleado> GetEmpleadoAsync(Guid empleadoId, Guid negocioId)
    {
        return await _context.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId && e.NegocioId == negocioId && e.IsActive);
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE EMPLEADOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un empleado y le asigna horario por defecto (Lun-Sáb 9-17, Dom inactivo)
    /// </summary>
    public async Task<(bool success, string message)> CrearEmpleadoAsync(EmpleadoCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return (false, "El nombre es obligatorio");
        if (string.IsNullOrWhiteSpace(dto.Apellido))
            return (false, "El apellido es obligatorio");

        var empleado = new Empleado
        {
            EmpleadoId = Guid.NewGuid(),
            NegocioId = dto.NegocioId,
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Telefono = dto.Telefono,
            Especialidad = dto.Especialidad,
            IsActive = true,
            FechaCreacion = TimeHelper.Now
        };

        _context.Empleados.Add(empleado);
        await _context.SaveChangesAsync();

        // Crear horario por defecto (Lunes a Sabado, 9-17)
        for (int dia = 1; dia <= 6; dia++)
        {
            _context.HorariosEmpleado.Add(new HorarioEmpleado
            {
                HorarioId = Guid.NewGuid(),
                EmpleadoId = empleado.EmpleadoId,
                NegocioId = dto.NegocioId,
                DiaSemana = dia,
                HoraInicio = "09:00",
                HoraFin = "17:00",
                Activo = true
            });
        }
        // Domingo inactivo
        _context.HorariosEmpleado.Add(new HorarioEmpleado
        {
            HorarioId = Guid.NewGuid(),
            EmpleadoId = empleado.EmpleadoId,
            NegocioId = dto.NegocioId,
            DiaSemana = 0,
            HoraInicio = "09:00",
            HoraFin = "17:00",
            Activo = false
        });

        await _context.SaveChangesAsync();

        return (true, "Empleado creado exitosamente");
    }

    /// <summary>
    /// Edita los datos básicos de un empleado existente
    /// </summary>
    public async Task<(bool success, string message)> EditarEmpleadoAsync(EmpleadoCreateDto dto)
    {
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == dto.EmpleadoId && e.NegocioId == dto.NegocioId && e.IsActive);

        if (empleado == null)
            return (false, "Empleado no encontrado");

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return (false, "El nombre es obligatorio");
        if (string.IsNullOrWhiteSpace(dto.Apellido))
            return (false, "El apellido es obligatorio");

        empleado.Nombre = dto.Nombre;
        empleado.Apellido = dto.Apellido;
        empleado.Telefono = dto.Telefono;
        empleado.Especialidad = dto.Especialidad;

        await _context.SaveChangesAsync();

        return (true, "Empleado actualizado exitosamente");
    }

    /// <summary>
    /// Elimina un empleado (lógicamente). Falla si tiene citas pendientes.
    /// </summary>
    public async Task<(bool success, string message)> EliminarEmpleadoAsync(Guid empleadoId, Guid negocioId)
    {
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId && e.NegocioId == negocioId && e.IsActive);

        if (empleado == null)
            return (false, "Empleado no encontrado");

        // Verificar si tiene citas pendientes
        var tieneCitasPendientes = await _context.Citas
            .AnyAsync(c => c.EmpleadoId == empleadoId
                && c.Estado != "cancelada" && c.Estado != "completada");

        if (tieneCitasPendientes)
            return (false, "No se puede eliminar. El empleado tiene citas pendientes.");

        empleado.IsActive = false;
        await _context.SaveChangesAsync();

        return (true, "Empleado eliminado exitosamente");
    }

    // ═══════════════════════════════════════════════════════════
    // HORARIOS SEMANALES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene el horario semanal de un empleado (7 días)
    /// </summary>
    public async Task<object> GetHorariosAsync(Guid empleadoId, Guid negocioId)
    {
        return await _context.HorariosEmpleado
            .Where(h => h.EmpleadoId == empleadoId && h.NegocioId == negocioId)
            .OrderBy(h => h.DiaSemana)
            .Select(h => new
            {
                h.HorarioId,
                h.DiaSemana,
                h.HoraInicio,
                h.HoraFin,
                h.Activo
            })
            .ToListAsync();
    }

    /// <summary>
    /// Guarda o actualiza el horario semanal completo de un empleado
    /// </summary>
    public async Task<(bool success, string message)> GuardarHorariosAsync(HorarioEmpleadoDto dto)
    {
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == dto.EmpleadoId && e.NegocioId == dto.NegocioId && e.IsActive);

        if (empleado == null)
            return (false, "Empleado no encontrado");

        foreach (var dia in dto.Dias)
        {
            // Validar formato de hora si el dia esta activo
            if (dia.Activo)
            {
                if (string.IsNullOrWhiteSpace(dia.HoraInicio) || string.IsNullOrWhiteSpace(dia.HoraFin))
                    return (false, $"Hora inicio y fin son obligatorias para días activos");
                if (!TimeSpan.TryParse(dia.HoraInicio, out var hi) || !TimeSpan.TryParse(dia.HoraFin, out var hf))
                    return (false, $"Formato de hora inválido (use HH:mm)");
                if (hi >= hf)
                    return (false, $"La hora de inicio debe ser anterior a la hora de fin");
            }

            var horario = await _context.HorariosEmpleado
                .FirstOrDefaultAsync(h => h.EmpleadoId == dto.EmpleadoId && h.DiaSemana == dia.DiaSemana);

            if (horario != null)
            {
                horario.HoraInicio = dia.HoraInicio;
                horario.HoraFin = dia.HoraFin;
                horario.Activo = dia.Activo;
            }
            else
            {
                _context.HorariosEmpleado.Add(new HorarioEmpleado
                {
                    HorarioId = Guid.NewGuid(),
                    EmpleadoId = dto.EmpleadoId,
                    NegocioId = dto.NegocioId,
                    DiaSemana = dia.DiaSemana,
                    HoraInicio = dia.HoraInicio,
                    HoraFin = dia.HoraFin,
                    Activo = dia.Activo
                });
            }
        }

        await _context.SaveChangesAsync();
        return (true, "Horario guardado exitosamente");
    }

    // ═══════════════════════════════════════════════════════════
    // DISPONIBILIDAD Y EXCEPCIONES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los empleados con su estado de disponibilidad para una fecha,
    /// considerando horarios normales, excepciones y citas del día
    /// </summary>
    public async Task<object> GetEmpleadosConDisponibilidadAsync(Guid negocioId, DateTime fecha)
    {
        var diaSemana = (int)fecha.DayOfWeek;

        var empleados = await _context.Empleados
            .Where(e => e.NegocioId == negocioId && e.IsActive)
            .OrderBy(e => e.Nombre)
            .ToListAsync();

        var empleadoIds = empleados.Select(e => e.EmpleadoId).ToList();

        var horarios = await _context.HorariosEmpleado
            .Where(h => empleadoIds.Contains(h.EmpleadoId) && h.DiaSemana == diaSemana)
            .ToListAsync();

        var excepciones = await _context.HorariosExcepcion
            .Where(h => empleadoIds.Contains(h.EmpleadoId) && h.Fecha.Date == fecha.Date)
            .ToListAsync();

        var citasDelDia = await _context.Citas
            .Where(c => c.NegocioId == negocioId
                && c.Estado != "cancelada"
                && c.FechaHoraInicio >= fecha.Date
                && c.FechaHoraInicio < fecha.Date.AddDays(1))
            .GroupBy(c => c.EmpleadoId)
            .Select(g => new { EmpleadoId = g.Key, Total = g.Count() })
            .ToListAsync();

        return empleados.Select(e =>
        {
            var excepcion = excepciones.FirstOrDefault(x => x.EmpleadoId == e.EmpleadoId);
            var horario = horarios.FirstOrDefault(h => h.EmpleadoId == e.EmpleadoId);
            var citasCount = citasDelDia.FirstOrDefault(c => c.EmpleadoId == e.EmpleadoId)?.Total ?? 0;

            bool trabaja;
            string horaInicio = null, horaFin = null;

            if (excepcion != null)
            {
                trabaja = !excepcion.EsDiaLibre;
                horaInicio = excepcion.HoraInicio;
                horaFin = excepcion.HoraFin;
            }
            else
            {
                trabaja = horario != null && horario.Activo;
                horaInicio = horario?.HoraInicio;
                horaFin = horario?.HoraFin;
            }

            return new
            {
                e.EmpleadoId,
                e.Nombre,
                e.Apellido,
                NombreCompleto = e.Nombre + " " + e.Apellido,
                e.Especialidad,
                Trabaja = trabaja,
                HoraInicio = horaInicio,
                HoraFin = horaFin,
                CitasHoy = citasCount
            };
        }).ToList();
    }

    /// <summary>
    /// Obtiene las excepciones de horario de un empleado en un rango de fechas
    /// </summary>
    public async Task<object> GetExcepcionesAsync(Guid empleadoId, Guid negocioId, DateTime? desde, DateTime? hasta)
    {
        var query = _context.HorariosExcepcion
            .Where(h => h.EmpleadoId == empleadoId && h.NegocioId == negocioId);

        if (desde.HasValue)
            query = query.Where(h => h.Fecha >= desde.Value);
        if (hasta.HasValue)
            query = query.Where(h => h.Fecha <= hasta.Value);

        return await query
            .OrderBy(h => h.Fecha)
            .Select(h => new
            {
                h.ExcepcionId,
                h.Fecha,
                h.EsDiaLibre,
                h.HoraInicio,
                h.HoraFin,
                h.Motivo
            })
            .ToListAsync();
    }

    /// <summary>
    /// Crea una excepción de horario (día libre o horario especial) para una fecha
    /// </summary>
    public async Task<(bool success, string message)> CrearExcepcionAsync(
        Guid empleadoId, Guid negocioId, DateTime fecha, bool esDiaLibre,
        string horaInicio, string horaFin, string motivo)
    {
        // Validar formato de horas si no es dia libre
        if (!esDiaLibre)
        {
            if (string.IsNullOrWhiteSpace(horaInicio) || string.IsNullOrWhiteSpace(horaFin))
                return (false, "Hora inicio y fin son obligatorias");
            if (!TimeSpan.TryParse(horaInicio, out var hi) || !TimeSpan.TryParse(horaFin, out var hf))
                return (false, "Formato de hora inválido (use HH:mm)");
            if (hi >= hf)
                return (false, "La hora de inicio debe ser anterior a la hora de fin");
        }

        var existe = await _context.HorariosExcepcion
            .AnyAsync(h => h.EmpleadoId == empleadoId && h.Fecha.Date == fecha.Date);

        if (existe)
            return (false, "Ya existe una excepción para esta fecha");

        var excepcion = new HorarioExcepcion
        {
            ExcepcionId = Guid.NewGuid(),
            EmpleadoId = empleadoId,
            NegocioId = negocioId,
            Fecha = fecha.Date,
            EsDiaLibre = esDiaLibre,
            HoraInicio = esDiaLibre ? null : horaInicio,
            HoraFin = esDiaLibre ? null : horaFin,
            Motivo = motivo
        };

        _context.HorariosExcepcion.Add(excepcion);
        await _context.SaveChangesAsync();

        return (true, "Excepción creada exitosamente");
    }

    /// <summary>
    /// Elimina una excepción de horario
    /// </summary>
    public async Task<(bool success, string message)> EliminarExcepcionAsync(Guid excepcionId, Guid negocioId)
    {
        var excepcion = await _context.HorariosExcepcion
            .FirstOrDefaultAsync(h => h.ExcepcionId == excepcionId && h.NegocioId == negocioId);

        if (excepcion == null)
            return (false, "Excepción no encontrada");

        _context.HorariosExcepcion.Remove(excepcion);
        await _context.SaveChangesAsync();

        return (true, "Excepción eliminada exitosamente");
    }
}
