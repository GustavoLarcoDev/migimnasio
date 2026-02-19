// ═══════════════════════════════════════════════════════════
// AdminSettings.cs — Configuración de credenciales del administrador
//
// Esta clase se usa para leer la sección "AdminSettings" de appsettings.json.
// Se inyecta en los controllers que necesitan autenticar al admin via IOptions<AdminSettings>.
//
// En appsettings.json (o appsettings.Production.json) se ve así:
//   "AdminSettings": {
//     "Email": "admin@minegocio.com",
//     "PasswordHash": "$2a$11$..."   <- hash BCrypt
//   }
//
// IMPORTANTE: nunca guardar la contraseña en texto plano en appsettings.json.
// El hash BCrypt se genera con BCrypt.Net-Next una sola vez y se copia aquí.
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Models;

/// <summary>
/// Contiene las credenciales del administrador del sistema.
/// Esta clase no es una entidad de base de datos; es un objeto de configuración
/// que se lee desde appsettings.json y se inyecta con el patrón IOptions de ASP.NET Core.
///
/// Solo existe un administrador en toda la plataforma (a diferencia de los negocios
/// que son multi-tenant). Sus credenciales se configuran en el archivo de configuración,
/// no en la base de datos.
/// </summary>
public class AdminSettings
{
    /// <summary>
    /// Correo electrónico del administrador.
    /// Se usa como nombre de usuario en el formulario de login del admin.
    /// Debe coincidir exactamente (case-sensitive) con lo que escriba en el formulario.
    /// Se configura en appsettings.json bajo "AdminSettings:Email".
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Hash BCrypt de la contraseña del administrador.
    /// Se genera una sola vez con BCrypt y se copia aquí; NUNCA se guarda en texto plano.
    /// Al hacer login, el sistema compara la contraseña ingresada contra este hash
    /// usando BCrypt.Verify(), que es resistente a ataques de fuerza bruta.
    /// Se configura en appsettings.json bajo "AdminSettings:PasswordHash".
    /// </summary>
    public string PasswordHash { get; set; }
}
