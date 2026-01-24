using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gimnasio.Models;

public class Gym
{
    [Key]
    public Guid GimnasioId { get; set; }

    [Required]
    public string GimnasioNombre { get; set; }

    public string DuenoGimnasio { get; set; }

    [Phone]
    public string Telefono { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    public string Password { get; set; }

    public bool IsActive { get; set; }
    public bool EsPrueba { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime FechaDeActualizacion { get; set; }

    // Lista de clientes
    public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
}
