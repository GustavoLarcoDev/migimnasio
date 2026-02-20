// ═══════════════════════════════════════════════════════════
// Negocio.cs — Modelo principal del negocio registrado en la plataforma
//
// Esta es la entidad central del sistema multi-tenant. Cada "negocio"
// es un cliente de MiNegocio: puede ser una barbería, un spa, un gimnasio, etc.
// El negocio tiene sus propios clientes, empleados, servicios y citas.
//
// NOTA HISTÓRICA: La clase se llama "Gym" porque el sistema nació como
// una app para gimnasios y fue renombrado a "Negocio" sin cambiar la clase.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gimnasio.Models;

/// <summary>
/// Representa un negocio registrado en la plataforma MiNegocio.
/// Cada negocio es un tenant independiente con su propio panel de control,
/// clientes, empleados y servicios.
///
/// Nota: el nombre de la clase es "Gym" por razones históricas; en la base de datos
/// la tabla y las relaciones ya usan el nombre "Negocio".
/// </summary>
public class Gym
{
    // ═══════════════════════════════════════════════════════════
    // DATOS BÁSICOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Identificador único del negocio.
    /// Es un GUID generado automáticamente por SQL Server al insertar el registro.
    /// Se usa como clave primaria ([Key]) y como referencia en todas las tablas relacionadas.
    /// </summary>
    [Key]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre comercial del negocio, por ejemplo: "Barbería El Rey" o "Gym Power House".
    /// [Required] = no puede guardarse en blanco.
    /// [MaxLength(200)] = límite en base de datos para evitar strings muy largos.
    /// Se muestra en el dashboard y en la lista de negocios del admin.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string NegocioNombre { get; set; }

    /// <summary>
    /// Nombre del dueño o propietario del negocio (persona física, no la empresa).
    /// Ejemplo: "Carlos Pérez".
    /// [MaxLength(200)] = límite en la base de datos.
    /// </summary>
    [MaxLength(200)]
    public string DuenoNegocio { get; set; }

    /// <summary>
    /// Número de teléfono de contacto del negocio.
    /// [Phone] = valida que el formato sea un número telefónico válido.
    /// [MaxLength(20)] = suficiente para teléfonos con código de país (ej: "+52 55 1234 5678").
    /// </summary>
    [Phone]
    [MaxLength(20)]
    public string Telefono { get; set; }

