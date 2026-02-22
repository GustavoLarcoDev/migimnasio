// ═══════════════════════════════════════════════════════════════════════════
// AdminController.cs — Controlador exclusivo del administrador de la plataforma
//
// Responsabilidades:
//   - Mostrar el panel de administración (vista principal)
//   - CRUD completo de negocios registrados
//   - Consultar estadísticas globales, logs y datos de ventas
//   - Exportar datos a Excel
//   - Impersonar (simular ser) cualquier negocio para soporte técnico
//
// Seguridad:
//   - Todos los endpoints validan con _authService.IsAdmin(User)
//   - La clase está marcada con [Authorize] para requerir sesión activa
//   - La impersonación usa claims de ASP.NET Core Identity (cookies)
//
// Ruta base: /Negocios  (compatible con los AJAX del frontend)
// ═══════════════════════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Gimnasio.Controllers;

/// <summary>
/// Controlador exclusivo del superadministrador de la plataforma MiNegocio.
/// Gestiona negocios, estadísticas, logs, exportaciones e impersonación de sesiones.
/// Todos sus endpoints son protegidos: requieren sesión activa y rol de Admin.
/// </summary>
[Route("Negocios")]
[Authorize] // Requiere que el usuario haya iniciado sesión antes de acceder a cualquier acción
public class AdminController : Controller
{
    // ── Servicios inyectados por el contenedor de dependencias (Program.cs) ──────────
    private readonly INegocioService _negocioService;
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly IComisionService _comisionService;
    private readonly IVendedorService _vendedorService;
    private readonly IReciboService _reciboService;
    private readonly IWhatsAppService _whatsAppService;

