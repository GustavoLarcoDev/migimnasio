// ═══════════════════════════════════════════════════════════
// AdminLog.cs — Modelo de log de acciones administrativas
// Registra cada acción del admin: crear, editar, eliminar,
// cambiar estado e impersonar negocios
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class AdminLog
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Tipo de acción: "Crear", "Editar", "Eliminar", "CambiarEstado", "Impersonar"</summary>
    [Required]
    public string Accion { get; set; }

    /// <summary>Descripción detallada de la acción realizada</summary>
    public string Detalle { get; set; }

    /// <summary>Fecha y hora en que se realizó la acción</summary>
    public DateTime Fecha { get; set; } = DateTime.Now;

    /// <summary>Nombre del negocio afectado (null para acciones generales)</summary>
    public string NegocioAfectado { get; set; }
}
