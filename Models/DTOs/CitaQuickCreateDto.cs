// ═══════════════════════════════════════════════════════════════════════════════
// CitaQuickCreateDto.cs
//
// ESTE DTO: recibe los datos del formulario de "Cita Rápida" del calendario
// artesanal. Permite crear una cita Y un cliente nuevo en un solo paso,
// o agendar una cita para un cliente que ya existe.
//
// LÓGICA DE CLIENTE (manejada en CitaService):
//   Si ClienteId tiene valor → se usa el cliente existente; Nombre/Apellido/Telefono se ignoran.
//   Si ClienteId es null    → se crea un cliente nuevo con Nombre/Apellido/Telefono/Email.
//
// CASO DE USO TÍPICO: el teléfono suena, un cliente nuevo llama para agendar.
// El dueño no quiere salir del calendario a crear primero el cliente; con este
// formulario crea ambos registros de una sola vez.
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para crear una cita junto con un cliente nuevo en un solo paso (flujo rápido).
/// También permite agendar una cita para un cliente ya existente si se provee ClienteId.
/// El servicio decide qué camino tomar según si ClienteId viene con valor o nulo:
/// - ClienteId con valor → usa el cliente existente e ignora Nombre/Apellido/Telefono/Email.
/// - ClienteId null      → crea un cliente nuevo con los datos de nombre/teléfono/email
///                         y luego crea la cita para ese cliente recién creado.
/// </summary>
public class CitaQuickCreateDto
{
    /// <summary>
    /// Id del negocio que agenda la cita. Obligatorio.
    /// Se valida en el servicio para garantizar que el empleado y el servicio
    /// seleccionados pertenezcan a este negocio.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Id del empleado que atenderá la cita. Obligatorio.
    /// El servicio verifica la disponibilidad del empleado en el horario solicitado
    /// según su configuración de HorarioEmpleado antes de confirmar la cita.
    /// </summary>
    [Required]
    public Guid EmpleadoId { get; set; }

    /// <summary>
    /// Id del servicio que se realizará. Obligatorio.
    /// Determina la duración de la cita (DuracionMinutos) y el precio base.
    /// El servicio calcula FechaHoraFin = FechaHoraInicio + ServicioNegocio.DuracionMinutos.
    /// </summary>
    [Required]
    public Guid ServicioId { get; set; }

    /// <summary>
    /// Fecha y hora en que comienza la cita. Obligatorio.
    /// Se recibe en formato ISO 8601 desde el selector de fecha/hora del formulario.
    /// Ejemplo: "2026-02-18T11:00:00".
    /// </summary>
    [Required]
    public DateTime FechaHoraInicio { get; set; }

    /// <summary>
    /// Id del cliente existente a quien se le agenda la cita. Opcional.
    /// Si tiene valor → el servicio usa ese cliente; los campos Nombre, Apellido,
    ///                  Telefono y Email de este DTO son ignorados.
    /// Si es null     → el servicio crea un cliente nuevo usando Nombre/Apellido/Telefono/Email.
    /// Se puede obtener buscando el cliente por nombre/teléfono en el autocomplete del formulario.
    /// </summary>
    public Guid? ClienteId { get; set; }

    /// <summary>
    /// Nombre de pila del cliente nuevo. Opcional (obligatorio solo si ClienteId es null).
    /// Máximo 100 caracteres.
    /// Si ClienteId viene con valor, este campo se ignora aunque tenga contenido.
    /// </summary>
    [MaxLength(100)]
    public string Nombre { get; set; }

    /// <summary>
    /// Apellido del cliente nuevo. Opcional (obligatorio solo si ClienteId es null).
    /// Máximo 100 caracteres.
    /// Si ClienteId viene con valor, este campo se ignora aunque tenga contenido.
    /// </summary>
    [MaxLength(100)]
    public string Apellido { get; set; }

    /// <summary>
    /// Teléfono del cliente nuevo. Opcional, máximo 20 caracteres.
    /// Se usa para enviar recordatorios de la cita por WhatsApp.
    /// Solo se usa si ClienteId es null (creación de cliente nuevo).
    /// Ejemplo: "+52 55 1234 5678".
    /// </summary>
    [MaxLength(20)]
    public string Telefono { get; set; }

    /// <summary>
    /// Correo electrónico del cliente nuevo. Opcional, máximo 200 caracteres.
    /// [EmailAddress] valida que tenga formato correcto (usuario@dominio.com).
    /// Solo se procesa si ClienteId es null (creación de cliente nuevo).
    /// Se usa para enviar confirmaciones de cita si el negocio lo tiene configurado.
    /// </summary>
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; }
}
