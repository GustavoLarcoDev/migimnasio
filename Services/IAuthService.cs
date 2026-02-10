using System.Security.Claims;
using Gimnasio.Models;

namespace Gimnasio.Services;

public interface IAuthService
{
    Task<(bool success, string role, Gym negocio, string error)> LoginAsync(string email, string password);
    bool IsAdmin(ClaimsPrincipal user);
    bool IsImpersonating(ClaimsPrincipal user);
    Guid? GetNegocioId(ClaimsPrincipal user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
