// ═══════════════════════════════════════════════════════════════════════════════
// ClienteArtesanalCreateDto.cs
//
// ESTE DTO: recibe los datos del formulario "Agregar / Editar Cliente" para
// negocios del modelo artesanal (peluquerías, spas, consultorios, etc.).
//
// DIFERENCIA CON ClienteCreateDto:
// Los negocios artesanales NO manejan membresías periódicas. Sus clientes
// simplemente agendan citas y pagan por servicio, por lo que este DTO
// omite todos los campos de membresía (FechaInicio, FechaFin, Dias, Precio)
// y solo captura los datos de contacto del cliente.
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para crear o editar un cliente en un negocio de modelo artesanal.
/// Solo incluye datos personales de contacto; no maneja membresías ni pagos
/// periódicos porque los negocios artesanales cobran por cita/servicio individual.
/// Cuando ClienteId viene vacío (Guid.Empty), se crea un cliente nuevo.
/// Cuando ClienteId tiene valor, se actualiza el cliente existente.
/// </summary>
public class ClienteArtesanalCreateDto
{
    /// <summary>
    /// Identificador único del cliente.
    /// Guid.Empty = crear nuevo cliente.
    /// Guid con valor = editar el cliente cuyo Id coincida.
    /// </summary>
    public Guid ClienteId { get; set; }

    /// <summary>
    /// Id del negocio artesanal al que pertenece el cliente.
    /// Obligatorio. Se envía como campo oculto en el formulario para
    /// garantizar que el cliente quede asociado al negocio correcto.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre de pila del cliente. Obligatorio, máximo 100 caracteres.
    /// Se muestra en el calendario de citas y en el historial del cliente.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; }

    /// <summary>
    /// Apellido del cliente. Obligatorio, máximo 100 caracteres.
    /// Se combina con Nombre para mostrar el nombre completo en la UI.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Apellido { get; set; }

    /// <summary>
    /// Correo electrónico del cliente. Opcional, máximo 200 caracteres.
    /// [EmailAddress] valida que tenga formato correcto (usuario@dominio.com).
    /// Se usa para enviar confirmaciones de cita si el negocio lo configura.
    /// </summary>
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; }

    /// <summary>
    /// Número de teléfono del cliente. Opcional, máximo 20 caracteres.
    /// [Phone] valida que sea un número telefónico reconocible.
    /// Principal canal de contacto para recordatorios de cita por WhatsApp.
    /// Ejemplo: "+52 55 1234 5678".
    /// </summary>
    [Phone]
    [MaxLength(20)]
    public string Telefono { get; set; }

    /// <summary>
    /// Dirección del cliente. Opcional, máximo 500 caracteres.
    /// Campo de texto libre para registrar domicilio o referencias de ubicación.
    /// Útil para negocios que ofrecen servicio a domicilio.
    /// </summary>
    [MaxLength(500)]
    public string Direccion { get; set; }
}
