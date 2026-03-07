#nullable enable
// ═══════════════════════════════════════════════════════════════════════════
// DeliveryRegistrationController.cs — Public delivery registration controller
//
// RESPONSABILIDADES:
//   - Formularios públicos de registro para motorizados y restaurantes
//   - Conversión de archivos subidos (IFormFile) a Base64 data URIs
//   - Consulta de estado de solicitudes de registro
//
// SEGURIDAD:
//   - Todos los endpoints son [AllowAnonymous] (registro público)
//   - POST endpoints usan [IgnoreAntiforgeryToken] (API JSON, no forms MVC)
//   - Rate limiting "registro" para prevenir abuso
//
// VISTAS:
//   /Delivery/Registro/Motorizado      → wizard de registro de motorizado
//   /Delivery/Registro/Restaurante     → wizard de registro de restaurante
//   /Delivery/Registro/Estado/{id}     → pantalla de seguimiento de solicitud
//
// API:
//   POST RegistrarMotorizado   → procesa registro con 5 fotos
//   POST RegistrarRestaurante  → procesa registro con logo opcional
//   GET  GetEstadoSolicitud    → consulta estado de solicitud
// ═══════════════════════════════════════════════════════════════════════════

using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gimnasio.Controllers;

[Route("Delivery/Registro")]
public class DeliveryRegistrationController : Controller
{
    private readonly IDeliveryAdminService _deliveryAdminService;

