// ═══════════════════════════════════════════════════════════
// IClienteService.cs — Contrato del servicio de clientes
//
// Define todas las operaciones disponibles para gestionar
// clientes en el sistema. Este contrato es lo que los
// controllers usan: nunca dependen directamente de
// ClienteService (patrón de inyección de dependencias).
//
// MULTI-TENANCY: cada método recibe un negocioId para
// asegurar que un negocio nunca pueda ver ni modificar
// datos de otro negocio. Es el pilar del aislamiento SaaS.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

public interface IClienteService
{
    // ═══════════════════════════════════════════════════════════
    // ESTADÍSTICAS DEL DASHBOARD
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula todas las métricas que se muestran en el dashboard del negocio.
    /// </summary>
    /// <param name="negocioId">
    /// ID del negocio actual. Se usa para filtrar y aislar los datos
    /// de este negocio respecto a todos los demás (multi-tenancy).
    /// </param>
    /// <returns>
    /// Objeto anónimo con: totalClientes, clientesActivos, clientesVencidos,
    /// clientesNuevosHoy, clientesNuevosMes, ingresosMes, ingresosHoy,
    /// y una lista de proximosVencer (clientes que vencen en los próximos 5 días).
    /// </returns>
    Task<object> GetDashboardStatsAsync(Guid negocioId);

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene la lista completa de clientes del negocio.
    /// Incluye campos calculados como DiasRestantes, EstaActivo y PorEmpezar
    /// que el frontend necesita para mostrar el estado visual de cada cliente.
    /// </summary>
    /// <param name="negocioId">ID del negocio propietario de los clientes.</param>
    /// <returns>Lista de clientes con campos enriquecidos, ordenados por fecha de vencimiento descendente.</returns>
    Task<object> GetClientesAsync(Guid negocioId);

    /// <summary>
    /// Obtiene un único cliente por su ID, validando que pertenezca al negocio indicado.
    /// Nunca retorna un cliente de otro negocio aunque el ID sea correcto.
    /// </summary>
    /// <param name="id">ID único del cliente.</param>
    /// <param name="negocioId">ID del negocio que hace la consulta (aislamiento multi-tenant).</param>
    /// <returns>El objeto Cliente, o null si no existe o no pertenece al negocio.</returns>
    Task<Cliente> GetClienteAsync(Guid id, Guid negocioId);

    /// <summary>
    /// Obtiene únicamente los clientes marcados como "diario" (EsDiario = true).
    /// Estos son clientes que pagan por día y se gestionan de forma diferente:
    /// el dueño les envía un mensaje de WhatsApp y luego limpia la lista.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <returns>Lista simplificada con ID, nombre y teléfono de los clientes diarios.</returns>
    Task<object> GetClientesDiariosAsync(Guid negocioId);

    // ═══════════════════════════════════════════════════════════
    // CREAR Y EDITAR
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo cliente con membresía.
    /// La fecha de fin puede calcularse de dos formas (en orden de prioridad):
    /// 1. FechaFin explícita proporcionada en el DTO.
    /// 2. FechaInicio + Dias (número de días de membresía).
    /// Siempre registra un log inmutable con el monto pagado, lo que
    /// garantiza que los ingresos no se pierdan si se borra el cliente.
    /// </summary>
    /// <param name="model">DTO con todos los datos del nuevo cliente.</param>
    /// <returns>Tupla (success, message): success=true si se creó, con mensaje descriptivo.</returns>
    Task<(bool success, string message)> CrearClienteAsync(ClienteCreateDto model);

    /// <summary>
    /// Edita un cliente existente.
    /// Detecta cada campo que cambió y lo registra individualmente en el log
    /// de auditoría, para que el dueño pueda ver el historial completo de cambios.
    /// </summary>
    /// <param name="model">DTO con los datos actualizados. Debe incluir ClienteId para encontrar el registro.</param>
    /// <returns>Tupla (success, message).</returns>
    Task<(bool success, string message)> EditarClienteAsync(ClienteCreateDto model);

    // ═══════════════════════════════════════════════════════════
    // ELIMINAR Y RENOVAR
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Elimina un cliente del sistema.
    /// IMPORTANTE: Los logs de pago del cliente NO se eliminan, ya que son
    /// registros financieros inmutables. Los ingresos históricos permanecen intactos.
    /// No permite eliminar si el cliente tiene citas asociadas (en negocios artesanales).
    /// </summary>
    /// <param name="id">ID del cliente a eliminar.</param>
    /// <param name="negocioId">ID del negocio (verificación de seguridad multi-tenant).</param>
    /// <returns>Tupla (success, message).</returns>
    Task<(bool success, string message)> EliminarClienteAsync(Guid id, Guid negocioId);

