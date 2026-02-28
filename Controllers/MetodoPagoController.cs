using Gimnasio.Helpers;
using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

public class MetodoPagoController : NegocioBaseController
{
    private readonly IMetodoPagoService _service;

    public MetodoPagoController(IMetodoPagoService service, IAuthService authService)
        : base(authService) => _service = service;

    // ── Consultas ──

    [HttpGet("GetMetodosPago")]
    public Task<IActionResult> GetMetodosPago(Guid negocioId)
        => Execute(negocioId, async nId =>
        {
            var metodos = await _service.GetMetodosPagoAsync(nId);
            return Ok(metodos.Select(m => new
            {
                m.MetodoPagoId,
                m.Nombre,
                m.NumeroCuenta,
                m.Cedula,
                m.NombreTitular,
                ImagenQR = m.ImagenQR != null,
                m.Instrucciones,
                m.EsPredeterminado,
                m.Orden,
                FechaCreacion = m.FechaCreacion.ToString("dd/MM/yyyy")
            }));
        });

    [HttpGet("GetMetodoPago")]
    public Task<IActionResult> GetMetodoPago(Guid negocioId, Guid metodoPagoId)
        => Execute(negocioId, async nId =>
        {
            var metodo = await _service.GetMetodoPagoAsync(nId, metodoPagoId);
            if (metodo == null)
                return NotFound(new { success = false, message = "Metodo de pago no encontrado" });

            return Ok(new
            {
                metodo.MetodoPagoId,
                metodo.Nombre,
                metodo.NumeroCuenta,
                metodo.Cedula,
                metodo.NombreTitular,
                metodo.ImagenQR,
                metodo.Instrucciones,
                metodo.EsPredeterminado,
                metodo.Orden
            });
        });

    [HttpGet("GetPaymentStatsByMethod")]
    public Task<IActionResult> GetPaymentStatsByMethod(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _service.GetPaymentStatsByMethodAsync(nId)));

    // ── CRUD ──

    [HttpPost("CrearMetodoPago")]
    public Task<IActionResult> CrearMetodoPago([FromForm] MetodoPagoCreateDto dto)
        => Execute(dto.NegocioId, async nId =>
        {
            var r = await _service.CrearMetodoPagoAsync(nId, dto);
            return r.success
                ? Ok(new { r.success, r.message, r.metodoPagoId })
                : BadRequest(new { r.success, r.message });
        });

    [HttpPost("EditarMetodoPago")]
    public Task<IActionResult> EditarMetodoPago([FromForm] MetodoPagoCreateDto dto)
        => Execute(dto.NegocioId, async nId => ServiceResult(await _service.EditarMetodoPagoAsync(nId, dto)));

    [HttpPost("EliminarMetodoPago")]
    public Task<IActionResult> EliminarMetodoPago([FromForm] Guid negocioId, [FromForm] Guid metodoPagoId)
        => Execute(negocioId, async nId => ServiceResult(await _service.EliminarMetodoPagoAsync(nId, metodoPagoId)));

    // ── Imagen QR ──

    [HttpPost("SubirImagenQR")]
    public Task<IActionResult> SubirImagenQR(Guid negocioId, Guid metodoPagoId, IFormFile imagen)
        => Execute(negocioId, async nId =>
        {
            var (valid, error, dataUri) = await ImageUploadHelper.ProcessAsync(imagen, maxMb: 5);
            if (!valid)
                return BadRequest(new { success = false, message = error });

            return ServiceResult(await _service.SubirImagenQRAsync(nId, metodoPagoId, dataUri!));
        });

    [HttpPost("EliminarImagenQR")]
    public Task<IActionResult> EliminarImagenQR(Guid negocioId, Guid metodoPagoId)
        => Execute(negocioId, async nId => ServiceResult(await _service.SubirImagenQRAsync(nId, metodoPagoId, null!)));

    // ── Orden y predeterminado ──

    [HttpPost("ReordenarMetodosPago")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> ReordenarMetodosPago(Guid negocioId, [FromBody] List<Guid> orderedIds)
        => Execute(negocioId, async nId => ServiceResult(await _service.ReordenarMetodosPagoAsync(nId, orderedIds)));

    [HttpPost("SetMetodoPagoPredeterminado")]
    public Task<IActionResult> SetMetodoPagoPredeterminado(Guid negocioId, Guid metodoPagoId)
        => Execute(negocioId, async nId => ServiceResult(await _service.SetPredeterminadoAsync(nId, metodoPagoId)));
}
