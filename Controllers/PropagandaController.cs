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

    private Guid? GetNId() => AuthService.GetNegocioId(User);

    [HttpGet("GetDisenos")]
    public Task<IActionResult> GetDisenos(Guid negocioId)
        => Execute(negocioId, async nId => Ok(await _propagandaService.GetDisenosAsync(nId)));

    [HttpGet("GetDisenosEditor")]
    public async Task<IActionResult> GetDisenosEditor()
    {
        var nId = GetNId();
        if (!nId.HasValue) return Ok(new List<object>());
        return Ok(await _propagandaService.GetDisenosAsync(nId.Value));
    }

    [HttpGet("GetDiseno")]
    public async Task<IActionResult> GetDiseno(Guid disenoId)
    {
        var nId = GetNId();
        if (!nId.HasValue) return BadRequest(new { success = false, message = "Debes acceder desde un negocio" });
        var diseno = await _propagandaService.GetDisenoAsync(disenoId, nId.Value);
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
    {
        var nId = GetNId();
        if (!nId.HasValue) return BadRequest(new { success = false, message = "Debes acceder desde un negocio para crear disenos" });
        return ServiceResult(await _propagandaService.CrearDisenoAsync(nId.Value, req.Nombre, req.CanvasJson, req.ThumbnailDataUri, req.Ancho, req.Alto));
    }

    [HttpPost("GuardarDiseno")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GuardarDiseno([FromBody] GuardarDisenoRequest req)
    {
        var nId = GetNId();
        if (!nId.HasValue) return BadRequest(new { success = false, message = "Debes acceder desde un negocio" });
        return ServiceResult(await _propagandaService.GuardarDisenoAsync(req.DisenoId, nId.Value, req.Nombre, req.CanvasJson, req.ThumbnailDataUri));
    }

    [HttpPost("EliminarDiseno")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> EliminarDiseno([FromBody] EliminarDisenoRequest req)
    {
        var nId = GetNId();
        if (!nId.HasValue) return BadRequest(new { success = false, message = "Debes acceder desde un negocio" });
        return ServiceResult(await _propagandaService.EliminarDisenoAsync(req.DisenoId, nId.Value));
    }

    [HttpPost("DuplicarDiseno")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DuplicarDiseno([FromBody] DuplicarDisenoRequest req)
    {
        var nId = GetNId();
        if (!nId.HasValue) return BadRequest(new { success = false, message = "Debes acceder desde un negocio" });
        return ServiceResult(await _propagandaService.DuplicarDisenoAsync(req.DisenoId, nId.Value));
    }

    public record CrearDisenoRequest(string Nombre, string CanvasJson, string ThumbnailDataUri, int Ancho, int Alto);
    public record GuardarDisenoRequest(Guid DisenoId, string Nombre, string CanvasJson, string ThumbnailDataUri);
    public record EliminarDisenoRequest(Guid DisenoId);
    public record DuplicarDisenoRequest(Guid DisenoId);
}
