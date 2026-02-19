// ═══════════════════════════════════════════════════════════
// ServiciosController.cs — Controlador de servicios del modelo artesanal
//
// Responsabilidad: exponer los endpoints HTTP para gestionar el
// catálogo de servicios que ofrece el negocio (ej. "Corte de pelo",
// "Manicure", "Masaje relajante", etc.).
//
// Un "servicio" en este contexto define:
//   - Nombre del servicio (ej. "Corte + lavado")
//   - Precio cobrado al cliente
//   - Duración en minutos
//     → La duración es crítica: el motor de slots (GetSlotsDisponibles)
//       la usa para calcular cuánto tiempo bloquear en la agenda del
//       empleado cuando se crea una cita con ese servicio.
//
// Relación con otros controladores:
//   - CitasController.CrearCita / CrearCitaRapida → requieren un ServicioId
//     válido del catálogo de este controlador.
//   - EmpleadosController → los empleados no están vinculados a servicios
//     específicos; cualquier empleado puede realizar cualquier servicio.
//     (Si se necesitara esa restricción, habría que agregar una tabla
//     EmpleadoServicio, pero actualmente no existe.)
//
// Ruta base: /Negocios  (todos los endpoints son AJAX)
// Requiere: cookie de autenticación [Authorize]
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class ServiciosController : Controller
{
    // ── Servicios inyectados por el contenedor de dependencias ──

    private readonly IServicioNegocioService _servicioService; // Lógica de negocio del catálogo de servicios
    private readonly IAuthService _authService;                // Extrae el negocioId del claim del usuario logueado

    public ServiciosController(IServicioNegocioService servicioService, IAuthService authService)
    {
        _servicioService = servicioService;
        _authService = authService;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS DE SERVICIOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Devuelve el catálogo completo de servicios activos del negocio.
    /// Solo incluye servicios con IsActive = true; los eliminados lógicamente
    /// no aparecen para no contaminar los formularios con opciones obsoletas.
    ///
    /// Se usa en dos lugares principales:
    ///   1. El listado de servicios en la sección "Configuración" del dashboard.
    ///   2. El dropdown "Tipo de servicio" en el modal de nueva cita, para que
    ///      el dueño pueda seleccionar qué servicio va a recibir el cliente.
    ///      Cuando el usuario selecciona un servicio, la duración del mismo
    ///      se usa para calcular los slots disponibles del empleado.
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyo catálogo de servicios se lista.</param>
    /// <returns>Array JSON de servicios ordenados por nombre, con precio y duración.</returns>
    [HttpGet("GetServicios")]
    public async Task<IActionResult> GetServicios(Guid negocioId)
    {
        try
        {
            // Seguridad multi-tenant: un negocio solo puede ver su propio catálogo.
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var servicios = await _servicioService.GetServiciosAsync(negocioId);
            return Ok(servicios);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Devuelve el detalle de un servicio específico por su ID.
    /// Se usa para pre-poblar el formulario de edición cuando el usuario
    /// hace clic en "Editar" en la tarjeta de un servicio del catálogo.
    /// </summary>
    /// <param name="servicioId">ID único del servicio a consultar.</param>
    /// <param name="negocioId">ID del negocio (verificación de pertenencia).</param>
    /// <returns>Objeto JSON con nombre, precio y duración del servicio, o 404 si no existe.</returns>
    [HttpGet("GetServicio")]
    public async Task<IActionResult> GetServicio(Guid servicioId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var servicio = await _servicioService.GetServicioAsync(servicioId, negocioId);

            // El servicio devuelve null si el servicioId no existe o no pertenece al negocio.
            if (servicio == null)
                return NotFound(new { success = false, message = "Servicio no encontrado" });

            return Ok(servicio);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE SERVICIOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo servicio en el catálogo del negocio.
    ///
    /// El servicio (IServicioNegocioService) valida:
    ///   - Que el nombre no esté vacío.
    ///   - Que el precio sea mayor a cero.
    ///   - Que la duración en minutos sea mayor a cero.
    ///
    /// Importante: la duración en minutos impacta directamente en la
    /// disponibilidad de agenda. Si un servicio dura 60 minutos, al agendar
    /// ese servicio se bloqueará una hora completa en la agenda del empleado,
    /// por lo que ese slot y los inmediatamente siguientes no estarán disponibles.
    /// </summary>
    /// <param name="dto">
    ///   Datos del nuevo servicio: negocioId, nombre, descripcion (opcional),
    ///   precio (decimal) y duracionMinutos (int).
    /// </param>
    [HttpPost("CrearServicio")]
    public async Task<IActionResult> CrearServicio([FromForm] ServicioCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _servicioService.CrearServicioAsync(dto);
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
    /// Actualiza los datos de un servicio existente en el catálogo.
    ///
    /// Nota importante sobre la duración: si se cambia la duración de un servicio,
    /// las citas FUTURAS de ese tipo de servicio verán afectado el tiempo que
    /// se bloquea en la agenda al recalcular slots. Las citas ya creadas no
    /// se modifican retroactivamente (conservan su FechaHoraFin original).
    ///
    /// Reutiliza el mismo DTO de creación (ServicioCreateDto); el servicio
    /// distingue entre crear y editar por la presencia del campo ServicioId.
    /// </summary>
    /// <param name="dto">Datos actualizados del servicio, incluyendo su ServicioId.</param>
    [HttpPost("EditarServicio")]
    public async Task<IActionResult> EditarServicio([FromForm] ServicioCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var (success, message) = await _servicioService.EditarServicioAsync(dto);
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
    /// Elimina un servicio del catálogo de forma lógica (marca IsActive = false).
    ///
    /// Por qué eliminación lógica y no física:
    ///   - Las citas históricas que usaron ese servicio contienen el ServicioId
    ///     como referencia. Borrarlo físicamente de la BD causaría un error de
    ///     violación de clave foránea (FK) o dejaría citas con un ServicioId
    ///     "huérfano" sin datos de precio ni duración.
    ///   - Al marcarlo como inactivo (IsActive = false), el servicio desaparece
    ///     del catálogo y los dropdowns, pero las citas históricas siguen teniendo
    ///     acceso a su nombre y precio para reportes.
    ///
    /// El servicio (IServicioNegocioService) puede optar por verificar si el
    /// servicio tiene citas futuras pendientes antes de permitir la eliminación.
    /// </summary>
    /// <param name="servicioId">ID del servicio a eliminar lógicamente.</param>
    /// <param name="negocioId">ID del negocio (verificación de pertenencia).</param>
    [HttpPost("EliminarServicio")]
    public async Task<IActionResult> EliminarServicio(Guid servicioId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _servicioService.EliminarServicioAsync(servicioId, negocioId);
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
