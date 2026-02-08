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

    public async Task<(bool success, string role, Gym gimnasio, string error)> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, null, null, "Email y contraseña son obligatorios");

        // Check admin
        if (email == _adminSettings.Email && password == _adminSettings.Password)
            return (true, "Admin", null, null);

        // Check gimnasio
        var gimnasio = await _context.Gimnasios.FirstOrDefaultAsync(g => g.Email == email);
        if (gimnasio == null)
            return (false, null, null, "Credenciales inválidas");

        // Try BCrypt first, then plaintext with lazy migration
        bool passwordValid = false;

        if (gimnasio.Password.StartsWith("$2"))
        {
            // Already hashed with BCrypt
            passwordValid = VerifyPassword(password, gimnasio.Password);
        }
        else
        {
            // Plaintext comparison + lazy migration
            if (gimnasio.Password == password)
            {
                passwordValid = true;
                // Migrate to BCrypt hash
                gimnasio.Password = HashPassword(password);
                gimnasio.FechaDeActualizacion = DateTime.Now;
                _context.Update(gimnasio);
                await _context.SaveChangesAsync();
            }
        }

        if (!passwordValid)
            return (false, null, null, "Credenciales inválidas");

        if (!gimnasio.IsActive && !gimnasio.EsPrueba)
            return (false, null, null, "Su cuenta no está activa. Contacte al administrador.");

        return (true, "Gimnasio", gimnasio, null);
    }

    public bool IsAdmin(ClaimsPrincipal user)
    {
        if (!user.Identity.IsAuthenticated)
            return false;

        var emailClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        return emailClaim != null && emailClaim.Value == _adminSettings.Email;
    }

    public Guid? GetGimnasioId(ClaimsPrincipal user)
    {
        var claim = user.Claims.FirstOrDefault(c => c.Type == "GimnasioId");
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
