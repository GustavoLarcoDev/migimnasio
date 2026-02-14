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

    public async Task<(bool success, string role, Gym negocio, string error)> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, null, null, "Email y contraseña son obligatorios");

        // Check admin
        if (email == _adminSettings.Email && password == _adminSettings.Password)
            return (true, "Admin", null, null);

        // Check negocio by email or phone
        var negocio = email.Contains('@')
            ? await _context.Negocios.FirstOrDefaultAsync(g => g.Email == email)
            : await _context.Negocios.FirstOrDefaultAsync(g => g.Telefono == email);
        if (negocio == null)
            return (false, null, null, "Credenciales inválidas");

        // Try BCrypt first, then plaintext with lazy migration
        bool passwordValid = false;

        if (negocio.Password.StartsWith("$2"))
        {
            // Already hashed with BCrypt
            passwordValid = VerifyPassword(password, negocio.Password);
        }
        else
        {
            // Plaintext comparison + lazy migration
            if (negocio.Password == password)
            {
                passwordValid = true;
                // Migrate to BCrypt hash
                negocio.Password = HashPassword(password);
                negocio.FechaDeActualizacion = DateTime.Now;
                _context.Update(negocio);
                await _context.SaveChangesAsync();
            }
        }

        if (!passwordValid)
            return (false, null, null, "Credenciales inválidas");

        if (!negocio.IsActive && !negocio.EsPrueba)
            return (false, null, null, "Su cuenta no está activa. Contacte al administrador.");

        // Check subscription expiry
        if (negocio.FechaExpiracion.HasValue && negocio.FechaExpiracion.Value.Date < DateTime.Now.Date)
            return (false, null, null, "EXPIRED");

        return (true, "Negocio", negocio, null);
    }

    public bool IsAdmin(ClaimsPrincipal user)
    {
        if (!user.Identity.IsAuthenticated)
            return false;

        // Not admin while impersonating
        if (IsImpersonating(user))
            return false;

        var emailClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        return emailClaim != null && emailClaim.Value == _adminSettings.Email;
    }

    public bool IsImpersonating(ClaimsPrincipal user)
    {
        return user.Claims.Any(c => c.Type == "AdminImpersonating" && c.Value == "true");
    }

    public Guid? GetNegocioId(ClaimsPrincipal user)
    {
        var claim = user.Claims.FirstOrDefault(c => c.Type == "NegocioId");
        if (claim != null && Guid.TryParse(claim.Value, out var id))
            return id;
        return null;
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

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
