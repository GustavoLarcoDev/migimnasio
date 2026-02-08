using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class ClienteCreateDto
{
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
    public string Email { get; set; }

    [Required]
    [Phone]
    public string Telefono { get; set; }

    public string Direccion { get; set; }

    public bool EsDiario { get; set; }

    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    public int Dias { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal Precio { get; set; }
}
