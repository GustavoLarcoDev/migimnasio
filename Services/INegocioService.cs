// ═══════════════════════════════════════════════════════════
// INegocioService.cs — Contrato del servicio de negocios
// Define las operaciones disponibles para gestión de negocios:
// CRUD, exportación Excel, estadísticas admin y logs de admin
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;

namespace Gimnasio.Services;

public interface INegocioService
{
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los negocios con estadísticas de suscripción y total de clientes
    /// </summary>
    Task<object> GetAllNegociosAsync();

    /// <summary>
    /// Obtiene los datos de un negocio específico por su ID
    /// </summary>
    Task<object> GetNegocioAsync(Guid id);

    /// <summary>
    /// Obtiene el objeto Gym completo para impersonación (incluye Password)
    /// </summary>
    Task<Gym> GetNegocioForImpersonationAsync(Guid id);

    // ═══════════════════════════════════════════════════════════
    // CRUD
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo negocio con datos de suscripción opcionales.
    /// Hashea la contraseña con BCrypt antes de guardar.
    /// </summary>
    Task<(bool success, string message)> CreateNegocioAsync(string nombre, string dueno, string telefono, string email, string password, bool isActive, bool esPrueba,
        DateTime? fechaPago = null, DateTime? fechaExpiracion = null, decimal? precioSuscripcion = null, int? diasPagados = null);

    /// <summary>
    /// Edita un negocio existente. Si se envía contraseña nueva, se re-hashea.
    /// </summary>
    Task<(bool success, string message)> EditarNegocioAsync(Gym negocio);

    /// <summary>
    /// Elimina un negocio si no tiene clientes asociados
    /// </summary>
    Task<(bool success, string message)> EliminarNegocioAsync(Guid id);

    /// <summary>
    /// Alterna el estado del negocio entre Pago (Activo) y Prueba
    /// </summary>
    Task<(bool success, string message, bool? isActive, bool? esPrueba)> CambiarEstadoAsync(Guid id);

    // ═══════════════════════════════════════════════════════════
    // EXPORTACIÓN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Exporta todos los negocios a un archivo Excel en formato byte[]
    /// </summary>
    Task<byte[]> ExportExcelAsync();

    // ═══════════════════════════════════════════════════════════
    // ESTADÍSTICAS Y LOGS DE ADMIN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene estadísticas del dashboard admin: totales, MRR e ingresos por mes
    /// </summary>
    Task<object> GetAdminDashboardStatsAsync();

    /// <summary>
    /// Registra una acción administrativa en la tabla AdminLogs
    /// </summary>
    Task RegistrarAdminLogAsync(string accion, string detalle, string negocioAfectado = null);

    /// <summary>
    /// Obtiene las últimas 200 acciones del admin ordenadas por fecha
    /// </summary>
    Task<object> GetAdminLogsAsync();

    /// <summary>
    /// Obtiene datos financieros para la pestaña de Ventas del panel admin:
    /// ingresos totales, promedio por negocio, negocios pagando y lista detallada
    /// </summary>
    Task<object> GetVentasAdminAsync();
}
