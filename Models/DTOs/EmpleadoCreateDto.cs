// ═══════════════════════════════════════════════════════════════════════════════
// EmpleadoCreateDto.cs
//
// ESTE DTO: recibe los datos del formulario "Agregar / Editar Empleado" del
// dashboard artesanal (EmpleadosController → EmpleadoService).
//
// FLUJO: el dueño del negocio llena el formulario de empleado → se envía por
// AJAX/POST → EmpleadosController recibe este DTO → EmpleadoService crea o
// actualiza el empleado en la tabla Empleados y lo asocia al negocio.
//
// MODO DUAL (crear / editar):
// EmpleadoId = Guid.Empty → INSERT de empleado nuevo.
// EmpleadoId con valor   → UPDATE del empleado existente.
//
// NOTA: los horarios del empleado se configuran por separado usando
// HorarioEmpleadoDto. Este DTO solo captura los datos básicos del perfil.
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para crear o editar un empleado del negocio artesanal.
/// Captura únicamente los datos básicos del perfil del empleado:
/// nombre, teléfono y especialidad.
/// Los horarios de trabajo del empleado se configuran después mediante
/// el endpoint de horarios, usando HorarioEmpleadoDto.
/// Cuando EmpleadoId viene vacío (Guid.Empty), se crea un empleado nuevo.
/// Cuando EmpleadoId tiene valor, se actualiza el empleado existente.
/// </summary>
public class EmpleadoCreateDto
{
    /// <summary>
    /// Identificador único del empleado.
    /// Guid.Empty = crear empleado nuevo.
    /// Guid con valor = editar el empleado cuyo Id coincida en la base de datos.
    /// </summary>
    public Guid EmpleadoId { get; set; }

    /// <summary>
    /// Id del negocio al que pertenece el empleado. Obligatorio.
    /// Se envía como campo oculto en el formulario para asociar el empleado
    /// al negocio correcto. El calendario de citas solo muestra los empleados
    /// que pertenezcan al negocio del usuario logueado.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre de pila del empleado. Obligatorio, máximo 100 caracteres.
    /// Se muestra en el selector de empleado al crear citas, en el calendario
    /// y en los reportes de ingresos por empleado.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; }

    /// <summary>
    /// Apellido del empleado. Obligatorio, máximo 100 caracteres.
    /// Se combina con Nombre para mostrar el nombre completo en la UI y en
    /// las notificaciones de cita enviadas al cliente.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Apellido { get; set; }

    /// <summary>
    /// Número de teléfono del empleado. Opcional, máximo 20 caracteres.
    /// Campo de contacto interno; no se comparte con los clientes automáticamente.
    /// Ejemplo: "+52 55 9876 5432".
    /// </summary>
    [MaxLength(20)]
    public string Telefono { get; set; }

    /// <summary>
    /// Correo electrónico del empleado. Opcional, máximo 200 caracteres.
    /// Si se proporciona, el empleado recibirá recordatorios de cita por email
    /// 30 minutos antes de cada cita asignada.
    /// Ejemplo: "ana.ramirez@gmail.com".
    /// </summary>
    [MaxLength(200)]
    public string Email { get; set; }

    /// <summary>
    /// Especialidad o área de trabajo del empleado. Opcional, máximo 100 caracteres.
    /// Se muestra en el perfil del empleado y puede usarse para filtrar empleados
    /// al asignar citas según el tipo de servicio.
    /// Ejemplos: "Corte de cabello", "Masajes terapéuticos", "Uñas acrílicas".
    /// </summary>
    [MaxLength(100)]
    public string Especialidad { get; set; }
}
