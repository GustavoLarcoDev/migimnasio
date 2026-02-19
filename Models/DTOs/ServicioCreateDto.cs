// ═══════════════════════════════════════════════════════════════════════════════
// ServicioCreateDto.cs
//
// ESTE DTO: recibe los datos del formulario "Agregar / Editar Servicio" del
// dashboard artesanal (ServiciosController → ServicioNegocioService).
//
// FLUJO: el dueño del negocio configura sus servicios (cortes, masajes, etc.)
// → llena el formulario → se envía por AJAX/POST → el controlador recibe este DTO
// → el servicio crea o actualiza el registro en la tabla ServiciosNegocio.
//
// MODO DUAL (crear / editar):
// ServicioId = Guid.Empty → INSERT de servicio nuevo.
// ServicioId con valor   → UPDATE del servicio existente.
//
// NOTA SOBRE ItemsIncluidos: se almacena como una sola cadena de texto
// con los ítems separados por el carácter "|". Ejemplo: "Lavado|Corte|Secado".
// En la vista se parte por "|" para mostrar los ítems como lista.
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para crear o editar un servicio ofrecido por el negocio artesanal.
/// Define el catálogo de servicios que los clientes pueden reservar mediante citas.
/// Cada servicio tiene nombre, descripción, duración, precio y puede ser un combo.
/// La duración (DuracionMinutos) es crítica porque determina cuánto tiempo bloquea
/// en el calendario del empleado cuando se agenda una cita de ese servicio.
/// Cuando ServicioId viene vacío (Guid.Empty), se crea un servicio nuevo.
/// Cuando ServicioId tiene valor, se actualiza el servicio existente.
/// </summary>
public class ServicioCreateDto
{
    /// <summary>
    /// Identificador único del servicio.
    /// Guid.Empty = crear servicio nuevo.
    /// Guid con valor = editar el servicio cuyo Id coincida en la base de datos.
    /// </summary>
    public Guid ServicioId { get; set; }

    /// <summary>
    /// Id del negocio dueño de este servicio. Obligatorio.
    /// Se envía como campo oculto en el formulario. El catálogo de servicios
    /// que aparece al crear citas está filtrado por NegocioId para que cada negocio
    /// solo vea y use sus propios servicios.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre del servicio. Obligatorio, máximo 200 caracteres.
    /// Es el nombre que el cliente verá al elegir su cita y el que aparece
    /// en el calendario, el historial de citas y los reportes de ingresos.
    /// Ejemplos: "Corte de Cabello", "Masaje Relajante 60min", "Manicure Básico".
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Nombre { get; set; }

    /// <summary>
    /// Descripción detallada del servicio. Opcional, máximo 1000 caracteres.
    /// Se muestra en el perfil del servicio para dar más información al cliente.
    /// Ejemplo: "Corte personalizado con lavado y secado incluidos".
    /// </summary>
    [MaxLength(1000)]
    public string Descripcion { get; set; }

    /// <summary>
    /// Lista de ítems o beneficios incluidos en el servicio. Opcional, máximo 2000 caracteres.
    /// FORMATO IMPORTANTE: los ítems se separan con el carácter "|" (pipe).
    /// Ejemplo de valor en BD: "Lavado|Corte|Secado|Peinado".
    /// En la vista se hace split por "|" para mostrar cada ítem como un punto de la lista.
    /// Este campo es especialmente útil para servicios de tipo combo (EsCombo = true).
    /// </summary>
    [MaxLength(2000)]
    public string ItemsIncluidos { get; set; }

    /// <summary>
    /// Duración del servicio en minutos. Por defecto 30 minutos.
    /// Este valor es fundamental para el calendario: cuando se agenda una cita de este
    /// servicio, el sistema bloquea FechaHoraInicio hasta FechaHoraInicio + DuracionMinutos
    /// en la agenda del empleado para evitar solapamientos con otras citas.
    /// Ejemplos comunes: 30 = corte rápido, 60 = masaje, 90 = tratamiento completo.
    /// </summary>
    public int DuracionMinutos { get; set; } = 30;

    /// <summary>
    /// Precio base del servicio. Obligatorio, debe ser mayor a 0.
    /// [Range(0.01, double.MaxValue)] garantiza que no se registre un precio
    /// de cero o negativo, lo que causaría inconsistencias en los reportes de ingresos.
    /// Es el monto que se carga al cliente al registrar el pago de la cita (PagoCitaDto).
    /// Se puede complementar con MontoExtra en el momento del pago.
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Precio { get; set; }

    /// <summary>
    /// Indica si este servicio es un combo (paquete de varios servicios juntos).
    /// True  = es un combo; se recomienda rellenar ItemsIncluidos con los servicios del paquete.
    /// False = servicio individual.
    /// La UI puede mostrar los combos de forma diferenciada (icono, sección separada)
    /// para que el cliente identifique fácilmente los paquetes disponibles.
    /// </summary>
    public bool EsCombo { get; set; }
}
