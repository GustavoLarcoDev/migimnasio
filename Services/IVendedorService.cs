// ═══════════════════════════════════════════════════════════
// IVendedorService.cs — Contrato del servicio de vendedores
//
// Define todas las operaciones para gestionar vendedores,
// que son los agentes de ventas de la plataforma SaaS.
//
// Los vendedores tienen un rol especial:
//   - Son usuarios intermedios entre el superadmin y los negocios.
//   - Cada vendedor tiene asignados los negocios que creó.
//   - Tienen su propio portal de login y dashboard con estadísticas
//     de sus negocios y una cola de leads comerciales.
//   - NO tienen acceso a datos de negocios de otros vendedores.
//
// DIFERENCIAS CON ADMIN Y NEGOCIO:
//   - Admin: ve y gestiona TODOS los negocios y vendedores.
//   - Vendedor: ve solo sus negocios asignados (VendedorId = suyo).
//   - Negocio: ve solo sus propios clientes (NegocioId = suyo).
// ═══════════════════════════════════════════════════════════

#nullable enable
using Gimnasio.Models;

namespace Gimnasio.Services;

public interface IVendedorService
{
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los vendedores activos (IsActive = true) con
    /// el conteo de negocios que tienen asignados y que están pagando.
    ///
    /// Solo retorna vendedores activos: los eliminados (soft delete)
    /// no aparecen aquí para no confundir al admin.
    /// </summary>
    /// <returns>Lista de vendedores activos con negociosCreados calculado.</returns>
    Task<List<object>> GetAllVendedoresAsync();

    /// <summary>
    /// Obtiene un vendedor específico por su ID.
    /// Solo retorna el vendedor si está activo (IsActive = true),
    /// ya que los vendedores eliminados no deben ser accesibles.
    /// </summary>
    /// <param name="id">ID único del vendedor.</param>
    /// <returns>El objeto Vendedor, o null si no existe o está inactivo.</returns>
    Task<Vendedor?> GetVendedorAsync(Guid id);

    // ═══════════════════════════════════════════════════════════
    // CRUD DE VENDEDORES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo vendedor en la plataforma.
    ///
    /// Validaciones:
    ///   - Nombre y apellido obligatorios.
    ///   - Correo obligatorio y único entre vendedores activos.
    ///   - Contraseña mínimo 6 caracteres (se hashea con BCrypt).
    ///
    /// El correo es el identificador de login del vendedor en su portal.
    /// </summary>
    /// <param name="nombre">Nombre del vendedor.</param>
    /// <param name="apellido">Apellido del vendedor.</param>
    /// <param name="correo">Email de login (debe ser único entre vendedores activos).</param>
    /// <param name="telefono">Teléfono de contacto (opcional, puede ser vacío).</param>
    /// <param name="password">Contraseña en texto plano (mínimo 6 caracteres, se hashea).</param>
    /// <param name="nombreBanco">Nombre del banco para transferencias (opcional).</param>
    /// <param name="numeroCedula">Número de cédula del vendedor (opcional).</param>
    /// <param name="numeroCuenta">Número de cuenta bancaria (opcional).</param>
    /// <returns>Tupla (success, message).</returns>
    Task<(bool success, string message)> CrearVendedorAsync(
        string nombre, string apellido, string correo, string telefono, string password,
        string? nombreBanco = null, string? numeroCedula = null, string? numeroCuenta = null);

    /// <summary>
    /// Edita los datos de un vendedor existente.
    ///
    /// Contraseña opcional: si se envía en blanco, se conserva la actual.
    /// Si se envía con valor, debe tener mínimo 6 caracteres y se re-hashea.
    ///
    /// Valida unicidad de correo excluyendo al propio vendedor que se edita.
    /// </summary>
    /// <param name="id">ID del vendedor a editar.</param>
    /// <param name="nombre">Nuevo nombre.</param>
    /// <param name="apellido">Nuevo apellido.</param>
    /// <param name="correo">Nuevo correo (debe ser único).</param>
    /// <param name="telefono">Nuevo teléfono.</param>
    /// <param name="password">Nueva contraseña (null o vacío = conservar la actual).</param>
    /// <param name="nombreBanco">Nombre del banco para transferencias (opcional).</param>
    /// <param name="numeroCedula">Número de cédula del vendedor (opcional).</param>
    /// <param name="numeroCuenta">Número de cuenta bancaria (opcional).</param>
    /// <returns>Tupla (success, message).</returns>
    Task<(bool success, string message)> EditarVendedorAsync(
        Guid id, string nombre, string apellido, string correo, string telefono, string? password,
        string? nombreBanco = null, string? numeroCedula = null, string? numeroCuenta = null);

    /// <summary>
    /// Elimina un vendedor de forma LÓGICA (soft delete).
    /// No borra el registro de la BD: solo pone IsActive = false.
    ///
    /// Usamos soft delete por dos razones:
    ///   1. Los negocios creados por el vendedor siguen teniendo VendedorId asignado
    ///      y queremos mantener ese historial intacto.
    ///   2. Si el vendedor vuelve, es más sencillo reactivarlo que recrearlo.
    /// </summary>
    /// <param name="id">ID del vendedor a marcar como inactivo.</param>
    /// <returns>Tupla (success, message).</returns>
    Task<(bool success, string message)> EliminarVendedorAsync(Guid id);

