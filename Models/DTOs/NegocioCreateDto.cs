using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class NegocioCreateDto
{
    [Required]
    public string NombreNegocio { get; set; }

    [Required]
    public string DuenoNegocio { get; set; }

    [Required]
    [Phone]
    public string Telefono { get; set; }

    [Required]
    [EmailAddress]
    public string EmailNegocio { get; set; }

    [Required]
    public string PasswordNegocio { get; set; }

    public bool IsActive { get; set; }
    public bool EsPrueba { get; set; }
}
