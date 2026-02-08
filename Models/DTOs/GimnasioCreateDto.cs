using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class GimnasioCreateDto
{
    [Required]
    public string NombreGimnasio { get; set; }

    [Required]
    public string DuenoGimnasio { get; set; }

    [Required]
    [Phone]
    public string Telefono { get; set; }

    [Required]
    [EmailAddress]
    public string EmailGimnasio { get; set; }

    [Required]
    public string PasswordGimnasio { get; set; }

    public bool IsActive { get; set; }
    public bool EsPrueba { get; set; }
}
