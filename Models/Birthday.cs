using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class BirthdayComida
{
    [Key]
    public Guid ComidaId { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    public string? FotoUrl { get; set; }

    public bool EsGringo { get; set; }

    public bool IsActive { get; set; } = true;
}

public class BirthdayRegalo
{
    [Key]
    public Guid RegaloId { get; set; }

    [Required]
    [StringLength(200)]
    public string Nombre { get; set; } = "";

    public string? FotoUrl { get; set; }

    public string? Link { get; set; }

    public bool Claimed { get; set; }

    public bool IsActive { get; set; } = true;
}

public class BirthdayRsvp
{
    [Key]
    public Guid RsvpId { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    public bool PlusOne { get; set; }

    [StringLength(100)]
    public string? PlusOneNombre { get; set; }

    public string? ComidasJson { get; set; }

    public string? Extra { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
