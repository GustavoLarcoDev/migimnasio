// ═══════════════════════════════════════════════════════════
// CitaService.cs — Servicio de gestión de citas (modelo artesanal)
// Maneja CRUD de citas, validación de conflictos de horario,
// slots disponibles, pagos y estadísticas del dashboard.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class CitaService : ICitaService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogService _logService;

    public CitaService(ApplicationDbContext context, ILogService logService)
    {
        _context = context;
        _logService = logService;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS DE CALENDARIO Y ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las citas de un rango de fechas formateadas para FullCalendar
    /// </summary>
    public async Task<object> GetCitasCalendarioAsync(Guid negocioId, DateTime start, DateTime end)
    {
        return await _context.Citas
            .Where(c => c.NegocioId == negocioId
                && c.FechaHoraInicio >= start
                && c.FechaHoraInicio <= end)
            .OrderBy(c => c.FechaHoraInicio)
            .Select(c => new
            {
                id = c.CitaId,
                title = c.NombreServicio + " - " + c.NombreCliente,
                start = c.FechaHoraInicio,
                end = c.FechaHoraFin,
                className = "cita-" + c.Estado,
                extendedProps = new
                {
                    c.CitaId,
                    c.Estado,
                    c.NombreCliente,
                    c.NombreEmpleado,
                    c.NombreServicio,
                    c.PrecioServicio,
                    c.DuracionMinutos,
                    c.EmpleadoId
                }
            })
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene el detalle de una cita incluyendo información de pago
    /// </summary>
    public async Task<object> GetCitaAsync(Guid citaId, Guid negocioId)
    {
        var cita = await _context.Citas
            .FirstOrDefaultAsync(c => c.CitaId == citaId && c.NegocioId == negocioId);

        if (cita == null) return null;

        var pago = await _context.PagosCita
            .FirstOrDefaultAsync(p => p.CitaId == citaId);

        return new
        {
            cita.CitaId,
            cita.NegocioId,
            cita.ClienteId,
            cita.EmpleadoId,
            cita.ServicioId,
            cita.NombreCliente,
            cita.NombreEmpleado,
            cita.NombreServicio,
            cita.PrecioServicio,
            cita.FechaHoraInicio,
            cita.FechaHoraFin,
            cita.DuracionMinutos,
            cita.Estado,
            cita.MotivoCancelacion,
            cita.FechaCreacion,
            TienePago = pago != null,
            Pago = pago == null ? null : new
            {
                pago.PagoId,
                pago.MontoServicio,
                pago.MontoExtra,
                pago.DetalleExtra,
                pago.Propina,
                pago.Total,
                pago.MetodoPago,
                pago.EsRegalo,
                pago.MotivoRegalo
            }
        };
    }

    /// <summary>
    /// Calcula los slots de tiempo disponibles para un empleado en una fecha,
    /// considerando horarios, excepciones y citas existentes
    /// </summary>
    public async Task<object> GetSlotsDisponiblesAsync(Guid negocioId, Guid empleadoId, DateTime fecha, int duracionMinutos)
    {
        var diaSemana = (int)fecha.DayOfWeek;

        // Verificar excepcion para esta fecha
        var excepcion = await _context.HorariosExcepcion
            .FirstOrDefaultAsync(h => h.EmpleadoId == empleadoId && h.Fecha.Date == fecha.Date);

        if (excepcion != null && excepcion.EsDiaLibre)
            return new { slots = Array.Empty<object>(), mensaje = "El empleado no trabaja este día" };

        string horaInicio, horaFin;

        if (excepcion != null)
        {
            // Excepcion con horario custom — validar que tenga horas
            if (string.IsNullOrWhiteSpace(excepcion.HoraInicio) || string.IsNullOrWhiteSpace(excepcion.HoraFin))
                return new { slots = Array.Empty<object>(), mensaje = "El empleado no tiene horario definido para este día" };
            horaInicio = excepcion.HoraInicio;
            horaFin = excepcion.HoraFin;
        }
        else
        {
            var horario = await _context.HorariosEmpleado
                .FirstOrDefaultAsync(h => h.EmpleadoId == empleadoId && h.DiaSemana == diaSemana);

            if (horario == null || !horario.Activo)
                return new { slots = Array.Empty<object>(), mensaje = "El empleado no trabaja este día" };

            horaInicio = horario.HoraInicio;
            horaFin = horario.HoraFin;
        }

        // Validar formato de hora (HH:mm)
        if (!TimeSpan.TryParse(horaInicio, out var inicio) || !TimeSpan.TryParse(horaFin, out var fin))
            return new { slots = Array.Empty<object>(), mensaje = "Horario del empleado tiene formato inválido" };

        if (inicio >= fin)
            return new { slots = Array.Empty<object>(), mensaje = "El horario de inicio debe ser anterior al de fin" };

        // Obtener citas existentes del empleado para este dia
        var inicioDelDia = fecha.Date;
        var finDelDia = fecha.Date.AddDays(1);

        var citasExistentes = await _context.Citas
            .Where(c => c.EmpleadoId == empleadoId
                && c.Estado != "cancelada"
                && c.FechaHoraInicio >= inicioDelDia
                && c.FechaHoraInicio < finDelDia)
            .Select(c => new { c.FechaHoraInicio, c.FechaHoraFin })
            .ToListAsync();

        // Generar slots de 15 minutos
        var slots = new List<object>();
        var duracion = TimeSpan.FromMinutes(duracionMinutos);
        var paso = TimeSpan.FromMinutes(15);

        for (var current = inicio; current + duracion <= fin; current += paso)
        {
            var slotInicio = fecha.Date + current;
            var slotFin = slotInicio.AddMinutes(duracionMinutos);

            // Verificar conflicto
            var conflicto = citasExistentes.Any(c =>
                c.FechaHoraInicio < slotFin && c.FechaHoraFin > slotInicio);

            if (!conflicto)
            {
                slots.Add(new
                {
                    hora = current.ToString(@"hh\:mm"),
                    inicio = slotInicio,
                    fin = slotFin
                });
            }
        }

        return new { slots, mensaje = (string)null };
    }

    /// <summary>
    /// Calcula estadísticas del día: citas, ingresos, completadas y canceladas
    /// </summary>
    public async Task<object> GetDashboardStatsAsync(Guid negocioId)
    {
        var hoy = TimeHelper.Now.Date;
        var manana = hoy.AddDays(1);

        var citasHoy = await _context.Citas
            .Where(c => c.NegocioId == negocioId
                && c.FechaHoraInicio >= hoy && c.FechaHoraInicio < manana)
            .ToListAsync();

        var pagosHoy = await _context.PagosCita
            .Where(p => p.NegocioId == negocioId
                && p.FechaCreacion >= hoy && p.FechaCreacion < manana)
            .ToListAsync();

        return new
        {
            citasHoy = citasHoy.Count(c => c.Estado != "cancelada"),
            ingresosHoy = pagosHoy.Sum(p => p.Total),
            completadas = citasHoy.Count(c => c.Estado == "completada"),
            canceladas = citasHoy.Count(c => c.Estado == "cancelada")
        };
    }

    /// <summary>
    /// Obtiene las últimas 50 citas de un cliente para su historial
    /// </summary>
    public async Task<object> GetHistorialClienteAsync(Guid clienteId, Guid negocioId)
    {
        return await _context.Citas
            .Where(c => c.ClienteId == clienteId && c.NegocioId == negocioId)
            .OrderByDescending(c => c.FechaHoraInicio)
            .Select(c => new
            {
                c.CitaId,
                c.NombreServicio,
                c.NombreEmpleado,
                c.FechaHoraInicio,
                c.DuracionMinutos,
                c.PrecioServicio,
                c.Estado
            })
            .Take(50)
            .ToListAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // CREACIÓN Y GESTIÓN DE CITAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea una cita rápida: permite crear cliente nuevo inline o usar uno existente.
    /// Valida conflictos de horario antes de confirmar.
    /// </summary>
    public async Task<(bool success, string message, Guid? citaId)> CrearCitaRapidaAsync(CitaQuickCreateDto dto)
    {
        // Resolver cliente: existente o crear nuevo
        Guid clienteId;
        string nombreCliente;

        if (dto.ClienteId.HasValue)
        {
            var clienteExistente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.ClienteId == dto.ClienteId.Value && c.NegocioId == dto.NegocioId);
            if (clienteExistente == null)
                return (false, "Cliente no encontrado", null);
            clienteId = clienteExistente.ClienteId;
            nombreCliente = $"{clienteExistente.Nombre} {clienteExistente.Apellido}";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
                return (false, "Nombre y Apellido son obligatorios para cliente nuevo", null);

            var nuevoCliente = new Cliente
            {
                ClienteId = Guid.NewGuid(),
                NegocioId = dto.NegocioId,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Email = dto.Email,
                Telefono = dto.Telefono,
                Dias = 0,
                Precio = 0,
                EsDiario = false,
                FechaDeCreacion = TimeHelper.Now,
                FechaDeActualizacion = TimeHelper.Now,
                FechaQueTermina = TimeHelper.Now
            };
            _context.Clientes.Add(nuevoCliente);
            clienteId = nuevoCliente.ClienteId;
            nombreCliente = $"{dto.Nombre} {dto.Apellido}";
        }

        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == dto.EmpleadoId && e.NegocioId == dto.NegocioId && e.IsActive);
        if (empleado == null)
            return (false, "Empleado no encontrado", null);

        var servicio = await _context.ServiciosNegocio
            .FirstOrDefaultAsync(s => s.ServicioId == dto.ServicioId && s.NegocioId == dto.NegocioId && s.IsActive);
        if (servicio == null)
            return (false, "Servicio no encontrado", null);

        var fechaHoraFin = dto.FechaHoraInicio.AddMinutes(servicio.DuracionMinutos);

        var conflicto = await _context.Citas
            .AnyAsync(c => c.EmpleadoId == dto.EmpleadoId
                && c.Estado != "cancelada"
                && c.FechaHoraInicio < fechaHoraFin
                && c.FechaHoraFin > dto.FechaHoraInicio);

        if (conflicto)
            return (false, "El empleado ya tiene una cita en ese horario", null);

        var cita = new Cita
        {
            CitaId = Guid.NewGuid(),
            NegocioId = dto.NegocioId,
            ClienteId = clienteId,
            EmpleadoId = dto.EmpleadoId,
            ServicioId = dto.ServicioId,
            NombreCliente = nombreCliente,
            NombreEmpleado = $"{empleado.Nombre} {empleado.Apellido}",
            NombreServicio = servicio.Nombre,
            PrecioServicio = servicio.Precio,
            FechaHoraInicio = dto.FechaHoraInicio,
            FechaHoraFin = fechaHoraFin,
            DuracionMinutos = servicio.DuracionMinutos,
            Estado = "pendiente",
            RecordatorioEnviado = false,
            FechaCreacion = TimeHelper.Now,
            FechaDeActualizacion = TimeHelper.Now
        };

        _context.Citas.Add(cita);
        await _context.SaveChangesAsync();

        return (true, "Cita creada exitosamente", cita.CitaId);
    }

    /// <summary>
    /// Crea una cita estándar con cliente existente.
    /// Incluye prevención de doble-booking.
    /// </summary>
    public async Task<(bool success, string message)> CrearCitaAsync(CitaCreateDto dto)
    {
        // Obtener datos relacionados
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == dto.ClienteId && c.NegocioId == dto.NegocioId);
        if (cliente == null)
            return (false, "Cliente no encontrado");

        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == dto.EmpleadoId && e.NegocioId == dto.NegocioId && e.IsActive);
        if (empleado == null)
            return (false, "Empleado no encontrado");

        var servicio = await _context.ServiciosNegocio
            .FirstOrDefaultAsync(s => s.ServicioId == dto.ServicioId && s.NegocioId == dto.NegocioId && s.IsActive);
        if (servicio == null)
            return (false, "Servicio no encontrado");

        var fechaHoraFin = dto.FechaHoraInicio.AddMinutes(servicio.DuracionMinutos);

        // Prevencion de doble-booking
        var conflicto = await _context.Citas
            .AnyAsync(c => c.EmpleadoId == dto.EmpleadoId
                && c.Estado != "cancelada"
                && c.FechaHoraInicio < fechaHoraFin
                && c.FechaHoraFin > dto.FechaHoraInicio);

        if (conflicto)
            return (false, "El empleado ya tiene una cita en ese horario");

        var cita = new Cita
        {
            CitaId = Guid.NewGuid(),
            NegocioId = dto.NegocioId,
            ClienteId = dto.ClienteId,
            EmpleadoId = dto.EmpleadoId,
            ServicioId = dto.ServicioId,
            NombreCliente = $"{cliente.Nombre} {cliente.Apellido}",
            NombreEmpleado = $"{empleado.Nombre} {empleado.Apellido}",
            NombreServicio = servicio.Nombre,
            PrecioServicio = servicio.Precio,
            FechaHoraInicio = dto.FechaHoraInicio,
            FechaHoraFin = fechaHoraFin,
            DuracionMinutos = servicio.DuracionMinutos,
            Estado = "pendiente",
            RecordatorioEnviado = false,
            FechaCreacion = TimeHelper.Now,
            FechaDeActualizacion = TimeHelper.Now
        };

        _context.Citas.Add(cita);
        await _context.SaveChangesAsync();

        return (true, "Cita creada exitosamente");
    }

    /// <summary>
    /// Mueve una cita a una nueva fecha/hora, revalidando conflictos
    /// </summary>
    public async Task<(bool success, string message)> MoverCitaAsync(CitaMoveDto dto)
    {
        var cita = await _context.Citas
            .FirstOrDefaultAsync(c => c.CitaId == dto.CitaId && c.NegocioId == dto.NegocioId);

        if (cita == null)
            return (false, "Cita no encontrada");

        if (cita.Estado == "completada" || cita.Estado == "cancelada")
            return (false, "No se puede mover una cita completada o cancelada");

        var nuevaFechaFin = dto.NuevaFechaHoraInicio.AddMinutes(cita.DuracionMinutos);

        // Re-validar conflictos en nueva posicion
        var conflicto = await _context.Citas
            .AnyAsync(c => c.EmpleadoId == cita.EmpleadoId
                && c.CitaId != cita.CitaId
                && c.Estado != "cancelada"
                && c.FechaHoraInicio < nuevaFechaFin
                && c.FechaHoraFin > dto.NuevaFechaHoraInicio);

        if (conflicto)
            return (false, "El empleado ya tiene una cita en ese horario");

        cita.FechaHoraInicio = dto.NuevaFechaHoraInicio;
        cita.FechaHoraFin = nuevaFechaFin;
        cita.FechaDeActualizacion = TimeHelper.Now;

        await _context.SaveChangesAsync();

        return (true, "Cita movida exitosamente");
    }

    /// <summary>
    /// Cambia el estado de una cita validando transiciones permitidas
    /// </summary>
    public async Task<(bool success, string message)> CambiarEstadoCitaAsync(
        Guid citaId, Guid negocioId, string nuevoEstado, string motivoCancelacion = null)
    {
        var estadosValidos = new[] { "pendiente", "confirmada", "en_progreso", "completada", "cancelada" };
        if (!estadosValidos.Contains(nuevoEstado))
            return (false, "Estado no válido");

        var cita = await _context.Citas
            .FirstOrDefaultAsync(c => c.CitaId == citaId && c.NegocioId == negocioId);

        if (cita == null)
            return (false, "Cita no encontrada");

        // Validar transiciones de estado permitidas
        var transicionesPermitidas = new Dictionary<string, string[]>
        {
            ["pendiente"] = new[] { "confirmada", "cancelada" },
            ["confirmada"] = new[] { "en_progreso", "cancelada" },
            ["en_progreso"] = new[] { "completada", "cancelada" },
            ["completada"] = Array.Empty<string>(),
            ["cancelada"] = Array.Empty<string>()
        };

        if (!transicionesPermitidas.TryGetValue(cita.Estado, out var permitidos) || !permitidos.Contains(nuevoEstado))
            return (false, $"No se puede cambiar de '{cita.Estado}' a '{nuevoEstado}'");

        if (nuevoEstado == "cancelada" && string.IsNullOrWhiteSpace(motivoCancelacion))
            return (false, "El motivo de cancelación es obligatorio");

        cita.Estado = nuevoEstado;
        cita.FechaDeActualizacion = TimeHelper.Now;

        if (nuevoEstado == "cancelada")
            cita.MotivoCancelacion = motivoCancelacion;

        await _context.SaveChangesAsync();

        return (true, $"Estado cambiado a {nuevoEstado}");
    }

    // ═══════════════════════════════════════════════════════════
    // REGISTRO DE PAGOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Registra el pago de una cita completada. Soporta pagos normales y regalos.
    /// Crea un log inmutable con el detalle financiero.
    /// </summary>
    public async Task<(bool success, string message)> RegistrarPagoAsync(PagoCitaDto dto)
    {
        var cita = await _context.Citas
            .FirstOrDefaultAsync(c => c.CitaId == dto.CitaId && c.NegocioId == dto.NegocioId);

        if (cita == null)
            return (false, "Cita no encontrada");

        if (cita.Estado != "completada")
            return (false, "La cita debe estar completada para registrar el pago");

        // Verificar que no exista un pago previo
        var pagoExistente = await _context.PagosCita
            .AnyAsync(p => p.CitaId == dto.CitaId);

        if (pagoExistente)
            return (false, "Ya existe un pago registrado para esta cita");

        if (dto.EsRegalo && string.IsNullOrWhiteSpace(dto.MotivoRegalo))
            return (false, "El motivo del regalo es obligatorio");

        if (dto.MontoExtra < 0)
            return (false, "El monto extra no puede ser negativo");
        if (dto.Propina < 0)
            return (false, "La propina no puede ser negativa");

        if (!dto.EsRegalo && string.IsNullOrWhiteSpace(dto.MetodoPago))
            return (false, "El método de pago es obligatorio");

        var total = dto.EsRegalo ? 0 : cita.PrecioServicio + dto.MontoExtra + dto.Propina;

        var pago = new PagoCita
        {
            PagoId = Guid.NewGuid(),
            CitaId = dto.CitaId,
            NegocioId = dto.NegocioId,
            MontoServicio = cita.PrecioServicio,
            MontoExtra = dto.MontoExtra,
            DetalleExtra = dto.DetalleExtra,
            Propina = dto.Propina,
            Total = total,
            MetodoPago = dto.EsRegalo ? "regalo" : dto.MetodoPago,
            EsRegalo = dto.EsRegalo,
            MotivoRegalo = dto.MotivoRegalo,
            NombreCliente = cita.NombreCliente,
            NombreServicio = cita.NombreServicio,
            FechaCreacion = TimeHelper.Now
        };

        _context.PagosCita.Add(pago);
        await _context.SaveChangesAsync();

        // Crear log inmutable
        if (dto.EsRegalo)
        {
            await _logService.CreateLogAsync(dto.NegocioId, "cita_regalo",
                $"Cita regalo: {cita.NombreServicio} para {cita.NombreCliente}. Motivo: {dto.MotivoRegalo}",
                0, cita.ClienteId, cita.NombreCliente);
        }
        else
        {
            await _logService.CreateLogAsync(dto.NegocioId, "cita_completada",
                $"Cita completada: {cita.NombreServicio} para {cita.NombreCliente}. " +
                $"Servicio: ${cita.PrecioServicio:F2}" +
                (dto.MontoExtra > 0 ? $", Extra: ${dto.MontoExtra:F2}" : "") +
                (dto.Propina > 0 ? $", Propina: ${dto.Propina:F2}" : "") +
                $". Total: ${total:F2} ({dto.MetodoPago})",
                total, cita.ClienteId, cita.NombreCliente);
        }

        return (true, "Pago registrado exitosamente");
    }

    /// <summary>
    /// Obtiene el detalle de pago asociado a una cita
    /// </summary>
    public async Task<object> GetPagoCitaAsync(Guid citaId, Guid negocioId)
    {
        return await _context.PagosCita
            .Where(p => p.CitaId == citaId && p.NegocioId == negocioId)
            .Select(p => new
            {
                p.PagoId,
                p.MontoServicio,
                p.MontoExtra,
                p.DetalleExtra,
                p.Propina,
                p.Total,
                p.MetodoPago,
                p.EsRegalo,
                p.MotivoRegalo,
                p.NombreCliente,
                p.NombreServicio,
                p.FechaCreacion
            })
            .FirstOrDefaultAsync();
    }
}