    public DeliveryRegistrationController(IDeliveryAdminService deliveryAdminService)
    {
        _deliveryAdminService = deliveryAdminService;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // VISTAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Formulario wizard de registro para motorizados.
    /// Incluye datos personales, vehículo y carga de 5 fotos.
    /// </summary>
    [HttpGet("Motorizado")]
    [AllowAnonymous]
    public IActionResult RegistroMotorizado()
    {
        return View("RegistroMotorizado");
    }

    /// <summary>
    /// Formulario wizard de registro para restaurantes.
    /// Incluye datos del dueño, negocio, ubicación GPS y logo opcional.
    /// </summary>
    [HttpGet("Restaurante")]
    [AllowAnonymous]
    public IActionResult RegistroRestaurante()
    {
        return View("RegistroRestaurante");
    }

    /// <summary>
    /// Pantalla de seguimiento del estado de una solicitud de registro.
    /// El solicitante puede ver si su solicitud está pendiente, aprobada o rechazada.
    /// </summary>
    [HttpGet("Estado/{solicitudId}")]
    [AllowAnonymous]
    public IActionResult EstadoSolicitud(Guid solicitudId)
    {
        ViewData["SolicitudId"] = solicitudId;
        return View("EstadoSolicitud");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // API — REGISTRO
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Procesa el registro de un motorizado.
    /// Recibe datos personales, vehículo y 5 fotos obligatorias (IFormFile).
    /// Convierte cada foto a Base64 data URI y delega al servicio.
    /// </summary>
    [HttpPost("RegistrarMotorizado")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("registro")]
    public async Task<IActionResult> RegistrarMotorizado(
        [FromForm] string Nombre,
        [FromForm] string Apellido,
        [FromForm] string Email,
        [FromForm] string Password,
        [FromForm] string Telefono,
        [FromForm] string Cedula,
        [FromForm] string TipoVehiculo,
        [FromForm] string Placa,
        [FromForm] IFormFile FotoCedulaFrontal,
        [FromForm] IFormFile FotoCedulaTrasera,
        [FromForm] IFormFile FotoLicencia,
        [FromForm] IFormFile FotoSelfie,
        [FromForm] IFormFile FotoVehiculo)
    {
        try
        {
            // Validar que las 5 fotos obligatorias estén presentes
            if (FotoCedulaFrontal == null || FotoCedulaTrasera == null ||
                FotoLicencia == null || FotoSelfie == null || FotoVehiculo == null)
                return Json(new { success = false, message = "Las 5 fotos son obligatorias (cédula frontal, trasera, licencia, selfie, vehículo).", solicitudId = (Guid?)null });

            // Convertir las 5 fotos a Base64 data URIs (con validación de tipo y tamaño)
            var fotoCedulaFrontalBase64 = await ConvertToBase64Async(FotoCedulaFrontal);
            var fotoCedulaTraseraBase64 = await ConvertToBase64Async(FotoCedulaTrasera);
            var fotoLicenciaBase64 = await ConvertToBase64Async(FotoLicencia);
            var fotoSelfieBase64 = await ConvertToBase64Async(FotoSelfie);
            var fotoVehiculoBase64 = await ConvertToBase64Async(FotoVehiculo);

            var (success, message, solicitudId) = await _deliveryAdminService.RegistrarMotorizadoAsync(
                Nombre, Apellido, Email, Password,
                Telefono, Cedula, TipoVehiculo, Placa,
                fotoCedulaFrontalBase64 ?? "",
                fotoCedulaTraseraBase64 ?? "",
                fotoLicenciaBase64 ?? "",
                fotoSelfieBase64 ?? "",
                fotoVehiculoBase64 ?? "");

            return Json(new { success, message, solicitudId });
        }
        catch (ArgumentException ex)
        {
            return Json(new { success = false, message = ex.Message, solicitudId = (Guid?)null });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Error al procesar el registro. Intente nuevamente.", solicitudId = (Guid?)null });
        }
    }

    /// <summary>
    /// Procesa el registro de un restaurante.
    /// Recibe datos del dueño, negocio, ubicación GPS y logo opcional (IFormFile).
    /// Convierte el logo a Base64 data URI si se proporciona.
    /// </summary>
    [HttpPost("RegistrarRestaurante")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("registro")]
    public async Task<IActionResult> RegistrarRestaurante(
        [FromForm] string Nombre,
        [FromForm] string Apellido,
        [FromForm] string Email,
        [FromForm] string Password,
        [FromForm] string Telefono,
        [FromForm] string NombreNegocio,
        [FromForm] string DuenoNegocio,
        [FromForm] string Direccion,
        [FromForm] string Ciudad,
        [FromForm] double? Latitud,
        [FromForm] double? Longitud,
        [FromForm] string TiposComida,
        [FromForm] IFormFile? Logo)
    {
        try
        {
            var logoBase64 = await ConvertToBase64Async(Logo);

            var (success, message, solicitudId) = await _deliveryAdminService.RegistrarRestauranteAsync(
                Nombre, Apellido, Email, Password,
                Telefono, NombreNegocio, DuenoNegocio,
                Direccion, Ciudad, Latitud, Longitud,
                TiposComida, logoBase64 ?? "");

            return Json(new { success, message, solicitudId });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Error al procesar el registro. Intente nuevamente.", solicitudId = (Guid?)null });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // API — CONSULTA DE ESTADO
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Consulta el estado actual de una solicitud de registro.
    /// Retorna datos como: tipo, estado, fecha, motivo de rechazo (si aplica).
    /// </summary>
    [HttpGet("GetEstadoSolicitud/{solicitudId}")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetEstadoSolicitud(Guid solicitudId)
    {
        try
        {
            var data = await _deliveryAdminService.GetEstadoSolicitudAsync(solicitudId);

            if (data == null)
                return NotFound(new { success = false, message = "Solicitud no encontrada" });

            return Ok(data);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error al consultar el estado. Intente nuevamente." });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Convierte un IFormFile a una cadena Base64 data URI (data:image/{ext};base64,...).
    /// Retorna null si el archivo es null o vacío.
    /// </summary>
    private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private const long _maxFileSize = 10 * 1024 * 1024; // 10 MB

    private async Task<string?> ConvertToBase64Async(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        if (file.Length > _maxFileSize)
            throw new ArgumentException($"El archivo '{file.FileName}' excede el tamaño máximo de 10 MB.");

        var fileExt = Path.GetExtension(file.FileName);
        if (!_allowedExtensions.Contains(fileExt))
            throw new ArgumentException($"Formato no permitido: '{fileExt}'. Solo se aceptan JPG, PNG y WEBP.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();
        var ext = fileExt.TrimStart('.').ToLower();
        if (ext == "jpg") ext = "jpeg";
        return $"data:image/{ext};base64,{Convert.ToBase64String(bytes)}";
    }
}
