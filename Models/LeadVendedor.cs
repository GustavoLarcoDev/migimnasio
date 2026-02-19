// ═══════════════════════════════════════════════════════════
// LeadVendedor.cs — Modelo de prospecto/lead capturado por un vendedor
//
// Un "lead" es una empresa o persona que mostró interés en la plataforma
// y que un vendedor está tratando de convertir en cliente (negocio).
//
// Flujo típico:
//   1. Un prospecto llena un formulario de contacto en la landing page.
//   2. Se crea un LeadVendedor con Atendido = false.
//   3. Un vendedor revisa el lead y lo marca como atendido (Atendido = true).
//   4. Si el prospecto acepta, el vendedor crea un Negocio en la plataforma.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa un prospecto o "lead" de ventas: una empresa o persona interesada
/// en contratar la plataforma MiNegocio.
///
/// Los leads son gestionados por los vendedores desde su panel de control.
/// Un lead atendido puede convertirse en un negocio registrado (clase Gym).
/// </summary>
public class LeadVendedor
{
    /// <summary>
    /// Identificador único del lead (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla LeadsVendedor en la base de datos.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Nombre del contacto o representante del negocio prospecto.
    /// Ejemplo: "Carlos Martínez" (la persona que llenó el formulario).
    /// [Required] = campo obligatorio para poder hacer seguimiento.
    /// [MaxLength(120)] = suficiente para nombres completos.
    /// </summary>
    [Required, MaxLength(120)]
    public string Nombre { get; set; } = "";

    /// <summary>
    /// Nombre comercial del negocio prospecto que podría convertirse en cliente.
    /// Ejemplo: "Barbería Los Compadres" o "Spa Relajación Total".
    /// [Required] = obligatorio para identificar a qué negocio pertenece el lead.
    /// [MaxLength(150)] = suficiente para nombres de negocios.
    /// </summary>
    [Required, MaxLength(150)]
    public string NombreNegocio { get; set; } = "";

    /// <summary>
    /// Correo electrónico del contacto. Es el canal principal de seguimiento.
    /// [Required] = obligatorio para poder responder al prospecto.
    /// [MaxLength(180)] = límite estándar para emails con algo de margen.
    /// </summary>
    [Required, MaxLength(180)]
    public string Email { get; set; } = "";

    /// <summary>
    /// Número de teléfono del contacto (opcional).
    /// Se puede usar para llamar o enviar WhatsApp al prospecto.
    /// [MaxLength(40)] = más amplio que lo normal para aceptar extensiones o formatos internacionales.
    /// </summary>
    [MaxLength(40)]
    public string Telefono { get; set; }

    /// <summary>
    /// Mensaje libre que el prospecto escribió al contactar (opcional).
    /// Puede incluir preguntas, necesidades específicas o contexto del negocio.
    /// [MaxLength(1200)] = suficiente para un mensaje detallado.
    /// </summary>
    [MaxLength(1200)]
    public string Mensaje { get; set; }

    /// <summary>
    /// Indica si un vendedor ya revisó y atendió este lead.
    /// false = lead nuevo, pendiente de atención (aparece en la bandeja del vendedor).
    /// true  = lead ya contactado o descartado.
    /// Valor por defecto: false (recién creado, sin atender).
    /// </summary>
    public bool Atendido { get; set; } = false;

    /// <summary>
    /// ID del vendedor que marcó este lead como atendido.
    /// Es nullable (?) porque el lead puede estar sin atender (Atendido = false).
    /// Se asigna cuando el vendedor toma el lead y hace seguimiento.
    /// </summary>
    public Guid? AtendidoPorId { get; set; }

    /// <summary>
    /// Nombre del vendedor que atendió el lead (denormalizado).
    /// Se guarda como texto para mostrarlo en el historial sin hacer join.
    /// Es null mientras el lead no ha sido atendido.
    /// </summary>
    public string AtendidoPorNombre { get; set; }

    /// <summary>
    /// Fecha y hora en que el prospecto envió el formulario de contacto.
    /// Se asigna automáticamente con TimeHelper.Now al crear el registro.
    /// Permite ordenar leads por recientes y medir tiempos de respuesta.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;
}
