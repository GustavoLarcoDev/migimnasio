// ═══════════════════════════════════════════════════════════
// CitaService.cs — Implementación del servicio de gestión de citas
//
// Este archivo contiene toda la lógica de negocio relacionada con citas.
// Es la "capa de servicio": recibe pedidos del controller, aplica las
// reglas de negocio, y accede a la base de datos a través de _context.
//
// RESPONSABILIDADES DE ESTA CLASE:
//   1. Consultas del calendario (formato FullCalendar para el frontend)
//   2. Cálculo de slots disponibles (bloqueo de horario + anti-colisión)
//   3. Creación de citas rápidas y estándar
//   4. Máquina de estados: transiciones válidas entre estados de la cita
//   5. Registro de pagos con auditoría en log
//   6. Estadísticas del dashboard
//
// PATRÓN DE RETORNO:
//   Los métodos que modifican datos devuelven una Tupla (bool success, string message).
//   Esto permite al controller saber si la operación salió bien sin lanzar excepciones,
//   y enviar el mensaje exacto al frontend para mostrar al usuario.
//
// CICLO DE VIDA DE UNA CITA:
//   pendiente → confirmada → en_progreso → completada → (registrar pago)
//   Cualquier estado puede ir a → cancelada (excepto completada)
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Implementación concreta del servicio de citas para el modelo artesanal (peluquería, spa, etc.).
/// Gestiona el ciclo completo de una cita: creación, estados, slots disponibles y pagos.
/// </summary>
public class CitaService : ICitaService
{
    // _context: acceso a la base de datos. EF Core traduce nuestras consultas LINQ a SQL.
    private readonly ApplicationDbContext _context;

