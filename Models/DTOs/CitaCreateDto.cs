// ═══════════════════════════════════════════════════════════════════════════════
// CitaCreateDto.cs
//
// ESTE DTO: recibe los datos del formulario "Crear Cita" del calendario del
// dashboard artesanal (CitasController → CitaService).
//
// CASO DE USO: el dueño del negocio selecciona un cliente ya existente,
// elige un empleado, un servicio y un horario → se envía este DTO.
//
// DIFERENCIA CON CitaQuickCreateDto:
// Este DTO asume que el cliente YA EXISTE en el sistema (ClienteId es obligatorio).
// Para crear una cita y un cliente nuevo en un solo paso, usar CitaQuickCreateDto.
//
// CÁLCULO DE FechaHoraFin: el servicio calcula la hora de fin sumando
// ServicioNegocio.DuracionMinutos a FechaHoraInicio. No se envía desde el formulario.
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para crear una cita desde el formulario del calendario artesanal.
/// Requiere que el cliente, empleado y servicio ya existan en la base de datos.
/// La hora de fin de la cita se calcula automáticamente en el servicio según
/// la duración en minutos configurada para el servicio seleccionado.
/// </summary>
public class CitaCreateDto
{
    /// <summary>
    /// Id del negocio que crea la cita. Obligatorio.
    /// Se envía como campo oculto en el formulario para validar que todos los
    /// recursos seleccionados (cliente, empleado, servicio) pertenezcan a este negocio
    /// y evitar que un negocio cree citas usando datos de otro.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Id del cliente que asistirá a la cita. Obligatorio.
    /// El cliente debe existir previamente en el sistema. Si se necesita crear
    /// un cliente al mismo tiempo que la cita, usar CitaQuickCreateDto en su lugar.
    /// Se muestra en el evento del calendario y en el historial del cliente.
    /// </summary>
    [Required]
    public Guid ClienteId { get; set; }

    /// <summary>
    /// Id del empleado que atenderá la cita. Obligatorio.
    /// El servicio verifica que el empleado tenga horario disponible en la
    /// FechaHoraInicio solicitada antes de confirmar la cita.
    /// Se usa para bloquear ese slot en el calendario del empleado.
    /// </summary>
    [Required]
    public Guid EmpleadoId { get; set; }

    /// <summary>
    /// Id del servicio que se realizará en la cita. Obligatorio.
    /// Define la duración (DuracionMinutos) y el precio base de la cita.
    /// El servicio usa ServicioNegocio.DuracionMinutos para calcular FechaHoraFin
    /// automáticamente: FechaHoraFin = FechaHoraInicio + DuracionMinutos.
    /// </summary>
    [Required]
    public Guid ServicioId { get; set; }

    /// <summary>
    /// Fecha y hora en que comienza la cita. Obligatorio.
    /// Se recibe desde el calendario interactivo del frontend en formato ISO 8601.
    /// Ejemplo: "2026-02-18T10:30:00".
    /// El servicio valida que no exista otra cita del mismo empleado en ese horario
    /// antes de confirmar la reservación.
    /// </summary>
    [Required]
    public DateTime FechaHoraInicio { get; set; }
}
