using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Cliente
{
    [Key]
    public Guid ClienteId { get; set; }
    
    [Required]
    public Guid GimnasioId { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; }

    [Required]
    [StringLength(100)]
    public string Apellido { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    public bool EsDiario { get; set; }

    public DateTime FechaDeCreacion { get; set; } = DateTime.Now;
    public DateTime FechaDeActualizacion { get; set; }

    public DateTime FechaQueTermina { get; set; }

    public int Dias { get; set; }
    public decimal Precio { get; set; }
}
