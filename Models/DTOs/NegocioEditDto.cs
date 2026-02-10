using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class NegocioEditDto
{
    [Required]
    public Guid NegocioId { get; set; }

    [Required]
    public string NegocioNombre { get; set; }

    [Required]
    public string DuenoNegocio { get; set; }

    [Required]
    [Phone]
    public string Telefono { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    public string Password { get; set; }

    public bool IsActive { get; set; }
    public bool EsPrueba { get; set; }
}
