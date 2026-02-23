// =====================================================================
// MetodoPagoController.cs -- Controlador de metodos de pago del negocio
//
// Maneja el CRUD completo de metodos de pago que cada negocio puede
// configurar para mostrar a sus clientes (transferencia, QR, efectivo, etc.).
//
// Funcionalidades:
//   1. CRUD de metodos de pago (crear, editar, eliminar, listar)
//   2. Subida y eliminacion de imagenes QR (Base64 en BD)
//   3. Reordenamiento por drag & drop
//   4. Metodo predeterminado (uno por negocio)
//
// Todos los endpoints validan multi-tenancy comparando el NegocioId del
// claim de sesion con el NegocioId del request para evitar acceso cruzado.
// =====================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gimnasio.Models.DTOs;
using Gimnasio.Services;

namespace Gimnasio.Controllers;

[Route("Negocios")]
[Authorize]
public class MetodoPagoController : Controller
{
    // -- Dependencias inyectadas ------------------------------------------------
    private readonly IMetodoPagoService _metodoPagoService;
    private readonly IAuthService _authService;

    public MetodoPagoController(IMetodoPagoService metodoPagoService, IAuthService authService)
    {
        _metodoPagoService = metodoPagoService;
        _authService = authService;
    }

    // =========================================================================
    // SECCION 1 -- CONSULTAS (GET)
    // =========================================================================

