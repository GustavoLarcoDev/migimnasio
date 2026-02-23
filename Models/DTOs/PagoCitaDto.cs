// ═══════════════════════════════════════════════════════════════════════════════
// PagoCitaDto.cs
//
// ESTE DTO: recibe los datos del formulario "Registrar Pago" de una cita del
// dashboard artesanal (CitasController → CitaService → PagoCita en la BD).
//
// FLUJO: el empleado o dueño termina de atender una cita → abre el modal de pago
// → llena el método de pago, monto extra y propina opcionales → el sistema registra
// el pago y marca la cita como pagada (EstadoPago = "pagado").
//
// CONCEPTOS:
//   MontoBase  → precio del servicio (ya está en la cita, no se envía aquí).
//   MontoExtra → cargos adicionales (productos usados, extras del servicio).
//   Propina    → propina voluntaria para el empleado.
//   EsRegalo   → si la cita es cortesía/regalo, el monto total es $0.
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para registrar el pago de una cita completada.
/// Captura el método de pago, cargos extras opcionales, propina y si la cita
/// se ofrece como cortesía (regalo). El monto base del servicio ya está guardado
/// en la cita y no necesita enviarse de nuevo; el servicio calcula el total como:
/// Total = ServicioNegocio.Precio + MontoExtra + Propina (o 0 si EsRegalo = true).
/// </summary>
public class PagoCitaDto
{
    /// <summary>
    /// Id de la cita a la que se le registra el pago. Obligatorio.
    /// El servicio busca la cita con este Id para marcarla como pagada
    /// y crear el registro de PagoCita en la base de datos.
    /// Si la cita ya tiene un pago registrado, el servicio puede actualizar
    /// o rechazar el nuevo registro según la lógica del negocio.
    /// </summary>
    [Required]
    public Guid CitaId { get; set; }

    /// <summary>
    /// Id del negocio que registra el pago. Obligatorio.
    /// Se valida en el servicio para confirmar que la cita pertenece
    /// a este negocio antes de procesar el pago.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Forma de pago con la que el cliente pagó la cita. Obligatorio, máximo 100 caracteres.
    /// Valor por defecto: "Efectivo".
    /// Ahora soporta nombres dinamicos de metodos de pago configurados por el negocio
    /// (ej: "Banco Pichincha", "Tarjeta Visa", "Efectivo").
    /// Se registra en el historial de pagos y se muestra en los reportes de ingresos.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string MetodoPago { get; set; } = "Efectivo";

    /// <summary>
    /// Monto adicional cobrado aparte del precio base del servicio. Opcional, por defecto 0.
    /// Ejemplos de uso: productos utilizados durante el servicio, tratamientos extra,
    /// materiales adicionales, etc.
    /// El servicio suma este valor al precio base del servicio para calcular el total.
    /// </summary>
    public decimal MontoExtra { get; set; }

    /// <summary>
    /// Descripción del cargo extra registrado en MontoExtra. Opcional, máximo 500 caracteres.
    /// Permite al dueño del negocio saber qué se cobró adicionalmente.
    /// Solo es relevante cuando MontoExtra > 0.
    /// Ejemplo: "Tinte extra para cabello largo".
    /// </summary>
    [MaxLength(500)]
    public string DetalleExtra { get; set; }

    /// <summary>
    /// Propina que el cliente dejó al empleado. Opcional, por defecto 0.
    /// Se registra separada del precio del servicio para poder reportarla
    /// individualmente en los ingresos del empleado.
    /// No afecta el precio base del servicio; es un ingreso adicional.
    /// </summary>
    public decimal Propina { get; set; }

    /// <summary>
    /// Indica si la cita se ofrece como cortesía o regalo sin costo para el cliente.
    /// True  = la cita es gratis; el total registrado en PagoCita será $0 independientemente
    ///         del precio del servicio o del MontoExtra.
    /// False = la cita se cobra normalmente.
    /// Útil para citas de cortesía para clientes frecuentes o por corrección de errores.
    /// </summary>
    public bool EsRegalo { get; set; }

    /// <summary>
    /// Justificación o motivo por el cual la cita se ofrece como regalo. Opcional, máximo 500 caracteres.
    /// Solo es relevante cuando EsRegalo = true.
    /// Se registra en el historial para que el dueño del negocio pueda auditar
    /// las citas cortesía y sus razones.
    /// Ejemplo: "Cliente frecuente, 10ma visita de cortesía".
    /// </summary>
    [MaxLength(500)]
    public string MotivoRegalo { get; set; }
}
