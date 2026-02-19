// ═══════════════════════════════════════════════════════════
// Vendedor.cs — Modelo del vendedor/agente de ventas
//
// Los vendedores son agentes comerciales que registran y administran
// negocios en la plataforma a cambio de una comisión.
// Tienen su propio panel de acceso (VendedorController) separado del admin.
//
// Flujo típico:
//   1. El admin crea un vendedor.
//   2. El vendedor inicia sesión y crea negocios.
//   3. Cada negocio queda vinculado al vendedor via NegocioId→VendedorId.
//   4. El admin puede ver cuántos negocios creó cada vendedor (NegociosCreados).
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa un agente de ventas que puede registrar negocios en la plataforma.
/// Tiene su propio login y panel de control independiente del admin.
/// </summary>
public class Vendedor
{
    /// <summary>
    /// Identificador único del vendedor (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla Vendedores en la base de datos.
    /// </summary>
    [Key]
    public Guid VendedorId { get; set; }

    /// <summary>
    /// Nombre(s) del vendedor, por ejemplo: "Luis" o "Ana María".
    /// [Required] = campo obligatorio; no se puede crear un vendedor sin nombre.
    /// [StringLength(100)] = máximo 100 caracteres en la base de datos.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; }

    /// <summary>
    /// Apellido(s) del vendedor, por ejemplo: "Ramírez" o "Torres Vega".
    /// [Required] = campo obligatorio.
    /// [StringLength(100)] = máximo 100 caracteres.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Apellido { get; set; }

    /// <summary>
    /// Correo electrónico del vendedor. Se usa como nombre de usuario para el login.
    /// [Required] = obligatorio; sin email no puede iniciar sesión.
    /// [EmailAddress] = valida que tenga formato de email válido (ej: "vendedor@empresa.com").
    /// [StringLength(200)] = límite estándar para emails.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Correo { get; set; }

    /// <summary>
    /// Número de teléfono del vendedor (para contacto interno).
    /// [Required] = obligatorio en este modelo.
    /// [Phone] = valida el formato telefónico.
    /// [StringLength(20)] = suficiente para números con código de país.
    /// </summary>
    [Required]
    [Phone]
    [StringLength(20)]
    public string Telefono { get; set; }

    /// <summary>
    /// Contraseña del vendedor para iniciar sesión, almacenada como hash BCrypt.
    /// [Required] = obligatorio; sin contraseña no puede acceder.
    /// [MaxLength(200)] = los hashes BCrypt tienen ~60 caracteres; se deja margen amplio.
    /// NUNCA almacenar en texto plano; siempre hashear con BCrypt antes de guardar.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Password { get; set; }

    /// <summary>
    /// Indica si el vendedor puede iniciar sesión y operar en la plataforma.
    /// true  = vendedor activo (puede acceder y crear negocios).
    /// false = vendedor desactivado (no puede iniciar sesión, pero sus negocios existen).
    /// El admin puede activar o desactivar vendedores sin eliminarlos.
    /// Valor por defecto: true (se activa al crearse).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Fecha y hora en que el vendedor fue dado de alta en el sistema.
    /// Se asigna automáticamente con TimeHelper.Now al crear el registro.
    /// No se modifica después.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    /// <summary>
    /// Contador de negocios que este vendedor ha registrado en la plataforma.
    /// Se incrementa cada vez que el vendedor crea un nuevo negocio.
    /// Sirve para calcular comisiones y medir el rendimiento del vendedor.
    /// Valor por defecto: 0 (ningún negocio al registrarse).
    /// </summary>
    public int NegociosCreados { get; set; } = 0;
}
