// ═══════════════════════════════════════════════════════════
// IAuthService.cs — Contrato del servicio de autenticación
// Define las operaciones de login, verificación de roles,
// y manejo de contraseñas con BCrypt
// ═══════════════════════════════════════════════════════════

using System.Security.Claims;
using Gimnasio.Models;

namespace Gimnasio.Services;

public interface IAuthService
{
    /// <summary>
    /// Valida credenciales contra AdminSettings, Vendedores y Negocios.
    /// Soporta login por email o teléfono.
    /// Retorna: (éxito, rol, objeto negocio, vendedorId, vendedorNombre, mensaje de error)
    /// </summary>
    Task<(bool success, string role, Gym negocio, Guid? vendedorId, string vendedorNombre, string error)> LoginAsync(string email, string password);

    /// <summary>
    /// Verifica si el usuario autenticado es admin (no impersonando)
    /// </summary>
    bool IsAdmin(ClaimsPrincipal user);

    /// <summary>
    /// Verifica si el admin está impersonando un negocio
    /// </summary>
    bool IsImpersonating(ClaimsPrincipal user);

    /// <summary>
    /// Extrae el NegocioId del claim del usuario autenticado
    /// </summary>
    Guid? GetNegocioId(ClaimsPrincipal user);

    /// <summary>
    /// Hashea una contraseña con BCrypt
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifica una contraseña contra su hash BCrypt
    /// </summary>
    bool VerifyPassword(string password, string hash);
}
