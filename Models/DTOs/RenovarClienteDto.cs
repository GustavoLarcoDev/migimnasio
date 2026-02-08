using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class RenovarClienteDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid GimnasioId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Dias { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Precio { get; set; }
}
