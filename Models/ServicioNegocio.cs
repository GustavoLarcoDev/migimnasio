// ═══════════════════════════════════════════════════════════
// ServicioNegocio.cs — Modelo de servicio ofrecido por un negocio artesanal
//
// Representa cada uno de los servicios que un negocio tiene en su catálogo.
// Ejemplos para una barbería: "Corte de cabello", "Rasurado", "Corte + Barba".
// Ejemplos para un spa de uñas: "Manicure clásico", "Manicure gel", "Pedicure".
//
// Este modelo aplica SOLO para negocios con TipoNegocio = "artesanal".
//
// Características del catálogo de servicios:
//   - EsCombo = true indica que es un paquete de varios servicios juntos.
//   - ItemsIncluidos lista los componentes del combo separados por "|".
//   - DuracionMinutos se usa para calcular la disponibilidad del empleado en la agenda.
//   - El soft delete (IsActive = false) oculta el servicio sin borrar el historial de citas.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa un servicio del catálogo de un negocio artesanal.
/// Cada servicio tiene un precio, una duración y puede ser un servicio individual o un combo.
///
/// Solo existe en negocios con TipoNegocio = "artesanal".
/// Los servicios se asignan a las citas (Cita.ServicioId) para definir qué se realizará.
/// </summary>
public class ServicioNegocio
{
    /// <summary>
    /// Identificador único del servicio (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla ServiciosNegocio.
    /// </summary>
    [Key]
    public Guid ServicioId { get; set; }

    /// <summary>
    /// ID del negocio al que pertenece este servicio.
    /// [Required] = todo servicio debe pertenecer a un negocio.
    /// Se usa para filtrar servicios por negocio (aislamiento multi-tenant).
    /// Relación muchos-a-uno: muchos servicios → un negocio.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre del servicio que verá el cliente en la agenda y en el menú.
    /// Ejemplos: "Corte de cabello", "Manicure Gel", "Masaje relajante 60 min".
    /// [Required] = el nombre es obligatorio para identificar el servicio.
    /// [MaxLength(200)] = suficiente para cualquier nombre descriptivo.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Nombre { get; set; }

    /// <summary>
    /// Descripción detallada del servicio para el cliente (opcional).
    /// Puede incluir qué está incluido, materiales usados, o instrucciones previas.
    /// Ejemplo: "Corte de cabello personalizado según la forma del rostro. Incluye lavado."
    /// [MaxLength(1000)] = suficiente para una descripción completa.
    /// </summary>
    [MaxLength(1000)]
    public string Descripcion { get; set; }

    /// <summary>
    /// Lista de los elementos incluidos en el servicio, separados por el caracter "|".
    /// Solo aplica cuando EsCombo = true o cuando el servicio tiene pasos enumerables.
    /// Ejemplo: "Limado|Cuticula|Esmaltado base|Color|Top coat".
    /// Se puede mostrar como lista con viñetas en el frontend separando por "|".
    /// [MaxLength(2000)] = suficiente para una lista larga de ítems.
    /// </summary>
    [MaxLength(2000)]
    public string ItemsIncluidos { get; set; }

    /// <summary>
    /// Tiempo que tarda en realizarse el servicio, expresado en minutos.
    /// Se usa para:
    ///   1. Calcular la hora de fin de la cita (FechaHoraFin = FechaHoraInicio + DuracionMinutos).
    ///   2. Verificar que el empleado tenga ese bloque de tiempo libre en su agenda.
    ///   3. Mostrar la duración estimada al cliente al agendar.
    /// Ejemplo: 30 = media hora, 60 = una hora, 90 = hora y media.
    /// Valor por defecto: 30 minutos.
    /// </summary>
    public int DuracionMinutos { get; set; } = 30;

    /// <summary>
    /// Precio del servicio (en moneda local, ej: pesos mexicanos).
    /// [Required] = todo servicio debe tener un precio definido (puede ser 0 para servicios gratis).
    /// Se copia a Cita.PrecioServicio y PagoCita.MontoServicio al agendar/pagar.
    /// Ejemplo: 350.00 = $350 MXN.
    /// </summary>
    [Required]
    public decimal Precio { get; set; }

    /// <summary>
    /// Indica si el servicio es un combo (paquete) de varios servicios incluidos.
    /// true  = es un paquete; ItemsIncluidos lista sus componentes.
    ///         Ejemplo: "Combo Novias" que incluye manicure + pedicure + maquillaje.
    /// false = servicio individual (la mayoría de los servicios).
    /// Se puede usar en el frontend para mostrar un ícono o etiqueta "COMBO".
    /// </summary>
    public bool EsCombo { get; set; }

    /// <summary>
    /// Indica si el servicio está disponible para nuevas citas.
    /// true  = servicio activo (aparece en el catálogo al agendar citas).
    /// false = servicio dado de baja (soft delete; no aparece en nuevas citas,
    ///         pero las citas históricas que lo usaron se conservan intactas).
    /// El dueño puede desactivar servicios descontinuados sin perder el historial.
    /// Valor por defecto: true.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Fecha y hora en que el servicio fue agregado al catálogo del negocio.
    /// Se asigna automáticamente con TimeHelper.Now al crear el registro.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    /// <summary>
    /// Fecha y hora de la última modificación del servicio.
    /// Se actualiza cuando el dueño edita el nombre, precio, duración, etc.
    /// </summary>
    public DateTime FechaDeActualizacion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties de Entity Framework)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Negocio al que pertenece este servicio.
    /// Relación muchos-a-uno: muchos servicios → un negocio.
    /// EF Core carga este objeto solo cuando se hace .Include(s => s.Negocio).
    /// </summary>
    public Gym Negocio { get; set; }

    /// <summary>
    /// Lista de todas las citas en las que se ha usado este servicio.
    /// Relación uno-a-muchos: un servicio puede haber sido usado en muchas citas.
    /// EF Core carga esta lista solo cuando se hace .Include(s => s.Citas).
    /// Útil para reportes de popularidad de servicios o para impedir eliminar
    /// un servicio que tiene citas futuras pendientes.
    /// </summary>
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
