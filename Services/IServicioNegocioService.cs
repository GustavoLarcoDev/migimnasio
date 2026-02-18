// ═══════════════════════════════════════════════════════════
// IServicioNegocioService.cs — Contrato del servicio de servicios del negocio
// Define las operaciones para gestión de servicios ofrecidos:
// CRUD con validación de precio, duración y eliminación lógica
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

public interface IServicioNegocioService
{
    /// <summary>
    /// Obtiene todos los servicios activos del negocio ordenados por nombre
    /// </summary>
    Task<object> GetServiciosAsync(Guid negocioId);

    /// <summary>
    /// Obtiene un servicio específico por su ID dentro de un negocio
    /// </summary>
    Task<ServicioNegocio> GetServicioAsync(Guid servicioId, Guid negocioId);

    /// <summary>
    /// Crea un nuevo servicio validando nombre, precio y duración
    /// </summary>
    Task<(bool success, string message)> CrearServicioAsync(ServicioCreateDto dto);

    /// <summary>
    /// Edita un servicio existente con validación de campos obligatorios
    /// </summary>
    Task<(bool success, string message)> EditarServicioAsync(ServicioCreateDto dto);

    /// <summary>
    /// Elimina un servicio de forma lógica (IsActive = false)
    /// </summary>
    Task<(bool success, string message)> EliminarServicioAsync(Guid servicioId, Guid negocioId);
}
