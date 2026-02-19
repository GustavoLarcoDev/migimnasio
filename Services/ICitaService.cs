// ═══════════════════════════════════════════════════════════
// ICitaService.cs — Contrato (interfaz) del servicio de citas
//
// ¿Qué es una interfaz? Es un "contrato" que obliga a la clase
// que lo implemente (CitaService) a tener exactamente estos métodos.
// Esto nos permite cambiar la implementación sin tocar el resto del
// sistema, y facilita las pruebas unitarias.
//
// Este servicio gestiona todo el ciclo de vida de una cita:
//   1. Consulta del calendario y disponibilidad de slots
//   2. Creación de citas (rápida o estándar)
//   3. Cambios de estado (máquina de estados)
//   4. Registro de pagos al finalizar
//   5. Estadísticas del dashboard
//
// CICLO DE VIDA DE UNA CITA (máquina de estados):
//
//   pendiente ──► confirmada ──► en_progreso ──► completada
//       │              │              │
//       └──────────────┴──────────────┴──────────► cancelada
//
//   - pendiente:   La cita fue creada pero el cliente aún no confirmó
//   - confirmada:  El cliente confirmó su asistencia
//   - en_progreso: El servicio está siendo realizado en este momento
//   - completada:  El servicio terminó; se puede registrar el pago
//   - cancelada:   La cita fue cancelada (requiere motivo obligatorio)
//
// IMPORTANTE: Las transiciones son de una sola dirección.
// Una cita completada o cancelada NO puede cambiar de estado.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

/// <summary>
/// Define el contrato para la gestión completa de citas en el modelo artesanal.
/// Incluye calendario, disponibilidad de slots, CRUD de citas, pagos y estadísticas.
/// La implementación concreta está en <see cref="CitaService"/>.
/// </summary>
public interface ICitaService
{
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS DE CALENDARIO Y ESTADÍSTICAS
    // Métodos de solo lectura para visualización y reportes
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las citas de un rango de fechas formateadas para FullCalendar.
    /// FullCalendar es la librería de calendario del frontend; espera un formato
    /// específico de JSON con campos como <c>id</c>, <c>title</c>, <c>start</c>, <c>end</c>
    /// y <c>className</c> (usado para colorear el evento según el estado).
    /// </summary>
    /// <param name="negocioId">ID del negocio para filtrar solo sus citas.</param>
    /// <param name="start">Fecha y hora de inicio del rango (inclusive).</param>
    /// <param name="end">Fecha y hora de fin del rango (inclusive).</param>
    /// <returns>Lista de objetos JSON listos para consumir por FullCalendar.</returns>
    Task<object> GetCitasCalendarioAsync(Guid negocioId, DateTime start, DateTime end);

    /// <summary>
    /// Obtiene el detalle completo de una cita, incluyendo datos del pago si ya fue registrado.
    /// Se usa al hacer clic en un evento del calendario para mostrar el panel lateral de detalles.
    /// </summary>
    /// <param name="citaId">ID único de la cita.</param>
    /// <param name="negocioId">ID del negocio (seguridad: evita que un negocio vea citas de otro).</param>
    /// <returns>Objeto con todos los campos de la cita más los datos del pago (si existe), o <c>null</c>.</returns>
    Task<object> GetCitaAsync(Guid citaId, Guid negocioId);

    /// <summary>
    /// Calcula los slots de tiempo disponibles para reservar una cita con un empleado en una fecha.
    ///
    /// El algoritmo funciona así:
    ///   1. Verifica si hay una excepción de horario para ese día (día libre o cambio de horario)
    ///   2. Si no hay excepción, usa el horario semanal normal del empleado
    ///   3. Divide el horario en bloques de 15 minutos
    ///   4. Para cada bloque, verifica si el empleado ya tiene una cita que se solaparía
    ///   5. Devuelve solo los bloques libres donde cabe la duración del servicio solicitado
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <param name="empleadoId">ID del empleado para quien se buscan slots.</param>
    /// <param name="fecha">Fecha en la que se quiere hacer la cita.</param>
    /// <param name="duracionMinutos">Duración del servicio en minutos (ej: 30, 60, 90).</param>
    /// <returns>Objeto con lista de slots disponibles y mensaje de error si no hay ninguno.</returns>
    Task<object> GetSlotsDisponiblesAsync(Guid negocioId, Guid empleadoId, DateTime fecha, int duracionMinutos);

    /// <summary>
    /// Obtiene estadísticas del día actual para mostrar en el dashboard:
    /// total de citas, ingresos, citas completadas y canceladas.
    /// Se calcula dinámicamente cada vez que se carga el dashboard.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <returns>Objeto con <c>citasHoy</c>, <c>ingresosHoy</c>, <c>completadas</c> y <c>canceladas</c>.</returns>
    Task<object> GetDashboardStatsAsync(Guid negocioId);

