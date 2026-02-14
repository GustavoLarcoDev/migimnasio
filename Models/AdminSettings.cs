// ═══════════════════════════════════════════════════════════
// AdminSettings.cs — Configuración del administrador del sistema
// Se lee desde appsettings.json > sección "AdminSettings".
// Contiene las credenciales del admin (comparación directa).
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

    /// <summary>Contraseña del administrador (comparación directa, no hasheada)</summary>
    public string Password { get; set; }
}
