// ═══════════════════════════════════════════════════════════
// ICitaService.cs — Contrato del servicio de citas
// Define las operaciones para gestión de citas artesanales:
// calendario, slots disponibles, CRUD, pagos y estadísticas
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

public interface ICitaService
{
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS DE CALENDARIO Y ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las citas de un rango de fechas formateadas para FullCalendar
    /// </summary>
    Task<object> GetCitasCalendarioAsync(Guid negocioId, DateTime start, DateTime end);

    /// <summary>
    /// Obtiene el detalle completo de una cita incluyendo datos de pago
    /// </summary>
    Task<object> GetCitaAsync(Guid citaId, Guid negocioId);

    /// <summary>
    /// Calcula los slots de tiempo disponibles para un empleado en una fecha
    /// </summary>
    Task<object> GetSlotsDisponiblesAsync(Guid negocioId, Guid empleadoId, DateTime fecha, int duracionMinutos);

    /// <summary>
    /// Obtiene estadísticas del día: citas, ingresos, completadas y canceladas
    /// </summary>
    Task<object> GetDashboardStatsAsync(Guid negocioId);

    /// <summary>
    /// Obtiene las últimas 50 citas de un cliente para su historial
    /// </summary>
    Task<object> GetHistorialClienteAsync(Guid clienteId, Guid negocioId);

    // ═══════════════════════════════════════════════════════════
    // CREACIÓN Y GESTIÓN DE CITAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea una cita rápida: permite crear cliente nuevo inline o usar uno existente
    /// </summary>
    Task<(bool success, string message, Guid? citaId)> CrearCitaRapidaAsync(CitaQuickCreateDto dto);

    /// <summary>
    /// Crea una cita estándar con cliente existente, incluye prevención de doble-booking
    /// </summary>
    Task<(bool success, string message)> CrearCitaAsync(CitaCreateDto dto);

    /// <summary>
    /// Mueve una cita a nueva fecha/hora revalidando conflictos de horario
    /// </summary>
    Task<(bool success, string message)> MoverCitaAsync(CitaMoveDto dto);

    /// <summary>
    /// Cambia el estado de una cita validando las transiciones permitidas
    /// </summary>
    Task<(bool success, string message)> CambiarEstadoCitaAsync(Guid citaId, Guid negocioId, string nuevoEstado, string motivoCancelacion = null);

    // ═══════════════════════════════════════════════════════════
    // REGISTRO DE PAGOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Registra el pago de una cita completada (normal o regalo)
    /// </summary>
    Task<(bool success, string message)> RegistrarPagoAsync(PagoCitaDto dto);

    /// <summary>
    /// Obtiene el detalle de pago asociado a una cita
    /// </summary>
    Task<object> GetPagoCitaAsync(Guid citaId, Guid negocioId);
}