    /// <summary>
    /// Renueva la membresía de un cliente extendiendo su fecha de vencimiento
    /// y registra el nuevo pago en los logs.
    /// La nueva fecha de fin debe ser posterior a la fecha de fin actual;
    /// esto evita "renovaciones" que en realidad acorten la membresía.
    /// </summary>
    /// <param name="id">ID del cliente a renovar.</param>
    /// <param name="negocioId">ID del negocio (verificación de seguridad).</param>
    /// <param name="nuevaFechaFin">Nueva fecha hasta la que estará activo el cliente.</param>
    /// <param name="precio">Monto cobrado en esta renovación (se guarda en logs).</param>
    /// <returns>Tupla (success, message).</returns>
    Task<(bool success, string message)> RenovarClienteAsync(Guid id, Guid negocioId, DateTime nuevaFechaFin, decimal precio);

    // ═══════════════════════════════════════════════════════════
    // CLIENTES DIARIOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Marca todos los clientes diarios del negocio como EsDiario = false, limpiando la lista.
    /// El flujo típico de uso es:
    ///   1. El dueño ve la lista de clientes diarios del día.
    ///   2. Envía mensajes por WhatsApp (via links wa.me) a cada uno.
    ///   3. Llama a este método para resetear la lista al día siguiente.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <returns>Cantidad de clientes que fueron marcados como no-diarios.</returns>
    Task<int> LimpiarClientesDiariosAsync(Guid negocioId);

    // ═══════════════════════════════════════════════════════════
    // CLIENTES ARTESANAL (negocios con modelo de citas)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Busca clientes por nombre o teléfono con búsqueda parcial (contains).
    /// Se usa para el autocomplete al crear una cita rápida: el recepcionista
    /// escribe el nombre y el sistema sugiere clientes existentes.
    /// Requiere mínimo 2 caracteres para evitar resultados demasiado amplios.
    /// Retorna máximo 10 resultados para mantener el UI ágil.
    /// </summary>
    /// <param name="negocioId">ID del negocio (aislamiento multi-tenant).</param>
    /// <param name="query">Texto a buscar en nombre, apellido o teléfono.</param>
    /// <returns>Lista de hasta 10 clientes con ID, nombre completo, teléfono y email.</returns>
    Task<object> BuscarClientesAsync(Guid negocioId, string query);

    /// <summary>
    /// Crea un cliente para negocios con modelo artesanal (citas y servicios),
    /// sin los campos de membresía (Dias, Precio, FechaQueTermina se inicializan en 0/ahora).
    /// La diferencia con CrearClienteAsync es que estos clientes no tienen membresía periódica:
    /// pagan por cada cita o servicio.
    /// </summary>
    /// <param name="model">DTO simplificado sin campos de membresía.</param>
    /// <returns>Tupla (success, message).</returns>
    Task<(bool success, string message)> CrearClienteArtesanalAsync(ClienteArtesanalCreateDto model);

    /// <summary>
    /// Edita los datos básicos de un cliente artesanal (nombre, contacto, dirección).
    /// No modifica campos de membresía porque los negocios artesanales no los usan.
    /// </summary>
    /// <param name="model">DTO con los datos a actualizar. Debe incluir ClienteId.</param>
    /// <returns>Tupla (success, message).</returns>
    Task<(bool success, string message)> EditarClienteArtesanalAsync(ClienteArtesanalCreateDto model);

    /// <summary>
    /// Obtiene todos los clientes de un negocio artesanal, enriquecidos con
    /// el total de citas que tiene cada uno. Esto permite al dueño saber qué
    /// clientes son los más frecuentes.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <returns>Lista de clientes con TotalCitas calculado.</returns>
    Task<object> GetClientesArtesanalAsync(Guid negocioId);

    // ═══════════════════════════════════════════════════════════
    // IMPORTACIÓN Y EXPORTACIÓN EXCEL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Exporta todos los clientes del negocio a un archivo Excel (.xlsx).
    /// Incluye: nombre, apellido, email, teléfono, fechas, días, precio, tipo y estado.
    /// El archivo se retorna como array de bytes para enviarlo directamente como
    /// respuesta HTTP con Content-Type application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.
    /// </summary>
    /// <param name="negocioId">ID del negocio.</param>
    /// <returns>Bytes del archivo .xlsx generado.</returns>
    Task<byte[]> ExportClientesExcelAsync(Guid negocioId);

    /// <summary>
    /// Importa clientes desde un archivo Excel subido por el usuario.
    /// Características clave del importador:
    /// - Detecta columnas automáticamente por nombre de encabezado (acepta tildes y variantes).
    /// - Si no detecta encabezados, asume columnas por posición (A=Nombre, B=Apellido, etc.).
    /// - Omite filas con nombre+apellido ya existentes (evita duplicados).
    /// - Procesa cada fila independientemente: un error en fila 5 no detiene las demás.
    /// - Retorna un resumen detallado: cuántos se crearon, omitieron y fallaron.
    /// </summary>
    /// <param name="negocioId">ID del negocio destino.</param>
    /// <param name="fileStream">Stream del archivo .xlsx recibido en el request HTTP.</param>
    /// <returns>
    /// Objeto con: success, message, clientesCreados (cantidad),
    /// clientesOmitidos (cantidad), erroresCount, y listas de detalle.
    /// </returns>
    Task<object> ImportarClientesExcelAsync(Guid negocioId, Stream fileStream);
}
