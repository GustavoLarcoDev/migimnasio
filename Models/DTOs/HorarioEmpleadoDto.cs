// ═══════════════════════════════════════════════════════════
// HorarioEmpleadoDto.cs — DTO para guardar horarios de empleados
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs;

public class HorarioEmpleadoDto
{
    [Required]
    public Guid EmpleadoId { get; set; }

    [Required]
    public Guid NegocioId { get; set; }

    public List<HorarioDiaDto> Dias { get; set; } = new();
}

public class HorarioDiaDto
{
    /// <summary>0=Domingo, 1=Lunes, ..., 6=Sabado</summary>
    public int DiaSemana { get; set; }

    public string HoraInicio { get; set; } = "09:00";
    public string HoraFin { get; set; } = "17:00";
    public bool Activo { get; set; }
}
