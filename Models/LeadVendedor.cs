#nullable enable
using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class LeadVendedor
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(120)]
    public string Nombre { get; set; } = "";

    [Required, MaxLength(150)]
    public string NombreNegocio { get; set; } = "";

    [Required, MaxLength(180)]
    public string Email { get; set; } = "";

    [MaxLength(40)]
    public string? Telefono { get; set; }

    [MaxLength(1200)]
    public string? Mensaje { get; set; }

    public bool Atendido { get; set; } = false;

    public Guid? AtendidoPorId { get; set; }

    public string? AtendidoPorNombre { get; set; }

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;
}
