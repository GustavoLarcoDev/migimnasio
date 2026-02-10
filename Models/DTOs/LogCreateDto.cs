using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class LogCreateDto
{
    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Message { get; set; }

    [Required]
    public decimal Monto { get; set; }
}
