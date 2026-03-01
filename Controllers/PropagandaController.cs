using Gimnasio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

public class PropagandaController : NegocioBaseController
{
    private readonly IPropagandaService _propagandaService;

    // GUID fijo para diseños del admin (no tiene NegocioId propio)
    private static readonly Guid AdminDesignOwner = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public PropagandaController(IPropagandaService propagandaService, IAuthService authService)
        : base(authService)
    {
        _propagandaService = propagandaService;
    }

    private Guid GetOwnerId() => AuthService.GetNegocioId(User) ?? AdminDesignOwner;

    [HttpGet("GetDisenos")]
    public Task<IActionResult> GetDisenos(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _propagandaService.GetDisenosAsync(nId)));

    [HttpGet("GetDisenosEditor")]
    public async Task<IActionResult> GetDisenosEditor()
        => Ok(await _propagandaService.GetDisenosAsync(GetOwnerId()));

    [HttpGet("GetDiseno")]
    public async Task<IActionResult> GetDiseno(Guid disenoId)
    {
        var diseno = await _propagandaService.GetDisenoAsync(disenoId, GetOwnerId());
        if (diseno == null) return NotFound(new { success = false, message = "Diseno no encontrado" });
        return Ok(new
        {
            diseno.DisenoId, diseno.Nombre, diseno.CanvasJson, diseno.ThumbnailDataUri,
            diseno.Ancho, diseno.Alto, diseno.FechaCreacion, diseno.FechaModificacion
        });
    }

    [HttpPost("CrearDiseno")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CrearDiseno([FromBody] CrearDisenoRequest req)
        => ServiceResult(await _propagandaService.CrearDisenoAsync(GetOwnerId(), req.Nombre, req.CanvasJson, req.ThumbnailDataUri, req.Ancho, req.Alto));

    [HttpPost("GuardarDiseno")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GuardarDiseno([FromBody] GuardarDisenoRequest req)
        => ServiceResult(await _propagandaService.GuardarDisenoAsync(req.DisenoId, GetOwnerId(), req.Nombre, req.CanvasJson, req.ThumbnailDataUri));

    [HttpPost("EliminarDiseno")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> EliminarDiseno([FromBody] EliminarDisenoRequest req)
        => ServiceResult(await _propagandaService.EliminarDisenoAsync(req.DisenoId, GetOwnerId()));

    [HttpPost("DuplicarDiseno")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DuplicarDiseno([FromBody] DuplicarDisenoRequest req)
        => ServiceResult(await _propagandaService.DuplicarDisenoAsync(req.DisenoId, GetOwnerId()));

    public record CrearDisenoRequest(string Nombre, string CanvasJson, string ThumbnailDataUri, int Ancho, int Alto);
    public record GuardarDisenoRequest(Guid DisenoId, string Nombre, string CanvasJson, string ThumbnailDataUri);
    public record EliminarDisenoRequest(Guid DisenoId);
    public record DuplicarDisenoRequest(Guid DisenoId);
}
