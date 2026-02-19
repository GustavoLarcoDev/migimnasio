// ═══════════════════════════════════════════════════════════
// Cliente.cs — Modelo de cliente de un negocio
//
// Un cliente pertenece a un negocio específico (multi-tenant).
// Para negocios de tipo "membresias", el cliente tiene fecha de vencimiento
// y cantidad de días. Para negocios "artesanal", el cliente tiene citas.
//
// Este archivo también contiene ClienteCreateModel, un modelo legacy
// para formularios anteriores al sistema DTO.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa un cliente registrado en un negocio de la plataforma MiNegocio.
/// Es la entidad principal del modelo de membresías.
///
/// Un cliente siempre pertenece a un único negocio (definido por NegocioId).
/// El acceso multi-tenant se garantiza filtrando siempre por NegocioId en las consultas.
/// </summary>
public class Cliente
{
    /// <summary>
    /// Identificador único del cliente (GUID generado automáticamente).
    /// [Key] = indica a EF Core que esta es la clave primaria de la tabla.
    /// </summary>
    [Key]
    public Guid ClienteId { get; set; }

    /// <summary>
    /// ID del negocio al que pertenece este cliente.
    /// [Required] = todo cliente debe estar asociado a un negocio; no puede ser nulo.
    /// Se usa para filtrar clientes por negocio (aislamiento multi-tenant).
    /// Relación muchos-a-uno: muchos clientes → un negocio.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DATOS PERSONALES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Nombre(s) del cliente, por ejemplo: "Juan" o "María José".
    /// [Required] = campo obligatorio para registrar al cliente.
    /// [StringLength(100)] = límite en base de datos; 100 caracteres es suficiente para nombres.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; }

    /// <summary>
    /// Apellido(s) del cliente, por ejemplo: "Pérez" o "García López".
    /// [Required] = campo obligatorio.
    /// [StringLength(100)] = límite de 100 caracteres.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Apellido { get; set; }

    /// <summary>
    /// Correo electrónico del cliente (opcional).
    /// Se puede usar para enviar recordatorios o comprobantes.
    /// [EmailAddress] = valida el formato (ej: "juan@gmail.com").
    /// [MaxLength(200)] = límite estándar para emails.
    /// </summary>
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; }

    /// <summary>
    /// Número de teléfono del cliente (opcional).
    /// Se usa principalmente para enviar mensajes de WhatsApp con recordatorios de vencimiento.
    /// [Phone] = valida que sea un número telefónico válido.
    /// [MaxLength(20)] = suficiente para números con código de país (ej: "+52 55 1234 5678").
    /// </summary>
    [Phone]
    [MaxLength(20)]
    public string Telefono { get; set; }

    /// <summary>
    /// Dirección física del cliente (opcional).
    /// Campo informativo; no se usa en lógica de negocio actual.
    /// [MaxLength(500)] = suficiente para una dirección completa.
    /// </summary>
    [MaxLength(500)]
    public string Direccion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // MEMBRESÍA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Indica si el cliente paga por visita individual en lugar de una membresía fija.
    /// true  = cliente diario (paga cada vez que viene, sin membresía mensual).
    /// false = cliente con membresía (paga un periodo fijo de días).
    /// Los clientes diarios no tienen fecha de vencimiento relevante.
    /// </summary>
    public bool EsDiario { get; set; }

    /// <summary>
    /// Fecha y hora en que el cliente fue registrado o en que inició su membresía actual.
    /// Se asigna automáticamente con TimeHelper.Now al crear el registro.
    /// No se modifica al renovar; para eso existe FechaDeActualizacion.
    /// </summary>
    public DateTime FechaDeCreacion { get; set; } = TimeHelper.Now;

    /// <summary>
    /// Fecha y hora de la última modificación del registro del cliente.
    /// Se actualiza cada vez que se editan los datos o se renueva la membresía.
    /// </summary>
    public DateTime FechaDeActualizacion { get; set; }

    /// <summary>
    /// Fecha en que vence la membresía del cliente.
    /// Cuando esta fecha pasa, el cliente aparece como "vencido" en el dashboard.
    /// El sistema genera notificaciones automáticas 3 días antes de esta fecha.
    /// Se calcula como: FechaDeCreacion + Dias.
    /// </summary>
    public DateTime FechaQueTermina { get; set; }

    /// <summary>
    /// Duración de la membresía en días.
    /// Ejemplo: 30 = membresía mensual, 90 = trimestral, 365 = anual.
    /// Se usa para calcular FechaQueTermina al registrar o renovar.
    /// </summary>
    public int Dias { get; set; }

    /// <summary>
    /// Precio cobrado por la membresía actual (en moneda local).
    /// Ejemplo: 500.00 = $500 MXN por el periodo.
    /// Se registra en los logs financieros del negocio al crear o renovar.
    /// </summary>
    public decimal Precio { get; set; }

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties de Entity Framework)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Lista de citas agendadas por este cliente (solo aplica para TipoNegocio = "artesanal").
    /// Relación uno-a-muchos: un cliente puede tener muchas citas a lo largo del tiempo.
    /// EF Core carga esta lista solo cuando se hace .Include(c => c.Citas).
    /// </summary>
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}

/// <summary>
/// Modelo legacy para la creación de clientes desde formularios anteriores al sistema DTO.
/// Se usa en algunos formularios HTML directamente en lugar de ClienteCreateDto.
///
/// Para nuevos formularios o endpoints de API, usar ClienteCreateDto en Models/DTOs/.
/// Este modelo tiene validaciones más estrictas ([Required] en más campos).
/// </summary>
public class ClienteCreateModel
{
    /// <summary>
    /// ID del cliente (se ignora al crear; EF Core genera un nuevo GUID automáticamente).
    /// </summary>
    public Guid ClienteId { get; set; }

    /// <summary>
    /// ID del negocio al que pertenecerá este cliente.
    /// [Required] = obligatorio; no se puede crear un cliente sin negocio.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre(s) del cliente.
    /// [Required] = campo obligatorio en el formulario.
    /// [StringLength(100)] = máximo 100 caracteres.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; }

    /// <summary>
    /// Apellido(s) del cliente.
    /// [Required] = campo obligatorio en el formulario.
    /// [StringLength(100)] = máximo 100 caracteres.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Apellido { get; set; }

    /// <summary>
    /// Correo electrónico del cliente (opcional en este modelo).
    /// [EmailAddress] = si se ingresa, debe tener formato válido.
    /// </summary>
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// Teléfono del cliente.
    /// [Required] = obligatorio en este modelo legacy (a diferencia de la entidad Cliente).
    /// [Phone] = valida el formato telefónico.
    /// </summary>
    [Required]
    [Phone]
    public string Telefono { get; set; }

    /// <summary>
    /// Dirección física del cliente (opcional).
    /// </summary>
    public string Direccion { get; set; }

    /// <summary>
    /// Indica si el cliente paga por día en lugar de membresía.
    /// true = cliente diario, false = membresía por periodo.
    /// </summary>
    public bool EsDiario { get; set; }

    /// <summary>
    /// Duración de la membresía en días.
    /// [Required] = obligatorio.
    /// [Range(1, int.MaxValue)] = debe ser al menos 1 día; muestra mensaje de error si es 0 o negativo.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Los días deben ser mayor a 0")]
    public int Dias { get; set; }

    /// <summary>
    /// Precio cobrado por la membresía.
    /// [Required] = obligatorio.
    /// [Range(0.01, double.MaxValue)] = debe ser mayor a cero; no se permiten membresías gratis por este modelo.
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal Precio { get; set; }
}
