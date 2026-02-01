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
    /// Tipo de log: "ingreso" o "gasto"
    /// </summary>
    [MaxLength(50)]
    public string Tipo { get; set; } = "ingreso";
    
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