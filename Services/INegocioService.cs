// ═══════════════════════════════════════════════════════════
// INegocioService.cs — Contrato del servicio de negocios
//
// Define todas las operaciones disponibles para gestionar
// negocios (tenants) dentro de la plataforma SaaS.
//
// Un "negocio" es la unidad raíz del sistema multi-tenant:
// cada gimnasio, barbería o taller que se registra tiene
// su propio negocio con email/password únicos.
//
// ROLES DE USO:
//   - AdminController: usa este servicio para gestionar todos
//     los negocios desde el panel de superadmin.
//   - VendedorController: usa partes de este servicio para
//     crear negocios y ver estadísticas de sus clientes.
//   - HomeController: usa GetNegocioForImpersonationAsync
//     para la función de impersonación de sesión.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

public interface INegocioService
{
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene la lista completa de negocios con sus estadísticas de suscripción.
    /// Incluye: días restantes, total de clientes, nombre del vendedor asignado
    /// y si la suscripción está "por empezar" (fecha de pago en el futuro).
    /// Solo el superadmin debería llamar a este método.
    /// </summary>
    /// <returns>Lista de negocios con campos calculados, ordenados por fecha de creación descendente.</returns>
    Task<object> GetAllNegociosAsync();

    /// <summary>
    /// Obtiene los datos básicos de un negocio específico por su ID.
    /// Retorna un objeto anónimo sin la contraseña ni datos sensibles.
    /// Útil para prellenar formularios de edición en el panel admin.
    /// </summary>
    /// <param name="id">ID del negocio a buscar.</param>
    /// <returns>Objeto con los datos del negocio, o null si no existe.</returns>
    Task<object> GetNegocioAsync(Guid id);

    /// <summary>
    /// Obtiene el objeto Gym completo (incluyendo la contraseña hasheada)
    /// para usarlo en el flujo de impersonación de sesión.
    ///
    /// ADVERTENCIA: Este método retorna la entidad completa con Password incluido.
    /// Solo debe usarse en el flujo de impersonación, nunca para serializar a JSON.
    /// </summary>
    /// <param name="id">ID del negocio a impersonar.</param>
    /// <returns>Objeto Gym completo, o null si no existe.</returns>
    Task<Gym> GetNegocioForImpersonationAsync(Guid id);

    // ═══════════════════════════════════════════════════════════
    // CRUD
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo negocio (tenant) en la plataforma.
    ///
    /// Reglas de negocio:
    ///   - Un negocio debe ser Activo (de pago) O en Prueba, nunca ambos ni ninguno.
    ///   - El email debe ser único en toda la plataforma (es el username de login).
    ///   - La contraseña se hashea con BCrypt antes de guardar.
    ///   - Si no se dan fechas, se calculan automáticamente:
    ///       Prueba: 7 días desde hoy | Activo: 30 días desde hoy.
    ///
    /// Parámetros opcionales: fechaPago, fechaExpiracion, precioSuscripcion,
    /// diasPagados, vendedorId y tipoNegocio permiten configurar el negocio
    /// desde el momento de la creación sin necesidad de editar después.
    /// </summary>
    Task<(bool success, string message)> CreateNegocioAsync(
        string nombre, string dueno, string telefono, string email,
        string password, bool isActive, bool esPrueba,
        DateTime? fechaPago = null, DateTime? fechaExpiracion = null,
        decimal? precioSuscripcion = null, int? diasPagados = null,
        Guid? vendedorId = null, string tipoNegocio = "membresias");

    /// <summary>
    /// Edita un negocio existente.
    ///
    /// Comportamiento especial con contraseña:
    ///   - Si negocio.Password viene en blanco, NO se modifica la contraseña actual.
    ///   - Si viene con valor, se re-hashea con BCrypt antes de guardar.
    ///   Esto permite editar cualquier dato sin necesidad de cambiar la contraseña.
    ///
    /// Valida que no exista otro negocio con el mismo email (excluyendo el actual).
    /// </summary>
    /// <param name="negocio">DTO con los campos editables del negocio. Si Password está vacío, se conserva el actual.</param>
    /// <returns>Tupla (success, message).</returns>
    Task<(bool success, string message)> EditarNegocioAsync(NegocioEditDto negocio);

