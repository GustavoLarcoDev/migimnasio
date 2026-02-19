// ═══════════════════════════════════════════════════════════
// PagoCita.cs — Modelo de pago al completar una cita
//
// Cuando una cita cambia a estado "completada", el negocio registra
// el pago correspondiente creando un PagoCita.
//
// Relación 1:1 con Cita: cada cita completada tiene exactamente un PagoCita.
//
// El total se calcula como:
//   Total = MontoServicio + MontoExtra + Propina
//
// Casos especiales:
//   - Si EsRegalo = true, el Total puede ser 0 (servicio gratuito).
//   - MontoExtra cubre cargos opcionales (productos usados, trabajo adicional).
//   - Propina es un pago adicional voluntario del cliente al empleado.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Registra el pago de una cita completada en un negocio artesanal.
/// Se crea cuando el empleado termina el servicio y el dueño registra el cobro.
///
/// Relación uno-a-uno con Cita: una cita completada tiene exactamente un PagoCita.
/// Los datos de NombreCliente y NombreServicio se guardan aquí (denormalizados)
/// para que el historial de pagos sea consistente incluso si los registros cambian.
/// </summary>
public class PagoCita
{
    /// <summary>
    /// Identificador único del pago (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla PagosCita.
    /// </summary>
    [Key]
    public Guid PagoId { get; set; }

    /// <summary>
    /// ID de la cita que originó este pago.
    /// [Required] = todo pago debe estar vinculado a una cita.
    /// Relación uno-a-uno: un PagoCita corresponde a exactamente una Cita.
    /// </summary>
    [Required]
    public Guid CitaId { get; set; }

    /// <summary>
    /// ID del negocio al que pertenece este pago.
    /// Se guarda directamente para filtrar por negocio sin hacer JOIN.
    /// [Required] = obligatorio para el aislamiento multi-tenant.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Precio base del servicio realizado (en moneda local).
    /// Se toma del campo PrecioServicio de la Cita, que a su vez viene de ServicioNegocio.Precio.
    /// Ejemplo: 350.00 = $350 MXN por el servicio base.
    /// </summary>
    public decimal MontoServicio { get; set; }

    /// <summary>
    /// Cargos adicionales cobrados al cliente más allá del precio base del servicio.
    /// Puede ser 0 si no hay extras.
    /// Ejemplos: productos adicionales usados, trabajo extra no contemplado, accesorios.
    /// El detalle de estos cargos se especifica en DetalleExtra.
    /// </summary>
    public decimal MontoExtra { get; set; }

    /// <summary>
    /// Descripción de los cargos adicionales incluidos en MontoExtra.
    /// Solo aplica cuando MontoExtra > 0.
    /// Ejemplo: "Tinte de cabello extra" o "Productos de keratina".
    /// [MaxLength(500)] = suficiente para una descripción clara.
    /// </summary>
    [MaxLength(500)]
    public string DetalleExtra { get; set; }

    /// <summary>
    /// Propina voluntaria que el cliente dejó al empleado (en moneda local).
    /// Puede ser 0 si el cliente no dejó propina.
    /// Se suma al total pero es un campo separado para llevar estadísticas.
    /// </summary>
    public decimal Propina { get; set; }

    /// <summary>
    /// Monto total cobrado al cliente por la cita completa (en moneda local).
    /// Se calcula como: MontoServicio + MontoExtra + Propina.
    /// Ejemplo: 350 + 50 + 20 = $420 MXN total.
    /// Si EsRegalo = true, este campo puede ser 0 (sin cobro al cliente).
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Forma de pago utilizada por el cliente.
    /// Valores válidos:
    ///   "efectivo"      = el cliente pagó en billetes/monedas.
    ///   "tarjeta"       = pago con tarjeta de débito o crédito.
    ///   "transferencia" = pago por transferencia bancaria o app de pago (ej: CoDi, SPEI).
    /// [Required] = siempre se debe registrar el método de pago.
    /// [MaxLength(20)] = suficiente para los valores enumerados.
    /// Valor por defecto: "efectivo" (el más común en negocios artesanales).
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string MetodoPago { get; set; } = "efectivo";

    /// <summary>
    /// Indica si el servicio fue realizado como regalo (sin costo para el cliente).
    /// true  = servicio gratis (el negocio absorbe el costo); Total puede ser 0.
    ///         Se debe indicar el motivo en MotivoRegalo.
    /// false = servicio cobrado normalmente.
    /// Útil para registrar cortesías a clientes frecuentes o promociones.
    /// </summary>
    public bool EsRegalo { get; set; }

    /// <summary>
    /// Razón por la que el servicio se otorgó como regalo.
    /// Solo aplica cuando EsRegalo = true; en otros casos es null.
    /// Ejemplo: "Cliente frecuente - décima visita gratis" o "Cortesía del empleado".
    /// [MaxLength(500)] = suficiente para una explicación.
    /// </summary>
    [MaxLength(500)]
    public string MotivoRegalo { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DATOS DENORMALIZADOS (copias de datos para historial)
    // Se guardan aquí para que el historial de pagos sea consistente
    // aunque el nombre del cliente o servicio cambie en el futuro.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Nombre completo del cliente al momento del pago (ej: "Juan Pérez").
    /// Se copia desde Cliente.Nombre + Cliente.Apellido al registrar el pago.
    /// Permite mostrar el historial de cobros sin hacer JOIN con la tabla de clientes.
    /// [MaxLength(200)] = suficiente para nombre + apellido completos.
    /// </summary>
    [MaxLength(200)]
    public string NombreCliente { get; set; }

    /// <summary>
    /// Nombre del servicio realizado al momento del pago (ej: "Manicure Gel").
    /// Se copia desde ServicioNegocio.Nombre al registrar el pago.
    /// Permite mostrar el historial de cobros sin hacer JOIN con la tabla de servicios.
    /// [MaxLength(200)] = igual al límite del nombre del servicio.
    /// </summary>
    [MaxLength(200)]
    public string NombreServicio { get; set; }

    /// <summary>
    /// Fecha y hora en que se registró el pago en el sistema.
    /// Se asigna automáticamente con TimeHelper.Now al crear el registro.
    /// Sirve como referencia para reportes financieros por día/semana/mes.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties de Entity Framework)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Cita a la que corresponde este pago.
    /// Relación uno-a-uno: un PagoCita → una Cita (y viceversa).
    /// EF Core carga este objeto solo cuando se hace .Include(p => p.Cita).
    /// </summary>
    public Cita Cita { get; set; }
}
