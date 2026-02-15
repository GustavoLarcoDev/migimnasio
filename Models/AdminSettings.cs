// ═══════════════════════════════════════════════════════════
// AdminSettings.cs — Configuración del administrador del sistema
// Se lee desde appsettings.json > sección "AdminSettings".
// Contiene las credenciales del admin (hash BCrypt).
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Models;

/// <summary>
/// Credenciales del administrador del sistema.
/// Se configuran en appsettings.json y se inyectan vía IOptions.
/// </summary>
public class AdminSettings
{
    /// <summary>Email del administrador para login</summary>
    public string Email { get; set; }

    /// <summary>Hash BCrypt de la contraseña del administrador</summary>
    public string PasswordHash { get; set; }
}
