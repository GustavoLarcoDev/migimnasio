// ═══════════════════════════════════════════════════════════
// EmpleadoService.cs — Implementación del servicio de empleados
//
// CONTEXTO GENERAL:
//   En el modelo artesanal, los empleados son el corazón del negocio.
//   Cada cita se asigna a un empleado específico, y el calendario de citas
//   se construye en base a la disponibilidad de cada empleado.
//
// SISTEMA DE HORARIOS (en orden de prioridad):
//
//   Prioridad 1 — HorarioExcepcion (tabla HorariosExcepcion):
//     Un registro puntual para una fecha exacta. Puede ser:
//     a) Día libre: EsDiaLibre=true → no trabaja ese día
//     b) Horario especial: EsDiaLibre=false → trabaja con horas distintas al normal
//     Las excepciones SIEMPRE tienen prioridad sobre el horario semanal.
//
//   Prioridad 2 — HorarioEmpleado (tabla HorariosEmpleado):
//     El horario semanal recurrente. Un registro por cada día de la semana (0-6).
//     Si Activo=false, el empleado no trabaja ese día de la semana.
//
//   Prioridad 3 — Sin horario:
//     Si no hay excepción ni horario semanal, se asume que no trabaja.
//
// BAJA LÓGICA:
//   IsActive=false en lugar de borrar el registro. Los empleados inactivos
//   no aparecen en búsquedas ni selectores, pero sus citas pasadas siguen
//   referenciándolos correctamente en el historial.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Helpers;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Implementación concreta del servicio de empleados.
/// Gestiona el CRUD de empleados, sus horarios semanales, excepciones de horario
/// y el cálculo de disponibilidad por fecha.
/// </summary>
public class EmpleadoService : IEmpleadoService
{
    // _context: acceso a la base de datos a través de Entity Framework Core.
    // Nos permite hacer consultas con LINQ que se traducen automáticamente a SQL.
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// ASP.NET Core inyecta el ApplicationDbContext automáticamente (configurado en Program.cs).
    /// </summary>
    public EmpleadoService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los empleados activos del negocio ordenados alfabéticamente por nombre.
    /// Solo incluye empleados con IsActive=true; los dados de baja quedan invisibles.
    /// Proyectamos un objeto anónimo (no la entidad completa) para enviar solo los campos
    /// necesarios al frontend y reducir el tamaño de la respuesta JSON.
    /// </summary>
    public async Task<object> GetEmpleadosAsync(Guid negocioId)
    {
        return await _context.Empleados.AsNoTracking()
            .Where(e => e.NegocioId == negocioId && e.IsActive)
            .OrderBy(e => e.Nombre)
            .Select(e => new
            {
                e.EmpleadoId,
                e.Nombre,
                e.Apellido,
                // Concatenamos nombre completo en el servidor para no tener que hacerlo en cada vista
                NombreCompleto = e.Nombre + " " + e.Apellido,
                e.Telefono,
                e.Email,
                e.Especialidad,
                e.FechaCreacion
            })
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un empleado específico por su ID.
    /// Devuelve la entidad <see cref="Empleado"/> completa porque algunos llamadores
    /// necesitan acceder a campos que no están en el objeto proyectado de GetEmpleadosAsync.
    /// Siempre filtra por negocioId para garantizar el aislamiento multi-tenant.
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
    /// Crea un nuevo empleado y le asigna un horario semanal por defecto.
    ///
    /// POR QUÉ CREAMOS EL HORARIO AUTOMÁTICAMENTE:
    ///   Si no creamos el horario, el empleado recién creado no aparecería en
    ///   el selector de disponibilidad y el sistema lo trataría como "no trabaja
    ///   ningún día". El horario por defecto (Lun-Sáb 9-17) es un punto de partida
    ///   razonable que el administrador puede ajustar después.
    ///
    /// HORARIO POR DEFECTO:
    ///   - Días 1 al 6 (Lunes a Sábado): activo, de 09:00 a 17:00
    ///   - Día 0 (Domingo): inactivo (HoraInicio/HoraFin se guardan pero Activo=false)
    ///
    /// Se realizan dos SaveChangesAsync separados porque el segundo depende del
    /// EmpleadoId generado en el primero.
    /// </summary>
    public async Task<(bool success, string message)> CrearEmpleadoAsync(EmpleadoCreateDto dto)
    {
        // Validaciones básicas antes de crear
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
            Telefono = PhoneHelper.NormalizeEcuador(dto.Telefono),
            Email = dto.Email?.Trim(),
            Especialidad = dto.Especialidad,
            IsActive = true,
            FechaCreacion = TimeHelper.Now
        };

        // Transacción explícita: empleado + horarios se guardan atómicamente.
        // Si la creación de horarios falla, el empleado tampoco se persiste,
        // evitando empleados huérfanos sin horario (que el sistema trataría como "no trabaja nunca").
        using var transaction = await _context.Database.BeginTransactionAsync();

        _context.Empleados.Add(empleado);
        // Primer guardado: necesitamos el EmpleadoId generado para los horarios
        await _context.SaveChangesAsync();

        // Crear el horario por defecto para Lunes (1) a Sábado (6).
        // .NET usa DayOfWeek donde 0=Domingo, 1=Lunes, ..., 6=Sábado.
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
                Activo = true   // Trabaja estos días por defecto
            });
        }

        // Domingo inactivo. Guardamos las horas igual (09:00-17:00) para que el formulario
        // de edición de horarios tenga valores predeterminados si el admin decide activarlo.
        _context.HorariosEmpleado.Add(new HorarioEmpleado
        {
            HorarioId = Guid.NewGuid(),
            EmpleadoId = empleado.EmpleadoId,
            NegocioId = dto.NegocioId,
            DiaSemana = 0,      // 0 = Domingo
            HoraInicio = "09:00",
            HoraFin = "17:00",
            Activo = false      // No trabaja los domingos por defecto
        });

        // Segundo guardado + commit: si falla, todo se revierte automáticamente
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, "Empleado creado exitosamente");
    }

    /// <summary>
    /// Edita los datos básicos de un empleado: nombre, apellido, teléfono y especialidad.
    /// El horario semanal se edita por separado con <see cref="GuardarHorariosAsync"/>.
    /// Solo se pueden editar empleados activos (IsActive=true).
    /// </summary>
    public async Task<(bool success, string message)> EditarEmpleadoAsync(EmpleadoCreateDto dto)
    {
        // Buscamos el empleado verificando que sea del negocio correcto y esté activo
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == dto.EmpleadoId && e.NegocioId == dto.NegocioId && e.IsActive);

        if (empleado == null)
            return (false, "Empleado no encontrado");

        // Validaciones en el método de edición también, por si alguien llama el servicio
        // directamente (no solo desde el formulario web que ya valida en cliente)
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return (false, "El nombre es obligatorio");
        if (string.IsNullOrWhiteSpace(dto.Apellido))
            return (false, "El apellido es obligatorio");

        // Actualizar solo los campos editables. No tocamos EmpleadoId, NegocioId, IsActive ni FechaCreacion.
        empleado.Nombre = dto.Nombre;
        empleado.Apellido = dto.Apellido;
        empleado.Telefono = PhoneHelper.NormalizeEcuador(dto.Telefono);
        empleado.Email = dto.Email?.Trim();
        empleado.Especialidad = dto.Especialidad;

        // EF Core detecta automáticamente los cambios en la entidad tracked y
        // genera el UPDATE de SQL solo con los campos modificados.
        await _context.SaveChangesAsync();

        return (true, "Empleado actualizado exitosamente");
    }

    /// <summary>
    /// Da de baja a un empleado de forma lógica (soft delete: IsActive = false).
    ///
    /// POR QUÉ NO BORRAMOS EL REGISTRO:
    ///   Si borráramos el empleado de la base de datos, todas las citas pasadas que lo
    ///   referencian quedarían con una clave foránea inválida (o se borrarían en cascada,
    ///   perdiendo el historial). Con el soft delete, el historial se preserva intacto.
    ///
    /// PROTECCIÓN CONTRA BAJA CON CITAS PENDIENTES:
    ///   Verificamos que el empleado no tenga citas en estados activos (pendiente,
    ///   confirmada, en_progreso). Si las tiene, obligamos al administrador a resolverlas
    ///   primero antes de dar de baja al empleado, evitando citas "huérfanas" sin atender.
    /// </summary>
    public async Task<(bool success, string message)> EliminarEmpleadoAsync(Guid empleadoId, Guid negocioId)
    {
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId && e.NegocioId == negocioId && e.IsActive);

        if (empleado == null)
            return (false, "Empleado no encontrado");

        // Verificar si tiene citas en estados activos (que aún no terminaron ni fueron canceladas).
        // Las citas "completada" y "cancelada" son estados terminales y no bloquean la baja.
        // SEGURIDAD MULTI-TENANT: Filtramos también por NegocioId para aislamiento entre negocios.
        var tieneCitasPendientes = await _context.Citas
            .AnyAsync(c => c.EmpleadoId == empleadoId
                && c.NegocioId == negocioId
                && c.Estado != "cancelada"
                && c.Estado != "completada");

        if (tieneCitasPendientes)
            return (false, "No se puede eliminar. El empleado tiene citas pendientes.");

        // Soft delete: marcamos como inactivo en vez de borrar
        empleado.IsActive = false;
        await _context.SaveChangesAsync();

        return (true, "Empleado eliminado exitosamente");
    }

    // ═══════════════════════════════════════════════════════════
    // HORARIOS SEMANALES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene el horario semanal de un empleado (los 7 días de la semana).
    /// Devuelve los registros ordenados por DiaSemana (0=Dom, 1=Lun, ..., 6=Sáb)
    /// para que el formulario de edición los muestre en orden lógico.
    /// </summary>
    public async Task<object> GetHorariosAsync(Guid empleadoId, Guid negocioId)
    {
        return await _context.HorariosEmpleado.AsNoTracking()
            .Where(h => h.EmpleadoId == empleadoId && h.NegocioId == negocioId)
            .OrderBy(h => h.DiaSemana)
            .Select(h => new
            {
                h.HorarioId,
                h.DiaSemana,    // 0=Dom, 1=Lun, 2=Mar, 3=Mié, 4=Jue, 5=Vie, 6=Sáb
                h.HoraInicio,   // Formato "HH:mm", ej: "09:00"
                h.HoraFin,      // Formato "HH:mm", ej: "17:30"
                h.Activo        // Si es false, el empleado no trabaja ese día de la semana
            })
            .ToListAsync();
    }

    /// <summary>
    /// Guarda o actualiza el horario semanal completo de un empleado.
    ///
    /// ESTRATEGIA UPSERT:
    ///   Para cada día que viene en el DTO, buscamos si ya existe un HorarioEmpleado
    ///   para ese DiaSemana. Si existe, lo actualizamos; si no, lo creamos.
    ///   Esto permite que el formulario envíe todos los días de una vez (upsert bulk)
    ///   sin preocuparse de si ya existían o no.
    ///
    /// VALIDACIONES POR DÍA:
    ///   Solo validamos horas para días activos. Un día inactivo (Activo=false) puede
    ///   tener horas vacías o inválidas sin problema, porque no se usarán para nada.
    /// </summary>
    public async Task<(bool success, string message)> GuardarHorariosAsync(HorarioEmpleadoDto dto)
    {
        // Verificamos que el empleado exista y esté activo antes de guardar sus horarios
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == dto.EmpleadoId && e.NegocioId == dto.NegocioId && e.IsActive);

        if (empleado == null)
            return (false, "Empleado no encontrado");

        // Pre-cargar horarios existentes en diccionario (elimina N+1)
        var horariosExistentes = await _context.HorariosEmpleado
            .Where(h => h.EmpleadoId == dto.EmpleadoId && h.NegocioId == dto.NegocioId)
            .ToDictionaryAsync(h => h.DiaSemana);

        // Procesamos cada día enviado en el DTO
        foreach (var dia in dto.Dias)
        {
            // Solo validamos el formato de horas si el día está marcado como activo.
            // No tiene sentido validar horas de un día libre.
            if (dia.Activo)
            {
                // Ambas horas son obligatorias si el día está activo
                if (string.IsNullOrWhiteSpace(dia.HoraInicio) || string.IsNullOrWhiteSpace(dia.HoraFin))
                    return (false, $"Hora inicio y fin son obligatorias para días activos");

                // Verificar que el formato sea parseable como TimeSpan ("HH:mm")
                // TryParse devuelve false si el string no es un tiempo válido
                if (!TimeSpan.TryParse(dia.HoraInicio, out var hi) || !TimeSpan.TryParse(dia.HoraFin, out var hf))
                    return (false, $"Formato de hora inválido (use HH:mm)");

                // La jornada laboral tiene sentido solo si el inicio es antes que el fin
                if (hi >= hf)
                    return (false, $"La hora de inicio debe ser anterior a la hora de fin");
            }

            // Buscar en el diccionario pre-cargado (O(1) en vez de consulta SQL por día).
            // SEGURIDAD MULTI-TENANT: El diccionario ya fue filtrado por NegocioId arriba.
            var horario = horariosExistentes.GetValueOrDefault(dia.DiaSemana);

            if (horario != null)
            {
                // ACTUALIZAR: el registro ya existe, solo modificamos los valores
                horario.HoraInicio = dia.HoraInicio;
                horario.HoraFin = dia.HoraFin;
                horario.Activo = dia.Activo;
                // EF Core detecta el cambio automáticamente (no necesitamos llamar Update)
            }
            else
            {
                // INSERTAR: no existía registro para este día, creamos uno nuevo
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

        // Un solo SaveChangesAsync al final para persistir todos los cambios en una transacción
        await _context.SaveChangesAsync();
        return (true, "Horario guardado exitosamente");
    }

    // ═══════════════════════════════════════════════════════════
    // DISPONIBILIDAD Y EXCEPCIONES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los empleados activos con su disponibilidad calculada para una fecha.
    ///
    /// OPTIMIZACIÓN DE CONSULTAS (N+1 Prevention):
    ///   En vez de hacer una consulta por empleado (N+1 problema), hacemos
    ///   3 consultas batch para toda la lista de empleados de una vez:
    ///     1. Traer todos los empleados activos
    ///     2. Traer todos sus horarios para ese día de la semana
    ///     3. Traer todas sus excepciones para esa fecha
    ///     4. Traer el conteo de citas por empleado para ese día
    ///   Luego en memoria cruzamos la información con LINQ (no SQL).
    ///   Esto es mucho más eficiente que N consultas individuales.
    ///
    /// LÓGICA DE DISPONIBILIDAD (aplicada en memoria):
    ///   Para cada empleado: primero buscamos su excepción para esa fecha.
    ///   Si tiene excepción → usamos los datos de la excepción.
    ///   Si no tiene excepción → usamos su horario semanal para ese día.
    /// </summary>
    public async Task<object> GetEmpleadosConDisponibilidadAsync(Guid negocioId, DateTime fecha)
    {
        // El día de la semana como entero (0=Dom, 1=Lun, ..., 6=Sáb)
        var diaSemana = (int)fecha.DayOfWeek;

        // CONSULTA 1: Todos los empleados activos del negocio
        var empleados = await _context.Empleados.AsNoTracking()
            .Where(e => e.NegocioId == negocioId && e.IsActive)
            .OrderBy(e => e.Nombre)
            .ToListAsync();

        // Lista de IDs para usarlos en las consultas batch siguientes
        var empleadoIds = empleados.Select(e => e.EmpleadoId).ToList();

        // CONSULTA 2: Horarios semanales de todos los empleados para ese día de semana.
        // Traemos solo el DiaSemana que corresponde a la fecha pedida.
        var horarios = await _context.HorariosEmpleado.AsNoTracking()
            .Where(h => empleadoIds.Contains(h.EmpleadoId) && h.DiaSemana == diaSemana)
            .ToListAsync();

        // CONSULTA 3: Excepciones de horario de todos los empleados para esa fecha exacta.
        // Comparamos solo la parte de la fecha (sin hora) usando .Date
        var excepciones = await _context.HorariosExcepcion.AsNoTracking()
            .Where(h => empleadoIds.Contains(h.EmpleadoId) && h.Fecha.Date == fecha.Date)
            .ToListAsync();

        // CONSULTA 4: Conteo de citas por empleado para ese día (para mostrar carga de trabajo).
        // GroupBy en SQL genera un COUNT(*) GROUP BY EmpleadoId eficientemente.
        var citasDelDia = await _context.Citas.AsNoTracking()
            .Where(c => c.NegocioId == negocioId
                && c.Estado != "cancelada"
                && c.FechaHoraInicio >= fecha.Date
                && c.FechaHoraInicio < fecha.Date.AddDays(1))
            .GroupBy(c => c.EmpleadoId)
            .Select(g => new { EmpleadoId = g.Key, Total = g.Count() })
            .ToListAsync();

        // CRUCE EN MEMORIA: para cada empleado, determinamos su disponibilidad
        return empleados.Select(e =>
        {
            // Buscar la excepción específica para este empleado en esa fecha
            var excepcion = excepciones.FirstOrDefault(x => x.EmpleadoId == e.EmpleadoId);
            // Buscar el horario semanal normal de este empleado para ese día
            var horario = horarios.FirstOrDefault(h => h.EmpleadoId == e.EmpleadoId);
            // Cuántas citas tiene este empleado hoy (0 si no tiene ninguna)
            var citasCount = citasDelDia.FirstOrDefault(c => c.EmpleadoId == e.EmpleadoId)?.Total ?? 0;

            bool trabaja;
            string horaInicio = null, horaFin = null;

            if (excepcion != null)
            {
                // HAY EXCEPCIÓN: tiene prioridad total sobre el horario semanal.
                // Si es día libre (EsDiaLibre=true), trabaja=false y no hay horas.
                // Si es horario especial (EsDiaLibre=false), trabaja=true con las horas de la excepción.
                trabaja = !excepcion.EsDiaLibre;
                horaInicio = excepcion.HoraInicio;  // null si es día libre
                horaFin = excepcion.HoraFin;         // null si es día libre
            }
            else
            {
                // SIN EXCEPCIÓN: usamos el horario semanal normal.
                // Si no hay registro de horario para ese día, o Activo=false, no trabaja.
                trabaja = horario != null && horario.Activo;
                horaInicio = horario?.HoraInicio;   // null si no tiene horario
                horaFin = horario?.HoraFin;          // null si no tiene horario
            }

            return new
            {
                e.EmpleadoId,
                e.Nombre,
                e.Apellido,
                NombreCompleto = e.Nombre + " " + e.Apellido,
                e.Especialidad,
                Trabaja = trabaja,          // ¿Trabaja ese día? (bool)
                HoraInicio = horaInicio,    // Hora de entrada o null
                HoraFin = horaFin,          // Hora de salida o null
                CitasHoy = citasCount       // Cuántas citas tiene agendadas ese día
            };
        }).ToList();
    }

    /// <summary>
    /// Obtiene las excepciones de horario de un empleado, opcionalmente filtradas por rango de fechas.
    /// Si no se pasan fechas, devuelve todas las excepciones del empleado sin límite.
    /// Las excepciones se ordenan por fecha para facilitar la lectura cronológica.
    /// </summary>
    public async Task<object> GetExcepcionesAsync(Guid empleadoId, Guid negocioId, DateTime? desde, DateTime? hasta)
    {
        // Iniciamos con el filtro base: las excepciones del empleado en este negocio
        var query = _context.HorariosExcepcion.AsNoTracking()
            .Where(h => h.EmpleadoId == empleadoId && h.NegocioId == negocioId);

        // Aplicamos los filtros opcionales de fecha solo si se proporcionaron.
        // IQueryable permite encadenar condiciones sin ejecutar la consulta todavía.
        if (desde.HasValue)
            query = query.Where(h => h.Fecha >= desde.Value);
        if (hasta.HasValue)
            query = query.Where(h => h.Fecha <= hasta.Value);

        // Ejecutar la consulta y proyectar los campos necesarios
        return await query
            .OrderBy(h => h.Fecha)
            .Select(h => new
            {
                h.ExcepcionId,
                h.Fecha,
                h.EsDiaLibre,   // true=día libre, false=horario especial
                h.HoraInicio,   // null si EsDiaLibre=true
                h.HoraFin,      // null si EsDiaLibre=true
                h.Motivo        // Descripción opcional (ej: "Vacaciones de invierno")
            })
            .ToListAsync();
    }

    /// <summary>
    /// Crea una excepción de horario para una fecha específica.
    ///
    /// DOS TIPOS DE EXCEPCIÓN:
    ///
    ///   1. Día libre (esDiaLibre=true):
    ///      El empleado no trabaja ese día. Las horas no son relevantes y se guardan como null.
    ///      Caso de uso: vacaciones, feriados nacionales, día de enfermedad.
    ///
    ///   2. Horario especial (esDiaLibre=false):
    ///      El empleado trabaja, pero con horas diferentes a su horario semanal normal.
    ///      Las horas son obligatorias y deben ser válidas.
    ///      Caso de uso: turno corto por médico, jornada extendida por evento especial.
    ///
    /// UNICIDAD POR FECHA:
    ///   Solo puede existir una excepción por empleado por fecha. Si ya existe una,
    ///   la operación falla y el administrador debe eliminar la anterior primero.
    ///   Esto evita ambigüedad: para una fecha hay exactamente una regla especial.
    ///
    /// GUARDADO DE FECHA:
    ///   Guardamos solo la parte de fecha (.Date) para ignorar la hora. Así, si el
    ///   frontend envía "2026-02-18T14:30:00", se normaliza a "2026-02-18T00:00:00".
    /// </summary>
    public async Task<(bool success, string message)> CrearExcepcionAsync(
        Guid empleadoId, Guid negocioId, DateTime fecha, bool esDiaLibre,
        string horaInicio, string horaFin, string motivo)
    {
        // Validar horas solo si el empleado trabaja ese día con horario especial
        if (!esDiaLibre)
        {
            // Si no es día libre, las horas de entrada y salida son obligatorias
            if (string.IsNullOrWhiteSpace(horaInicio) || string.IsNullOrWhiteSpace(horaFin))
                return (false, "Hora inicio y fin son obligatorias");

            // Verificar que el formato sea "HH:mm" válido
            if (!TimeSpan.TryParse(horaInicio, out var hi) || !TimeSpan.TryParse(horaFin, out var hf))
                return (false, "Formato de hora inválido (use HH:mm)");

            // El inicio debe ser antes del fin (no pueden ser iguales ni invertidos)
            if (hi >= hf)
                return (false, "La hora de inicio debe ser anterior a la hora de fin");
        }

        // Verificar que no exista ya una excepción para esta fecha.
        // SEGURIDAD MULTI-TENANT: Filtramos por NegocioId para aislamiento entre negocios.
        // (usamos .Date para comparar solo la parte de la fecha, ignorando la hora)
        var existe = await _context.HorariosExcepcion
            .AnyAsync(h => h.EmpleadoId == empleadoId && h.NegocioId == negocioId && h.Fecha.Date == fecha.Date);

        if (existe)
            return (false, "Ya existe una excepción para esta fecha");

        var excepcion = new HorarioExcepcion
        {
            ExcepcionId = Guid.NewGuid(),
            EmpleadoId = empleadoId,
            NegocioId = negocioId,
            Fecha = fecha.Date,              // Normalizar a medianoche para consistencia
            EsDiaLibre = esDiaLibre,
            // Si es día libre, no guardamos horas (null). Si es horario especial, guardamos las horas.
            HoraInicio = esDiaLibre ? null : horaInicio,
            HoraFin = esDiaLibre ? null : horaFin,
            Motivo = motivo                  // Puede ser null si no se proporcionó motivo
        };

        _context.HorariosExcepcion.Add(excepcion);
        await _context.SaveChangesAsync();

        return (true, "Excepción creada exitosamente");
    }

    /// <summary>
    /// Elimina una excepción de horario.
    /// Tras la eliminación, el empleado vuelve a regirse por su horario semanal normal
    /// para esa fecha. No hay soft delete aquí porque las excepciones no tienen
    /// referencias a otras tablas (no son FK de ningún lado).
    /// </summary>
    public async Task<(bool success, string message)> EliminarExcepcionAsync(Guid excepcionId, Guid negocioId)
    {
        // Filtramos también por negocioId para evitar que un negocio borre excepciones de otro
        var excepcion = await _context.HorariosExcepcion
            .FirstOrDefaultAsync(h => h.ExcepcionId == excepcionId && h.NegocioId == negocioId);

        if (excepcion == null)
            return (false, "Excepción no encontrada");

        // Borrado físico: las excepciones no tienen historial que preservar
        _context.HorariosExcepcion.Remove(excepcion);
        await _context.SaveChangesAsync();

        return (true, "Excepción eliminada exitosamente");
    }
}
