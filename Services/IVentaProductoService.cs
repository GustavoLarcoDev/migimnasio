// ═══════════════════════════════════════════════════════════
// IVentaProductoService.cs — Contrato POS (Tienda)
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;

namespace Gimnasio.Services;

public interface IVentaProductoService
{
    Task<(bool success, string message, Guid? ordenId, Guid? reciboId)> RegistrarVentaAsync(
        Guid negocioId,
        string nombreCliente,
        string emailCliente,
        List<DetalleOrdenVentaDto> items,
        decimal descuentoAdicional,
        decimal porcentajeIva,
        string tipoOrden = "local",
        Guid? mesaId = null,
        Guid? empleadoId = null,
        string direccionEntrega = null,
        string metodoPago = "Efectivo",
        string numeroConfirmacion = null,
        List<CargoExtraDto> cargosExtra = null
    );

    Task<OrdenVenta> GetOrdenVentaAsync(Guid ordenVentaId, Guid negocioId);

    Task<List<OrdenVenta>> GetOrdenesVentaAsync(Guid negocioId);
}

public class DetalleOrdenVentaDto
{
    public Guid ProductoId { get; set; }
    public int Cantidad { get; set; }
}

public class CargoExtraDto
{
    public string Descripcion { get; set; }
    public decimal Monto { get; set; }
}

public class OrdenVentaCreateRequest
{
    public string NombreCliente { get; set; }
    public string EmailCliente { get; set; }
    public decimal DescuentoAdicional { get; set; }
    public decimal PorcentajeIva { get; set; }
    public List<DetalleOrdenVentaDto> Items { get; set; }
    public List<CargoExtraDto> CargosExtra { get; set; }

    // Campos adicionales para restaurante
    public string TipoOrden { get; set; } = "local";
    public Guid? MesaId { get; set; }
    public Guid? EmpleadoId { get; set; }
    public string DireccionEntrega { get; set; }

    // Metodo de pago
    public string MetodoPago { get; set; } = "Efectivo";
    public string NumeroConfirmacion { get; set; }
}
