using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Pago de una cita completada. 1 pago por cita (relacion 1:1).
/// </summary>
public class PagoCita
{
    [Key]
    public Guid PagoId { get; set; }

    [Required]
    public Guid CitaId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>Precio base del servicio</summary>
    public decimal MontoServicio { get; set; }

    /// <summary>Cargos adicionales</summary>
    public decimal MontoExtra { get; set; }

    /// <summary>Detalle de los cargos extra</summary>
    [MaxLength(500)]
    public string DetalleExtra { get; set; }

    /// <summary>Propina</summary>
    public decimal Propina { get; set; }

    /// <summary>Total = MontoServicio + MontoExtra + Propina (0 si regalo)</summary>
    public decimal Total { get; set; }

    /// <summary>Metodo de pago: "efectivo" | "tarjeta" | "transferencia"</summary>
    [Required]
    [MaxLength(20)]
    public string MetodoPago { get; set; } = "efectivo";

    /// <summary>True si el servicio fue un regalo</summary>
    public bool EsRegalo { get; set; }

    /// <summary>Motivo del regalo (obligatorio si EsRegalo)</summary>
    [MaxLength(500)]
    public string MotivoRegalo { get; set; }

    // Datos denormalizados
    [MaxLength(200)]
    public string NombreCliente { get; set; }

    [MaxLength(200)]
    public string NombreServicio { get; set; }

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    // Relaciones
    public Cita Cita { get; set; }
}
