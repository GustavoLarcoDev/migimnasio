// ═══════════════════════════════════════════════════════════
// Logs.cs — Modelo de registro/log financiero
// Los logs son la fuente inmutable de verdad financiera.
// Cada acción sobre clientes genera un log automático,
// y los usuarios pueden crear logs manuales de ingresos/gastos.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Registro financiero inmutable. Eliminar un cliente no borra sus logs.
/// </summary>
public class Logs
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>ID del negocio dueño de este log</summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>Descripción del registro (ej: "Nuevo cliente registrado: Juan Pérez")</summary>
    [Required]
    [MaxLength(300)]
    public string Message { get; set; }

    /// <summary>Monto del log. Positivo = ingreso, Negativo = gasto</summary>
    public decimal Monto { get; set; } = 0;

    /// <summary>
    /// Tipo de log: "ingreso", "gasto", "cliente_creado", "cliente_editado",
    /// "cliente_renovado", "cliente_eliminado", "sesion_inicio", "sesion_cierre"
    /// </summary>
    [MaxLength(50)]
    public string Tipo { get; set; } = "ingreso";

    /// <summary>ID del cliente relacionado (si aplica)</summary>
    public Guid? ClienteId { get; set; }

    /// <summary>Nombre del cliente para referencia histórica (no se borra si el cliente se elimina)</summary>
    [MaxLength(200)]
    public string NombreCliente { get; set; }

    public DateTime Fecha { get; set; } = TimeHelper.Now;
}

/// <summary>
/// Modelo legacy para creación de logs desde formularios.
/// Para nuevos formularios usar LogCreateDto.
/// </summary>
public class LogCreateModel
{
    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Message { get; set; }

    [Required]
    public decimal Monto { get; set; }
}
