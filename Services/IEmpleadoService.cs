// ═══════════════════════════════════════════════════════════
// IEmpleadoService.cs — Contrato (interfaz) del servicio de empleados
//
// Un empleado en el sistema artesanal es la persona que realiza los servicios
// (ej: estilista, manicurista, barbero). Cada empleado tiene:
//   - Datos básicos: nombre, apellido, teléfono, especialidad
//   - Horario semanal: qué días trabaja y en qué rango de horas
//   - Excepciones de horario: días libres puntuales o cambios de turno
//
// SISTEMA DE HORARIOS (prioridad de mayor a menor):
//   1. HorarioExcepcion para esa fecha exacta (si existe, tiene prioridad total)
//   2. HorarioEmpleado para el día de la semana (horario normal recurrente)
//   3. Si no hay ninguno: el empleado no trabaja ese día
//
// BAJA LÓGICA:
//   Los empleados nunca se borran de la base de datos. En cambio, se marca
//   IsActive = false. Esto preserva el historial de citas pasadas asociadas al empleado.
//   Para eliminar un empleado, primero deben resolverse todas sus citas pendientes.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

/// <summary>
/// Define el contrato para la gestión de empleados, sus horarios semanales y excepciones.
/// La implementación concreta está en <see cref="EmpleadoService"/>.
/// </summary>
public interface IEmpleadoService
{
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // Solo lectura: no modifican datos en la base de datos
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los empleados activos del negocio ordenados por nombre.
    /// Solo devuelve empleados con IsActive=true (los dados de baja no aparecen).
    /// </summary>
    /// <param name="negocioId">ID del negocio del cual obtener los empleados.</param>
    /// <returns>Lista de objetos con datos básicos de cada empleado activo.</returns>
    Task<object> GetEmpleadosAsync(Guid negocioId);

    /// <summary>
    /// Obtiene un empleado específico por su ID dentro de un negocio.
    /// Devuelve la entidad completa <see cref="Empleado"/> (no un DTO anónimo)
    /// porque a veces el código que llama necesita acceder a todos los campos del modelo.
    /// </summary>
    /// <param name="empleadoId">ID único del empleado.</param>
    /// <param name="negocioId">ID del negocio (seguridad: evita acceso cruzado entre negocios).</param>
    /// <returns>El empleado si existe y está activo, o <c>null</c>.</returns>
    Task<Empleado> GetEmpleadoAsync(Guid empleadoId, Guid negocioId);

    // ═══════════════════════════════════════════════════════════
    // CRUD DE EMPLEADOS
    // Operaciones que crean, modifican o dan de baja empleados
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo empleado y le asigna automáticamente un horario por defecto.
    /// El horario por defecto es: Lunes a Sábado de 09:00 a 17:00, Domingo inactivo.
    /// Esto evita que el nuevo empleado quede sin horario y no aparezca en el calendario.
    /// </summary>
    /// <param name="dto">DTO con los datos del empleado: nombre, apellido, teléfono, especialidad.</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo.</returns>
    Task<(bool success, string message)> CrearEmpleadoAsync(EmpleadoCreateDto dto);

    /// <summary>
    /// Edita los datos básicos de un empleado existente (nombre, teléfono, especialidad).
    /// No modifica el horario; para eso se usa <see cref="GuardarHorariosAsync"/>.
    /// </summary>
    /// <param name="dto">DTO con los nuevos valores. El campo EmpleadoId identifica cuál editar.</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo.</returns>
    Task<(bool success, string message)> EditarEmpleadoAsync(EmpleadoCreateDto dto);

    /// <summary>
    /// Da de baja a un empleado de forma lógica (IsActive = false).
    /// No se borra físicamente de la base de datos para preservar el historial de citas.
    ///
    /// REGLA DE NEGOCIO IMPORTANTE:
    ///   Si el empleado tiene citas en estado "pendiente", "confirmada" o "en_progreso",
    ///   la baja falla. El administrador debe reasignar o cancelar esas citas primero.
    ///   Las citas ya completadas o canceladas no bloquean la baja.
    /// </summary>
    /// <param name="empleadoId">ID del empleado a dar de baja.</param>
    /// <param name="negocioId">ID del negocio (seguridad).</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo (incluyendo el motivo si falla).</returns>
    Task<(bool success, string message)> EliminarEmpleadoAsync(Guid empleadoId, Guid negocioId);

    // ═══════════════════════════════════════════════════════════
    // HORARIOS SEMANALES
    // El horario semanal define los días y horas recurrentes de trabajo
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene el horario semanal de un empleado (los 7 días de la semana).
    /// Cada entrada indica si trabaja ese día (Activo) y en qué rango de horas.
    /// DiaSemana sigue la convención de .NET: 0=Domingo, 1=Lunes, ..., 6=Sábado.
    /// </summary>
    /// <param name="empleadoId">ID del empleado.</param>
    /// <param name="negocioId">ID del negocio (seguridad).</param>
    /// <returns>Lista de 7 objetos con DiaSemana, HoraInicio, HoraFin y Activo.</returns>
    Task<object> GetHorariosAsync(Guid empleadoId, Guid negocioId);

