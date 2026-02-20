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
        decimal porcentajeIva
    );

    Task<OrdenVenta> GetOrdenVentaAsync(Guid ordenVentaId, Guid negocioId);

    Task<List<OrdenVenta>> GetOrdenesVentaAsync(Guid negocioId);
}

public class DetalleOrdenVentaDto
{
    public Guid ProductoId { get; set; }
    public int Cantidad { get; set; }
}

public class OrdenVentaCreateRequest
{
    public string NombreCliente { get; set; }
    public string EmailCliente { get; set; }
    public decimal DescuentoAdicional { get; set; }
    public decimal PorcentajeIva { get; set; }
    public List<DetalleOrdenVentaDto> Items { get; set; }
}
