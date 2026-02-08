using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class GimnasioEditDto
{
    [Required]
    public Guid GimnasioId { get; set; }

    [Required]
    public string GimnasioNombre { get; set; }

    [Required]
    public string DuenoGimnasio { get; set; }

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