    // ═══════════════════════════════════════════════════════════
    // LOGIN Y SESIÓN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Valida las credenciales de un vendedor y retorna el objeto si son correctas.
    ///
    /// Proceso:
    ///   1. Normaliza el correo a minúsculas (case-insensitive login).
    ///   2. Busca el vendedor activo con ese correo.
    ///   3. Verifica el password contra el hash BCrypt almacenado.
    ///   4. Retorna el objeto Vendedor si todo es correcto, o null si falla.
    ///
    /// Retornar null (en lugar de lanzar excepción) es una práctica segura:
    /// no revela si el correo existe o si fue el password el incorrecto.
    /// </summary>
    /// <param name="correo">Correo ingresado por el vendedor.</param>
    /// <param name="password">Contraseña en texto plano a verificar.</param>
    /// <returns>El objeto Vendedor si las credenciales son correctas, null si son inválidas.</returns>
    Task<Vendedor?> LoginVendedorAsync(string correo, string password);

    // ═══════════════════════════════════════════════════════════
    // NEGOCIOS ASIGNADOS Y ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los negocios donde VendedorId coincide con el vendedor dado.
    /// Incluye estadísticas básicas de cada negocio: total de clientes y días restantes.
    ///
    /// El vendedor usa esto para ver "su cartera" de negocios:
    /// cuántos tiene, cuáles están por vencer, cuáles tienen más clientes.
    /// </summary>
    /// <param name="vendedorId">ID del vendedor autenticado.</param>
    /// <returns>Lista de negocios asignados al vendedor con métricas enriquecidas.</returns>
    Task<List<object>> GetNegociosByVendedorAsync(Guid vendedorId);

    /// <summary>
    /// Calcula las estadísticas de rendimiento del vendedor:
    ///   - totalNegocios: todos los negocios que ha creado (activos y en prueba).
    ///   - activos: negocios que están pagando suscripción.
    ///   - enPrueba: negocios en período de prueba gratuita.
    ///   - totalClientes: suma de todos los clientes en sus negocios.
    ///
    /// Estas métricas se muestran en las tarjetas superiores del dashboard del vendedor.
    /// </summary>
    /// <param name="vendedorId">ID del vendedor.</param>
    /// <returns>Objeto anónimo con totalNegocios, activos, enPrueba y totalClientes.</returns>
    Task<object> GetVendedorStatsAsync(Guid vendedorId);

    // ═══════════════════════════════════════════════════════════
    // GESTIÓN DE LEADS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los leads del sistema ordenados por prioridad:
    ///   1. Los NO atendidos primero (OrderBy Atendido = false < true).
    ///   2. Dentro de cada grupo, los más recientes primero.
    ///
    /// Los leads son prospectos que completaron el formulario de contacto
    /// en la landing page. Cualquier vendedor puede verlos y atenderlos.
    /// </summary>
    /// <returns>Lista completa de leads con formato de fecha amigable.</returns>
    Task<List<object>> GetLeadsAsync();

    /// <summary>
    /// Marca un lead como atendido por el vendedor que lo reclamó.
    ///
    /// Una vez atendido, no se puede desmarcar (es un estado final).
    /// Si otro vendedor ya lo atendió, retorna error con el nombre del
    /// vendedor que lo tomó, para evitar trabajo duplicado.
    ///
    /// Se guarda el ID y nombre del vendedor que lo atendió para
    /// tener trazabilidad de qué vendedor manejó cada lead.
    /// </summary>
    /// <param name="leadId">ID del lead a marcar como atendido.</param>
    /// <param name="vendedorId">ID del vendedor que lo atiende.</param>
    /// <param name="vendedorNombre">Nombre del vendedor (para guardarlo en el lead sin JOIN).</param>
    /// <returns>Tupla (success, message). Falla si el lead ya fue atendido.</returns>
    Task<(bool success, string message)> MarcarLeadAtendidoAsync(Guid leadId, Guid vendedorId, string vendedorNombre);

    /// <summary>
    /// Retorna la cantidad de leads no atendidos (Atendido = false).
    /// Se usa para mostrar el badge de notificaciones en la sidebar del vendedor,
    /// indicando cuántos leads están esperando ser tomados.
    /// </summary>
    /// <returns>Número de leads pendientes de atención.</returns>
    Task<int> GetLeadsCountAsync();

    // ═══════════════════════════════════════════════════════════
    // TÉRMINOS Y CONDICIONES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Verifica si un vendedor ha aceptado los términos y condiciones.
    /// </summary>
    Task<bool> HasAceptadoTerminosAsync(Guid vendedorId);

    /// <summary>
    /// Marca que un vendedor aceptó los términos y condiciones.
    /// </summary>
    Task<(bool success, string message)> AceptarTerminosAsync(Guid vendedorId);

    /// <summary>
    /// Permite al vendedor actualizar sus propios datos bancarios para recibir comisiones.
    /// </summary>
    Task<(bool success, string message)> ActualizarDatosBancariosAsync(
        Guid vendedorId, string nombreBanco, string numeroCuenta, string numeroCedula);
}
