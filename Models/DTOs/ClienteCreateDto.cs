// ═══════════════════════════════════════════════════════════════════════════════
// ClienteCreateDto.cs
//
// ¿QUÉ ES UN DTO?
// DTO significa "Data Transfer Object" (Objeto de Transferencia de Datos).
// Su propósito es recibir y validar los datos que vienen del formulario HTML
// antes de que lleguen al servicio o a la base de datos.
//
// ¿POR QUÉ USAMOS DTOs EN VEZ DE PASAR EL MODELO DIRECTAMENTE?
// 1. Seguridad: evita "over-posting" (que el usuario envíe campos que no debe tocar).
// 2. Validación: los atributos [Required], [MaxLength], etc. corren automáticamente.
// 3. Separación de capas: el modelo de BD puede cambiar sin afectar el formulario.
//
// ESTE DTO: recibe los datos del formulario "Agregar / Editar Cliente" del dashboard.
// Soporta dos modos de cálculo de fin de membresía:
//   Modo 1 — FechaFin explícita (tiene prioridad si viene con valor).
//   Modo 2 — FechaInicio + Dias (se calcula FechaFin = FechaInicio + Dias).
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

/// <summary>
/// DTO para crear o editar un cliente desde el formulario del dashboard.
/// Recibe y valida todos los datos personales y de membresía del cliente.
/// Cuando ClienteId viene vacío (Guid.Empty), se interpreta como creación;
/// cuando trae un Guid válido, se interpreta como edición del cliente existente.
/// </summary>
public class ClienteCreateDto
{
    /// <summary>
    /// Identificador único del cliente.
    /// En modo creación este valor es Guid.Empty (vacío).
    /// En modo edición contiene el Id del cliente que se va a actualizar.
    /// El servicio usa este campo para decidir si hace INSERT o UPDATE.
    /// </summary>
    public Guid ClienteId { get; set; }

    /// <summary>
    /// Id del negocio al que pertenece el cliente.
    /// Obligatorio. Se obtiene de la sesión del usuario logueado y se envía
    /// como campo oculto (hidden) en el formulario para asociar el cliente
    /// al negocio correcto en la base de datos.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    // ───────────────────────────────────────────────────────────────────────
    // DATOS PERSONALES
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Nombre de pila del cliente. Obligatorio, máximo 100 caracteres.
    /// Se combina con Apellido para mostrar el nombre completo en la tabla
    /// de clientes, reportes y notificaciones de WhatsApp.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; }

    /// <summary>
    /// Apellido del cliente. Obligatorio, máximo 100 caracteres.
    /// Se combina con Nombre para formar el nombre completo.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Apellido { get; set; }

    /// <summary>
    /// Correo electrónico del cliente. Opcional.
    /// [EmailAddress] verifica que tenga formato válido (ej. usuario@dominio.com).
    /// Se usa para enviar recordatorios y comunicaciones del negocio.
    /// Máximo 200 caracteres.
    /// </summary>
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; }

    /// <summary>
    /// Número de teléfono del cliente. Obligatorio, máximo 20 caracteres.
    /// [Phone] valida que sea un número telefónico con formato reconocible.
    /// Se usa como canal principal de contacto por WhatsApp.
    /// Ejemplo: "+52 55 1234 5678" o "5551234567".
    /// </summary>
    [Required]
    [Phone]
    [MaxLength(20)]
    public string Telefono { get; set; }

    /// <summary>
    /// Dirección del cliente. Opcional, máximo 500 caracteres.
    /// Campo libre de texto para registrar domicilio u observaciones de ubicación.
    /// </summary>
    [MaxLength(500)]
    public string Direccion { get; set; }

    /// <summary>
    /// Indica si el cliente paga por día en lugar de tener una membresía mensual.
    /// True = cliente diario (se cobra por cada visita).
    /// False = cliente con membresía (se cobra por el período completo).
    /// Afecta cómo se calcula el vencimiento y qué campos del formulario se muestran.
    /// </summary>
    public bool EsDiario { get; set; }

    // ───────────────────────────────────────────────────────────────────────
    // MEMBRESÍA
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fecha de inicio de la membresía. Opcional.
    /// Si viene null, el servicio usa DateTime.Now como fecha de inicio.
    /// Se envía desde el datepicker del formulario en formato ISO 8601.
    /// </summary>
    public DateTime? FechaInicio { get; set; }

    /// <summary>
    /// Fecha de fin explícita de la membresía. Opcional.
    /// Si viene con valor, tiene prioridad sobre el campo Dias.
    /// Permite al administrador fijar una fecha exacta de vencimiento sin
    /// importar cuántos días sean.
    /// </summary>
    public DateTime? FechaFin { get; set; }

    /// <summary>
    /// Duración de la membresía en días. Se usa solo cuando FechaFin es null.
    /// El servicio calcula FechaFin = FechaInicio + Dias días.
    /// Ejemplo: 30 = membresía mensual, 365 = membresía anual.
    /// </summary>
    public int Dias { get; set; }

    /// <summary>
    /// Monto cobrado por la membresía o por el día. Obligatorio, debe ser mayor a 0.
    /// [Range(0.01, double.MaxValue)] garantiza que no se guarde un precio de cero
    /// o negativo, lo que evitaría inconsistencias en los reportes de ingresos.
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal Precio { get; set; }
}
