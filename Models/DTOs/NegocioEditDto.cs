// ═══════════════════════════════════════════════════════════════════════════════
// NegocioEditDto.cs
//
// ESTE DTO: recibe los datos del formulario "Editar Negocio" del panel de
// administración. Solo el administrador de la plataforma puede editar negocios.
//
// DIFERENCIA CON NegocioCreateDto:
// 1. Incluye NegocioId para identificar qué negocio editar.
// 2. La contraseña es OPCIONAL. Si el campo Password llega vacío, el servicio
//    conserva la contraseña actual sin modificarla. Si llega con valor, la
//    re-hashea con BCrypt y reemplaza la anterior.
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para editar los datos de un negocio existente desde el panel de administración.
/// Todos los campos son iguales a NegocioCreateDto excepto que:
/// - NegocioId identifica al negocio que se va a modificar.
/// - Password es opcional: si viene vacío, la contraseña actual NO se cambia;
///   si viene con valor, se re-hashea con BCrypt y se actualiza en la BD.
/// </summary>
public class NegocioEditDto
{
    /// <summary>
    /// Identificador único del negocio que se desea editar. Obligatorio.
    /// Se envía como campo oculto en el formulario para que el servicio sepa
    /// qué registro de la base de datos debe actualizar.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre comercial del negocio. Obligatorio, máximo 200 caracteres.
    /// Actualiza el nombre visible en el dashboard y reportes del negocio.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string NegocioNombre { get; set; }

    /// <summary>
    /// Nombre completo del dueño o responsable del negocio. Obligatorio, máximo 200 caracteres.
    /// Se usa en comunicaciones administrativas y en el encabezado del dashboard.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DuenoNegocio { get; set; }

    /// <summary>
    /// Número de teléfono de contacto del negocio. Obligatorio, máximo 20 caracteres.
    /// [Phone] valida que el formato sea un número telefónico reconocible.
    /// </summary>
    [Required]
    [Phone]
    [MaxLength(20)]
    public string Telefono { get; set; }

    /// <summary>
    /// Correo electrónico del negocio. Obligatorio, máximo 200 caracteres.
    /// [EmailAddress] verifica formato válido (usuario@dominio.com).
    /// Es el identificador de login del negocio; si se cambia aquí, el dueño
    /// debe usar el nuevo email para iniciar sesión a partir de ese momento.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; }

    /// <summary>
    /// Nueva contraseña del negocio en texto plano. Opcional, máximo 200 caracteres.
    /// COMPORTAMIENTO:
    /// - Si viene vacío o null → el servicio NO modifica la contraseña actual.
    /// - Si viene con valor → el servicio la hashea con BCrypt y la reemplaza en la BD.
    /// Esto permite al admin actualizar solo los datos del perfil sin obligarlo
    /// a ingresar la contraseña cada vez.
    /// </summary>
    [MaxLength(200)]
    public string Password { get; set; }

    /// <summary>
    /// Indica si el negocio tiene suscripción activa y de pago.
    /// True = acceso completo a la plataforma.
    /// False = cuenta suspendida o pendiente de pago.
    /// El admin puede activar/desactivar negocios desde este formulario.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Indica si el negocio sigue en periodo de prueba gratuita.
    /// True = cuenta en prueba (puede tener funciones limitadas o avisos).
    /// False = negocio fuera del periodo de prueba.
    /// </summary>
    public bool EsPrueba { get; set; }

    /// <summary>
    /// Cantidad de días cubiertos por el último pago de suscripción.
    /// Se usa junto con FechaPago para calcular FechaExpiracion.
    /// </summary>
    public int? DiasPagados { get; set; }

    /// <summary>
    /// Monto mensual pactado de la suscripción en dólares.
    /// </summary>
    public decimal? PrecioSuscripcion { get; set; }

    /// <summary>
    /// Fecha en que vence la suscripción actual del negocio.
    /// Después de esta fecha, el login del negocio muestra "EXPIRED".
    /// </summary>
    public DateTime? FechaExpiracion { get; set; }
}
