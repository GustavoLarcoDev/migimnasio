using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Sugerencia
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    [MaxLength(200)]
    public string NegocioNombre { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Mensaje { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public bool Leida { get; set; } = false;
}
