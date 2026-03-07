// ═══════════════════════════════════════════════════════════
// SolicitudRegistro.cs — Modelo de solicitud de registro para delivery
//
// Almacena las solicitudes de registro tanto de motorizados como de
// restaurantes que desean unirse al sistema de delivery de My-Negocio.
//
// Discriminador TipoSolicitud:
//   "motorizado"  → conductor que quiere hacer entregas
//   "restaurante" → negocio que quiere recibir pedidos delivery
//
// Flujo:
//   Solicitante llena formulario público → se crea SolicitudRegistro
//   con Estado="pendiente" → admin revisa → aprueba (crea Motorizado/Gym)
//   o rechaza (con motivo).
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa una solicitud de registro pendiente de aprobación por el admin.
/// Puede ser de un motorizado (repartidor) o de un restaurante que quiere
/// unirse al sistema de delivery de My-Negocio.
/// </summary>
public class SolicitudRegistro
{
    /// <summary>
    /// Identificador único de la solicitud (GUID generado automáticamente).
    /// </summary>
    [Key]
    public Guid SolicitudId { get; set; }

    /// <summary>
    /// Discriminador del tipo de solicitud: "motorizado" o "restaurante".
    /// Determina qué campos son relevantes y qué entidad se crea al aprobar.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string TipoSolicitud { get; set; }

    /// <summary>
    /// Estado de la solicitud: "pendiente", "aprobada", "rechazada".
    /// Default "pendiente" — el admin debe revisarla.
    /// </summary>
    [StringLength(20)]
    public string Estado { get; set; } = "pendiente";

    // ═══════════════════════════════════════════════════════════
    // DATOS COMUNES (motorizado y restaurante)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Nombre del solicitante (persona o contacto del restaurante).
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; }

    /// <summary>
    /// Apellido del solicitante.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Apellido { get; set; }

    /// <summary>
    /// Email del solicitante. Se usará como credencial de login al aprobar.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; }

    /// <summary>
    /// Contraseña pre-hasheada con BCrypt al momento del registro.
    /// Se copia directamente a la entidad Motorizado/Gym al aprobar.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Password { get; set; }

    /// <summary>
    /// Teléfono del solicitante para contacto.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Telefono { get; set; }

    // ═══════════════════════════════════════════════════════════
    // SOLO MOTORIZADO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Cédula de identidad del motorizado (10-13 dígitos Ecuador).
    /// Unique filtrado para solicitudes no rechazadas.
    /// </summary>
    [StringLength(15)]
    public string? Cedula { get; set; }

    /// <summary>
    /// Tipo de vehículo: "moto", "bicicleta", "auto".
    /// </summary>
    [StringLength(20)]
    public string? TipoVehiculo { get; set; }

    /// <summary>
    /// Placa del vehículo (si aplica).
    /// </summary>
    [StringLength(20)]
    public string? Placa { get; set; }

    /// <summary>
    /// Foto de la cédula frontal en Base64 data URI.
    /// </summary>
    public string? FotoCedulaFrontal { get; set; }

    /// <summary>
    /// Foto de la cédula trasera en Base64 data URI.
    /// </summary>
    public string? FotoCedulaTrasera { get; set; }

    /// <summary>
    /// Foto de la licencia de conducir en Base64 data URI.
    /// </summary>
    public string? FotoLicencia { get; set; }

    /// <summary>
    /// Selfie del motorizado en Base64 data URI.
    /// Se usa como FotoUrl del motorizado al aprobar.
    /// </summary>
    public string? FotoSelfie { get; set; }

    /// <summary>
    /// Foto del vehículo en Base64 data URI.
    /// </summary>
    public string? FotoVehiculo { get; set; }

    // ═══════════════════════════════════════════════════════════
    // SOLO RESTAURANTE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Nombre comercial del restaurante.
    /// </summary>
    [StringLength(200)]
    public string? NombreNegocio { get; set; }

    /// <summary>
    /// Nombre del dueño del restaurante.
    /// </summary>
    [StringLength(200)]
    public string? DuenoNegocio { get; set; }

    /// <summary>
    /// Dirección física del restaurante.
    /// </summary>
    [StringLength(500)]
    public string? Direccion { get; set; }

    /// <summary>
    /// Ciudad donde se ubica el restaurante.
    /// </summary>
    [StringLength(100)]
    public string? Ciudad { get; set; }

    /// <summary>
    /// Latitud GPS del restaurante.
    /// </summary>
    public double? Latitud { get; set; }

    /// <summary>
    /// Longitud GPS del restaurante.
    /// </summary>
    public double? Longitud { get; set; }

    /// <summary>
    /// Tipos de comida que ofrece el restaurante, separados por coma.
    /// Ejemplo: "Clasica,Almuerzos,Comida Rapida".
    /// </summary>
    [StringLength(500)]
    public string? TiposComida { get; set; }

    /// <summary>
    /// Logo del restaurante como Base64 data URI.
    /// </summary>
    public string? LogoUrl { get; set; }

    // ═══════════════════════════════════════════════════════════
    // REVISIÓN ADMIN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Motivo del rechazo (solo si Estado="rechazada").
    /// </summary>
    [StringLength(500)]
    public string? MotivoRechazo { get; set; }

    /// <summary>
    /// Fecha en que el admin revisó la solicitud (aprobó o rechazó).
    /// </summary>
    public DateTime? FechaRevision { get; set; }

    /// <summary>
    /// Fecha de creación de la solicitud.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;
}
