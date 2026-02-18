// ═══════════════════════════════════════════════════════════
// IVendedorService.cs — Contrato del servicio de vendedores
// Define las operaciones para gestión de vendedores:
// CRUD, login, negocios asignados, estadísticas y leads
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
    /// Obtiene todos los vendedores activos con conteo de negocios asignados
    /// </summary>
    Task<List<object>> GetAllVendedoresAsync();

    /// <summary>
    /// Obtiene un vendedor específico por su ID
    /// </summary>
    Task<Vendedor?> GetVendedorAsync(Guid id);

    // ═══════════════════════════════════════════════════════════
    // CRUD DE VENDEDORES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo vendedor con contraseña hasheada (mínimo 6 caracteres)
    /// </summary>
    Task<(bool success, string message)> CrearVendedorAsync(string nombre, string apellido, string correo, string telefono, string password);

    /// <summary>
    /// Edita un vendedor existente. Si se envía contraseña nueva, se re-hashea.
    /// </summary>
    Task<(bool success, string message)> EditarVendedorAsync(Guid id, string nombre, string apellido, string correo, string telefono, string? password);

    /// <summary>
    /// Elimina un vendedor de forma lógica (IsActive = false)
    /// </summary>
    Task<(bool success, string message)> EliminarVendedorAsync(Guid id);

    // ═══════════════════════════════════════════════════════════
    // LOGIN Y SESIÓN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Valida credenciales de un vendedor y retorna el objeto si es válido
    /// </summary>
    Task<Vendedor?> LoginVendedorAsync(string correo, string password);

    // ═══════════════════════════════════════════════════════════
    // NEGOCIOS ASIGNADOS Y ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene los negocios asignados a un vendedor con estadísticas básicas
    /// </summary>
    Task<List<object>> GetNegociosByVendedorAsync(Guid vendedorId);

    /// <summary>
    /// Calcula estadísticas del vendedor: negocios totales, activos, en prueba y clientes
    /// </summary>
    Task<object> GetVendedorStatsAsync(Guid vendedorId);

    // ═══════════════════════════════════════════════════════════
    // GESTIÓN DE LEADS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los leads ordenados por estado y fecha
    /// </summary>
    Task<List<object>> GetLeadsAsync();

    /// <summary>
    /// Marca un lead como atendido por un vendedor específico
    /// </summary>
    Task<(bool success, string message)> MarcarLeadAtendidoAsync(Guid leadId, Guid vendedorId, string vendedorNombre);

    /// <summary>
    /// Obtiene el conteo de leads no atendidos (para badge)
    /// </summary>
    Task<int> GetLeadsCountAsync();
}
