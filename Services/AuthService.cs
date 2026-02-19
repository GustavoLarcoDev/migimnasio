// ═══════════════════════════════════════════════════════════
// AuthService.cs — Servicio de autenticación y seguridad
//
// FUNCIÓN: Centraliza toda la lógica de autenticación del sistema.
// Tres tipos de usuario pueden iniciar sesión:
//   1. ADMIN — credenciales en appsettings.json (AdminSettings)
//   2. VENDEDOR — tabla Vendedores, login por correo
//   3. NEGOCIO — tabla Negocios, login por email o teléfono
//
// SEGURIDAD DE CONTRASEÑAS (BCrypt):
// BCrypt es un algoritmo de hasheo diseñado específicamente para contraseñas.
// Es lento por diseño (protege contra ataques de fuerza bruta) y agrega
// una "sal" aleatoria automáticamente (protege contra rainbow tables).
// El hash resultante empieza con "$2" y contiene la sal embebida.
//
// MIGRACIÓN LAZY DE CONTRASEÑAS:
// Algunos negocios antiguos tenían contraseñas en texto plano en la DB.
// En lugar de migrarlos todos de golpe, se usa migración lazy:
// cuando el usuario hace login con texto plano, se hashea en ese momento.
// Así la migración ocurre gradualmente sin downtime.
//
// IMPERSONACIÓN:
// El admin puede "impersonar" un negocio (ver su dashboard como dueño).
// Mientras impersona, IsAdmin() retorna false — el sistema se comporta
// como si fuera el dueño del negocio, no el admin.
// ═══════════════════════════════════════════════════════════

using System.Security.Claims;
using Gimnasio.Data;
using Gimnasio.Helpers;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gimnasio.Services;