    /// <summary>
    /// Obtiene la lista de todos los metodos de pago del negocio.
    /// No incluye la imagen QR completa en el listado (solo un flag booleano)
    /// para evitar enviar grandes payloads Base64 en la lista.
    /// </summary>
    [HttpGet("GetMetodosPago")]
    public async Task<IActionResult> GetMetodosPago(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var metodos = await _metodoPagoService.GetMetodosPagoAsync(negocioId);
            return Ok(metodos.Select(m => new
            {
                m.MetodoPagoId,
                m.Nombre,
                m.NumeroCuenta,
                m.Cedula,
                m.NombreTitular,
                ImagenQR = m.ImagenQR != null, // Solo flag, no el Base64 completo
                m.Instrucciones,
                m.EsPredeterminado,
                m.Orden,
                FechaCreacion = m.FechaCreacion.ToString("dd/MM/yyyy")
            }));
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene los datos completos de un metodo de pago especifico,
    /// incluyendo la imagen QR en Base64 para mostrar en el modal de edicion.
    /// </summary>
    [HttpGet("GetMetodoPago")]
    public async Task<IActionResult> GetMetodoPago(Guid negocioId, Guid metodoPagoId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var metodo = await _metodoPagoService.GetMetodoPagoAsync(negocioId, metodoPagoId);
            if (metodo == null)
                return NotFound(new { success = false, message = "Metodo de pago no encontrado" });

            return Ok(new
            {
                metodo.MetodoPagoId,
                metodo.Nombre,
                metodo.NumeroCuenta,
                metodo.Cedula,
                metodo.NombreTitular,
                metodo.ImagenQR, // Base64 completo para vista individual
                metodo.Instrucciones,
                metodo.EsPredeterminado,
                metodo.Orden
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // =========================================================================
    // SECCION 2 -- CRUD (POST)
    // =========================================================================

    /// <summary>
    /// Crea un nuevo metodo de pago para el negocio.
    /// Las validaciones de negocio se delegan al servicio, no se hacen aqui.
    /// </summary>
    [HttpPost("CrearMetodoPago")]
    public async Task<IActionResult> CrearMetodoPago([FromForm] MetodoPagoCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var result = await _metodoPagoService.CrearMetodoPagoAsync(nId.Value, dto);

            if (!result.success)
                return BadRequest(new { result.success, result.message });

            return Ok(new { result.success, result.message, result.metodoPagoId });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Edita los datos de un metodo de pago existente.
    /// </summary>
    [HttpPost("EditarMetodoPago")]
    public async Task<IActionResult> EditarMetodoPago([FromForm] MetodoPagoCreateDto dto)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || dto.NegocioId != nId.Value)
                return Forbid();

            var result = await _metodoPagoService.EditarMetodoPagoAsync(nId.Value, dto);

            if (!result.success)
                return BadRequest(new { result.success, result.message });

            return Ok(new { result.success, result.message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Elimina un metodo de pago del negocio.
    /// </summary>
    [HttpPost("EliminarMetodoPago")]
    public async Task<IActionResult> EliminarMetodoPago(Guid negocioId, Guid metodoPagoId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var result = await _metodoPagoService.EliminarMetodoPagoAsync(nId.Value, metodoPagoId);

            if (!result.success)
                return BadRequest(new { result.success, result.message });

            return Ok(new { result.success, result.message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // =========================================================================
    // SECCION 3 -- IMAGENES QR (subida y eliminacion)
    //
    // Las imagenes QR se almacenan como data URI Base64 en la BD,
    // igual que las imagenes de productos. Esto funciona en cualquier
    // entorno de produccion sin depender del filesystem.
    // =========================================================================

    /// <summary>
    /// Sube una imagen QR para un metodo de pago. Validaciones:
    ///   - Tamano maximo: 5 MB
    ///   - Formatos permitidos: .png, .jpeg, .jpg, .gif, .webp
    /// La imagen se convierte a Base64 data URI y se almacena en la BD.
    /// </summary>
    [HttpPost("SubirImagenQR")]
    public async Task<IActionResult> SubirImagenQR(Guid negocioId, Guid metodoPagoId, IFormFile imagen)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            if (imagen == null || imagen.Length == 0)
                return BadRequest(new { success = false, message = "No se proporciono imagen" });

            // Validar tipo de archivo
            var allowedTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(imagen.ContentType.ToLower()))
                return BadRequest(new { success = false, message = "Tipo de archivo no permitido. Use PNG, JPG, GIF o WebP." });

            // Validar tamano (maximo 5MB)
            if (imagen.Length > 5 * 1024 * 1024)
                return BadRequest(new { success = false, message = "La imagen no debe superar 5MB" });

            // Convertir a Base64 data URI para almacenar en BD (produccion-safe)
            using var ms = new MemoryStream();
            await imagen.CopyToAsync(ms);
            var base64 = $"data:{imagen.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";

            var result = await _metodoPagoService.SubirImagenQRAsync(nId.Value, metodoPagoId, base64);

            if (!result.success)
                return BadRequest(new { result.success, result.message });

            return Ok(new { result.success, result.message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Elimina la imagen QR de un metodo de pago (pasa null al servicio para limpiarla).
    /// </summary>
    [HttpPost("EliminarImagenQR")]
    public async Task<IActionResult> EliminarImagenQR(Guid negocioId, Guid metodoPagoId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var result = await _metodoPagoService.SubirImagenQRAsync(nId.Value, metodoPagoId, null);

            if (!result.success)
                return BadRequest(new { result.success, result.message });

            return Ok(new { result.success, result.message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // =========================================================================
    // SECCION 4 -- ESTADISTICAS DE VENTAS POR METODO DE PAGO
    // =========================================================================

    /// <summary>
    /// Obtiene estadisticas de ventas agrupadas por metodo de pago.
    /// Combina datos de OrdenVenta (POS) y MovimientoInventario (ventas rapidas).
    /// Periodos: hoy, esta semana, este mes.
    /// </summary>
    [HttpGet("GetPaymentStatsByMethod")]
    public async Task<IActionResult> GetPaymentStatsByMethod(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var stats = await _metodoPagoService.GetPaymentStatsByMethodAsync(negocioId);
            return Ok(stats);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // =========================================================================
    // SECCION 5 -- ORDENAMIENTO Y PREDETERMINADO
    // =========================================================================

    /// <summary>
    /// Reordena los metodos de pago segun el orden de IDs recibido (drag & drop).
    /// </summary>
    [HttpPost("ReordenarMetodosPago")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ReordenarMetodosPago(Guid negocioId, [FromBody] List<Guid> orderedIds)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var result = await _metodoPagoService.ReordenarMetodosPagoAsync(nId.Value, orderedIds);

            if (!result.success)
                return BadRequest(new { result.success, result.message });

            return Ok(new { result.success, result.message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Establece un metodo de pago como predeterminado.
    /// Solo un metodo puede ser predeterminado por negocio; el servicio
    /// se encarga de quitar el flag del anterior.
    /// </summary>
    [HttpPost("SetMetodoPagoPredeterminado")]
    public async Task<IActionResult> SetMetodoPagoPredeterminado(Guid negocioId, Guid metodoPagoId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var result = await _metodoPagoService.SetPredeterminadoAsync(nId.Value, metodoPagoId);

            if (!result.success)
                return BadRequest(new { result.success, result.message });

            return Ok(new { result.success, result.message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }
}
