using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Logs
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid GimnasioId { get; set; }
    
    [Required]
    [MaxLength(300)]
    public string Message { get; set; }
    
    /// <summary>
    /// Monto del log. Positivo = ingreso, Negativo = gasto
    /// </summary>
    public decimal Monto { get; set; } = 0;
    
    /// <summary>
    /// Tipo de log: "ingreso", "gasto", "cliente_creado", "cliente_editado", "cliente_renovado", "cliente_eliminado"
    /// </summary>
    [MaxLength(50)]
    public string Tipo { get; set; } = "ingreso";
    
    /// <summary>
    /// ID del cliente relacionado (si aplica)
    /// </summary>
    public Guid? ClienteId { get; set; }
    
    /// <summary>
    /// Nombre del cliente relacionado (para referencia histórica)
    /// </summary>
    [MaxLength(200)]
    public string NombreCliente { get; set; }
    
    public DateTime Fecha { get; set; } = DateTime.Now;
}

public class LogCreateModel
{
    [Required]
    public Guid GimnasioId { get; set; }
    
    [Required]
    [MaxLength(300)]
    public string Message { get; set; }
    
    [Required]
    public decimal Monto { get; set; }
}