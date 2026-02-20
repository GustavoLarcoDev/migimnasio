// ═══════════════════════════════════════════════════════════
// ComisionVendedor.cs — Modelo de comisión generada para un vendedor
//
// Cada vez que un vendedor crea un negocio que cumple los requisitos
// mínimos ($15+ de precio, 30+ días), se genera un registro de comisión.
//
// LÓGICA DE CÁLCULO:
//   - Base: $5 por los primeros 30 días
//   - Incremento: $2 adicionales por cada mes adicional
//   - Ejemplo: 30d=$5, 60d=$7, 90d=$9, 365d=$27
//
// ESTADOS:
//   - Pagada = false → comisión pendiente de pago al vendedor
//   - Pagada = true  → el admin ya transfirió el dinero al vendedor
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Registro de comisión generada para un vendedor por crear o renovar un negocio.
/// Cada vez que un vendedor crea un negocio que cumple los requisitos mínimos
/// ($15+ de precio, 30+ días), se genera un registro de comisión.
/// </summary>
public class ComisionVendedor
{
    [Key]
    public Guid Id { get; set; }

    public Guid VendedorId { get; set; }
    public Guid NegocioId { get; set; }

    [StringLength(200)]
    public string NombreVendedor { get; set; } = "";

    [StringLength(200)]
    public string NombreNegocio { get; set; } = "";

    /// <summary>Monto de la comisión en dólares</summary>
    public decimal MontoComision { get; set; }

    /// <summary>"nueva" = primera venta, "renovacion" = renovación de negocio existente</summary>
    [StringLength(20)]
    public string TipoComision { get; set; } = "nueva";

    public int DiasContratados { get; set; }
    public decimal PrecioNegocio { get; set; }

    /// <summary>false = pendiente de pago, true = ya pagada al vendedor</summary>
    public bool Pagada { get; set; } = false;

    /// <summary>Fecha en que el admin pagó esta comisión al vendedor</summary>
    public DateTime? FechaPago { get; set; }

    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;
}
