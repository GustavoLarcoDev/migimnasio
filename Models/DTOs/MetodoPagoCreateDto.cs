using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.DTOs
{
    public class MetodoPagoCreateDto
    {
        public Guid MetodoPagoId { get; set; }

        public Guid NegocioId { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(50)]
        public string NumeroCuenta { get; set; }

        [StringLength(20)]
        public string Cedula { get; set; }

        [StringLength(200)]
        public string NombreTitular { get; set; }

        [StringLength(500)]
        public string Instrucciones { get; set; }

        public bool EsPredeterminado { get; set; } = false;

        public int Orden { get; set; } = 0;
    }
}
