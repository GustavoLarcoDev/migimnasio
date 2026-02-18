// ═══════════════════════════════════════════════════════════
// IClienteService.cs — Contrato del servicio de clientes
// Define las operaciones para gestión de clientes:
// CRUD, renovación, importación/exportación Excel y estadísticas
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

public interface IClienteService
{
    /// <summary>
    /// Obtiene estadísticas del dashboard: clientes activos/vencidos,
    /// ingresos del mes/hoy y próximos a vencer
    /// </summary>
    Task<object> GetDashboardStatsAsync(Guid negocioId);

    /// <summary>
    /// Obtiene la lista completa de clientes con días restantes y estado
    /// </summary>
    Task<object> GetClientesAsync(Guid negocioId);

    /// <summary>
    /// Obtiene un cliente específico por su ID dentro de un negocio
    /// </summary>
    Task<Cliente> GetClienteAsync(Guid id, Guid negocioId);

    /// <summary>
    /// Crea un nuevo cliente y registra un log con el pago
    /// </summary>
    Task<(bool success, string message)> CrearClienteAsync(ClienteCreateDto model);

    /// <summary>
    /// Edita un cliente existente y registra un log detallado con los cambios
    /// </summary>
    Task<(bool success, string message)> EditarClienteAsync(ClienteCreateDto model);

    /// <summary>
    /// Elimina un cliente y registra la acción en los logs
    /// </summary>
    Task<(bool success, string message)> EliminarClienteAsync(Guid id, Guid negocioId);

    /// <summary>
    /// Renueva la membresía extendiendo la fecha de fin y registra el pago en logs
    /// </summary>
    Task<(bool success, string message)> RenovarClienteAsync(Guid id, Guid negocioId, DateTime nuevaFechaFin, decimal precio);

    /// <summary>
    /// Exporta todos los clientes del negocio a Excel
    /// </summary>
    Task<byte[]> ExportClientesExcelAsync(Guid negocioId);

    /// <summary>
    /// Importa clientes desde un archivo Excel, detectando columnas automáticamente
    /// </summary>
    Task<object> ImportarClientesExcelAsync(Guid negocioId, Stream fileStream);

    /// <summary>
    /// Obtiene solo los clientes marcados como "diario" (pago por día)
    /// </summary>
    Task<object> GetClientesDiariosAsync(Guid negocioId);

    /// <summary>
    /// Marca todos los clientes diarios como EsDiario = false.
    /// Se llama después de que el dueño envió los mensajes via wa.me.
    /// </summary>
    Task<int> LimpiarClientesDiariosAsync(Guid negocioId);

    /// <summary>
    /// Busca clientes por nombre o telefono (autocomplete para booking rapido)
    /// </summary>
    Task<object> BuscarClientesAsync(Guid negocioId, string query);

    /// <summary>
    /// Crea un cliente para negocio artesanal (sin campos de membresía)
    /// </summary>
    Task<(bool success, string message)> CrearClienteArtesanalAsync(ClienteArtesanalCreateDto model);

    /// <summary>
    /// Edita un cliente artesanal (sin campos de membresía)
    /// </summary>
    Task<(bool success, string message)> EditarClienteArtesanalAsync(ClienteArtesanalCreateDto model);

    /// <summary>
    /// Obtiene clientes de un negocio artesanal (sin campos de membresía)
    /// </summary>
    Task<object> GetClientesArtesanalAsync(Guid negocioId);
}
