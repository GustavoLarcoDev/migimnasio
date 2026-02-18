using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Vendedor
{
    [Key]
    public Guid VendedorId { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; }

    [Required]
    [StringLength(100)]
    public string Apellido { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Correo { get; set; }

    [Required]
    [Phone]
    [StringLength(20)]
    public string Telefono { get; set; }

    [Required]
    [MaxLength(200)]
    public string Password { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    public int NegociosCreados { get; set; } = 0;
}