    /// <summary>
    /// Elimina un negocio de la plataforma.
    /// La eliminación se bloquea si el negocio tiene clientes registrados,
    /// para evitar dejar datos de clientes sin dueño (huérfanos).
    /// El admin debe eliminar primero todos los clientes antes de poder
    /// eliminar el negocio.
    /// </summary>
    /// <param name="id">ID del negocio a eliminar.</param>
    /// <returns>Tupla (success, message). Falla si tiene clientes.</returns>
    Task<(bool success, string message)> EliminarNegocioAsync(Guid id);

    /// <summary>
    /// Alterna el estado del negocio entre "Pago (Activo)" y "Prueba".
    ///
    /// Lógica de toggle:
    ///   - Si IsActive = true  → pasa a IsActive=false, EsPrueba=true  (modo prueba)
    ///   - Si IsActive = false → pasa a IsActive=true,  EsPrueba=false (modo pago)
    ///
    /// El admin usa esto para degradar un negocio a prueba cuando no paga,
    /// o para activarlo cuando regulariza su suscripción.
    /// </summary>
    /// <param name="id">ID del negocio a cambiar.</param>
    /// <returns>Tupla con (success, message, isActive, esPrueba) para que el frontend actualice la UI.</returns>
    Task<(bool success, string message, bool? isActive, bool? esPrueba)> CambiarEstadoAsync(Guid id);

    // ═══════════════════════════════════════════════════════════
    // EXPORTACIÓN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Exporta todos los negocios a un archivo Excel (.xlsx).
    /// Incluye: nombre, dueño, email, teléfono, estado, si es prueba,
    /// total de clientes y fecha de creación.
    /// El archivo se retorna como byte[] para enviarlo como respuesta HTTP.
    /// </summary>
    /// <returns>Bytes del archivo .xlsx generado con todos los negocios.</returns>
    Task<byte[]> ExportExcelAsync();

    // ═══════════════════════════════════════════════════════════
    // ESTADÍSTICAS Y LOGS DE ADMIN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula las métricas del dashboard del superadmin:
    ///   - totalNegocios: cuántos negocios hay en total.
    ///   - activos: negocios que pagan suscripción.
    ///   - prueba: negocios en modo de prueba gratuita.
    ///   - mrr: Monthly Recurring Revenue (suma de suscripciones activas con precio > 0).
    ///   - totalClientes: total de clientes en toda la plataforma.
    ///   - revenuePorMes: ingresos de los últimos 12 meses (para el gráfico de línea).
    ///     Los meses sin ingresos se incluyen con valor $0 para que el gráfico sea continuo.
    /// </summary>
    /// <returns>Objeto con todas las métricas del panel admin.</returns>
    Task<object> GetAdminDashboardStatsAsync();

    /// <summary>
    /// Registra una acción del superadmin en la tabla AdminLogs.
    /// Cada vez que el admin crea, edita, elimina o impersona un negocio,
    /// se llama a este método para dejar un rastro auditable.
    /// </summary>
    /// <param name="accion">Tipo de acción realizada (ej: "negocio_creado", "impersonacion").</param>
    /// <param name="detalle">Descripción detallada de lo que se hizo.</param>
    /// <param name="negocioAfectado">Nombre del negocio afectado (opcional, para facilitar búsquedas).</param>
    Task RegistrarAdminLogAsync(string accion, string detalle, string negocioAfectado = null);

    /// <summary>
    /// Obtiene las últimas 200 acciones del superadmin ordenadas por fecha descendente.
    /// Limitamos a 200 para no sobrecargar la UI; si se necesita más historial
    /// habría que agregar paginación.
    /// </summary>
    /// <returns>Lista de hasta 200 entradas de AdminLog.</returns>
    Task<object> GetAdminLogsAsync();

    /// <summary>
    /// Obtiene los datos financieros de la pestaña "Ventas" del panel admin:
    ///   - totalRevenue: suma de PrecioSuscripcion de todos los negocios que pagan.
    ///   - averageRevenuePerBusiness: ingreso promedio por negocio pagante.
    ///   - businessesPaying: cantidad de negocios activos con precio > 0.
    ///   - payingBusinessesList: detalle de cada negocio pagante con días restantes.
    ///
    /// Solo incluye negocios con IsActive=true, EsPrueba=false y PrecioSuscripcion > 0.
    /// </summary>
    /// <returns>Objeto con métricas financieras y lista detallada de negocios que pagan.</returns>
    Task<object> GetVentasAdminAsync();
}
