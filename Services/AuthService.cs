// ═══════════════════════════════════════════════════════════
// AuthService.cs — Servicio de autenticación y seguridad
// Maneja login (admin + negocios), verificación de roles,
// hasheo BCrypt y migración lazy de contraseñas en texto plano
// ═══════════════════════════════════════════════════════════

using System.Security.Claims;
using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gimnasio.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly AdminSettings _adminSettings;

    public AuthService(ApplicationDbContext context, IOptions<AdminSettings> adminSettings)
    {
        _context = context;
        _adminSettings = adminSettings.Value;
    }

    // ═══════════════════════════════════════════════════════════
    // LOGIN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Proceso de login:
    /// 1. Verifica si es admin comparando con AdminSettings
    /// 2. Busca negocio por email o teléfono
    /// 3. Verifica contraseña (BCrypt o texto plano con migración lazy)
    /// 4. Valida que la cuenta esté activa y no haya expirado
    /// </summary>
    public async Task<(bool success, string role, Gym negocio, string error)> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, null, null, "Email y contraseña son obligatorios");

        // Verificar credenciales de admin
        if (email == _adminSettings.Email && password == _adminSettings.Password)
            return (true, "Admin", null, null);

        // Buscar negocio por email o teléfono
        var negocio = email.Contains('@')
            ? await _context.Negocios.FirstOrDefaultAsync(g => g.Email == email)
            : await _context.Negocios.FirstOrDefaultAsync(g => g.Telefono == email);
        if (negocio == null)
            return (false, null, null, "Credenciales inválidas");

        // Verificar contraseña con migración lazy de texto plano a BCrypt
        bool passwordValid = false;

        if (negocio.Password.StartsWith("$2"))
        {
            // Ya está hasheada con BCrypt
            passwordValid = VerifyPassword(password, negocio.Password);
        }
        else
        {
            // Contraseña en texto plano: comparar y migrar a BCrypt
            if (negocio.Password == password)
            {
                passwordValid = true;
                negocio.Password = HashPassword(password);
                negocio.FechaDeActualizacion = DateTime.Now;
                _context.Update(negocio);
                await _context.SaveChangesAsync();
            }
        }

        if (!passwordValid)
            return (false, null, null, "Credenciales inválidas");

        // Verificar que la cuenta esté activa
        if (!negocio.IsActive && !negocio.EsPrueba)
            return (false, null, null, "Su cuenta no está activa. Contacte al administrador.");

        // Verificar expiración de suscripción
        if (negocio.FechaExpiracion.HasValue && negocio.FechaExpiracion.Value.Date < DateTime.Now.Date)
            return (false, null, null, "EXPIRED");

        return (true, "Negocio", negocio, null);
    }

    // ═══════════════════════════════════════════════════════════
    // VERIFICACIÓN DE ROLES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Verifica si el usuario es admin y NO está impersonando un negocio
    /// </summary>
    public bool IsAdmin(ClaimsPrincipal user)
    {
        if (!user.Identity.IsAuthenticated)
            return false;

        // No es admin mientras impersona
        if (IsImpersonating(user))
            return false;

        var emailClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        return emailClaim != null && emailClaim.Value == _adminSettings.Email;
    }

    /// <summary>
    /// Verifica si el admin está impersonando un negocio
    /// </summary>
    public bool IsImpersonating(ClaimsPrincipal user)
    {
        return user.Claims.Any(c => c.Type == "AdminImpersonating" && c.Value == "true");
    }

    /// <summary>
    /// Extrae el NegocioId del claim "NegocioId" del usuario
    /// </summary>
    public Guid? GetNegocioId(ClaimsPrincipal user)
    {
        var claim = user.Claims.FirstOrDefault(c => c.Type == "NegocioId");
        if (claim != null && Guid.TryParse(claim.Value, out var id))
            return id;
        return null;
    }

    // ═══════════════════════════════════════════════════════════
    // MANEJO DE CONTRASEÑAS (BCrypt)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Hashea una contraseña usando BCrypt
    /// </summary>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifica una contraseña contra un hash BCrypt.
    /// Retorna false si el hash es inválido.
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
