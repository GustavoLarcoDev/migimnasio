using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Cita/appointment del modelo artesanal.
/// Estados: "pendiente" | "confirmada" | "en_progreso" | "completada" | "cancelada"
/// </summary>
public class Cita
{
    [Key]
    public Guid CitaId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    public Guid ClienteId { get; set; }

    [Required]
    public Guid EmpleadoId { get; set; }

    [Required]
    public Guid ServicioId { get; set; }

    // Datos denormalizados para consultas rapidas
    [MaxLength(200)]
    public string NombreCliente { get; set; }

    [MaxLength(200)]
    public string NombreEmpleado { get; set; }

    [MaxLength(200)]
    public string NombreServicio { get; set; }

    public decimal PrecioServicio { get; set; }

    // Fecha y hora
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public int DuracionMinutos { get; set; }

    /// <summary>Estado: "pendiente" | "confirmada" | "en_progreso" | "completada" | "cancelada"</summary>
    [Required]
    [MaxLength(20)]
    public string Estado { get; set; } = "pendiente";

    /// <summary>Motivo de cancelacion (obligatorio si estado = cancelada)</summary>
    [MaxLength(500)]
    public string MotivoCancelacion { get; set; }

    /// <summary>True si ya se envio el recordatorio WhatsApp</summary>
    public bool RecordatorioEnviado { get; set; }

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;
    public DateTime FechaDeActualizacion { get; set; }

    // Relaciones
    public Gym Negocio { get; set; }
    public Cliente Cliente { get; set; }
    public Empleado Empleado { get; set; }
    public ServicioNegocio Servicio { get; set; }
    public PagoCita Pago { get; set; }
}
