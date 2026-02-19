// ═══════════════════════════════════════════════════════════════════════════════
// HorarioEmpleadoDto.cs
//
// ESTE DTO: recibe los datos del formulario "Configurar Horario" de un empleado
// desde el dashboard artesanal (EmpleadosController → EmpleadoService).
//
// ESTRUCTURA: contiene dos clases relacionadas:
//   HorarioEmpleadoDto  → el contenedor principal con el empleado y la lista de días.
//   HorarioDiaDto       → representa la configuración de UN día de la semana.
//
// FLUJO: el dueño configura los días y horas de trabajo de cada empleado en la UI
// → el formulario envía todos los días de la semana como un arreglo JSON → el
// servicio borra los horarios anteriores del empleado y los reemplaza con los nuevos.
//
// IMPORTANTE: DiaSemana usa la convención de .NET DayOfWeek:
//   0 = Domingo, 1 = Lunes, 2 = Martes, ..., 6 = Sábado.
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO contenedor para guardar o actualizar el horario semanal de un empleado.
/// Agrupa el identificador del empleado con la lista completa de sus días laborables.
/// El servicio reemplaza todos los registros de HorarioEmpleado existentes
/// para este empleado con los que lleguen en la lista Dias.
/// </summary>
public class HorarioEmpleadoDto
{
    /// <summary>
    /// Id del empleado cuyo horario se está configurando. Obligatorio.
    /// El servicio lo usa para borrar los horarios actuales del empleado
    /// y guardar la nueva configuración enviada en la lista Dias.
    /// </summary>
    [Required]
    public Guid EmpleadoId { get; set; }

    /// <summary>
    /// Id del negocio al que pertenece el empleado. Obligatorio.
    /// Se valida en el servicio para verificar que el empleado realmente
    /// pertenezca a este negocio antes de modificar su horario.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Lista de configuraciones por día de la semana (de domingo a sábado).
    /// Cada elemento de la lista es un HorarioDiaDto con el número de día,
    /// la hora de entrada, la hora de salida y si ese día está activo.
    /// Normalmente se envían los 7 días; los días con Activo = false se ignoran
    /// al validar disponibilidad para nuevas citas.
    /// Si la lista viene vacía, el empleado queda sin horario configurado.
    /// </summary>
    public List<HorarioDiaDto> Dias { get; set; } = new();
}

/// <summary>
/// DTO que representa la configuración de trabajo de un empleado para UN día específico.
/// Se usa dentro de HorarioEmpleadoDto como elemento de la lista de días.
/// Solo los días con Activo = true se consideran laborables al validar disponibilidad.
/// </summary>
public class HorarioDiaDto
{
    /// <summary>
    /// Número del día de la semana usando la convención de .NET (DayOfWeek):
    ///   0 = Domingo
    ///   1 = Lunes
    ///   2 = Martes
    ///   3 = Miércoles
    ///   4 = Jueves
    ///   5 = Viernes
    ///   6 = Sábado
    /// El servicio usa este valor para comparar contra la fecha de una nueva cita.
    /// </summary>
    public int DiaSemana { get; set; }

    /// <summary>
    /// Hora en que el empleado comienza a trabajar ese día. Formato "HH:mm" (24 horas).
    /// Valor por defecto: "09:00" (nueve de la mañana).
    /// El servicio rechaza citas cuya FechaHoraInicio sea antes de esta hora.
    /// Ejemplo: "08:30" para comenzar a las 8:30 AM.
    /// </summary>
    public string HoraInicio { get; set; } = "09:00";

    /// <summary>
    /// Hora en que el empleado termina de trabajar ese día. Formato "HH:mm" (24 horas).
    /// Valor por defecto: "17:00" (cinco de la tarde).
    /// El servicio rechaza citas cuya FechaHoraFin sea después de esta hora.
    /// Ejemplo: "19:00" para terminar a las 7:00 PM.
    /// </summary>
    public string HoraFin { get; set; } = "17:00";

    /// <summary>
    /// Indica si el empleado trabaja ese día de la semana.
    /// True  = día laborable; el empleado puede recibir citas en el rango HoraInicio–HoraFin.
    /// False = día libre; el sistema no permite agendar citas para el empleado en ese día.
    /// Ejemplo: Activo = false en Domingo y Lunes representa un empleado que no trabaja esos días.
    /// </summary>
    public bool Activo { get; set; }
}
