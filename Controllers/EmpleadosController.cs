// ═══════════════════════════════════════════════════════════
// EmpleadosController.cs — Controlador de empleados del modelo artesanal
//
// Responsabilidad: exponer los endpoints HTTP para gestionar los
// empleados que prestan servicios en el negocio (ej. peluqueros,
// esteticistas, masajistas, etc.).
//
// Este controlador maneja tres conceptos estrechamente relacionados:
//
//   1. EMPLEADOS (datos básicos: nombre, especialidad, foto)
//      Un empleado es quien realiza los servicios y tiene una agenda propia.
//
//   2. HORARIOS SEMANALES (HorarioEmpleado)
//      Define en qué días y en qué franja horaria trabaja el empleado
//      de forma recurrente (ej. Lunes-Viernes 9:00-18:00, Sábado 9:00-14:00).
//      Sin un horario configurado el empleado no aparecerá con slots disponibles.
//
//   3. EXCEPCIONES DE HORARIO (HorarioExcepcion)
//      Modificaciones puntuales al horario semanal para fechas específicas.
//      Ejemplos de uso:
//        - Un empleado toma vacaciones del 15 al 20 de enero → esDiaLibre = true
//        - Un empleado trabaja sábado 10:00-12:00 solo ese fin de semana
//      Las excepciones SIEMPRE tienen prioridad sobre el horario semanal.
//      El cálculo de slots disponibles (GetSlotsDisponibles) las consulta primero.
//
// Ruta base: /Negocios  (todos los endpoints son AJAX)
// Requiere: cookie de autenticación [Authorize]
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class EmpleadosController : Controller
{
    // ── Servicios inyectados por el contenedor de dependencias ──

    private readonly IEmpleadoService _empleadoService;
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;

    public EmpleadosController(
        IEmpleadoService empleadoService,
        IAuthService authService,
        ApplicationDbContext context)
    {
        _empleadoService = empleadoService;
        _authService = authService;
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE EMPLEADOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Devuelve la lista de todos los empleados activos del negocio.
    /// Se usa para poblar la lista de empleados en el sidebar del dashboard
    /// y en los dropdowns de "seleccionar empleado" al crear una cita.
    /// Solo devuelve empleados activos (IsActive = true); los eliminados
    /// lógicamente no aparecen en esta lista.
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyos empleados se listan.</param>
    /// <returns>Array JSON con los empleados y sus datos básicos (nombre, especialidad, foto).</returns>
    [HttpGet("GetEmpleados")]
    public async Task<IActionResult> GetEmpleados(Guid negocioId)
    {
        try
        {
            // Verificación multi-tenant: el negocio logueado solo puede ver sus propios empleados.
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var empleados = await _empleadoService.GetEmpleadosAsync(negocioId);
            return Ok(empleados);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Devuelve el detalle completo de un empleado específico.
    /// Se usa para pre-poblar el formulario de edición cuando el usuario
    /// hace clic en "Editar empleado" desde la lista o el panel de empleados.
    /// </summary>
    /// <param name="empleadoId">ID del empleado a consultar.</param>
    /// <param name="negocioId">ID del negocio (verificación de pertenencia).</param>
    /// <returns>Objeto JSON con todos los campos del empleado, o 404 si no existe.</returns>
    [HttpGet("GetEmpleado")]
    public async Task<IActionResult> GetEmpleado(Guid empleadoId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var empleado = await _empleadoService.GetEmpleadoAsync(empleadoId, negocioId);

            // El servicio devuelve null si el empleado no existe o no pertenece al negocio.
            if (empleado == null)
                return NotFound(new { success = false, message = "Empleado no encontrado" });

            return Ok(empleado);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crea un nuevo empleado en el negocio.
    /// El servicio asigna automáticamente un horario por defecto al empleado
    /// recién creado (Lunes a Sábado, 9:00-17:00) para que pueda recibir citas
    /// de inmediato sin que el dueño tenga que configurar el horario primero.
    /// El horario siempre se puede modificar después con GuardarHorarios.
    /// </summary>
    /// <param name="dto">
    ///   Datos del nuevo empleado: negocioId, nombre, apellido,
    ///   especialidad (ej. "Colorista"), teléfono y email opcionales.
    /// </param>
    [HttpPost("CrearEmpleado")]
    public async Task<IActionResult> CrearEmpleado([FromForm] EmpleadoCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _empleadoService.CrearEmpleadoAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualiza los datos básicos de un empleado existente (nombre, especialidad, etc.).
    /// Nota: este endpoint solo modifica los datos personales del empleado.
    /// Para cambiar su horario semanal, usar GuardarHorarios.
    /// Para agregar días libres o excepciones, usar CrearExcepcionHorario.
    ///
    /// Reutiliza el mismo DTO de creación (EmpleadoCreateDto); el servicio
    /// distingue entre crear y editar por la presencia del campo EmpleadoId.
    /// </summary>
    /// <param name="dto">Datos actualizados del empleado, incluyendo su EmpleadoId.</param>
    [HttpPost("EditarEmpleado")]
    public async Task<IActionResult> EditarEmpleado([FromForm] EmpleadoCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _empleadoService.EditarEmpleadoAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Elimina un empleado de forma lógica (marca IsActive = false, no borra de la BD).
    /// Esto preserva la integridad del historial: las citas pasadas del empleado
    /// siguen existiendo en la base de datos para reportes e historial de clientes.
    ///
    /// El servicio primero verifica que el empleado no tenga citas futuras pendientes
    /// o confirmadas. Si las tiene, la eliminación falla con un mensaje descriptivo
    /// para que el dueño pueda reasignar o cancelar esas citas manualmente antes de proceder.
    /// </summary>
    /// <param name="empleadoId">ID del empleado a eliminar.</param>
    /// <param name="negocioId">ID del negocio (verificación de pertenencia).</param>
    [HttpPost("EliminarEmpleado")]
    public async Task<IActionResult> EliminarEmpleado(Guid empleadoId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _empleadoService.EliminarEmpleadoAsync(empleadoId, negocioId);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Devuelve la lista de empleados indicando si cada uno tiene disponibilidad
    /// en una fecha concreta. Se usa al seleccionar una fecha en el formulario
    /// de nueva cita: el frontend deshabilita visualmente a los empleados sin
    /// horario en ese día (ej. un empleado que solo trabaja Lunes-Miércoles no
    /// aparecerá disponible un Jueves).
    ///
    /// La disponibilidad se calcula así:
    ///   - Si hay una excepción para esa fecha con esDiaLibre = true → no disponible.
    ///   - Si hay una excepción con horario especial → disponible en esa franja.
    ///   - Si no hay excepción, se consulta el horario semanal para ese día de la semana.
    ///   - Si no tiene horario configurado para ese día → no disponible.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <param name="fecha">Fecha específica para la que se evalúa la disponibilidad.</param>
    /// <returns>Array JSON de empleados con campo "disponible" (true/false) y franja horaria.</returns>
    [HttpGet("GetEmpleadosConDisponibilidad")]
    public async Task<IActionResult> GetEmpleadosConDisponibilidad(Guid negocioId, DateTime fecha)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var empleados = await _empleadoService.GetEmpleadosConDisponibilidadAsync(negocioId, fecha);
            return Ok(empleados);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // HORARIOS SEMANALES
    //
    // El horario semanal es la plantilla recurrente de trabajo de
    // cada empleado. Se define una vez y se aplica todas las semanas.
    // Ejemplo: María trabaja Lunes 9:00-18:00, Miércoles 10:00-17:00.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Devuelve el horario semanal configurado para un empleado.
    /// Se usa para pre-poblar el formulario de horarios en la pantalla
    /// de configuración del empleado, mostrando los 7 días con sus
    /// franjas horarias actuales o vacío si aún no se ha configurado.
    /// </summary>
    /// <param name="empleadoId">ID del empleado cuyo horario se consulta.</param>
    /// <param name="negocioId">ID del negocio (verificación de pertenencia).</param>
    /// <returns>Array JSON con los 7 días de la semana y sus horarios (o null si día libre).</returns>
    [HttpGet("GetHorarios")]
    public async Task<IActionResult> GetHorarios(Guid empleadoId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var horarios = await _empleadoService.GetHorariosAsync(empleadoId, negocioId);
            return Ok(horarios);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Guarda o actualiza el horario semanal completo de un empleado.
    /// Este endpoint recibe [FromBody] (JSON) en lugar de [FromForm] porque
    /// el DTO contiene un array de días de la semana, lo cual es más natural
    /// de enviar como JSON desde el frontend que como form-data plano.
    ///
    /// El servicio usa una estrategia de "borrar y reemplazar": elimina todos
    /// los registros de horario existentes del empleado y los vuelve a insertar.
    /// Esto simplifica enormemente el código evitando comparar qué días cambiaron.
    /// </summary>
    /// <param name="dto">
    ///   negocioId, empleadoId y array de franjas horarias por día de la semana.
    ///   Cada día puede marcarse como "no trabaja" (horaInicio y horaFin en null).
    /// </param>
    [HttpPost("GuardarHorarios")]
    public async Task<IActionResult> GuardarHorarios([FromBody] HorarioEmpleadoDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _empleadoService.GuardarHorariosAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // EXCEPCIONES DE HORARIO
    //
    // Las excepciones permiten sobrescribir el horario semanal para
    // fechas puntuales sin modificar la plantilla recurrente.
    //
    // Por qué son necesarias las excepciones:
    //   Un empleado que normalmente trabaja todos los lunes puede
    //   tomar un lunes libre por un feriado, vacaciones o una cita
    //   médica. Sin excepciones, habría que modificar el horario
    //   semanal permanente y volver a cambiarlo después — propenso
    //   a errores. Con excepciones, se agrega un registro puntual
    //   y el horario semanal permanece intacto.
    //
    // Tipos de excepción:
    //   - esDiaLibre = true  → el empleado NO trabaja ese día
    //   - esDiaLibre = false → el empleado trabaja pero en un horario
    //     diferente al semanal (horaInicio y horaFin son obligatorios)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las excepciones de horario de un empleado, opcionalmente filtradas por rango de fechas.
    /// Se usa para mostrar en el calendario del administrador los días libres o con horario especial.
    /// </summary>
    /// <param name="empleadoId">ID del empleado cuyas excepciones se consultan.</param>
    /// <param name="negocioId">ID del negocio (verificación de pertenencia).</param>
    /// <param name="desde">Fecha de inicio del rango (opcional).</param>
    /// <param name="hasta">Fecha de fin del rango (opcional).</param>
    [HttpGet("GetExcepciones")]
    public async Task<IActionResult> GetExcepciones(Guid empleadoId, Guid negocioId, DateTime? desde, DateTime? hasta)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var excepciones = await _empleadoService.GetExcepcionesAsync(empleadoId, negocioId, desde, hasta);
            return Ok(excepciones);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crea una excepción de horario para un día específico.
    ///
    /// Casos de uso típicos:
    ///   1. Día libre: empleadoId, fecha=2025-01-01, esDiaLibre=true, motivo="Año Nuevo"
    ///      → El empleado no aparecerá disponible ese día.
    ///
    ///   2. Horario especial: empleadoId, fecha=2025-01-15, esDiaLibre=false,
    ///      horaInicio="10:00", horaFin="14:00", motivo="Evento de medio día"
    ///      → El empleado solo tiene slots entre 10:00 y 14:00 ese día.
    ///
    /// El cálculo de slots (GetSlotsDisponibles en CitasController) consulta
    /// las excepciones ANTES de consultar el horario semanal, dando prioridad
    /// a la excepción si existe para esa fecha.
    /// </summary>
    /// <param name="empleadoId">ID del empleado al que aplica la excepción.</param>
    /// <param name="negocioId">ID del negocio (verificación de pertenencia).</param>
    /// <param name="fecha">Fecha exacta del día excepcional.</param>
    /// <param name="esDiaLibre">
    ///   true = el empleado no trabaja ese día.
    ///   false = trabaja con horario especial (ver horaInicio y horaFin).
    /// </param>
    /// <param name="horaInicio">Hora de inicio en formato "HH:mm" (requerido si esDiaLibre = false).</param>
    /// <param name="horaFin">Hora de fin en formato "HH:mm" (requerido si esDiaLibre = false).</param>
    /// <param name="motivo">Descripción opcional del motivo (ej. "Vacaciones", "Feriado").</param>
    [HttpPost("CrearExcepcionHorario")]
    public async Task<IActionResult> CrearExcepcionHorario(
        Guid empleadoId, Guid negocioId, DateTime fecha, bool esDiaLibre,
        string horaInicio, string horaFin, string motivo)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            // Los parámetros se pasan directamente al servicio porque son tipos primitivos.
            // Si fueran un objeto complejo, usaríamos un DTO con [FromBody] o [FromForm].
            var (success, message, excepcionId) = await _empleadoService.CrearExcepcionAsync(
                empleadoId, negocioId, fecha, esDiaLibre, horaInicio, horaFin, motivo);

            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message, excepcionId });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Elimina una excepción de horario existente.
    /// Al eliminarla, el día vuelve a regirse por el horario semanal normal.
    /// Se usa cuando el dueño quiere revertir un día libre o un horario
    /// especial que ya no aplica (ej. el empleado canceló sus vacaciones).
    /// </summary>
    /// <param name="excepcionId">ID del registro de excepción a eliminar.</param>
    /// <param name="negocioId">
    ///   ID del negocio. Se pasa para que el servicio verifique que la excepción
    ///   pertenece a un empleado de este negocio y no de otro.
    /// </param>
    [HttpPost("EliminarExcepcionHorario")]
    public async Task<IActionResult> EliminarExcepcionHorario(Guid excepcionId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _empleadoService.EliminarExcepcionAsync(excepcionId, negocioId);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }
}