/// <summary>
/// Implementación del servicio de autenticación. Maneja login de los tres
/// tipos de usuario (Admin, Vendedor, Negocio), verificación de roles por
/// claims, y hasheo/verificación de contraseñas con BCrypt.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;

    // AdminSettings: contiene el email y hash BCrypt del administrador.
    // Se configura en appsettings.json → sección "AdminSettings".
    // IOptions<T> permite leer configuraciones tipadas desde DI.
    private readonly AdminSettings _adminSettings;

    /// <summary>
    /// Constructor: recibe el DbContext y la configuración del admin por DI.
    /// </summary>
    public AuthService(ApplicationDbContext context, IOptions<AdminSettings> adminSettings)
    {
        _context = context;
        _adminSettings = adminSettings.Value;
    }

    // ═══════════════════════════════════════════════════════════
    // LOGIN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Proceso completo de login. Intenta autenticar en este orden:
    ///
    /// 1. Admin: compara email y verifica hash BCrypt contra AdminSettings.
    /// 2. Vendedor: si el email contiene "@", busca en tabla Vendedores.
    /// 3. Negocio: busca por email (si contiene "@") o por teléfono.
    ///    - Soporta contraseñas BCrypt (empiezan con "$2") y texto plano
    ///      (migración lazy: al verificar, se hashea y guarda en la DB).
    ///    - Valida que la cuenta esté activa y no haya expirado.
    ///
    /// Retorna una tupla con: (éxito, rol, objeto negocio, vendedorId, vendedorNombre, error).
    /// Solo uno de {negocio, vendedorId} estará poblado dependiendo del rol.
    /// </summary>
    /// <param name="email">Email o teléfono del usuario.</param>
    /// <param name="password">Contraseña en texto plano (se verifica contra el hash).</param>
    public async Task<(bool success, string role, Gym negocio, Guid? vendedorId, string vendedorNombre, string error)> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, null, null, null, null, "Email y contraseña son obligatorios");

        // PASO 1: Verificar si es el admin.
        // BCrypt.Verify compara el texto plano contra el hash almacenado en appsettings.
        if (email == _adminSettings.Email && BCrypt.Net.BCrypt.Verify(password, _adminSettings.PasswordHash))
            return (true, "Admin", null, null, null, null);

        // PASO 2: Verificar si es un vendedor (por email o por teléfono).
        // Los vendedores pueden iniciar sesión con su correo o su número de teléfono.
        {
            Vendedor vendedor = null;

            if (email.Contains('@'))
            {
                // Buscar por correo electrónico
                vendedor = await _context.Vendedores
                    .FirstOrDefaultAsync(v => v.Correo == email.Trim().ToLower() && v.IsActive);
            }
            else
            {
                // Buscar por teléfono: normalizar el input para matchear con el formato almacenado (+593...)
                var telefonoNormalizado = PhoneHelper.NormalizeForLookup(email);
                vendedor = await _context.Vendedores
                    .FirstOrDefaultAsync(v => v.Telefono == telefonoNormalizado && v.IsActive);
            }

            if (vendedor != null)
            {
                if (VerifyPassword(password, vendedor.Password))
                    return (true, "Vendedor", null, vendedor.VendedorId, $"{vendedor.Nombre} {vendedor.Apellido}", null);
            }
        }

        // PASO 3: Buscar negocio por email o por teléfono.
        // Esto permite que los dueños inicien sesión con cualquiera de los dos.
        // Normalizar el teléfono para buscar en formato +593...
        Gym negocio;
        if (email.Contains('@'))
        {
            negocio = await _context.Negocios.FirstOrDefaultAsync(g => g.Email == email);
        }
        else
        {
            var telefonoNormalizado = PhoneHelper.NormalizeForLookup(email);
            negocio = await _context.Negocios.FirstOrDefaultAsync(g => g.Telefono == telefonoNormalizado);
        }

        if (negocio == null)
            return (false, null, null, null, null, "Credenciales inválidas");

        // PASO 4: Verificar contraseña con soporte de migración lazy.
        // Si el hash empieza con "$2", es BCrypt → usar BCrypt.Verify.
        // Si no, es texto plano (cuenta antigua) → comparar directo y hashear.
        bool passwordValid = false;

        if (negocio.Password.StartsWith("$2"))
        {
            // Contraseña ya hasheada con BCrypt — verificación segura
            passwordValid = VerifyPassword(password, negocio.Password);
        }
        else
        {
            // Contraseña en texto plano (cuenta antigua sin hashear).
            // Si coincide: hashearla ahora y guardar en la DB para futuras sesiones.
            // Esta es la "migración lazy" — se hace sin intervención manual.
            if (negocio.Password == password)
            {
                passwordValid = true;
                negocio.Password = HashPassword(password);
                negocio.FechaDeActualizacion = TimeHelper.Now;
                _context.Update(negocio);
                await _context.SaveChangesAsync();
            }
        }

        if (!passwordValid)
            return (false, null, null, null, null, "Credenciales inválidas");

        // PASO 5: Validar que la cuenta esté activa.
        // Un negocio inactivo (ni activo ni en prueba) no puede iniciar sesión.
        if (!negocio.IsActive && !negocio.EsPrueba)
            return (false, null, null, null, null, "Su cuenta no está activa. Contacte al administrador.");

        // PASO 6: Validar que la suscripción no haya expirado.
        // "EXPIRED" es un código especial que el controlador interpreta para
        // redirigir al dueño a una página explicando que venció su plan.
        if (negocio.FechaExpiracion.HasValue && negocio.FechaExpiracion.Value.Date < TimeHelper.Now.Date)
            return (false, null, null, null, null, "EXPIRED");

        return (true, "Negocio", negocio, null, null, null);
    }

    // ═══════════════════════════════════════════════════════════
    // VERIFICACIÓN DE ROLES POR CLAIMS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Verifica si el usuario autenticado es el administrador del sistema.
    ///
    /// Retorna false si el admin está impersonando un negocio, porque en ese
    /// estado el sistema debe comportarse como si fuera el dueño del negocio.
    ///
    /// Claims son datos encriptados en la cookie de sesión que identifican
    /// al usuario sin necesidad de consultar la base de datos en cada petición.
    /// </summary>
    /// <param name="user">El ClaimsPrincipal del usuario autenticado (viene de HttpContext.User).</param>
    public bool IsAdmin(ClaimsPrincipal user)
    {
        if (!user.Identity.IsAuthenticated)
            return false;

        // Si está impersonando, tratarlo como el negocio, no como admin
        if (IsImpersonating(user))
            return false;

        // Verificar el claim de rol "Admin" que se asigna durante el login.
        // Es más seguro que comparar el email porque:
        //   1. El rol solo se asigna tras verificar BCrypt en LoginAsync.
        //   2. Desacopla la autorización del valor de configuración.
        //   3. Es el patrón estándar de ASP.NET Core para verificar roles.
        return user.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    /// <summary>
    /// Verifica si el admin está actualmente impersonando un negocio.
    /// El claim "AdminImpersonating" se agrega a la cookie cuando el admin
    /// hace clic en "Ver como dueño" desde el panel de administración.
    /// </summary>
    /// <param name="user">El ClaimsPrincipal del usuario autenticado.</param>
    public bool IsImpersonating(ClaimsPrincipal user)
    {
        return user.Claims.Any(c => c.Type == "AdminImpersonating" && c.Value == "true");
    }

    /// <summary>
    /// Extrae el NegocioId del claim de sesión del usuario.
    /// El claim "NegocioId" se agrega a la cookie cuando un negocio
    /// (o un admin impersonando) inicia sesión exitosamente.
    ///
    /// Retorna null si el usuario no tiene NegocioId (es admin puro o vendedor).
    /// </summary>
    /// <param name="user">El ClaimsPrincipal del usuario autenticado.</param>
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
    /// Hashea una contraseña usando BCrypt con el factor de costo por defecto.
    /// El resultado incluye la sal aleatoria embebida y empieza con "$2".
    ///
    /// Se usa al crear negocios/vendedores, al editar contraseñas,
    /// y en la migración lazy de contraseñas en texto plano.
    /// </summary>
    /// <param name="password">Contraseña en texto plano a hashear.</param>
    /// <returns>Hash BCrypt listo para guardar en la base de datos.</returns>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifica si una contraseña en texto plano coincide con un hash BCrypt.
    ///
    /// BCrypt extrae la sal del hash automáticamente para la comparación,
    /// por lo que no se necesita guardar la sal por separado.
    ///
    /// Si el hash está malformado o es inválido, captura la excepción y
    /// retorna false en lugar de propagar el error.
    /// </summary>
    /// <param name="password">Contraseña en texto plano ingresada por el usuario.</param>
    /// <param name="hash">Hash BCrypt almacenado en la base de datos.</param>
    /// <returns>true si la contraseña coincide con el hash, false en caso contrario.</returns>
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // El hash puede ser inválido si fue corrompido o si vino de un sistema diferente.
            // En lugar de lanzar una excepción, simplemente indicar que no coincide.
            return false;
        }
    }
}
