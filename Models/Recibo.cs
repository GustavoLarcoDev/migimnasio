using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Recibo
{
    [Key]
    public Guid ReciboId { get; set; }

    public int NumeroRecibo { get; set; }

    public Guid? NegocioId { get; set; }

    [StringLength(50)]
    public string TipoRecibo { get; set; } = "";

    [StringLength(200)]
    public string DestinatarioEmail { get; set; } = "";

    [StringLength(200)]
    public string DestinatarioNombre { get; set; } = "";

    [StringLength(200)]
    public string NegocioNombre { get; set; } = "";

    [StringLength(500)]
    public string Concepto { get; set; } = "";

    public decimal Monto { get; set; }

    public string ContenidoHtml { get; set; } = "";

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;
}
