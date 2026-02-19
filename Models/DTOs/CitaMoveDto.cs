// ═══════════════════════════════════════════════════════════════════════════════
// CitaMoveDto.cs
//
// ESTE DTO: recibe los datos necesarios para mover (reprogramar) una cita
// existente a una nueva fecha y hora desde el calendario del dashboard artesanal.
//
// CASO DE USO: el dueño del negocio arrastra un evento del calendario a otro
// horario (drag & drop) o edita la hora manualmente. El frontend envía solo
// los tres campos necesarios para reubicar la cita.
//
// FLUJO: el usuario arrastra la cita en el calendario → el JavaScript captura
// la nueva fecha/hora → se envía por AJAX/POST → CitasController recibe este DTO
// → CitaService recalcula FechaHoraFin = NuevaFechaHoraInicio + DuracionMinutos
// → actualiza la cita en la base de datos.
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para mover (reprogramar) una cita a una nueva fecha y hora.
/// Es intencionalmente pequeño: solo contiene los tres campos que cambian
/// cuando se reprograma una cita. El resto de los datos de la cita (cliente,
/// empleado, servicio) no se modifican con esta operación.
/// </summary>
public class CitaMoveDto
{
    /// <summary>
    /// Id de la cita que se desea reprogramar. Obligatorio.
    /// El servicio lo usa para localizar la cita en la base de datos
    /// antes de aplicar el cambio de fecha y hora.
    /// Si no se encuentra la cita con este Id, la operación retorna un error.
    /// </summary>
    [Required]
    public Guid CitaId { get; set; }

    /// <summary>
    /// Id del negocio dueño de la cita. Obligatorio.
    /// Se valida en el servicio para garantizar que solo el negocio dueño de
    /// la cita pueda reprogramarla; previene que un negocio mueva citas de otro.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nueva fecha y hora de inicio a la que se reprograma la cita. Obligatorio.
    /// Se recibe en formato ISO 8601 desde el evento de drag & drop del calendario.
    /// Ejemplo: "2026-02-20T14:00:00".
    /// El servicio recalcula FechaHoraFin = NuevaFechaHoraInicio + DuracionMinutos
    /// del servicio, sin necesidad de que el frontend envíe la hora de fin.
    /// El servicio también valida disponibilidad del empleado en el nuevo horario.
    /// </summary>
    [Required]
    public DateTime NuevaFechaHoraInicio { get; set; }
}
