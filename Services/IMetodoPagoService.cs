// ═══════════════════════════════════════════════════════════════════════════
// IMetodoPagoService.cs — Contrato del servicio de métodos de pago
//
// Define todas las operaciones posibles sobre los métodos de pago de un negocio:
//   - CRUD de métodos de pago (crear, leer, editar, eliminar lógico)
//   - Gestión de imagen QR (subir base64)
//   - Reordenamiento por drag & drop
//   - Establecer método predeterminado
//
// PATRÓN GENERAL:
//   Cada operación recibe un (negocioId) para garantizar aislamiento multi-tenant.
//   Las operaciones de escritura devuelven (bool success, string message) para
//   que el controlador pueda responder al frontend con un mensaje claro.
// ═══════════════════════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

/// <summary>
/// Contrato del servicio de gestión de métodos de pago.
/// Implementado por <see cref="MetodoPagoService"/>.
/// </summary>
public interface IMetodoPagoService
{
    // ═══════════════════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los métodos de pago activos del negocio.
    /// Ordenados por el campo Orden para respetar la prioridad del usuario.
    /// Usa AsNoTracking para optimizar lectura sin necesidad de tracking de cambios.
    /// </summary>
    Task<List<MetodoPago>> GetMetodosPagoAsync(Guid negocioId);

    /// <summary>
    /// Obtiene un método de pago específico por su ID.
    /// Siempre filtra por <paramref name="negocioId"/> para evitar que un negocio
    /// acceda a los métodos de pago de otro (seguridad multi-tenant).
    /// Devuelve <c>null</c> si el método no existe o fue eliminado lógicamente.
    /// </summary>
    Task<MetodoPago> GetMetodoPagoAsync(Guid negocioId, Guid metodoPagoId);

    // ═══════════════════════════════════════════════════════════════════════
    // CRUD
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo método de pago con validación previa.
    /// Reglas de negocio:
    ///   - El nombre no puede estar vacío
    ///   - Si se marca como predeterminado, limpia el flag de los demás métodos
    ///   - El orden se asigna automáticamente al final si es 0
    /// </summary>
    Task<(bool success, string message, Guid? metodoPagoId)> CrearMetodoPagoAsync(Guid negocioId, MetodoPagoCreateDto dto);

    /// <summary>
    /// Edita un método de pago existente.
    /// Busca por MetodoPagoId + NegocioId para seguridad multi-tenant.
    /// Si se establece como predeterminado, limpia el flag de los demás.
    /// </summary>
    Task<(bool success, string message)> EditarMetodoPagoAsync(Guid negocioId, MetodoPagoCreateDto dto);

    /// <summary>
    /// Elimina un método de pago de forma LÓGICA: pone <c>IsActive = false</c>.
    /// No permite eliminar si es el único método activo del negocio,
    /// ya que siempre debe existir al menos un método de pago disponible.
    /// </summary>
    Task<(bool success, string message)> EliminarMetodoPagoAsync(Guid negocioId, Guid metodoPagoId);

    // ═══════════════════════════════════════════════════════════════════════
    // IMAGEN QR
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sube una imagen QR codificada en Base64 al método de pago.
    /// Reemplaza cualquier imagen QR existente.
    /// </summary>
    Task<(bool success, string message)> SubirImagenQRAsync(Guid negocioId, Guid metodoPagoId, string base64Image);

    // ═══════════════════════════════════════════════════════════════════════
    // ORDENAMIENTO Y PREDETERMINADO
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reordena los métodos de pago según el orden de IDs recibido desde el frontend
    /// (resultado de un drag &amp; drop). La posición en la lista = nuevo valor de Orden.
    /// </summary>
    Task<(bool success, string message)> ReordenarMetodosPagoAsync(Guid negocioId, List<Guid> orderedIds);

    /// <summary>
    /// Establece un método de pago como predeterminado y quita el flag
    /// de todos los demás métodos activos del negocio.
    /// Solo un método puede ser predeterminado a la vez.
    /// </summary>
    Task<(bool success, string message)> SetPredeterminadoAsync(Guid negocioId, Guid metodoPagoId);

    // ═══════════════════════════════════════════════════════════════════════
    // ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene estadísticas de ventas agrupadas por método de pago para un negocio.
    /// Combina datos de OrdenVenta (ventas POS) y MovimientoInventario tipo "venta"
    /// (ventas rápidas individuales) para dar un panorama completo.
    /// Periodos incluidos: hoy, esta semana (desde domingo) y este mes.
    /// </summary>
    Task<List<MetodoPagoStatsDto>> GetPaymentStatsByMethodAsync(Guid negocioId);
}