    // _logService: servicio de auditoría. Cuando se registra un pago, también se graba
    // un log inmutable para que quede registro permanente en el historial del negocio.
    private readonly ILogService _logService;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// ASP.NET Core inyecta estas instancias automáticamente cuando alguien
    /// pide un ICitaService (configurado en Program.cs).
    /// </summary>
    public CitaService(ApplicationDbContext context, ILogService logService)
    {
        _context = context;
        _logService = logService;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS DE CALENDARIO Y ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las citas de un rango de fechas formateadas para FullCalendar.
    /// La librería FullCalendar del frontend requiere un JSON con estructura específica;
    /// el campo <c>className</c> (ej: "cita-pendiente") se usa en CSS para colorear
    /// cada evento según su estado actual.
    /// </summary>
    public async Task<object> GetCitasCalendarioAsync(Guid negocioId, DateTime start, DateTime end)
    {
        return await _context.Citas.AsNoTracking()
            .Where(c => c.NegocioId == negocioId
                && c.FechaHoraInicio >= start
                && c.FechaHoraInicio <= end)
            .OrderBy(c => c.FechaHoraInicio)
            // Proyectamos solo los campos que necesita el calendario.
            // Usamos una proyección anónima para no traer columnas innecesarias de la DB.
            .Select(c => new
            {
                id = c.CitaId,
                // El título del evento en el calendario: "Corte de cabello - Juan Pérez"
                title = c.NombreServicio + " - " + c.NombreCliente,
                start = c.FechaHoraInicio,
                end = c.FechaHoraFin,
                // CSS class para colorear el evento: "cita-pendiente", "cita-completada", etc.
                className = "cita-" + c.Estado,
                // Datos extra que FullCalendar guarda en el evento pero no muestra visualmente.
                // Se usan al hacer clic en el evento para mostrar el panel de detalles.
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
    /// Obtiene el detalle completo de una cita incluyendo información del pago si ya existe.
    /// Se llama cuando el usuario hace clic en un evento del calendario y se abre el panel lateral.
    /// El campo <c>TienePago</c> es útil en el frontend para mostrar u ocultar el botón de "Registrar pago".
    /// </summary>
    public async Task<object> GetCitaAsync(Guid citaId, Guid negocioId)
    {
        // Siempre filtramos también por negocioId para garantizar que un negocio
        // no pueda ver datos de otro negocio (seguridad multi-tenant).
        var cita = await _context.Citas.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CitaId == citaId && c.NegocioId == negocioId);

        if (cita == null) return null;

        // Buscamos el pago asociado (puede no existir si la cita no fue completada aún)
        var pago = await _context.PagosCita.AsNoTracking()
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
            // Bandera booleana para el frontend: ¿ya se registró el pago?
            TienePago = pago != null,
            // Si no hay pago, devolvemos null. El frontend verifica TienePago antes de usar Pago.
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
    /// Calcula los slots de tiempo disponibles para un empleado en una fecha dada.
    ///
    /// ALGORITMO PASO A PASO:
    ///   Paso 1 - Verificar excepciones: antes de mirar el horario normal,
    ///            comprobamos si el empleado tiene una excepción para esa fecha exacta
    ///            (ej: día libre por vacaciones, o cambio de horario especial).
    ///
    ///   Paso 2 - Determinar el horario del día: si no hay excepción, buscamos
    ///            el horario regular para ese día de la semana (0=Domingo, 1=Lunes, etc.).
    ///
    ///   Paso 3 - Obtener citas existentes: traemos todas las citas activas (no canceladas)
    ///            del empleado para ese día, para saber qué horarios ya están ocupados.
    ///
    ///   Paso 4 - Generar y filtrar slots: iteramos cada 15 minutos desde la hora de inicio
    ///            hasta la hora de fin, y para cada posición verificamos si cabe el servicio
    ///            completo sin solaparse con ninguna cita existente.
    ///
    /// DETECCIÓN DE SOLAPAMIENTO:
    ///   Dos bloques de tiempo se solapan si: inicio_A < fin_B AND fin_A > inicio_B
    ///   Este es el algoritmo estándar de detección de intervalos superpuestos.
    /// </summary>
    public async Task<object> GetSlotsDisponiblesAsync(Guid negocioId, Guid empleadoId, DateTime fecha, int duracionMinutos)
    {
        // DayOfWeek devuelve 0=Domingo, 1=Lunes, ..., 6=Sábado.
        // Esto debe coincidir con cómo se guarda DiaSemana en HorarioEmpleado.
        var diaSemana = (int)fecha.DayOfWeek;

        // PASO 1: Buscar si hay una excepción de horario para esta fecha exacta.
        // Las excepciones tienen prioridad sobre el horario semanal normal.
        // Ejemplos: feriado, vacaciones, cambio de turno puntual.
        // SEGURIDAD MULTI-TENANT: Filtramos también por NegocioId para garantizar
        // que no se filtren datos de empleados de otro negocio.
        var excepcion = await _context.HorariosExcepcion
            .FirstOrDefaultAsync(h => h.EmpleadoId == empleadoId && h.NegocioId == negocioId && h.Fecha.Date == fecha.Date);

        // Si el empleado tiene marcado ese día como "día libre", no hay slots disponibles.
        if (excepcion != null && excepcion.EsDiaLibre)
            return new { slots = Array.Empty<object>(), mensaje = "El empleado no trabaja este día" };

        string horaInicio, horaFin;

        if (excepcion != null)
        {
            // Hay una excepción con horario personalizado (no es día libre, pero trabaja diferente).
            // Validamos que tenga horas definidas porque es un campo opcional al crear la excepción.
            if (string.IsNullOrWhiteSpace(excepcion.HoraInicio) || string.IsNullOrWhiteSpace(excepcion.HoraFin))
                return new { slots = Array.Empty<object>(), mensaje = "El empleado no tiene horario definido para este día" };

            horaInicio = excepcion.HoraInicio;
            horaFin = excepcion.HoraFin;
        }
        else
        {
            // PASO 2: No hay excepción, usamos el horario semanal normal.
            var horario = await _context.HorariosEmpleado
                .FirstOrDefaultAsync(h => h.EmpleadoId == empleadoId && h.NegocioId == negocioId && h.DiaSemana == diaSemana);

            // Si no hay horario para ese día (ej: Domingo inactivo), no hay slots.
            if (horario == null || !horario.Activo)
                return new { slots = Array.Empty<object>(), mensaje = "El empleado no trabaja este día" };

            horaInicio = horario.HoraInicio;
            horaFin = horario.HoraFin;
        }

        // Validar que las horas tengan formato HH:mm correcto.
        // TimeSpan.TryParse convierte "09:00" en TimeSpan(9, 0, 0).
        // Si el formato es inválido (ej: "9h00"), devolvemos error.
        if (!TimeSpan.TryParse(horaInicio, out var inicio) || !TimeSpan.TryParse(horaFin, out var fin))
            return new { slots = Array.Empty<object>(), mensaje = "Horario del empleado tiene formato inválido" };

        // Sanidad: el horario de inicio debe ser antes del de fin.
        if (inicio >= fin)
            return new { slots = Array.Empty<object>(), mensaje = "El horario de inicio debe ser anterior al de fin" };

        // PASO 3: Traer todas las citas activas del empleado para ese día.
        // Excluimos las canceladas porque esas no "bloquean" el horario.
        // Usamos fecha.Date para obtener 00:00:00 y AddDays(1) para el límite superior del día.
        var inicioDelDia = fecha.Date;
        var finDelDia = fecha.Date.AddDays(1);

        var citasExistentes = await _context.Citas
            .Where(c => c.EmpleadoId == empleadoId
                && c.NegocioId == negocioId
                && c.Estado != "cancelada"
                && c.FechaHoraInicio >= inicioDelDia
                && c.FechaHoraInicio < finDelDia)
            // Solo traemos los campos que necesitamos para la detección de conflictos,
            // para reducir el tamaño de los datos traídos de la base de datos.
            .Select(c => new { c.FechaHoraInicio, c.FechaHoraFin })
            .ToListAsync();

        // PASO 4: Generar todos los posibles slots y filtrar los que estén libres.
        var slots = new List<object>();
        var duracion = TimeSpan.FromMinutes(duracionMinutos);
        // Avanzamos de 15 en 15 minutos para ofrecer granularidad razonable.
        // Ej: si el servicio dura 30 min, ofrecemos slots a las 09:00, 09:15, 09:30...
        var paso = TimeSpan.FromMinutes(15);

        // Iteramos desde el inicio del horario hasta el último momento en que cabe el servicio.
        // La condición "current + duracion <= fin" garantiza que el servicio termina antes del cierre.
        for (var current = inicio; current + duracion <= fin; current += paso)
        {
            // Convertimos el TimeSpan del horario a un DateTime completo con la fecha del día.
            var slotInicio = fecha.Date + current;
            var slotFin = slotInicio.AddMinutes(duracionMinutos);

            // Verificar si este slot se solapa con alguna cita existente.
            // Algoritmo de solapamiento: A solapa con B si inicio_A < fin_B AND fin_A > inicio_B.
            // Equivalente a: NO están separados (A termina antes de que empiece B, o viceversa).
            var conflicto = citasExistentes.Any(c =>
                c.FechaHoraInicio < slotFin && c.FechaHoraFin > slotInicio);

            // Solo agregamos el slot si no hay ningún conflicto
            if (!conflicto)
            {
                slots.Add(new
                {
                    // Hora formateada para mostrar al usuario: "09:00", "09:15", etc.
                    hora = current.ToString(@"hh\:mm"),
                    // Fecha y hora completa de inicio (se envía al backend al crear la cita)
                    inicio = slotInicio,
                    fin = slotFin
                });
            }
        }

        // Si no hay slots, la lista estará vacía y mensaje será null.
        // El frontend verifica si la lista tiene elementos para mostrar el mensaje "sin disponibilidad".
        return new { slots, mensaje = (string)null };
    }

    /// <summary>
    /// Calcula las estadísticas del día actual para mostrar en las tarjetas del dashboard.
    /// Usa <c>TimeHelper.Now</c> (en vez de <c>DateTime.Now</c>) para que el tiempo
    /// sea consistente en toda la aplicación (facilita las pruebas y evita bugs de zona horaria).
    /// </summary>
    public async Task<object> GetDashboardStatsAsync(Guid negocioId)
    {
        var hoy = TimeHelper.Now.Date;     // Ejemplo: 2026-02-18 00:00:00
        var manana = hoy.AddDays(1);       // Ejemplo: 2026-02-19 00:00:00

        // Traemos todas las citas del día (incluyendo canceladas, para el contador de canceladas)
        var citasHoy = await _context.Citas.AsNoTracking()
            .Where(c => c.NegocioId == negocioId
                && c.FechaHoraInicio >= hoy && c.FechaHoraInicio < manana)
            .ToListAsync();

        // Los pagos son una tabla separada; solo existen para citas completadas y cobradas.
        var pagosHoy = await _context.PagosCita.AsNoTracking()
            .Where(p => p.NegocioId == negocioId
                && p.FechaCreacion >= hoy && p.FechaCreacion < manana)
            .ToListAsync();

        return new
        {
            // Total de citas del día excluyendo las canceladas (las canceladas no "cuentan" como citas)
            citasHoy = citasHoy.Count(c => c.Estado != "cancelada"),
            // Suma de todos los pagos registrados hoy (incluye servicio + extra + propina)
            ingresosHoy = pagosHoy.Sum(p => p.Total),
            // Cuántas citas llegaron al final del ciclo exitosamente
            completadas = citasHoy.Count(c => c.Estado == "completada"),
            // Cuántas citas se cancelaron hoy (útil para medir tasa de ausentismo)
            canceladas = citasHoy.Count(c => c.Estado == "cancelada")
        };
    }

    /// <summary>
    /// Obtiene las últimas 50 citas de un cliente para mostrar su historial.
    /// El límite de 50 es arbitrario pero razonable: evita cargar miles de registros
    /// en memoria cuando un cliente tiene muchos años de historial.
    /// </summary>
    public async Task<object> GetHistorialClienteAsync(Guid clienteId, Guid negocioId)
    {
        return await _context.Citas.AsNoTracking()
            .Where(c => c.ClienteId == clienteId && c.NegocioId == negocioId)
            // Las más recientes primero para que el historial sea intuitivo
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
    /// Crea una cita rápida con soporte para cliente nuevo o existente.
    ///
    /// FLUJO DE DECISIÓN DEL CLIENTE:
    ///   - Si dto.ClienteId tiene valor → busca y usa ese cliente (ya registrado en el sistema).
    ///   - Si dto.ClienteId es null → crea un cliente nuevo con los datos del formulario.
    ///     El cliente nuevo se crea con Dias=0 y Precio=0 porque aún no tiene membresía;
    ///     solo existe para poder asociarle citas.
    ///
    /// PREVENCIÓN DE DOBLE-BOOKING:
    ///   Antes de guardar, verificamos que el empleado no tenga otra cita activa
    ///   que se solape con el horario solicitado. Usamos el mismo algoritmo de
    ///   solapamiento de intervalos que en GetSlotsDisponiblesAsync.
    ///
    /// La cita se crea siempre en estado "pendiente" (primer estado del ciclo).
    /// </summary>
    public async Task<(bool success, string message, Guid? citaId)> CrearCitaRapidaAsync(CitaQuickCreateDto dto)
    {
        // Validar que la cita no sea en el pasado (usar TimeHelper.Now para zona horaria Ecuador UTC-5)
        if (dto.FechaHoraInicio < TimeHelper.Now)
            return (false, "No se pueden crear citas en el pasado", null);

        // --- RESOLVER CLIENTE ---
        // Determinamos el clienteId y el nombre antes de seguir.
        Guid clienteId;
        string nombreCliente;

        if (dto.ClienteId.HasValue)
        {
            // Caso 1: el recepcionista seleccionó un cliente existente del buscador.
            // Verificamos que pertenezca a este negocio (seguridad multi-tenant).
            var clienteExistente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.ClienteId == dto.ClienteId.Value && c.NegocioId == dto.NegocioId);
            if (clienteExistente == null)
                return (false, "Cliente no encontrado", null);

            clienteId = clienteExistente.ClienteId;
            nombreCliente = $"{clienteExistente.Nombre} {clienteExistente.Apellido}";
        }
        else
        {
            // Caso 2: cliente nuevo "al vuelo". Requiere nombre y apellido mínimamente.
            if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
                return (false, "Nombre y Apellido son obligatorios para cliente nuevo", null);

            // Creamos el cliente con valores mínimos. Los campos de membresía (Dias, Precio)
            // se actualizan después si el cliente decide suscribirse.
            var nuevoCliente = new Cliente
            {
                ClienteId = Guid.NewGuid(),
                NegocioId = dto.NegocioId,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Email = dto.Email,
                Telefono = PhoneHelper.NormalizeEcuador(dto.Telefono),
                Dias = 0,              // Sin membresía por ahora
                Precio = 0,            // Sin precio de membresía
                EsDiario = false,
                FechaDeCreacion = TimeHelper.Now,
                FechaDeActualizacion = TimeHelper.Now,
                FechaQueTermina = TimeHelper.Now  // Membresía vencida (no tiene)
            };
            _context.Clientes.Add(nuevoCliente);
            clienteId = nuevoCliente.ClienteId;
            nombreCliente = $"{dto.Nombre} {dto.Apellido}";
        }

        // --- VALIDAR EMPLEADO ---
        // Verificamos que el empleado exista, pertenezca al negocio y esté activo.
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == dto.EmpleadoId && e.NegocioId == dto.NegocioId && e.IsActive);
        if (empleado == null)
            return (false, "Empleado no encontrado", null);

        // --- VALIDAR SERVICIO ---
        // Verificamos que el servicio exista, pertenezca al negocio y esté activo.
        var servicio = await _context.ServiciosNegocio
            .FirstOrDefaultAsync(s => s.ServicioId == dto.ServicioId && s.NegocioId == dto.NegocioId && s.IsActive);
        if (servicio == null)
            return (false, "Servicio no encontrado", null);

        // Calculamos la hora de fin sumando la duración del servicio al inicio.
        var fechaHoraFin = dto.FechaHoraInicio.AddMinutes(servicio.DuracionMinutos);

        // --- ANTI-DOBLE-BOOKING ---
        // Verificamos si el empleado ya tiene alguna cita activa que se solape con este nuevo slot.
        // Excluimos las canceladas porque esas no ocupan el calendario.
        // SEGURIDAD MULTI-TENANT: Filtramos por NegocioId para evitar colisiones cruzadas.
        // Usamos el algoritmo clásico de intersección de intervalos:
        //   Dos intervalos [A_inicio, A_fin] y [B_inicio, B_fin] se solapan si:
        //   A_inicio < B_fin AND A_fin > B_inicio
        var conflicto = await _context.Citas
            .AnyAsync(c => c.EmpleadoId == dto.EmpleadoId
                && c.NegocioId == dto.NegocioId
                && c.Estado != "cancelada"
                && c.FechaHoraInicio < fechaHoraFin
                && c.FechaHoraFin > dto.FechaHoraInicio);

        if (conflicto)
            return (false, "El empleado ya tiene una cita en ese horario", null);

        // --- CREAR LA CITA ---
        // Guardamos los nombres desnormalizados (en la misma tabla) para que si luego
        // se cambia el nombre del empleado o servicio, el histórico de citas no se altere.
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
            PrecioServicio = servicio.Precio,      // Precio al momento de la reserva (snapshot)
            FechaHoraInicio = dto.FechaHoraInicio,
            FechaHoraFin = fechaHoraFin,
            DuracionMinutos = servicio.DuracionMinutos,
            Estado = "pendiente",                  // Siempre inicia en estado pendiente
            RecordatorioEnviado = false,            // El servicio de recordatorios lo cambiará a true
            FechaCreacion = TimeHelper.Now,
            FechaDeActualizacion = TimeHelper.Now
        };

        _context.Citas.Add(cita);
        // SaveChangesAsync persiste tanto el cliente nuevo (si aplica) como la cita, en la misma transacción.
        await _context.SaveChangesAsync();

        return (true, "Cita creada exitosamente", cita.CitaId);
    }

    /// <summary>
    /// Crea una cita estándar con un cliente ya registrado en el sistema.
    /// A diferencia de <see cref="CrearCitaRapidaAsync"/>, este método no crea clientes nuevos.
    /// Comparte la misma lógica de anti-doble-booking.
    /// </summary>
    public async Task<(bool success, string message)> CrearCitaAsync(CitaCreateDto dto)
    {
        // Validar que la cita no sea en el pasado (usar TimeHelper.Now para zona horaria Ecuador UTC-5)
        if (dto.FechaHoraInicio < TimeHelper.Now)
            return (false, "No se pueden crear citas en el pasado");

        // Obtener y validar los tres actores de la cita: cliente, empleado y servicio.
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

        // Prevención de doble-booking: el empleado no puede tener dos citas activas en el mismo horario.
        // SEGURIDAD MULTI-TENANT: Filtramos por NegocioId para aislamiento entre negocios.
        // Nota: excluimos las propias citas canceladas porque no ocupan el calendario.
        var conflicto = await _context.Citas
            .AnyAsync(c => c.EmpleadoId == dto.EmpleadoId
                && c.NegocioId == dto.NegocioId
                && c.Estado != "cancelada"
                && c.FechaHoraInicio < fechaHoraFin
                && c.FechaHoraFin > dto.FechaHoraInicio);

        if (conflicto)
            return (false, "El empleado ya tiene una cita en ese horario");

        // Creamos la cita con nombres desnormalizados para preservar el histórico
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
    /// Mueve una cita a una nueva fecha y hora. Se llama cuando el usuario arrastra
    /// un evento en el calendario (drag-and-drop de FullCalendar).
    ///
    /// REGLAS DE NEGOCIO:
    ///   - No se pueden mover citas completadas o canceladas (son registros definitivos).
    ///   - Se revalida anti-doble-booking en la nueva posición.
    ///   - Al verificar conflictos, excluimos la propia cita que estamos moviendo
    ///     (c.CitaId != cita.CitaId) para evitar un falso positivo con ella misma.
    ///   - La duración del servicio no cambia: si duraba 45 min antes, sigue durando 45 min.
    /// </summary>
    public async Task<(bool success, string message)> MoverCitaAsync(CitaMoveDto dto)
    {
        var cita = await _context.Citas
            .FirstOrDefaultAsync(c => c.CitaId == dto.CitaId && c.NegocioId == dto.NegocioId);

        if (cita == null)
            return (false, "Cita no encontrada");

        // No tiene sentido mover una cita que ya terminó o fue cancelada
        if (cita.Estado == "completada" || cita.Estado == "cancelada")
            return (false, "No se puede mover una cita completada o cancelada");

        // La duración se conserva; solo cambia el punto de inicio
        var nuevaFechaFin = dto.NuevaFechaHoraInicio.AddMinutes(cita.DuracionMinutos);

        // Verificar conflictos en la nueva posición.
        // IMPORTANTE: excluimos la propia cita (c.CitaId != cita.CitaId) porque
        // de lo contrario la cita colisionaría consigo misma si no se mueve a otro día.
        // SEGURIDAD MULTI-TENANT: Filtramos por NegocioId para aislamiento entre negocios.
        var conflicto = await _context.Citas
            .AnyAsync(c => c.EmpleadoId == cita.EmpleadoId
                && c.NegocioId == dto.NegocioId
                && c.CitaId != cita.CitaId           // Excluir la cita que estamos moviendo
                && c.Estado != "cancelada"
                && c.FechaHoraInicio < nuevaFechaFin
                && c.FechaHoraFin > dto.NuevaFechaHoraInicio);

        if (conflicto)
            return (false, "El empleado ya tiene una cita en ese horario");

        // Actualizar la cita con la nueva posición
        cita.FechaHoraInicio = dto.NuevaFechaHoraInicio;
        cita.FechaHoraFin = nuevaFechaFin;
        cita.FechaDeActualizacion = TimeHelper.Now;

        await _context.SaveChangesAsync();

        return (true, "Cita movida exitosamente");
    }

    /// <summary>
    /// Cambia el estado de una cita implementando una máquina de estados estricta.
    ///
    /// ¿QUÉ ES UNA MÁQUINA DE ESTADOS?
    ///   Es un patrón donde un objeto puede estar en un conjunto finito de "estados",
    ///   y solo puede cambiar de uno a otro a través de "transiciones" definidas.
    ///   Esto evita estados inconsistentes (ej: pagar una cita que no está completada).
    ///
    /// MAPA DE TRANSICIONES:
    ///   "pendiente"   → puede ir a: "confirmada", "cancelada"
    ///   "confirmada"  → puede ir a: "en_progreso", "cancelada"
    ///   "en_progreso" → puede ir a: "completada", "cancelada"
    ///   "completada"  → no puede ir a ningún estado (estado terminal)
    ///   "cancelada"   → no puede ir a ningún estado (estado terminal)
    ///
    /// Si se intenta una transición no permitida, la operación falla con un mensaje descriptivo.
    /// </summary>
    public async Task<(bool success, string message)> CambiarEstadoCitaAsync(
        Guid citaId, Guid negocioId, string nuevoEstado, string motivoCancelacion = null)
    {
        // Validar que el estado pedido sea uno de los estados conocidos del sistema
        var estadosValidos = new[] { "pendiente", "confirmada", "en_progreso", "completada", "cancelada" };
        if (!estadosValidos.Contains(nuevoEstado))
            return (false, "Estado no válido");

        var cita = await _context.Citas
            .FirstOrDefaultAsync(c => c.CitaId == citaId && c.NegocioId == negocioId);

        if (cita == null)
            return (false, "Cita no encontrada");

        // Definir el mapa completo de transiciones permitidas.
        // Las claves son el estado actual; los valores son los estados destino permitidos.
        // Los estados terminales (completada, cancelada) tienen un array vacío: no pueden transicionar.
        var transicionesPermitidas = new Dictionary<string, string[]>
        {
            ["pendiente"]   = new[] { "confirmada", "cancelada" },
            ["confirmada"]  = new[] { "en_progreso", "cancelada" },
            ["en_progreso"] = new[] { "completada", "cancelada" },
            ["completada"]  = Array.Empty<string>(),   // Estado terminal: no hay vuelta atrás
            ["cancelada"]   = Array.Empty<string>()    // Estado terminal: no hay vuelta atrás
        };

        // Verificar si la transición solicitada está permitida desde el estado actual
        if (!transicionesPermitidas.TryGetValue(cita.Estado, out var permitidos) || !permitidos.Contains(nuevoEstado))
            return (false, $"No se puede cambiar de '{cita.Estado}' a '{nuevoEstado}'");

        // Si se está cancelando, el motivo es obligatorio para mantener un registro claro
        if (nuevoEstado == "cancelada" && string.IsNullOrWhiteSpace(motivoCancelacion))
            return (false, "El motivo de cancelación es obligatorio");

        // Aplicar el cambio de estado
        cita.Estado = nuevoEstado;
        cita.FechaDeActualizacion = TimeHelper.Now;

        // Solo guardamos el motivo si la cita está siendo cancelada
        if (nuevoEstado == "cancelada")
            cita.MotivoCancelacion = motivoCancelacion;

        await _context.SaveChangesAsync();

        return (true, $"Estado cambiado a {nuevoEstado}");
    }

    // ═══════════════════════════════════════════════════════════
    // REGISTRO DE PAGOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Registra el pago de una cita que ya fue completada.
    ///
    /// REGLAS DE NEGOCIO:
    ///   1. La cita debe estar en estado "completada" (no se paga antes de terminar el servicio).
    ///   2. Solo puede haber un pago por cita (idempotencia: evita cobrar dos veces).
    ///   3. Si es un regalo (EsRegalo=true), el total es $0 y el método de pago es "regalo".
    ///   4. Si no es regalo, el método de pago es obligatorio.
    ///   5. Los montos extra y propinas no pueden ser negativos.
    ///
    /// CÁLCULO DEL TOTAL:
    ///   - Si es regalo: total = 0 (se perdona el cobro)
    ///   - Si es pago normal: total = PrecioServicio + MontoExtra + Propina
    ///     Donde PrecioServicio se toma de la cita (no del DTO) para usar el precio
    ///     que se acordó al momento de reservar.
    ///
    /// AUDITORÍA:
    ///   Además de guardar el PagoCita, creamos un log inmutable en el sistema de auditoría.
    ///   Esto es importante para contabilidad: los logs no se pueden borrar ni editar.
    /// </summary>
    public async Task<(bool success, string message)> RegistrarPagoAsync(PagoCitaDto dto)
    {
        var cita = await _context.Citas
            .FirstOrDefaultAsync(c => c.CitaId == dto.CitaId && c.NegocioId == dto.NegocioId);

        if (cita == null)
            return (false, "Cita no encontrada");

        // El pago solo es posible después de que el servicio fue realizado
        if (cita.Estado != "completada")
            return (false, "La cita debe estar completada para registrar el pago");

        // Verificar que no exista ya un pago previo para esta cita.
        // Si ya hay uno, no permitimos sobreescribirlo (protección contra doble cobro).
        var pagoExistente = await _context.PagosCita
            .AnyAsync(p => p.CitaId == dto.CitaId);

        if (pagoExistente)
            return (false, "Ya existe un pago registrado para esta cita");

        // Validaciones específicas para cada tipo de pago
        if (dto.EsRegalo && string.IsNullOrWhiteSpace(dto.MotivoRegalo))
            return (false, "El motivo del regalo es obligatorio");

        // Los montos deben ser no-negativos (0 está permitido, significa que no aplica)
        if (dto.MontoExtra < 0)
            return (false, "El monto extra no puede ser negativo");
        if (dto.Propina < 0)
            return (false, "La propina no puede ser negativa");

        // Si no es regalo, el método de pago es obligatorio para el registro contable
        if (!dto.EsRegalo && string.IsNullOrWhiteSpace(dto.MetodoPago))
            return (false, "El método de pago es obligatorio");

        // Calcular el total final según si es regalo o pago normal
        var total = dto.EsRegalo ? 0 : cita.PrecioServicio + dto.MontoExtra + dto.Propina;

        var pago = new PagoCita
        {
            PagoId = Guid.NewGuid(),
            CitaId = dto.CitaId,
            NegocioId = dto.NegocioId,
            MontoServicio = cita.PrecioServicio,    // Precio del servicio (tomado de la cita, no del DTO)
            MontoExtra = dto.MontoExtra,             // Cargo adicional opcional (ej: productos usados)
            DetalleExtra = dto.DetalleExtra,         // Descripción del cargo extra (ej: "Tinte extra")
            Propina = dto.Propina,                   // Propina voluntaria del cliente
            Total = total,
            MetodoPago = dto.EsRegalo ? "regalo" : dto.MetodoPago,  // Si es regalo, el método es "regalo"
            EsRegalo = dto.EsRegalo,
            MotivoRegalo = dto.MotivoRegalo,
            // Desnormalizamos el nombre del cliente y servicio para que el registro
            // de pago sea legible sin necesidad de joins en reportes de contabilidad
            NombreCliente = cita.NombreCliente,
            NombreServicio = cita.NombreServicio,
            FechaCreacion = TimeHelper.Now
        };

        _context.PagosCita.Add(pago);
        await _context.SaveChangesAsync();

        // Crear log inmutable de auditoría. Esto sirve para que el dueño del negocio
        // pueda auditar todos los ingresos del día incluso si alguien intenta borrar pagos.
        if (dto.EsRegalo)
        {
            // Log simplificado para regalos: no hay monto involucrado
            await _logService.CreateLogAsync(dto.NegocioId, "cita_regalo",
                $"Cita regalo: {cita.NombreServicio} para {cita.NombreCliente}. Motivo: {dto.MotivoRegalo}",
                0, cita.ClienteId, cita.NombreCliente);
        }
        else
        {
            // Log detallado para pagos: incluye desglose de montos para contabilidad
            await _logService.CreateLogAsync(dto.NegocioId, "cita_completada",
                $"Cita completada: {cita.NombreServicio} para {cita.NombreCliente}. " +
                $"Servicio: ${cita.PrecioServicio:F2}" +
                // Solo incluimos Extra y Propina en el log si fueron mayores a 0
                (dto.MontoExtra > 0 ? $", Extra: ${dto.MontoExtra:F2}" : "") +
                (dto.Propina > 0 ? $", Propina: ${dto.Propina:F2}" : "") +
                $". Total: ${total:F2} ({dto.MetodoPago})",
                total, cita.ClienteId, cita.NombreCliente);
        }

        return (true, "Pago registrado exitosamente");
    }

    /// <summary>
    /// Obtiene el detalle del pago asociado a una cita.
    /// Devuelve <c>null</c> si la cita aún no tiene pago (ej: está en estado "completada"
    /// pero el recepcionista aún no registró el cobro).
    /// </summary>
    public async Task<object> GetPagoCitaAsync(Guid citaId, Guid negocioId)
    {
        return await _context.PagosCita.AsNoTracking()
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
