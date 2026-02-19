// ═══════════════════════════════════════════════════════════
// Sugerencia.cs — Modelo de sugerencia o feedback de un negocio al admin
//
// Permite que los dueños de negocios envíen comentarios, ideas de mejora
// o reportes de problemas directamente al administrador de la plataforma.
//
// Flujo:
//   1. El dueño del negocio escribe y envía una sugerencia desde su dashboard.
//   2. Se crea una Sugerencia con Leida = false.
//   3. El administrador la ve en su panel y la marca como leída (Leida = true).
//
// Limitación de 1000 caracteres en Mensaje para mantener los mensajes concisos.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa un mensaje de feedback o sugerencia enviado por un negocio al administrador.
/// Es el canal de comunicación del negocio hacia la plataforma.
///
/// Solo el administrador puede ver y gestionar las sugerencias desde su panel.
/// Los negocios solo pueden enviarlas; no pueden ver las sugerencias de otros negocios.
/// </summary>
public class Sugerencia
{
    /// <summary>
    /// Identificador único de la sugerencia (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla Sugerencias.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// ID del negocio que envió la sugerencia.
    /// [Required] = toda sugerencia debe venir de un negocio identificado.
    /// Se usa para que el admin sepa de qué negocio viene el comentario.
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Nombre del negocio que envió la sugerencia (denormalizado).
    /// Se guarda como texto para que el admin pueda ver el nombre sin hacer JOIN.
    /// Esto es importante si el negocio cambia su nombre después de enviar la sugerencia.
    /// [Required] = se requiere para identificar al remitente.
    /// [MaxLength(200)] = igual al límite de NegocioNombre en la clase Gym.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string NegocioNombre { get; set; }

    /// <summary>
    /// Contenido de la sugerencia, comentario o reporte de problema.
    /// Puede incluir ideas de nuevas funcionalidades, reportes de bugs, o quejas.
    /// [Required] = el mensaje es el campo principal; sin él la sugerencia no tiene sentido.
    /// [MaxLength(1000)] = límite para mantener mensajes concisos y legibles.
    ///                     1000 caracteres son suficientes para ~170 palabras.
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Mensaje { get; set; }

    /// <summary>
    /// Fecha y hora en que el negocio envió la sugerencia.
    /// Se asigna automáticamente con TimeHelper.Now al crear el registro.
    /// Permite al admin ordenar sugerencias por más recientes.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    /// <summary>
    /// Indica si el administrador ya revisó esta sugerencia.
    /// false = sugerencia nueva, pendiente de revisión (aparece en el panel del admin).
    /// true  = sugerencia ya leída y procesada por el admin.
    /// El admin puede marcarla como leída después de revisarla.
    /// Valor por defecto: false.
    /// </summary>
    public bool Leida { get; set; } = false;
}
