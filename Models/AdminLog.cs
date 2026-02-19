// ═══════════════════════════════════════════════════════════
// AdminLog.cs — Modelo de auditoría de acciones del administrador
//
// Cada vez que el administrador realiza una acción importante
// (crear, editar, eliminar negocios, cambiar estados, impersonar),
// el sistema registra un AdminLog como pista de auditoría.
//
// Estos logs son inmutables: nunca se editan, solo se agregan.
// Permiten saber quién hizo qué y cuándo en el panel de admin.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Registro inmutable de una acción realizada por el administrador del sistema.
/// Sirve como pista de auditoría para saber el historial de cambios en la plataforma.
///
/// Acciones típicas: crear un negocio, editarlo, eliminarlo, activarlo/desactivarlo,
/// o impersonarlo (iniciar sesión como ese negocio para ver su dashboard).
/// </summary>
public class AdminLog
{
    /// <summary>
    /// Identificador único del log de auditoría (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla AdminLogs.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Categoría de la acción realizada por el admin.
    /// Valores predefinidos utilizados en el sistema:
    ///   "Crear"         = el admin creó un nuevo negocio o vendedor.
    ///   "Editar"        = el admin modificó los datos de un negocio.
    ///   "Eliminar"      = el admin eliminó un negocio de la plataforma.
    ///   "CambiarEstado" = el admin activó o desactivó un negocio (IsActive).
    ///   "Impersonar"    = el admin inició sesión como un negocio para revisar su panel.
    /// [Required] = el tipo de acción es obligatorio para que el log tenga sentido.
    /// [MaxLength(50)] = suficiente para los valores enumerados arriba.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Accion { get; set; }

    /// <summary>
    /// Texto descriptivo con el detalle completo de lo que se hizo.
    /// Ejemplo: "Se cambió el estado de 'Barbería El Rey' de Inactivo a Activo".
    /// Incluye nombres, valores anteriores y nuevos cuando aplica.
    /// [MaxLength(1000)] = suficiente para descripciones detalladas.
    /// </summary>
    [MaxLength(1000)]
    public string Detalle { get; set; }

    /// <summary>
    /// Fecha y hora exacta en que el admin realizó la acción.
    /// Se asigna automáticamente con TimeHelper.Now al crear el log.
    /// No se puede modificar después de registrado (inmutabilidad de auditoría).
    /// </summary>
    public DateTime Fecha { get; set; } = TimeHelper.Now;

    /// <summary>
    /// Nombre del negocio que fue afectado por la acción del admin.
    /// Se guarda como texto (denormalizado) para que el log conserve el nombre
    /// incluso si el negocio es eliminado en el futuro.
    /// Puede ser null para acciones generales no relacionadas a un negocio específico.
    /// [MaxLength(200)] = igual que el límite de NegocioNombre en la clase Gym.
    /// </summary>
    [MaxLength(200)]
    public string NegocioAfectado { get; set; }
}
