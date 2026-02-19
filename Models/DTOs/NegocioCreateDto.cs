// ═══════════════════════════════════════════════════════════════════════════════
// NegocioCreateDto.cs
//
// ESTE DTO: recibe los datos del formulario "Crear Negocio" del panel de
// administración (AdminController). Solo el administrador de la plataforma
// puede crear negocios nuevos; los dueños de negocio no tienen acceso a esto.
//
// FLUJO: Admin llena el formulario → se envía por AJAX/POST → el controlador
// recibe este DTO → AdminService crea el registro en la tabla Negocios
// hasheando la contraseña con BCrypt antes de guardarla.
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para registrar un nuevo negocio en la plataforma desde el panel de administración.
/// Contiene todos los datos necesarios para crear la cuenta del negocio:
/// información del establecimiento, datos del dueño, credenciales de acceso
/// y el estado inicial de la cuenta (activa o en prueba).
/// La contraseña se almacena hasheada con BCrypt; nunca se guarda en texto plano.
/// </summary>
public class NegocioCreateDto
{
    /// <summary>
    /// Nombre comercial del negocio. Obligatorio, máximo 200 caracteres.
    /// Ejemplos: "Gym ProFit", "Salón Belleza XYZ", "Consultorio Dental López".
    /// Se muestra en el dashboard, reportes y comunicaciones hacia los clientes.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string NombreNegocio { get; set; }

    /// <summary>
    /// Nombre completo del dueño o responsable del negocio. Obligatorio, máximo 200 caracteres.
    /// Se usa para personalizar comunicaciones administrativas y reportes.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DuenoNegocio { get; set; }

    /// <summary>
    /// Número de teléfono de contacto del negocio. Obligatorio, máximo 20 caracteres.
    /// [Phone] valida que tenga un formato telefónico reconocible.
    /// Se usa para comunicaciones de soporte y notificaciones de la plataforma.
    /// </summary>
    [Required]
    [Phone]
    [MaxLength(20)]
    public string Telefono { get; set; }

    /// <summary>
    /// Correo electrónico del negocio. Obligatorio, máximo 200 caracteres.
    /// [EmailAddress] verifica que el formato sea válido (usuario@dominio.com).
    /// Este email se usa como nombre de usuario para iniciar sesión en la plataforma.
    /// Debe ser único en la tabla de negocios.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string EmailNegocio { get; set; }

    /// <summary>
    /// Contraseña inicial del negocio en texto plano. Obligatoria, máximo 200 caracteres.
    /// El servicio la hashea con BCrypt antes de guardarla en la base de datos,
    /// por lo que nunca se persiste en texto plano.
    /// El dueño del negocio usará esta contraseña para iniciar sesión.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string PasswordNegocio { get; set; }

    /// <summary>
    /// Indica si el negocio tiene una suscripción activa y de pago.
    /// True = negocio con suscripción paga, tiene acceso completo a la plataforma.
    /// False = negocio sin suscripción activa (puede estar en prueba o suspendido).
    /// El sistema puede restringir funciones si IsActive es false.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Indica si el negocio está en periodo de prueba gratuita.
    /// True = acceso temporal sin pago para evaluar la plataforma.
    /// False = negocio fuera de periodo de prueba.
    /// Cuando EsPrueba es true, el sistema puede mostrar avisos de expiración
    /// o limitar ciertas funcionalidades avanzadas.
    /// </summary>
    public bool EsPrueba { get; set; }
}
