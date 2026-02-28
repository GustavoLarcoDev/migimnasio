using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public abstract class NegocioBaseController : Controller
{
    protected readonly IAuthService AuthService;

    protected NegocioBaseController(IAuthService authService) => AuthService = authService;

    protected async Task<IActionResult> Execute(Guid negocioId, Func<Guid, Task<IActionResult>> action)
    {
        try
        {
            var nId = AuthService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();
            return await action(negocioId);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    protected async Task<IActionResult> ExecuteSelf(Func<Guid, Task<IActionResult>> action)
    {
        try
        {
            var nId = AuthService.GetNegocioId(User);
            if (!nId.HasValue) return Forbid();
            return await action(nId.Value);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    protected IActionResult ServiceResult((bool success, string message) r)
        => r.success ? Ok(new { success = true, message = r.message }) : BadRequest(new { success = false, message = r.message });

    protected IActionResult ServiceResult((bool success, string message, Guid? dataId) r)
        => r.success ? Ok(new { success = true, message = r.message, dataId = r.dataId }) : BadRequest(new { success = false, message = r.message });
}
