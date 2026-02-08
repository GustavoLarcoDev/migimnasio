using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Notificacion
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid GimnasioId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Mensaje { get; set; }

    [MaxLength(50)]
    public string Tipo { get; set; } = "vencimiento";

    public Guid? ClienteId { get; set; }

    [MaxLength(200)]
    public string NombreCliente { get; set; }

    public bool Leida { get; set; } = false;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}