    /// <summary>
    /// Correo electrónico del negocio. También sirve como usuario de login.
    /// [EmailAddress] = valida que tenga formato de email (ej: "mitienda@gmail.com").
    /// [MaxLength(200)] = límite razonable para emails.
    /// </summary>
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; }

    /// <summary>
    /// Contraseña del negocio para iniciar sesión en la plataforma.
    /// Se almacena hasheada con BCrypt (algoritmo de encriptación seguro).
    /// Los negocios creados antes de la migración BCrypt pueden tener contraseña
    /// en texto plano; el sistema la convierte automáticamente en el próximo login.
    /// [MaxLength(200)] = los hashes BCrypt tienen ~60 caracteres, pero se deja margen.
    /// </summary>
    [MaxLength(200)]
    public string Password { get; set; }

    // ═══════════════════════════════════════════════════════════
    // ESTADO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Indica si el negocio tiene una suscripción activa de pago.
    /// true = puede acceder a la plataforma con todas las funciones.
    /// false = cuenta suspendida o cancelada (el admin puede reactivarla).
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Indica si el negocio está en periodo de prueba gratuita.
    /// true = cuenta de prueba (puede tener acceso limitado o temporal).
    /// false = cuenta de producción normal.
    /// Un negocio puede ser EsPrueba=true e IsActive=true al mismo tiempo.
    /// </summary>
    public bool EsPrueba { get; set; }

    // ═══════════════════════════════════════════════════════════
    // TIPO DE NEGOCIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Define el modelo de negocio y qué funcionalidades están disponibles.
    /// Valores posibles:
    ///   "membresias" (default) = negocios de membresías recurrentes, como gimnasios.
    ///                            Los clientes pagan mensualmente y tienen fecha de vencimiento.
    ///   "artesanal"            = negocios de servicios por cita, como barberías, spas o salones
    ///                            de belleza. Manejan empleados, agenda de citas y pagos por servicio.
    /// [MaxLength(20)] = suficiente para los valores "membresias" y "artesanal".
    /// </summary>
    [MaxLength(20)]
    public string TipoNegocio { get; set; } = "membresias";

    /// <summary>
    /// Dirección física del negocio. Opcional.
    /// Usado en restaurantes para delivery y en recibos.
    /// </summary>
    [MaxLength(500)]
    public string Direccion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // FECHAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Fecha y hora en que el negocio fue registrado en la plataforma.
    /// Se asigna automáticamente usando TimeHelper.Now (zona horaria local configurada).
    /// No se modifica después de la creación.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    /// <summary>
    /// Fecha y hora de la última modificación del registro del negocio.
    /// Se actualiza manualmente en el servicio cada vez que se edita el negocio.
    /// </summary>
    public DateTime FechaDeActualizacion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // SUSCRIPCIÓN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Número de días que el negocio tiene contratados en su suscripción actual.
    /// Ejemplo: 30 = suscripción mensual estándar, 365 = suscripción anual.
    /// El valor por defecto es 30 días.
    /// </summary>
    public int DiasPagados { get; set; } = 30;

    /// <summary>
    /// Precio mensual que paga el negocio por usar la plataforma (en moneda local).
    /// Ejemplo: 299.00 = $299 MXN por mes.
    /// El valor por defecto es 0 (cuentas de prueba o sin precio asignado).
    /// </summary>
    public decimal PrecioSuscripcion { get; set; } = 0;

    /// <summary>
    /// Fecha en que el negocio realizó su último pago de suscripción.
    /// Es nullable (?) porque un negocio de prueba puede no haber pagado nunca.
    /// Se usa para calcular cuándo vence la suscripción.
    /// </summary>
    public DateTime? FechaPago { get; set; }

    /// <summary>
    /// Fecha en que vence la suscripción actual del negocio.
    /// Es nullable (?) porque puede no estar definida en cuentas de prueba.
    /// Cuando esta fecha pasa, el negocio queda suspendido (IsActive = false).
    /// </summary>
    public DateTime? FechaExpiracion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // VENDEDOR (quién vendió este negocio)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// ID del vendedor que registró y vendió este negocio en la plataforma.
    /// Es nullable (?) porque algunos negocios los crea el admin directamente (sin vendedor).
    /// Se usa para calcular comisiones y estadísticas por vendedor.
    /// Relación: muchos Gym → uno Vendedor (muchos negocios pueden ser de un mismo vendedor).
    /// </summary>
    public Guid? VendedorId { get; set; }

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties de Entity Framework)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Lista de todos los clientes registrados en este negocio.
    /// Relación uno-a-muchos: un negocio tiene muchos clientes.
    /// EF Core carga esta lista solo cuando se hace .Include(n => n.Clientes).
    /// Se inicializa como lista vacía para evitar NullReferenceException.
    /// </summary>
    public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();

    /// <summary>
    /// Lista de empleados del negocio (solo aplica para TipoNegocio = "artesanal").
    /// Relación uno-a-muchos: un negocio tiene muchos empleados.
    /// Ejemplos: manicuristas, estilistas, barberos.
    /// </summary>
    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    /// <summary>
    /// Lista de servicios que ofrece el negocio (solo para TipoNegocio = "artesanal").
    /// Relación uno-a-muchos: un negocio tiene muchos servicios.
    /// Ejemplos: "Corte de cabello", "Manicure", "Pedicure".
    /// </summary>
    public ICollection<ServicioNegocio> Servicios { get; set; } = new List<ServicioNegocio>();

    /// <summary>
    /// Lista de citas/appointments del negocio (solo para TipoNegocio = "artesanal").
    /// Relación uno-a-muchos: un negocio tiene muchas citas.
    /// Cada cita vincula un cliente con un empleado y un servicio en una fecha y hora.
    /// </summary>
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
