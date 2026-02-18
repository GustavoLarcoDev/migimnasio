// ═══════════════════════════════════════════════════════════
// Negocio.cs — Modelo principal de negocio (clase Gym)
// Representa un negocio registrado en la plataforma con sus
// datos de contacto, estado de suscripción y lista de clientes
// ═══════════════════════════════════════════════════════════

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gimnasio.Models;

public class Gym
{
    // ═══════════════════════════════════════════════════════════
    // DATOS BÁSICOS
    // ═══════════════════════════════════════════════════════════

    [Key]
    public Guid NegocioId { get; set; }

    /// <summary>Nombre del negocio</summary>
    [Required]
    [MaxLength(200)]
    public string NegocioNombre { get; set; }

    /// <summary>Nombre del dueño del negocio</summary>
    [MaxLength(200)]
    public string DuenoNegocio { get; set; }

    [Phone]
    [MaxLength(20)]
    public string Telefono { get; set; }

    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; }

    /// <summary>Contraseña hasheada con BCrypt (o texto plano pre-migración)</summary>
    [MaxLength(200)]
    public string Password { get; set; }

    // ═══════════════════════════════════════════════════════════
    // ESTADO
    // ═══════════════════════════════════════════════════════════

    /// <summary>True = negocio de pago activo</summary>
    public bool IsActive { get; set; }

    /// <summary>True = negocio en periodo de prueba</summary>
    public bool EsPrueba { get; set; }

    // ═══════════════════════════════════════════════════════════
    // TIPO DE NEGOCIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>Tipo de negocio: "membresias" (default) o "artesanal"</summary>
    [MaxLength(20)]
    public string TipoNegocio { get; set; } = "membresias";

    // ═══════════════════════════════════════════════════════════
    // FECHAS
    // ═══════════════════════════════════════════════════════════

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;
    public DateTime FechaDeActualizacion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // SUSCRIPCIÓN
    // ═══════════════════════════════════════════════════════════

    /// <summary>Cantidad de días que tiene pagados</summary>
    public int DiasPagados { get; set; } = 30;

    /// <summary>Precio mensual de la suscripción</summary>
    public decimal PrecioSuscripcion { get; set; } = 0;

    /// <summary>Fecha del último pago de suscripción</summary>
    public DateTime? FechaPago { get; set; }

    /// <summary>Fecha en que expira la suscripción</summary>
    public DateTime? FechaExpiracion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // VENDEDOR (quién vendió este negocio)
    // ═══════════════════════════════════════════════════════════

    /// <summary>ID del vendedor que registró este negocio (null = admin directo)</summary>
    public Guid? VendedorId { get; set; }

    // ═══════════════════════════════════════════════════════════
    // RELACIONES
    // ═══════════════════════════════════════════════════════════

    /// <summary>Lista de clientes registrados en este negocio</summary>
    public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();

    /// <summary>Empleados del negocio (modelo artesanal)</summary>
    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    /// <summary>Servicios ofrecidos por el negocio (modelo artesanal)</summary>
    public ICollection<ServicioNegocio> Servicios { get; set; } = new List<ServicioNegocio>();

    /// <summary>Citas/appointments del negocio (modelo artesanal)</summary>
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