    public AdminController(INegocioService negocioService, IAuthService authService, IEmailService emailService, IComisionService comisionService, IVendedorService vendedorService, IReciboService reciboService, IWhatsAppService whatsAppService)
    {
        _negocioService = negocioService;
        _authService = authService;
        _emailService = emailService;
        _comisionService = comisionService;
        _vendedorService = vendedorService;
        _reciboService = reciboService;
        _whatsAppService = whatsAppService;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VISTAS — Acciones que devuelven páginas HTML completas (no AJAX)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Muestra la vista principal del panel de administración con la tabla de negocios.
    /// Responde tanto a GET /Negocios como a GET /Negocios/Index.
    /// Si el usuario no es admin, lo redirige al login con un mensaje de error.
    /// La vista carga los datos de negocios de forma asíncrona via AJAX (GetNegocios).
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        // Doble verificación de seguridad: aunque [Authorize] bloquea usuarios sin sesión,
        // aquí también verificamos que el rol sea específicamente "Admin" y no un negocio normal.
        if (!_authService.IsAdmin(User))
        {
            TempData["Error"] = "No tiene permisos para acceder a esta página";
            return RedirectToAction("Login", "Auth");
        }

        // Renderiza la vista sin pasar modelo: el contenido se carga por AJAX al abrir la página
        return View("~/Views/Negocios/Index.cshtml");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ENDPOINTS DE CONSULTA (GET) — Devuelven JSON para llamadas AJAX del frontend
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene la lista completa de todos los negocios registrados en la plataforma.
    /// Incluye estadísticas básicas de cada negocio (clientes, estado, fecha de pago, etc.).
    /// Utilizado por DataTables en la vista Index para mostrar y filtrar negocios.
    /// </summary>
    /// <returns>
    /// 200 OK con arreglo JSON de negocios, o 403 si no es admin, o 500 ante error inesperado.
    /// </returns>
    [HttpGet("GetNegocios")]
    public async Task<IActionResult> GetNegocios()
    {
        try
        {
            // Verificar permisos antes de cualquier consulta a la base de datos
            if (!_authService.IsAdmin(User))
                return Forbid();

            // Delegar la consulta al servicio (que usa EF Core para traer los datos)
            var negocios = await _negocioService.GetAllNegociosAsync();
            return Ok(negocios);
        }
        catch (Exception)
        {
            // Retornar 500 con el mensaje de error para facilitar el debugging en el frontend
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene los datos detallados de un negocio específico por su ID.
    /// Usado por el modal de edición para pre-llenar el formulario con los valores actuales.
    /// </summary>
    /// <param name="id">GUID único del negocio que se quiere consultar.</param>
    /// <returns>
    /// 200 OK con el objeto JSON del negocio, 404 si no existe, 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpGet("GetNegocio/{id}")]
    public async Task<IActionResult> GetNegocio(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var negocio = await _negocioService.GetNegocioAsync(id);

            // Si el GUID no corresponde a ningún negocio, devolver 404 explícito
            if (negocio == null)
                return NotFound(new { success = false, message = "Negocio no encontrado" });

            return Ok(negocio);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene las métricas clave para las tarjetas de resumen del dashboard admin:
    /// total de negocios, negocios activos (de pago), negocios en prueba,
    /// MRR (Monthly Recurring Revenue) e ingresos agrupados por mes para el gráfico.
    /// </summary>
    /// <returns>
    /// 200 OK con objeto JSON de estadísticas, 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpGet("GetAdminDashboardStats")]
    public async Task<IActionResult> GetAdminDashboardStats()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            // El servicio calcula los totales y agrupaciones directamente en la base de datos
            // para evitar traer todos los registros a memoria innecesariamente
            var stats = await _negocioService.GetAdminDashboardStatsAsync();
            return Ok(stats);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el historial de las últimas 200 acciones realizadas por el admin
    /// (crear, editar, eliminar, impersonar negocios).
    /// Usado por la pestaña "Logs" del panel admin para auditoria de cambios.
    /// El límite de 200 registros evita cargar demasiados datos en una sola consulta.
    /// </summary>
    /// <returns>
    /// 200 OK con arreglo JSON de logs ordenados por fecha descendente,
    /// 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpGet("GetAdminLogs")]
    public async Task<IActionResult> GetAdminLogs()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var logs = await _negocioService.GetAdminLogsAsync();
            return Ok(logs);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene los datos financieros consolidados para la pestaña "Ventas" del panel admin:
    /// ingresos por período, negocios con pagos pendientes, historial de cobros, etc.
    /// </summary>
    /// <returns>
    /// 200 OK con objeto JSON de datos de ventas, 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpGet("GetVentasAdmin")]
    public async Task<IActionResult> GetVentasAdmin()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var ventas = await _negocioService.GetVentasAdminAsync();
            return Ok(ventas);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CRUD DE NEGOCIOS (POST) — Crear, editar, eliminar y cambiar estado
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo negocio en la plataforma y envía un correo de bienvenida al dueño.
    /// Después de crear el negocio, registra la acción en el log de auditoría del admin.
    /// El correo se envía de forma asíncrona "fire and forget" (no bloquea la respuesta).
    /// </summary>
    /// <param name="NombreNegocio">Nombre visible del negocio (ej: "Gym Olympia").</param>
    /// <param name="duenoNegocio">Nombre completo del propietario.</param>
    /// <param name="telefono">Teléfono de contacto del negocio.</param>
    /// <param name="EmailNegocio">Correo electrónico para el login del negocio.</param>
    /// <param name="passwordNegocio">Contraseña inicial (el servicio la hashea con BCrypt).</param>
    /// <param name="isActive">Si true, el negocio está en estado "Pago/Activo".</param>
    /// <param name="esPrueba">Si true, el negocio está en período de prueba gratuita.</param>
    /// <param name="fechaPago">Fecha en que el negocio realizó su último pago (opcional).</param>
    /// <param name="fechaExpiracion">Fecha en que vence la suscripción actual (opcional).</param>
    /// <param name="precioSuscripcion">Monto mensual pactado en pesos (opcional).</param>
    /// <param name="diasPagados">Cantidad de días que cubre el pago recibido (opcional).</param>
    /// <param name="tipoNegocio">Tipo de flujo del negocio: "membresias" (gym) o "artesanal" (citas). Por defecto "membresias".</param>
    /// <returns>
    /// 200 OK con { success, message } si se creó correctamente,
    /// 400 si hay validaciones fallidas, 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpPost("Create")]
    public async Task<IActionResult> Create(string NombreNegocio, string duenoNegocio, string telefono, string EmailNegocio,
        string passwordNegocio, bool isActive, bool esPrueba,
        DateTime? fechaPago, DateTime? fechaExpiracion, decimal? precioSuscripcion, int? diasPagados,
        string tipoNegocio = "membresias")
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            // Delegar la lógica de creación al servicio: valida email duplicado, hashea password, guarda en BD
            var (success, message) = await _negocioService.CreateNegocioAsync(
                NombreNegocio, duenoNegocio, telefono, EmailNegocio, passwordNegocio, isActive, esPrueba,
                fechaPago, fechaExpiracion, precioSuscripcion, diasPagados, tipoNegocio: tipoNegocio);

            // Si el servicio devuelve error (ej: email ya existe), lo propagamos al frontend
            if (!success)
                return BadRequest(new { success = false, message });

            // Registrar en el log de auditoría quién creó el negocio y de qué tipo
            await _negocioService.RegistrarAdminLogAsync(
                "Crear",
                $"Negocio '{NombreNegocio}' creado ({(esPrueba ? "Prueba" : "Pago")})",
                NombreNegocio);

            // Enviar correo de bienvenida en segundo plano
            _ = Task.Run(async () =>
            {
                try { await _emailService.EnviarBienvenidaNegocioAsync(EmailNegocio, NombreNegocio, duenoNegocio, EmailNegocio, passwordNegocio, telefono, null, tipoNegocio); }
                catch { }
            });

            // Enviar WhatsApp de bienvenida al negocio en segundo plano
            if (!string.IsNullOrWhiteSpace(telefono))
            {
                _ = Task.Run(async () =>
                {
                    try { await _whatsAppService.EnviarBienvenidaNegocioWhatsAppAsync(telefono, NombreNegocio, duenoNegocio, EmailNegocio, passwordNegocio, tipoNegocio); }
                    catch { }
                });
            }

            // Enviar recibo de pago si no es prueba y guardarlo en BD (admin-scope: NegocioId=null)
            if (!esPrueba && precioSuscripcion.HasValue && precioSuscripcion.Value > 0)
            {
                try
                {
                    var numRecibo = await _reciboService.ObtenerSiguienteNumeroAsync(null);
                    var concepto = $"Suscripción {NombreNegocio} x{diasPagados ?? 30} días";
                    var (enviado, html) = await _emailService.EnviarReciboPagoNegocioAsync(
                        EmailNegocio, NombreNegocio, duenoNegocio,
                        diasPagados ?? 30, precioSuscripcion.Value, null, null, numRecibo);
                    await _reciboService.CrearReciboAsync(null, numRecibo, "suscripcion_negocio",
                        EmailNegocio, duenoNegocio, NombreNegocio, concepto, precioSuscripcion.Value, html);

                    // Enviar recibo de suscripción por WhatsApp
                    if (!string.IsNullOrWhiteSpace(telefono))
                    {
                        _ = Task.Run(async () =>
                        {
                            try { await _whatsAppService.EnviarReciboPagoSuscripcionWhatsAppAsync(telefono, NombreNegocio, diasPagados ?? 30, precioSuscripcion.Value, numRecibo); }
                            catch { }
                        });
                    }
                }
                catch { }
            }

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Edita los datos de un negocio existente (nombre, email, contraseña, suscripción, etc.).
    /// Si se envía una contraseña nueva en el formulario, el servicio la hashea automáticamente con BCrypt.
    /// Si no se envía contraseña, la existente se mantiene sin cambios.
    /// Registra la edición en el log de auditoría del admin.
    /// </summary>
    /// <param name="negocio">
    /// Objeto Gym enlazado desde el formulario HTML vía [FromForm].
    /// Contiene todos los campos editables del negocio.
    /// </param>
    /// <returns>
    /// 200 OK con { success, message } si se editó correctamente,
    /// 400 si hay errores de validación, 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpPost("Editar")]
    public async Task<IActionResult> Editar([FromForm] NegocioEditDto negocio)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            // El servicio maneja: verificar que exista, hashear password si cambió, guardar
            var (success, message) = await _negocioService.EditarNegocioAsync(negocio);

            if (!success)
                return BadRequest(new { success = false, message });

            // Registrar en auditoría qué negocio fue editado
            await _negocioService.RegistrarAdminLogAsync(
                "Editar",
                $"Negocio '{negocio.NegocioNombre}' editado",
                negocio.NegocioNombre);

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Elimina un negocio de la plataforma de forma permanente.
    /// La eliminación solo se permite si el negocio no tiene clientes registrados,
    /// para proteger la integridad referencial de los datos.
    /// Registra la eliminación en el log de auditoría.
    /// </summary>
    /// <param name="id">GUID único del negocio a eliminar.</param>
    /// <returns>
    /// 200 OK si se eliminó correctamente,
    /// 400 si tiene clientes registrados (no se puede eliminar),
    /// 404 si el GUID no existe, 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpPost("Eliminar")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            // Obtener el nombre antes de eliminar porque después ya no existirá en la BD.
            // Lo necesitamos para el log de auditoría.
            var negocioData = await _negocioService.GetNegocioAsync(id);
            var (success, message) = await _negocioService.EliminarNegocioAsync(id);

            if (!success)
            {
                // Distinguir entre "no encontrado" (404) y "tiene clientes" (400)
                // para que el frontend muestre el mensaje correcto al usuario
                if (message.Contains("no encontrado"))
                    return NotFound(new { success = false, message });
                return BadRequest(new { success = false, message });
            }

            // Guardar en el log solo el ID porque el nombre ya no está disponible después del delete
            await _negocioService.RegistrarAdminLogAsync(
                "Eliminar",
                $"Negocio eliminado (ID: {id})",
                null);

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Alterna el estado de suscripción de un negocio entre "Pago (Activo)" y "Prueba".
    /// Funciona como un toggle: si está activo lo pasa a prueba, y viceversa.
    /// Útil para activar un negocio después de recibir pago, o degradarlo si no renueva.
    /// Registra el cambio en el log de auditoría.
    /// </summary>
    /// <param name="id">GUID único del negocio cuyo estado se quiere cambiar.</param>
    /// <returns>
    /// 200 OK con { success, message, isActive, esPrueba } para actualizar la UI sin recargar,
    /// 404 si el negocio no existe, 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpPost("CambiarEstado")]
    public async Task<IActionResult> CambiarEstado(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            // El servicio invierte los flags isActive/esPrueba y guarda el cambio en BD
            var (success, message, isActive, esPrueba) = await _negocioService.CambiarEstadoAsync(id);

            if (!success)
                return NotFound(new { success = false, message });

            // Construir la descripción del nuevo estado para el log de auditoría
            var estado = isActive == true ? "Pago (Activo)" : "Prueba";
            await _negocioService.RegistrarAdminLogAsync(
                "CambiarEstado",
                $"Estado cambiado a '{estado}' (ID: {id})",
                null);

            // Devolver los nuevos valores de isActive y esPrueba para que el frontend
            // pueda actualizar el badge de estado sin recargar la tabla completa
            return Ok(new { success = true, message, isActive, esPrueba });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Bloquea un negocio en la plataforma, impidiendo su acceso al sistema.
    /// Se usa cuando la suscripción expira sin renovación o por decisión administrativa.
    /// Registra la acción en el log de auditoría.
    /// </summary>
    /// <param name="id">GUID único del negocio a bloquear.</param>
    /// <returns>
    /// 200 OK con { success, message } si se bloqueó correctamente,
    /// 404 si el negocio no existe, 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpPost("BloquearNegocio")]
    public async Task<IActionResult> BloquearNegocio(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message) = await _negocioService.BloquearNegocioAsync(id);

            if (!success)
                return NotFound(new { success = false, message });

            await _negocioService.RegistrarAdminLogAsync(
                "BloquearNegocio",
                $"Negocio bloqueado (ID: {id})",
                null);

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Desbloquea un negocio previamente bloqueado, restaurando su acceso al sistema.
    /// Se usa después de confirmar la renovación de pago o por decisión administrativa.
    /// Registra la acción en el log de auditoría.
    /// </summary>
    /// <param name="id">GUID único del negocio a desbloquear.</param>
    /// <returns>
    /// 200 OK con { success, message } si se desbloqueó correctamente,
    /// 404 si el negocio no existe, 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpPost("DesbloquearNegocio")]
    public async Task<IActionResult> DesbloquearNegocio(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message) = await _negocioService.DesbloquearNegocioAsync(id);

            if (!success)
                return NotFound(new { success = false, message });

            await _negocioService.RegistrarAdminLogAsync(
                "DesbloquearNegocio",
                $"Negocio desbloqueado (ID: {id})",
                null);

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EXPORTACIÓN EXCEL — Descarga de datos para análisis externo
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Exporta la lista completa de negocios a un archivo Excel (.xlsx) y lo descarga.
    /// El archivo se genera en memoria con ClosedXML y se nombra con la fecha actual
    /// (ej: Negocios_20260218.xlsx) para facilitar el control de versiones manual.
    /// </summary>
    /// <returns>
    /// Archivo .xlsx como descarga directa en el navegador,
    /// 403 si no es admin, o 500 ante error de generación.
    /// </returns>
    [HttpGet("ExportExcel")]
    public async Task<IActionResult> ExportExcel()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            // El servicio genera los bytes del archivo .xlsx con ClosedXML
            var content = await _negocioService.ExportExcelAsync();

            // File() envía la respuesta como descarga con el Content-Type correcto para Excel
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Negocios_{TimeHelper.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IMPERSONACIÓN — El admin puede iniciar sesión como cualquier negocio
    //
    // Propósito: permite al equipo de soporte acceder al dashboard de un negocio
    // para diagnosticar problemas sin necesidad de conocer su contraseña.
    //
    // Mecanismo: se crea una nueva cookie de autenticación con los claims del negocio
    // más dos claims especiales: "AdminImpersonating=true" y "AdminEmail=<correo>".
    // Al terminar, StopImpersonation usa esos claims para restaurar la sesión admin.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Inicia la impersonación: el admin adopta la identidad de un negocio específico.
    /// Crea una nueva cookie de sesión que contiene los claims del negocio más marcadores
    /// especiales para poder revertir la impersonación después sin tener que re-loguear.
    /// Al finalizar, redirige al dashboard del negocio impersonado.
    /// </summary>
    /// <param name="id">GUID del negocio que se quiere impersonar.</param>
    /// <returns>
    /// Redirección al dashboard del negocio si tiene éxito,
    /// 404 si el negocio no existe, 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpPost("Impersonate/{id}")]
    public async Task<IActionResult> Impersonate(Guid id)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            // Cargar solo los datos mínimos del negocio necesarios para construir los claims
            var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
            if (negocio == null)
                return NotFound(new { success = false, message = "Negocio no encontrado" });

            // Auditar antes de cambiar la sesión, mientras todavía tenemos los claims del admin
            await _negocioService.RegistrarAdminLogAsync(
                "Impersonar",
                $"Impersonando negocio '{negocio.NegocioNombre}'",
                negocio.NegocioNombre);

            // Leer el email del admin de su claim actual ANTES de reemplazar la cookie.
            // Si no lo guardamos aquí, no podremos restaurar su sesión después.
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            // Construir la lista de claims para la nueva sesión del negocio impersonado.
            // "AdminImpersonating" y "AdminEmail" son claims propios de nuestra app,
            // no son estándar de .NET — los leemos en StopImpersonation para revertir.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,  negocio.NegocioNombre),
                new Claim(ClaimTypes.Email, negocio.Email ?? ""),
                new Claim("NegocioId",      negocio.NegocioId.ToString()),
                new Claim(ClaimTypes.Role,  "Negocio"),
                new Claim("AdminImpersonating", "true"),          // Marca que estamos impersonando
                new Claim("AdminEmail",     adminEmail ?? "")     // Email del admin para restaurar la sesión
            };

            // Reemplazar la cookie de sesión actual con la del negocio impersonado
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true }); // Mantener la cookie entre sesiones del browser

            // Redirigir al dashboard del negocio para comenzar la navegación como si fuera el dueño
            return RedirectToAction("Dashboard", "Clientes", new { id = negocio.NegocioId });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // COMISIONES DE VENDEDORES — Gestión de pagos a vendedores
    //
    // Los vendedores ganan comisiones automáticamente al crear negocios
    // que cumplan: precio >= $15 y días contratados >= 30.
    //
    // El flujo es:
    //   1. Vendedor crea negocio → se genera ComisionVendedor (Pagada=false)
    //   2. Admin consulta comisiones pendientes por vendedor
    //   3. Admin transfiere el dinero y marca como pagado (Pagada=true)
    //   4. La comisión pasa al historial como registro contable
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todas las comisiones pendientes de pago agrupadas por vendedor.
    /// Permite al admin ver cuánto le debe a cada vendedor y el detalle de cada comisión.
    /// </summary>
    /// <returns>
    /// 200 OK con lista de grupos por vendedor con totales y comisiones detalladas,
    /// 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpGet("GetComisionesPendientes")]
    public async Task<IActionResult> GetComisionesPendientes()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var comisiones = await _comisionService.GetComisionesPendientesAsync();
            return Ok(comisiones);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el resumen de comisiones por vendedor: pendiente, pagado y total histórico.
    /// Alimenta las tarjetas de estadísticas del panel de comisiones.
    /// </summary>
    /// <returns>
    /// 200 OK con lista de vendedores y sus métricas de comisiones,
    /// 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpGet("GetResumenComisiones")]
    public async Task<IActionResult> GetResumenComisiones()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var resumen = await _comisionService.GetResumenComisionesAsync();
            return Ok(resumen);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Marca todas las comisiones pendientes de un vendedor como pagadas.
    /// Se debe llamar después de haber transferido el dinero al vendedor.
    /// Retorna el total pagado y el detalle de los negocios involucrados.
    /// </summary>
    /// <param name="vendedorId">ID del vendedor a quien se le pagan todas sus comisiones pendientes.</param>
    /// <returns>
    /// 200 OK con { success, message, totalPagado, detalleNegocios } si hay pendientes y se pagaron,
    /// 400 si el vendedor no tiene comisiones pendientes,
    /// 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpPost("PagarComisionesVendedor")]
    public async Task<IActionResult> PagarComisionesVendedor(Guid vendedorId)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var (success, message, totalPagado, detalleNegocios) =
                await _comisionService.PagarComisionesVendedorAsync(vendedorId);

            if (!success)
                return BadRequest(new { success = false, message });

            // Registrar el pago en el log de auditoría del admin
            await _negocioService.RegistrarAdminLogAsync(
                "PagarComisiones",
                $"Comisiones pagadas al vendedor (ID: {vendedorId}). Total: ${totalPagado:F2}",
                null);

            // Enviar recibo de comisión y guardarlo en BD (admin-scope)
            var vendedor = await _vendedorService.GetVendedorAsync(vendedorId);
            if (vendedor != null && !string.IsNullOrWhiteSpace(vendedor.Correo))
            {
                var lineasDetalle = detalleNegocios.Select(item =>
                {
                    var json = JsonSerializer.Serialize(item);
                    var doc = JsonDocument.Parse(json).RootElement;
                    var nombre = doc.TryGetProperty("NombreNegocio", out var n) ? n.GetString() : "—";
                    var monto = doc.TryGetProperty("MontoComision", out var m) ? m.GetDecimal() : 0m;
                    var dias = doc.TryGetProperty("DiasContratados", out var d) ? d.GetInt32() : 0;
                    var precio = doc.TryGetProperty("PrecioNegocio", out var p) ? p.GetDecimal() : 0m;
                    return $"- {nombre}: ${monto:F2} (precio ${precio:F2} / {dias} días)";
                });
                var detalle = string.Join("\n", lineasDetalle);
                var vendedorNombre = $"{vendedor.Nombre} {vendedor.Apellido}";

                try
                {
                    var numRecibo = await _reciboService.ObtenerSiguienteNumeroAsync(null);
                    var concepto = $"Comisión vendedor {vendedorNombre} ({detalleNegocios.Count} negocios)";
                    var (enviado, html) = await _emailService.EnviarReciboComisionAsync(
                        vendedor.Correo, vendedorNombre, totalPagado,
                        detalleNegocios.Count, detalle, numRecibo);
                    await _reciboService.CrearReciboAsync(null, numRecibo, "pago_comision",
                        vendedor.Correo, vendedorNombre, "My-Negocio", concepto, totalPagado, html);

                    // Enviar recibo de comisión por WhatsApp
                    if (!string.IsNullOrWhiteSpace(vendedor.Telefono))
                    {
                        _ = Task.Run(async () =>
                        {
                            try { await _whatsAppService.EnviarReciboComisionWhatsAppAsync(vendedor.Telefono, vendedorNombre, totalPagado, detalleNegocios.Count, numRecibo); }
                            catch { }
                        });
                    }
                }
                catch { }
            }

            return Ok(new { success = true, message, totalPagado, detalleNegocios });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el historial de todas las comisiones ya pagadas, ordenadas por fecha de pago.
    /// Sirve como registro contable de los pagos realizados a vendedores.
    /// </summary>
    /// <returns>
    /// 200 OK con lista de comisiones pagadas con fecha, vendedor y monto,
    /// 403 si no es admin, o 500 ante error.
    /// </returns>
    [HttpGet("GetHistorialComisiones")]
    public async Task<IActionResult> GetHistorialComisiones()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();

            var historial = await _comisionService.GetHistorialComisionesAsync();
            return Ok(historial);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Finaliza la impersonación y restaura la sesión original del administrador.
    /// Lee el claim "AdminEmail" guardado en la cookie de impersonación para reconstruir
    /// los claims del admin sin necesidad de que vuelva a escribir su contraseña.
    /// NOTA DE SEGURIDAD: NO usar [AllowAnonymous] aquí. El usuario impersonando
    /// ya está autenticado con rol "Negocio", y [Authorize] solo requiere autenticación
    /// (cualquier rol), no un rol específico. Usar [AllowAnonymous] abriría una brecha
    /// donde un atacante podría forjar claims para obtener sesión admin.
    /// </summary>
    /// <returns>
    /// Redirección al panel admin si la restauración es exitosa,
    /// redirección al login si no hay impersonación activa, o 500 ante error.
    /// </returns>
    [HttpPost("StopImpersonation")]
    public async Task<IActionResult> StopImpersonation()
    {
        try
        {
            // Leer los claims especiales que se guardaron al iniciar la impersonación
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == "AdminEmail")?.Value;
            var isImpersonating = User.Claims.Any(c => c.Type == "AdminImpersonating" && c.Value == "true");

            // Seguridad: si alguien llega aquí sin estar impersonando, redirigir al login
            if (!isImpersonating || string.IsNullOrEmpty(adminEmail))
                return RedirectToAction("Login", "Auth");

            // Reconstruir los claims del administrador usando el email guardado en la cookie
            var adminClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,  "Administrador"),
                new Claim(ClaimTypes.Email, adminEmail),
                new Claim(ClaimTypes.Role,  "Admin")
            };

            // Reemplazar la cookie del negocio impersonado con la cookie original del admin
            var identity = new ClaimsIdentity(adminClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            // Volver al panel de administración
            return RedirectToAction("Index", "Admin");
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RECIBOS ADMIN
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("GetRecibosAdmin")]
    public async Task<IActionResult> GetRecibosAdmin()
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();
            var recibos = await _reciboService.GetRecibosAdminAsync();
            return Ok(recibos);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("GetReciboAdmin")]
    public async Task<IActionResult> GetReciboAdmin(Guid reciboId)
    {
        try
        {
            if (!_authService.IsAdmin(User))
                return Forbid();
            var recibo = await _reciboService.GetReciboAdminAsync(reciboId);
            if (recibo == null)
                return NotFound(new { success = false, message = "Recibo no encontrado" });
            return Ok(recibo);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }
}