    /// <summary>
    /// Obtiene las últimas 50 citas de un cliente para mostrar su historial.
    /// Se limita a 50 para evitar cargar demasiados datos de una sola vez.
    /// </summary>
    /// <param name="clienteId">ID del cliente.</param>
    /// <param name="negocioId">ID del negocio.</param>
    /// <returns>Lista de hasta 50 citas ordenadas de más reciente a más antigua.</returns>
    Task<object> GetHistorialClienteAsync(Guid clienteId, Guid negocioId);

    // ═══════════════════════════════════════════════════════════
    // CREACIÓN Y GESTIÓN DE CITAS
    // Métodos que modifican el estado de la base de datos
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea una cita "rápida" desde el calendario. Es el flujo más común:
    /// el recepcionista hace clic en un slot y llena el formulario modal.
    ///
    /// Diferencia con <see cref="CrearCitaAsync"/>: este método también puede
    /// crear un cliente nuevo "al vuelo" si no existe en el sistema todavía,
    /// sin necesidad de registrarlo previamente. Si se pasa un <c>ClienteId</c>,
    /// usa ese cliente existente; si no, crea uno nuevo con los datos del formulario.
    /// </summary>
    /// <param name="dto">DTO con los datos del formulario de creación rápida.</param>
    /// <returns>Tupla con éxito/fallo, mensaje descriptivo y el ID de la cita creada (si tuvo éxito).</returns>
    Task<(bool success, string message, Guid? citaId)> CrearCitaRapidaAsync(CitaQuickCreateDto dto);

    /// <summary>
    /// Crea una cita estándar. Requiere que el cliente ya exista en el sistema.
    /// Incluye prevención de doble-booking: si el empleado ya tiene una cita
    /// en ese horario, la operación falla con un mensaje de error claro.
    /// </summary>
    /// <param name="dto">DTO con los datos de la cita (cliente, empleado, servicio, fecha).</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo.</returns>
    Task<(bool success, string message)> CrearCitaAsync(CitaCreateDto dto);

    /// <summary>
    /// Mueve una cita a una nueva fecha y hora (drag-and-drop en el calendario).
    /// Revalida que no haya conflictos en la nueva posición antes de confirmar el movimiento.
    /// Solo se pueden mover citas que estén en estado <c>pendiente</c> o <c>confirmada</c>.
    /// </summary>
    /// <param name="dto">DTO con el ID de la cita y la nueva fecha/hora de inicio.</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo.</returns>
    Task<(bool success, string message)> MoverCitaAsync(CitaMoveDto dto);

    /// <summary>
    /// Cambia el estado de una cita siguiendo la máquina de estados definida.
    /// Rechaza transiciones inválidas (ej: no se puede ir de "completada" a "pendiente").
    ///
    /// Transiciones válidas:
    ///   pendiente    → confirmada, cancelada
    ///   confirmada   → en_progreso, cancelada
    ///   en_progreso  → completada, cancelada
    ///   completada   → (ninguna)
    ///   cancelada    → (ninguna)
    ///
    /// Si el nuevo estado es "cancelada", el <paramref name="motivoCancelacion"/> es obligatorio.
    /// </summary>
    /// <param name="citaId">ID de la cita a actualizar.</param>
    /// <param name="negocioId">ID del negocio (seguridad).</param>
    /// <param name="nuevoEstado">Estado destino: "pendiente", "confirmada", "en_progreso", "completada" o "cancelada".</param>
    /// <param name="motivoCancelacion">Razón de la cancelación (obligatorio si nuevoEstado es "cancelada").</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo.</returns>
    Task<(bool success, string message)> CambiarEstadoCitaAsync(Guid citaId, Guid negocioId, string nuevoEstado, string motivoCancelacion = null);

    // ═══════════════════════════════════════════════════════════
    // REGISTRO DE PAGOS
    // Los pagos solo se registran una vez que la cita está completada
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Registra el pago de una cita que ya fue completada.
    /// Solo se puede llamar cuando el estado de la cita es "completada".
    /// Cada cita solo puede tener un pago; si ya existe uno, la operación falla.
    ///
    /// Soporta dos modalidades:
    ///   - Pago normal: se cobra precio del servicio + monto extra (opcional) + propina (opcional).
    ///     Requiere método de pago (efectivo, tarjeta, etc.).
    ///   - Regalo: el total es $0. Requiere un motivo obligatorio.
    ///     Esto es útil para cortesías, pruebas de servicio, etc.
    ///
    /// Además, crea un log inmutable en el sistema de auditoría para tener
    /// un registro histórico que no se puede borrar ni modificar.
    /// </summary>
    /// <param name="dto">DTO con los datos del pago (montos, método, propina, etc.).</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo.</returns>
    Task<(bool success, string message)> RegistrarPagoAsync(PagoCitaDto dto);

    /// <summary>
    /// Obtiene el detalle del pago asociado a una cita específica.
    /// Devuelve <c>null</c> si la cita aún no tiene pago registrado.
    /// </summary>
    /// <param name="citaId">ID de la cita.</param>
    /// <param name="negocioId">ID del negocio (seguridad).</param>
    /// <returns>Objeto con todos los campos del pago, o <c>null</c>.</returns>
    Task<object> GetPagoCitaAsync(Guid citaId, Guid negocioId);
}