    /// <summary>
    /// Guarda o actualiza el horario semanal completo de un empleado.
    /// Para cada día recibido en el DTO, actualiza el registro existente o crea uno nuevo.
    ///
    /// VALIDACIONES:
    ///   - Si el día está activo (Activo=true), HoraInicio y HoraFin son obligatorias.
    ///   - Las horas deben tener formato HH:mm (ej: "09:00", "17:30").
    ///   - La hora de inicio debe ser estrictamente anterior a la hora de fin.
    /// </summary>
    /// <param name="dto">DTO que contiene el EmpleadoId y la lista de días con sus horarios.</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo.</returns>
    Task<(bool success, string message)> GuardarHorariosAsync(HorarioEmpleadoDto dto);

    // ═══════════════════════════════════════════════════════════
    // DISPONIBILIDAD Y EXCEPCIONES
    // Las excepciones permiten ajustar el horario para fechas específicas
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los empleados activos con su estado de disponibilidad para una fecha específica.
    /// Aplica la lógica de prioridad: excepciones > horario semanal.
    ///
    /// Para cada empleado devuelve:
    ///   - Si trabaja ese día (Trabaja: true/false)
    ///   - Su rango de horas (HoraInicio, HoraFin)
    ///   - Cuántas citas tiene agendadas para ese día (CitasHoy)
    ///
    /// Se usa en el selector de empleados al crear una cita, para que el recepcionista
    /// pueda ver de un vistazo quién está disponible y qué tan cargado está.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <param name="fecha">Fecha específica para evaluar la disponibilidad.</param>
    /// <returns>Lista de empleados con su disponibilidad calculada para esa fecha.</returns>
    Task<object> GetEmpleadosConDisponibilidadAsync(Guid negocioId, DateTime fecha);

    /// <summary>
    /// Obtiene las excepciones de horario de un empleado en un rango de fechas opcional.
    /// Si no se especifican fechas, devuelve todas las excepciones del empleado.
    /// Útil para mostrar en el calendario del administrador los días libres planificados.
    /// </summary>
    /// <param name="empleadoId">ID del empleado.</param>
    /// <param name="negocioId">ID del negocio (seguridad).</param>
    /// <param name="desde">Fecha de inicio del rango (inclusive). Si es null, no hay límite inferior.</param>
    /// <param name="hasta">Fecha de fin del rango (inclusive). Si es null, no hay límite superior.</param>
    /// <returns>Lista de excepciones ordenadas por fecha con sus detalles.</returns>
    Task<object> GetExcepcionesAsync(Guid empleadoId, Guid negocioId, DateTime? desde, DateTime? hasta);

    /// <summary>
    /// Crea una excepción de horario para una fecha puntual, con dos modalidades:
    ///
    ///   Modalidad 1 - Día libre (esDiaLibre=true):
    ///     El empleado no trabaja ese día. HoraInicio y HoraFin se ignoran y se guardan como null.
    ///     Ejemplos: vacaciones, feriado, permiso personal.
    ///
    ///   Modalidad 2 - Horario especial (esDiaLibre=false):
    ///     El empleado trabaja, pero con un horario diferente al semanal normal.
    ///     HoraInicio y HoraFin son obligatorias en este caso.
    ///     Ejemplos: turno reducido, jornada extendida.
    ///
    /// Solo puede existir una excepción por empleado por fecha.
    /// </summary>
    /// <param name="empleadoId">ID del empleado.</param>
    /// <param name="negocioId">ID del negocio.</param>
    /// <param name="fecha">Fecha exacta de la excepción (solo se usa la parte de la fecha, no la hora).</param>
    /// <param name="esDiaLibre">true si no trabaja ese día; false si trabaja con horario diferente.</param>
    /// <param name="horaInicio">Hora de inicio en formato "HH:mm". Requerida si esDiaLibre=false.</param>
    /// <param name="horaFin">Hora de fin en formato "HH:mm". Requerida si esDiaLibre=false.</param>
    /// <param name="motivo">Razón de la excepción (ej: "Vacaciones de verano"). Opcional.</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo.</returns>
    Task<(bool success, string message, Guid? excepcionId)> CrearExcepcionAsync(
        Guid empleadoId, Guid negocioId, DateTime fecha, bool esDiaLibre,
        string horaInicio, string horaFin, string motivo);

    /// <summary>
    /// Elimina una excepción de horario existente.
    /// Al eliminarse, el empleado vuelve a regirse por su horario semanal normal para esa fecha.
    /// </summary>
    /// <param name="excepcionId">ID único de la excepción a eliminar.</param>
    /// <param name="negocioId">ID del negocio (seguridad).</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo.</returns>
    Task<(bool success, string message)> EliminarExcepcionAsync(Guid excepcionId, Guid negocioId);
}
