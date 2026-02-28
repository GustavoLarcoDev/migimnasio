using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

public class PropagandaController : NegocioBaseController
{
    private readonly IPropagandaService _propagandaService;

    public PropagandaController(IPropagandaService propagandaService, IAuthService authService)
        : base(authService)
    {
        _propagandaService = propagandaService;
    }

    [HttpGet("GetDisenos")]
    public Task<IActionResult> GetDisenos(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _propagandaService.GetDisenosAsync(nId)));

    [HttpGet("GetDisenosEditor")]
    public async Task<IActionResult> GetDisenosEditor()
    {
        var nId = AuthService.GetNegocioId(User);
        if (!nId.HasValue) return Ok(new List<object>());
        return Ok(await _propagandaService.GetDisenosAsync(nId.Value));
    }

    [HttpGet("GetDiseno")]
    public Task<IActionResult> GetDiseno(Guid disenoId)
        => ExecuteSelf(async nId =>
        {
            var diseno = await _propagandaService.GetDisenoAsync(disenoId, nId);
            if (diseno == null) return NotFound(new { success = false, message = "Diseno no encontrado" });
            return Ok(new
            {
                diseno.DisenoId,
                diseno.Nombre,
                diseno.CanvasJson,
                diseno.ThumbnailDataUri,
                diseno.Ancho,
                diseno.Alto,
                diseno.FechaCreacion,
                diseno.FechaModificacion
            });
        });

    [HttpPost("CrearDiseno")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> CrearDiseno([FromBody] CrearDisenoRequest req)
        => ExecuteSelf(async nId => ServiceResult(
            await _propagandaService.CrearDisenoAsync(nId, req.Nombre, req.CanvasJson, req.ThumbnailDataUri, req.Ancho, req.Alto)));

    [HttpPost("GuardarDiseno")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> GuardarDiseno([FromBody] GuardarDisenoRequest req)
        => ExecuteSelf(async nId => ServiceResult(
            await _propagandaService.GuardarDisenoAsync(req.DisenoId, nId, req.Nombre, req.CanvasJson, req.ThumbnailDataUri)));

    [HttpPost("EliminarDiseno")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> EliminarDiseno([FromBody] EliminarDisenoRequest req)
        => ExecuteSelf(async nId => ServiceResult(
            await _propagandaService.EliminarDisenoAsync(req.DisenoId, nId)));

    [HttpPost("DuplicarDiseno")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> DuplicarDiseno([FromBody] DuplicarDisenoRequest req)
        => ExecuteSelf(async nId => ServiceResult(
            await _propagandaService.DuplicarDisenoAsync(req.DisenoId, nId)));

    public record CrearDisenoRequest(string Nombre, string CanvasJson, string ThumbnailDataUri, int Ancho, int Alto);
    public record GuardarDisenoRequest(Guid DisenoId, string Nombre, string CanvasJson, string ThumbnailDataUri);
    public record EliminarDisenoRequest(Guid DisenoId);
    public record DuplicarDisenoRequest(Guid DisenoId);
}
