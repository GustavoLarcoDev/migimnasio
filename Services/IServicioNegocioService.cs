// ═══════════════════════════════════════════════════════════
// IServicioNegocioService.cs — Contrato (interfaz) del servicio de servicios del negocio
//
// ¿QUÉ ES UN "SERVICIO DEL NEGOCIO"?
//   En el contexto artesanal (ej: peluquería, spa, barbería), un ServicioNegocio
//   es un producto/servicio que el negocio ofrece a sus clientes: corte de cabello,
//   manicure, masaje, etc. Tiene un precio fijo y una duración estimada.
//
// USO EN EL SISTEMA:
//   Cuando se crea una cita, se selecciona un ServicioNegocio. Ese servicio
//   determina automáticamente la duración de la cita y el precio base.
//   Los datos del servicio se "desnormalizan" en la cita (NombreServicio, PrecioServicio)
//   para preservar el histórico aunque el precio del servicio cambie después.
//
// TIPOS DE SERVICIO:
//   - Servicio simple: un solo servicio con su precio y duración (EsCombo=false)
//   - Combo: agrupación de servicios vendidos juntos (EsCombo=true)
//     Los ítems incluidos en el combo se guardan como texto en ItemsIncluidos.
//
// ELIMINACIÓN LÓGICA:
//   Los servicios se eliminan de forma "lógica" (IsActive=false), no se borran.
//   Esto preserva el historial de citas que referencian ese servicio.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

/// <summary>
/// Define el contrato para la gestión del catálogo de servicios del negocio.
/// Los servicios son los productos que ofrece el negocio (cortes, combos, tratamientos, etc.).
/// La implementación concreta está en <see cref="ServicioNegocioService"/>.
/// </summary>
public interface IServicioNegocioService
{
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // Solo lectura: no modifican datos en la base de datos
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los servicios activos del negocio ordenados alfabéticamente por nombre.
    /// Solo devuelve servicios con IsActive=true (los dados de baja no aparecen en el catálogo).
    /// Se usa en el formulario de creación de citas para que el recepcionista elija el servicio.
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyo catálogo de servicios se quiere consultar.</param>
    /// <returns>Lista de objetos con los datos del servicio (id, nombre, precio, duración, etc.).</returns>
    Task<object> GetServiciosAsync(Guid negocioId);

    /// <summary>
    /// Obtiene un servicio específico por su ID dentro de un negocio.
    /// Devuelve la entidad completa <see cref="ServicioNegocio"/> para que el código
    /// que llama tenga acceso a todos los campos (ej: para prellenar un formulario de edición).
    /// </summary>
    /// <param name="servicioId">ID único del servicio.</param>
    /// <param name="negocioId">ID del negocio (seguridad: evita acceso cruzado entre negocios).</param>
    /// <returns>El servicio si existe y está activo, o <c>null</c>.</returns>
    Task<ServicioNegocio> GetServicioAsync(Guid servicioId, Guid negocioId);

    // ═══════════════════════════════════════════════════════════
    // CRUD DE SERVICIOS
    // Operaciones que crean, modifican o dan de baja servicios
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo servicio en el catálogo del negocio.
    ///
    /// VALIDACIONES OBLIGATORIAS:
    ///   - Nombre: no puede estar vacío (es el nombre que ve el cliente)
    ///   - Precio: debe ser mayor a 0 (un servicio gratuito no tiene sentido en el sistema;
    ///     para cortesías se usa la opción "regalo" al registrar el pago de la cita)
    ///   - DuracionMinutos: debe ser mayor a 0 (determina cuánto tiempo bloquea el calendario)
    ///
    /// CAMPOS OPCIONALES:
    ///   - Descripcion: texto libre para describir el servicio al cliente
    ///   - ItemsIncluidos: lista de ítems para combos (ej: "Corte, Lavado, Peinado")
    ///   - EsCombo: indica si este servicio agrupa varios servicios (combo)
    /// </summary>
    /// <param name="dto">DTO con los datos del nuevo servicio.</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo.</returns>
    Task<(bool success, string message)> CrearServicioAsync(ServicioCreateDto dto);

    /// <summary>
    /// Edita un servicio existente en el catálogo del negocio.
    /// Aplica las mismas validaciones que <see cref="CrearServicioAsync"/>.
    ///
    /// IMPORTANTE SOBRE EL PRECIO:
    ///   Cambiar el precio de un servicio NO afecta las citas ya creadas.
    ///   El precio se "desnormaliza" al crear la cita (se guarda como PrecioServicio en la cita),
    ///   por lo que el historial siempre muestra el precio que se cobró en su momento.
    ///
    /// IMPORTANTE SOBRE LA DURACIÓN:
    ///   Cambiar la duración NO afecta las citas existentes, solo las nuevas.
    ///   La duración de citas existentes ya está guardada en DuracionMinutos de la cita.
    /// </summary>
    /// <param name="dto">DTO con los nuevos valores. El campo ServicioId identifica cuál editar.</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo.</returns>
    Task<(bool success, string message)> EditarServicioAsync(ServicioCreateDto dto);

    /// <summary>
    /// Da de baja un servicio del catálogo de forma lógica (IsActive = false).
    /// El servicio deja de aparecer en el formulario de creación de citas, pero
    /// no se borra físicamente para preservar el historial de citas pasadas.
    ///
    /// A diferencia del empleado, NO verificamos si hay citas futuras pendientes
    /// con este servicio, porque el nombre y precio ya están copiados en la cita
    /// (desnormalizados) y no dependen del registro del servicio para funcionar.
    /// </summary>
    /// <param name="servicioId">ID único del servicio a dar de baja.</param>
    /// <param name="negocioId">ID del negocio (seguridad).</param>
    /// <returns>Tupla con éxito/fallo y mensaje descriptivo.</returns>
    Task<(bool success, string message)> EliminarServicioAsync(Guid servicioId, Guid negocioId);
}
