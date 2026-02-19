// ═══════════════════════════════════════════════════════════════════════════════
// LoginDto.cs
//
// ESTE DTO: recibe las credenciales del formulario de inicio de sesión.
// El controlador de autenticación (HomeController / AuthController) lo recibe,
// busca el negocio por Email en la base de datos y compara la contraseña
// usando BCrypt. Si es correcto, genera la cookie de sesión del usuario.
//
// NOTA DE SEGURIDAD: nunca se almacena la contraseña en texto plano; solo se
// pasa por aquí momentáneamente para compararse con el hash guardado en BD.
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para el formulario de inicio de sesión.
/// Contiene únicamente los dos campos que el sistema necesita para autenticar
/// a un negocio: correo electrónico y contraseña.
/// Una vez validados, el servicio de autenticación genera la cookie de sesión.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Correo electrónico del negocio registrado. Obligatorio, máximo 200 caracteres.
    /// [Required] impide que el formulario se envíe vacío.
    /// [EmailAddress] verifica que tenga formato válido (usuario@dominio.com)
    /// antes de siquiera consultar la base de datos, lo que ahorra una llamada
    /// innecesaria si el usuario escribe mal el correo.
    /// Se usa como identificador único para buscar el negocio en la BD.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; }

    /// <summary>
    /// Contraseña del negocio en texto plano (solo durante el tránsito HTTP).
    /// Obligatorio, máximo 200 caracteres.
    /// NUNCA se guarda tal cual en la BD: el servicio compara este valor contra
    /// el hash BCrypt almacenado usando BCrypt.Verify(Password, hashGuardado).
    /// Si el negocio tiene contraseña en texto plano (migración legacy),
    /// el sistema la re-hashea automáticamente en el primer inicio de sesión exitoso.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Password { get; set; }
}
