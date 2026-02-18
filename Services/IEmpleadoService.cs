// ═══════════════════════════════════════════════════════════
// IEmpleadoService.cs — Contrato del servicio de empleados
// Define las operaciones para gestión de empleados:
// CRUD, horarios semanales, excepciones y disponibilidad
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

public interface IEmpleadoService
{
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los empleados activos del negocio
    /// </summary>
    Task<object> GetEmpleadosAsync(Guid negocioId);

    /// <summary>
    /// Obtiene un empleado específico por su ID dentro de un negocio
    /// </summary>
    Task<Empleado> GetEmpleadoAsync(Guid empleadoId, Guid negocioId);

    // ═══════════════════════════════════════════════════════════
    // CRUD DE EMPLEADOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un empleado y le asigna horario por defecto (Lun-Sáb 9-17)
    /// </summary>
    Task<(bool success, string message)> CrearEmpleadoAsync(EmpleadoCreateDto dto);

    /// <summary>
    /// Edita los datos básicos de un empleado existente
    /// </summary>
    Task<(bool success, string message)> EditarEmpleadoAsync(EmpleadoCreateDto dto);

    /// <summary>
    /// Elimina un empleado lógicamente. Falla si tiene citas pendientes.
    /// </summary>
    Task<(bool success, string message)> EliminarEmpleadoAsync(Guid empleadoId, Guid negocioId);

    // ═══════════════════════════════════════════════════════════
    // HORARIOS SEMANALES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene el horario semanal de un empleado (7 días)
    /// </summary>
    Task<object> GetHorariosAsync(Guid empleadoId, Guid negocioId);

    /// <summary>
    /// Guarda o actualiza el horario semanal completo de un empleado
    /// </summary>
    Task<(bool success, string message)> GuardarHorariosAsync(HorarioEmpleadoDto dto);

    // ═══════════════════════════════════════════════════════════
    // DISPONIBILIDAD Y EXCEPCIONES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene empleados con estado de disponibilidad para una fecha específica
    /// </summary>
    Task<object> GetEmpleadosConDisponibilidadAsync(Guid negocioId, DateTime fecha);

    /// <summary>
    /// Obtiene las excepciones de horario de un empleado en un rango de fechas
    /// </summary>
    Task<object> GetExcepcionesAsync(Guid empleadoId, Guid negocioId, DateTime? desde, DateTime? hasta);

    /// <summary>
    /// Crea una excepción de horario (día libre o horario especial)
    /// </summary>
    Task<(bool success, string message)> CrearExcepcionAsync(Guid empleadoId, Guid negocioId, DateTime fecha, bool esDiaLibre, string horaInicio, string horaFin, string motivo);

    /// <summary>
    /// Elimina una excepción de horario existente
    /// </summary>
    Task<(bool success, string message)> EliminarExcepcionAsync(Guid excepcionId, Guid negocioId);
}
