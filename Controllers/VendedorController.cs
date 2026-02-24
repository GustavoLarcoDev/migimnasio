#nullable enable

// ═══════════════════════════════════════════════════════════
// VendedorController.cs — Gestión completa del rol Vendedor
//
// Este controlador tiene DOS responsabilidades principales:
//
//  1. ADMIN → VENDEDORES
//     El administrador puede crear, editar y eliminar vendedores,
//     y también puede "impersonarlos" para ver el sistema desde
//     su perspectiva.
//
//  2. VENDEDOR → SUS NEGOCIOS
//     Un vendedor autenticado puede ver su dashboard, gestionar
//     los negocios que él mismo creó, impersonarlos para acceder
//     a su panel, y atender los leads que llegan del landing.
//
// Flujo de impersonación (cadena de sesiones):
//   Admin → [impersona] → Vendedor → [impersona] → Negocio
//   Cada nivel guarda en los claims quién está arriba, de modo
//   que al "volver atrás" se puede restaurar la sesión correcta.
//
// Todos los endpoints viven bajo la ruta base [Route("Negocios")]
// para mantener consistencia con el resto del proyecto.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gimnasio.Controllers;

// [Authorize] obliga a que el usuario esté autenticado en TODOS los endpoints.
// Las excepciones se marcan individualmente con [AllowAnonymous].
[Route("Negocios")]
[Authorize]
public class VendedorController : Controller
{
    // ─── Servicios inyectados por el contenedor de dependencias ───────────────
    private readonly IVendedorService _vendedorService;
    private readonly IAuthService _authService;
    private readonly INegocioService _negocioService;
    private readonly IEmailService _emailService;
    private readonly IComisionService _comisionService;
    private readonly IReciboService _reciboService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IMetodoPagoService _metodoPagoService;

    public VendedorController(
        IVendedorService vendedorService,
        IAuthService authService,
        INegocioService negocioService,
        IEmailService emailService,
        IComisionService comisionService,
        IReciboService reciboService,
        IWhatsAppService whatsAppService,
        IMetodoPagoService metodoPagoService)
    {
        _vendedorService = vendedorService;
        _authService = authService;
        _negocioService = negocioService;
        _emailService = emailService;
        _comisionService = comisionService;
        _reciboService = reciboService;
        _whatsAppService = whatsAppService;
        _metodoPagoService = metodoPagoService;
    }

    // ═══════════════════════════════════════════════════════════
    // SECCIÓN 1 — ADMIN: CRUD DE VENDEDORES
    //
    // Solo el administrador puede acceder a estos endpoints.
    // La comprobación se delega a _authService.IsAdmin(User),
    // que inspecciona el claim de rol en la cookie de sesión.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Devuelve la lista completa de todos los vendedores.
    /// Usado por la tabla de gestión en el panel de administración.
    /// </summary>
    [HttpGet("GetVendedores")]
    public async Task<IActionResult> GetVendedores()
    {
        // Solo el admin puede listar vendedores; devolver 403 si no lo es.
        if (!_authService.IsAdmin(User))
            return Forbid();

        var vendedores = await _vendedorService.GetAllVendedoresAsync();
        return Ok(vendedores);
    }

    /// <summary>
    /// Devuelve los datos de un vendedor específico por su ID.
    /// Se expone solo los campos seguros (sin contraseña).
    /// </summary>
    [HttpGet("GetVendedor")]
    public async Task<IActionResult> GetVendedor(Guid id)
    {
        if (!_authService.IsAdmin(User))
            return Forbid();

        var vendedor = await _vendedorService.GetVendedorAsync(id);

        // Si el ID no existe en la base de datos, se responde con 404.
        if (vendedor == null)
            return NotFound(new { success = false, message = "Vendedor no encontrado" });

        // Se proyecta manualmente para no exponer la contraseña hasheada ni campos internos.
        return Ok(new
        {
            vendedor.VendedorId,
            vendedor.Nombre,
            vendedor.Apellido,
            vendedor.Correo,
            vendedor.Telefono,
            vendedor.NombreBanco,
            vendedor.NumeroCedula,
            vendedor.NumeroCuenta
        });
    }

    /// <summary>
    /// Crea un nuevo vendedor con la contraseña hasheada.
    /// Después de crearlo, registra el evento en el log de auditoría
    /// y envía un email de bienvenida de forma asíncrona sin bloquear la respuesta.
    /// </summary>
    [HttpPost("CrearVendedor")]
    public async Task<IActionResult> CrearVendedor(
        string nombre, string apellido, string correo, string telefono, string password,
        string? nombreBanco = null, string? numeroCedula = null, string? numeroCuenta = null)
    {
        if (!_authService.IsAdmin(User))
            return Forbid();

        // El servicio se encarga de validar el correo único, hashear la contraseña,
        // y persistir en base de datos. Devuelve una tupla (éxito, mensaje).
        var (success, message) = await _vendedorService.CrearVendedorAsync(
            nombre, apellido, correo, telefono, password, nombreBanco, numeroCedula, numeroCuenta);

        if (!success)
            return BadRequest(new { success = false, message });

        // Registrar quién hizo qué y cuándo (trazabilidad / auditoría).
        await _negocioService.RegistrarAdminLogAsync(
            "CrearVendedor",
            $"Vendedor '{nombre} {apellido}' creado",
            null);

        // Enviar email en segundo plano con try-catch para evitar unobserved task exceptions.
        _ = Task.Run(async () =>
        {
            try { await _emailService.EnviarBienvenidaVendedorAsync(correo, $"{nombre} {apellido}", correo, password); }
            catch { /* El EmailService ya loguea internamente */ }
        });

        // Enviar WhatsApp de bienvenida al vendedor en segundo plano
        if (!string.IsNullOrWhiteSpace(telefono))
        {
            _ = Task.Run(async () =>
            {
                try { await _whatsAppService.EnviarBienvenidaVendedorWhatsAppAsync(telefono, $"{nombre} {apellido}", correo, password); }
                catch { }
            });
        }

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Edita los datos de un vendedor existente.
    /// Si se envía una contraseña nueva (campo opcional), el servicio la re-hashea.
    /// Si se omite la contraseña, la actual se mantiene intacta.
    /// </summary>
    [HttpPost("EditarVendedor")]
    public async Task<IActionResult> EditarVendedor(
        Guid id, string nombre, string apellido, string correo, string telefono, string? password,
        string? nombreBanco = null, string? numeroCedula = null, string? numeroCuenta = null)
    {
        if (!_authService.IsAdmin(User))
            return Forbid();

        // password es nullable: si viene null, el servicio omite el cambio de contraseña.
        var (success, message) = await _vendedorService.EditarVendedorAsync(
            id, nombre, apellido, correo, telefono, password, nombreBanco, numeroCedula, numeroCuenta);

        if (!success)
            return BadRequest(new { success = false, message });

        await _negocioService.RegistrarAdminLogAsync(
            "EditarVendedor",
            $"Vendedor '{nombre} {apellido}' editado",
            null);

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Elimina un vendedor del sistema.
    /// El servicio realiza una baja lógica (IsActive = false) en lugar de borrar
    /// el registro físicamente, para preservar la trazabilidad de los negocios
    /// que ese vendedor creó.
    /// </summary>
    [HttpPost("EliminarVendedor")]
    public async Task<IActionResult> EliminarVendedor(Guid id)
    {
        if (!_authService.IsAdmin(User))
            return Forbid();

        var (success, message) = await _vendedorService.EliminarVendedorAsync(id);

        if (!success)
            return BadRequest(new { success = false, message });

        // El mensaje del servicio ya describe qué vendedor se eliminó; se reutiliza en el log.
        await _negocioService.RegistrarAdminLogAsync("EliminarVendedor", message, null);

        return Ok(new { success = true, message });
    }

    // ═══════════════════════════════════════════════════════════
    // SECCIÓN 2 — ADMIN: IMPERSONAR VENDEDOR
    //
    // La impersonación permite al admin iniciar sesión "como" un
    // vendedor sin necesitar su contraseña. Esto es útil para
    // depurar o dar soporte viendo exactamente lo que el vendedor ve.
    //
    // Mecanismo:
    //   - Se crea una cookie de sesión nueva con el rol "Vendedor"
    //     y se añade un claim especial "AdminImpersonating = true"
    //     junto con el email del admin original.
    //   - Cuando el admin quiere volver, se lee ese claim para
    //     reconstruir su sesión de admin sin necesitar contraseña.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// El administrador asume la identidad de un vendedor específico.
    /// Reemplaza la cookie de sesión actual con una nueva que contiene
    /// los claims del vendedor más un marcador de impersonación.
    /// </summary>
    [HttpPost("AdminImpersonateVendedor/{id}")]
    public async Task<IActionResult> AdminImpersonateVendedor(Guid id)
    {
        if (!_authService.IsAdmin(User))
            return Forbid();

        var vendedor = await _vendedorService.GetVendedorAsync(id);
        if (vendedor == null)
            return NotFound(new { success = false, message = "Vendedor no encontrado" });

        // Guardamos el email del admin ANTES de sobrescribir la sesión,
        // para poder restaurarla cuando termine la impersonación.
        var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        // Construimos los claims que identificarán al vendedor en esta sesión.
        // "AdminImpersonating" y "AdminEmail" son claims propios del sistema,
        // no estándar de .NET, pero permiten detectar el modo impersonación.
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name,  $"{vendedor.Nombre} {vendedor.Apellido}"),
            new Claim(ClaimTypes.Email, vendedor.Correo),
            new Claim("VendedorId",     vendedor.VendedorId.ToString()),
            new Claim(ClaimTypes.Role,  "Vendedor"),
            new Claim("AdminImpersonating", "true"),  // marca que hay un admin detrás
            new Claim("AdminEmail",     adminEmail ?? "")  // para restaurar sesión de admin
        };

        // Sobrescribimos la cookie de autenticación con la identidad del vendedor.
        // IsPersistent = true hace que sobreviva al cierre del navegador.
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        await _negocioService.RegistrarAdminLogAsync(
            "AdminImpersonarVendedor",
            $"Admin impersonó al vendedor '{vendedor.Nombre} {vendedor.Apellido}'",
            null);

        // Redirigir al dashboard del vendedor para que el admin vea su vista.
        return RedirectToAction("VendedorDashboard");
    }

    /// <summary>
    /// El administrador abandona la impersonación y recupera su sesión original.
    /// SEGURIDAD: NO usar [AllowAnonymous]. El usuario impersonando ya está autenticado
    /// (tiene cookie válida con rol "Vendedor"), y [Authorize] solo requiere autenticación,
    /// no un rol específico. Usar [AllowAnonymous] abriría brecha de escalación de privilegios.
    /// </summary>
    [HttpPost("AdminStopImpersonateVendedor")]
    public async Task<IActionResult> AdminStopImpersonateVendedor()
    {
        // Recuperamos el email del admin original que se guardó como claim al impersonar.
        var adminEmail = User.Claims.FirstOrDefault(c => c.Type == "AdminEmail")?.Value;
        var isAdminImpersonating = User.Claims.Any(c => c.Type == "AdminImpersonating" && c.Value == "true");

        // Si no estamos en modo impersonación (acceso directo a la URL), redirigir al login.
        if (!isAdminImpersonating || string.IsNullOrEmpty(adminEmail))
            return RedirectToAction("Login", "Auth");

        // Guardamos el nombre del vendedor antes de cambiar la sesión, solo para el log.
        var vendedorNombre = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        // Reconstruimos la sesión del admin usando solo su email (ya verificado antes).
        // No se necesita contraseña porque confiamos en que el claim "AdminEmail"
        // fue puesto por nuestro propio código al iniciar la impersonación.
        var adminClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name,  "Administrador"),
            new Claim(ClaimTypes.Email, adminEmail),
            new Claim(ClaimTypes.Role,  "Admin")
        };

        var identity = new ClaimsIdentity(adminClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        await _negocioService.RegistrarAdminLogAsync(
            "AdminDejarImpersonarVendedor",
            $"Admin dejó de impersonar al vendedor '{vendedorNombre}'",
            null);

        // Devolver al admin a su panel principal.
        return RedirectToAction("Index", "Admin");
    }

    // ═══════════════════════════════════════════════════════════
    // SECCIÓN 3 — VENDEDOR: SU PROPIO DASHBOARD Y ESTADÍSTICAS
    //
    // Estos endpoints son usados por el vendedor autenticado
    // (ya sea que haya hecho login directamente, o que el admin
    // lo esté impersonando).
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Muestra la vista principal del dashboard del vendedor.
    /// Verifica el rol con IsVendedor() en lugar de un atributo de autorización
    /// porque [Authorize(Roles = "Vendedor")] rechazaría también al admin
    /// cuando impersona a un vendedor, y aquí queremos permitirlo.
    /// </summary>
    [HttpGet("VendedorDashboard")]
    public IActionResult VendedorDashboard()
    {
        // IsVendedor() comprueba el claim de rol en la cookie actual.
        // Si el admin está impersonando a un vendedor, su cookie tiene rol "Vendedor",
        // por lo que también pasa esta comprobación.
        if (!IsVendedor())
            return RedirectToAction("Login", "Auth");

        return View("~/Views/Vendedor/Dashboard.cshtml");
    }

    /// <summary>
    /// Devuelve la lista de negocios asignados al vendedor autenticado.
    /// Cada negocio incluye estadísticas básicas (clientes activos, expiración, etc.).
    /// </summary>
    [HttpGet("GetVendedorNegocios")]
    public async Task<IActionResult> GetVendedorNegocios()
    {
        // GetVendedorId() extrae el Guid del claim "VendedorId" de la sesión.
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var negocios = await _vendedorService.GetNegociosByVendedorAsync(vendedorId.Value);
        return Ok(negocios);
    }

    /// <summary>
    /// Devuelve las estadísticas globales del vendedor:
    /// total de negocios, cuántos están activos, cuántos en prueba
    /// y el conteo total de clientes en sus negocios.
    /// Alimenta las tarjetas de resumen (cards) del dashboard.
    /// </summary>
    [HttpGet("GetVendedorStats")]
    public async Task<IActionResult> GetVendedorStats()
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var stats = await _vendedorService.GetVendedorStatsAsync(vendedorId.Value);
        return Ok(stats);
    }

    // ═══════════════════════════════════════════════════════════
    // SECCIÓN 4 — VENDEDOR: CREAR E IMPERSONAR NEGOCIOS
    //
    // El vendedor puede dar de alta nuevos negocios y luego
    // acceder a su panel como si fuera el dueño del negocio.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// El vendedor crea un nuevo negocio y queda registrado como su propietario
    /// (campo VendedorId en el negocio). Esto es importante para que el vendedor
    /// solo pueda gestionar los negocios que él mismo creó.
    /// Al crearlo, se envía un email de bienvenida al dueño del negocio.
    /// </summary>
    [HttpPost("VendedorCrearNegocio")]
    public async Task<IActionResult> VendedorCrearNegocio(
        string NombreNegocio,
        string duenoNegocio,
        string telefono,
        string EmailNegocio,
        string passwordNegocio,
        bool esPrueba,
        DateTime? fechaPago,
        DateTime? fechaExpiracion,
        decimal? precioSuscripcion,
        int? diasPagados,
        string tipoNegocio = "membresias",
        string metodoPago = "Efectivo")
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        // Un negocio en prueba comienza inactivo (IsActive = false).
        // Uno de pago comienza activo inmediatamente.
        bool isActive = !esPrueba;

        // Se pasa el vendedorId para que el negocio quede vinculado a este vendedor.
        // Así se garantiza que solo él puede verlo y gestionarlo en el futuro.
        var (success, message) = await _negocioService.CreateNegocioAsync(
            NombreNegocio, duenoNegocio, telefono, EmailNegocio, passwordNegocio,
            isActive, esPrueba, fechaPago, fechaExpiracion, precioSuscripcion, diasPagados,
            vendedorId.Value, tipoNegocio);

        if (!success)
            return BadRequest(new { success = false, message });

        var vendedorNombre = User.Identity?.Name ?? "Vendedor";
        await _negocioService.RegistrarAdminLogAsync(
            "VendedorCrearNegocio",
            $"Vendedor {vendedorNombre} creó el negocio '{NombreNegocio}'",
            NombreNegocio);

        // ─── Generar comisión si el negocio cumple los requisitos ────────
        // Se necesita el ID del negocio recién creado, que no devuelve CreateNegocioAsync.
        // Lo recuperamos buscando por email (es único en la plataforma, acaba de ser creado).
        // La comisión se genera solo si precio >= $15 y días >= 30.
        // Se ejecuta dentro del request (contexto de DI todavía vivo) con try-catch
        // para no bloquear al vendedor si algo falla en la generación de comisión.
        try
        {
            var negocioCreado = await _negocioService.GetNegocioByEmailAsync(EmailNegocio);
            if (negocioCreado != null && precioSuscripcion.HasValue && diasPagados.HasValue)
            {
                await _comisionService.GenerarComisionNuevaNegocioAsync(
                    vendedorId.Value,
                    vendedorNombre,
                    negocioCreado.NegocioId,
                    NombreNegocio,
                    diasPagados.Value,
                    precioSuscripcion.Value);
            }
        }
        catch { /* No bloquear el flujo principal si falla la generación de comisión */ }

        // Enviar email de bienvenida en segundo plano
        _ = Task.Run(async () =>
        {
            try { await _emailService.EnviarBienvenidaNegocioAsync(EmailNegocio, NombreNegocio, duenoNegocio, EmailNegocio, passwordNegocio, telefono, vendedorNombre, tipoNegocio); }
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

        // Enviar recibo de pago si no es prueba y guardarlo en BD (admin-scope)
        if (!esPrueba && precioSuscripcion.HasValue && precioSuscripcion.Value > 0)
        {
            try
            {
                var vendedor = await _vendedorService.GetVendedorAsync(vendedorId.Value);
                var vendedorTelefono = vendedor?.Telefono;
                var numRecibo = await _reciboService.ObtenerSiguienteNumeroAsync(null);
                var concepto = $"Suscripción {NombreNegocio} x{diasPagados ?? 30} días | Método: {metodoPago}";
                var (enviado, html) = await _emailService.EnviarReciboPagoNegocioAsync(
                    EmailNegocio, NombreNegocio, duenoNegocio,
                    diasPagados ?? 30, precioSuscripcion.Value, vendedorNombre, vendedorTelefono, numRecibo,
                    metodoPago: metodoPago);
                await _reciboService.CrearReciboAsync(null, numRecibo, "suscripcion_negocio",
                    EmailNegocio, duenoNegocio, NombreNegocio, concepto, precioSuscripcion.Value, html);
            }
            catch { }
        }

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// El vendedor accede al panel de un negocio como si fuera su dueño.
    /// Solo puede impersonar negocios que él mismo creó (verificado por VendedorId).
    ///
    /// Caso especial — cadena de impersonación:
    /// Si un admin está impersonando a este vendedor y el vendedor luego impersona
    /// a un negocio, los claims del admin (AdminImpersonating + AdminEmail) se
    /// propagan a la nueva sesión. Esto permite que el admin pueda "desandar" todos
    /// los pasos: negocio → vendedor → admin, sin perder el hilo.
    /// </summary>
    [HttpPost("VendedorImpersonate/{id}")]
    public async Task<IActionResult> VendedorImpersonate(Guid id)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
        if (negocio == null)
            return NotFound(new { success = false, message = "Negocio no encontrado" });

        // Seguridad: el vendedor solo puede impersonar negocios que él creó.
        // Si intenta acceder a un negocio de otro vendedor, se devuelve 403.
        if (negocio.VendedorId != vendedorId.Value)
            return Forbid();

        // Guardamos la identidad del vendedor antes de cambiar la sesión,
        // para poder restaurarla cuando el vendedor quiera "volver atrás".
        var vendedorCorreo = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var vendedorNombre = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        // Leemos si hay un admin en la cadena de impersonación superior.
        var isAdminImpersonating = User.Claims.Any(c => c.Type == "AdminImpersonating" && c.Value == "true");
        var adminEmail = User.Claims.FirstOrDefault(c => c.Type == "AdminEmail")?.Value;

        // Claims de la nueva sesión: identidad del negocio + breadcrumb del vendedor.
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name,  negocio.NegocioNombre),
            new Claim(ClaimTypes.Email, negocio.Email ?? ""),
            new Claim("NegocioId",      negocio.NegocioId.ToString()),
            new Claim(ClaimTypes.Role,  "Negocio"),
            new Claim("VendedorImpersonating", "true"),    // marca que hay un vendedor detrás
            new Claim("VendedorId",     vendedorId.Value.ToString()),
            new Claim("VendedorEmail",  vendedorCorreo ?? ""),
            new Claim("VendedorNombre", vendedorNombre ?? "")
        };

        // Si hay un admin en la cadena, propagamos sus claims para no perderlos.
        // Sin esto, al salir del negocio volvería al vendedor, pero ya no podría
        // saber que hay que volver también al admin.
        if (isAdminImpersonating && !string.IsNullOrEmpty(adminEmail))
        {
            claims.Add(new Claim("AdminImpersonating", "true"));
            claims.Add(new Claim("AdminEmail", adminEmail));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        await _negocioService.RegistrarAdminLogAsync(
            "VendedorImpersonar",
            $"Vendedor {vendedorNombre} impersonó el negocio '{negocio.NegocioNombre}'",
            negocio.NegocioNombre);

        // Redirigir al panel de clientes del negocio impersonado.
        return RedirectToAction("Dashboard", "Clientes", new { id = negocio.NegocioId });
    }

    /// <summary>
    /// El vendedor abandona la impersonación de un negocio y vuelve a su propio dashboard.
    /// Si en la cadena había también un admin impersonando al vendedor, sus claims
    /// se restauran en la nueva sesión para que el admin también pueda volver a su panel.
    /// SEGURIDAD: NO usar [AllowAnonymous]. La cookie de impersonación tiene rol "Negocio"
    /// pero sigue siendo una sesión autenticada, y [Authorize] solo requiere autenticación.
    /// </summary>
    [HttpPost("VendedorStopImpersonation")]
    public async Task<IActionResult> VendedorStopImpersonation()
    {
        // Recuperamos todos los datos del vendedor que estaban guardados en los claims
        // cuando se inició la impersonación del negocio.
        var vendedorId     = User.Claims.FirstOrDefault(c => c.Type == "VendedorId")?.Value;
        var vendedorEmail  = User.Claims.FirstOrDefault(c => c.Type == "VendedorEmail")?.Value;
        var vendedorNombre = User.Claims.FirstOrDefault(c => c.Type == "VendedorNombre")?.Value;
        var isImpersonating = User.Claims.Any(c => c.Type == "VendedorImpersonating" && c.Value == "true");

        // Si no estamos en modo impersonación (acceso directo a la URL), redirigir al login.
        if (!isImpersonating || string.IsNullOrEmpty(vendedorId))
            return RedirectToAction("Login", "Auth");

        // Nombre del negocio que se estaba impersonando, solo para el log.
        var negocioNombre = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        // Verificamos si también hay un admin encima en la cadena de impersonación.
        var isAdminImpersonating = User.Claims.Any(c => c.Type == "AdminImpersonating" && c.Value == "true");
        var adminEmail = User.Claims.FirstOrDefault(c => c.Type == "AdminEmail")?.Value;

        // Reconstruimos la sesión del vendedor con sus datos originales.
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name,  vendedorNombre ?? "Vendedor"),
            new Claim(ClaimTypes.Email, vendedorEmail ?? ""),
            new Claim("VendedorId",     vendedorId),
            new Claim(ClaimTypes.Role,  "Vendedor")
        };

        // Si el admin estaba también en la cadena, propagamos sus claims para que
        // el botón "Volver al Admin" siga funcionando desde el dashboard del vendedor.
        if (isAdminImpersonating && !string.IsNullOrEmpty(adminEmail))
        {
            claims.Add(new Claim("AdminImpersonating", "true"));
            claims.Add(new Claim("AdminEmail", adminEmail));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        await _negocioService.RegistrarAdminLogAsync(
            "VendedorDejarImpersonar",
            $"Vendedor {vendedorNombre} dejó de impersonar '{negocioNombre}'",
            negocioNombre);

        return RedirectToAction("VendedorDashboard");
    }

    // ═══════════════════════════════════════════════════════════
    // SECCIÓN 5 — VENDEDOR: CRUD SOBRE SUS PROPIOS NEGOCIOS
    //
    // El vendedor puede ver, editar, eliminar y cambiar el estado
    // de los negocios que él mismo creó. En cada operación se
    // verifica que el negocio pertenezca al vendedor autenticado.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Devuelve los detalles completos de un negocio específico.
    /// Se verifica que el negocio pertenezca al vendedor autenticado
    /// antes de exponer los datos.
    /// </summary>
    [HttpGet("GetVendedorNegocio/{id}")]
    public async Task<IActionResult> GetVendedorNegocio(Guid id)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
        if (negocio == null)
            return NotFound(new { success = false, message = "Negocio no encontrado" });

        // Verificación de propiedad: un vendedor no puede ver datos de negocios ajenos.
        if (negocio.VendedorId != vendedorId.Value)
            return Forbid();

        // Se proyectan solo los campos relevantes para la gestión del vendedor.
        return Ok(new
        {
            negocio.NegocioId,
            negocio.NegocioNombre,
            negocio.DuenoNegocio,
            negocio.Telefono,
            negocio.Email,
            negocio.IsActive,
            negocio.EsPrueba,
            negocio.DiasPagados,
            negocio.PrecioSuscripcion,
            negocio.FechaPago,
            negocio.FechaExpiracion
        });
    }

    /// <summary>
    /// El vendedor edita los datos de un negocio que él creó.
    /// Usa [FromForm] para recibir el modelo completo del formulario HTML.
    /// Verifica que el negocio sea propiedad del vendedor antes de permitir la edición.
    /// </summary>
    [HttpPost("VendedorEditarNegocio")]
    public async Task<IActionResult> VendedorEditarNegocio([FromForm] NegocioEditDto negocio)
    {
        // Nota: el parámetro es de tipo "Gym" porque el modelo principal del negocio
        // aún conserva ese nombre de clase internamente (legado del proyecto original).
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        // Primero se carga el negocio existente para verificar su propietario.
        var existing = await _negocioService.GetNegocioForImpersonationAsync(negocio.NegocioId);
        if (existing == null)
            return NotFound(new { success = false, message = "Negocio no encontrado" });

        // Seguridad: el vendedor no puede editar negocios que no son suyos.
        if (existing.VendedorId != vendedorId.Value)
            return Forbid();

        var (success, message) = await _negocioService.EditarNegocioAsync(negocio);

        if (!success)
            return BadRequest(new { success = false, message });

        var vendedorNombre = User.Identity?.Name ?? "Vendedor";
        await _negocioService.RegistrarAdminLogAsync(
            "VendedorEditarNegocio",
            $"Vendedor {vendedorNombre} editó el negocio '{negocio.NegocioNombre}'",
            negocio.NegocioNombre);

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// El vendedor elimina un negocio que él creó.
    /// Se distingue entre "no encontrado" (404) y otros errores (400)
    /// para que el frontend pueda mostrar el mensaje correcto al usuario.
    /// </summary>
    [HttpPost("VendedorEliminarNegocio")]
    public async Task<IActionResult> VendedorEliminarNegocio(Guid id)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
        if (negocio == null)
            return NotFound(new { success = false, message = "Negocio no encontrado" });

        // Verificación de propiedad antes de eliminar.
        if (negocio.VendedorId != vendedorId.Value)
            return Forbid();

        // Se guarda el nombre antes de eliminar porque después ya no estará disponible.
        var negocioNombre = negocio.NegocioNombre;
        var (success, message) = await _negocioService.EliminarNegocioAsync(id);

        if (!success)
        {
            // El servicio puede fallar por "no encontrado" u otras razones (ej: tiene datos).
            // Diferenciamos para devolver el código HTTP más correcto al frontend.
            if (message.Contains("no encontrado"))
                return NotFound(new { success = false, message });

            return BadRequest(new { success = false, message });
        }

        var vendedorNombre = User.Identity?.Name ?? "Vendedor";
        await _negocioService.RegistrarAdminLogAsync(
            "VendedorEliminarNegocio",
            $"Vendedor {vendedorNombre} eliminó el negocio '{negocioNombre}'",
            negocioNombre);

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Alterna el estado activo/prueba de un negocio.
    /// Un negocio puede estar en dos estados: "Pago (Activo)" o "Prueba".
    /// El servicio hace el toggle y devuelve el nuevo estado, que se
    /// refleja directamente en la tarjeta del dashboard del vendedor.
    /// </summary>
    [HttpPost("VendedorCambiarEstado")]
    public async Task<IActionResult> VendedorCambiarEstado(Guid id)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
        if (negocio == null)
            return NotFound(new { success = false, message = "Negocio no encontrado" });

        // Verificación de propiedad antes de cambiar el estado.
        if (negocio.VendedorId != vendedorId.Value)
            return Forbid();

        // El servicio alterna entre los estados y devuelve los valores nuevos.
        var (success, message, isActive, esPrueba) = await _negocioService.CambiarEstadoAsync(id);

        if (!success)
            return NotFound(new { success = false, message });

        // Se construye una etiqueta legible del estado para el log de auditoría.
        var estado = isActive == true ? "Pago (Activo)" : "Prueba";
        var vendedorNombre = User.Identity?.Name ?? "Vendedor";
        await _negocioService.RegistrarAdminLogAsync(
            "VendedorCambiarEstado",
            $"Vendedor {vendedorNombre} cambió estado de '{negocio.NegocioNombre}' a {estado}",
            negocio.NegocioNombre);

        // Se devuelven isActive y esPrueba para que el frontend actualice el badge
        // de estado sin necesidad de recargar la página.
        return Ok(new { success = true, message, isActive, esPrueba });
    }

    /// <summary>
    /// Bloquea un negocio que pertenece al vendedor autenticado.
    /// El negocio bloqueado no puede iniciar sesión ni operar hasta ser
    /// desbloqueado. Solo el vendedor propietario puede ejecutar esta acción.
    /// </summary>
    [HttpPost("VendedorBloquearNegocio")]
    public async Task<IActionResult> VendedorBloquearNegocio(Guid id)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
        if (negocio == null)
            return NotFound(new { success = false, message = "Negocio no encontrado" });

        // Verificación de propiedad: solo el vendedor dueño puede bloquear.
        if (negocio.VendedorId != vendedorId.Value)
            return Forbid();

        var (success, message) = await _negocioService.BloquearNegocioAsync(id);

        if (!success)
            return BadRequest(new { success = false, message });

        var vendedorNombre = User.Identity?.Name ?? "Vendedor";
        await _negocioService.RegistrarAdminLogAsync(
            "VendedorBloquearNegocio",
            $"Vendedor {vendedorNombre} bloqueó el negocio '{negocio.NegocioNombre}'",
            negocio.NegocioNombre);

        // Notificar al dueño del negocio por WhatsApp
        if (!string.IsNullOrWhiteSpace(negocio.Telefono))
        {
            var tel = negocio.Telefono;
            var nom = negocio.NegocioNombre;
            var dueno = negocio.DuenoNegocio ?? "Estimado cliente";
            _ = Task.Run(async () =>
            {
                try { await _whatsAppService.EnviarNotificacionNegocioBloqueadoWhatsAppAsync(tel, nom, dueno); }
                catch { }
            });
        }

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Desbloquea un negocio que pertenece al vendedor autenticado.
    /// Restaura el acceso operacional del negocio. Solo el vendedor
    /// propietario puede ejecutar esta acción.
    /// </summary>
    [HttpPost("VendedorDesbloquearNegocio")]
    public async Task<IActionResult> VendedorDesbloquearNegocio(Guid id)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var negocio = await _negocioService.GetNegocioForImpersonationAsync(id);
        if (negocio == null)
            return NotFound(new { success = false, message = "Negocio no encontrado" });

        // Verificación de propiedad: solo el vendedor dueño puede desbloquear.
        if (negocio.VendedorId != vendedorId.Value)
            return Forbid();

        var (success, message) = await _negocioService.DesbloquearNegocioAsync(id);

        if (!success)
            return BadRequest(new { success = false, message });

        var vendedorNombre = User.Identity?.Name ?? "Vendedor";
        await _negocioService.RegistrarAdminLogAsync(
            "VendedorDesbloquearNegocio",
            $"Vendedor {vendedorNombre} desbloqueó el negocio '{negocio.NegocioNombre}'",
            negocio.NegocioNombre);

        // Notificar al dueño del negocio por WhatsApp
        if (!string.IsNullOrWhiteSpace(negocio.Telefono))
        {
            var tel = negocio.Telefono;
            var nom = negocio.NegocioNombre;
            var dueno = negocio.DuenoNegocio ?? "Estimado cliente";
            _ = Task.Run(async () =>
            {
                try { await _whatsAppService.EnviarNotificacionNegocioDesbloqueadoWhatsAppAsync(tel, nom, dueno); }
                catch { }
            });
        }

        return Ok(new { success = true, message });
    }

    // ═══════════════════════════════════════════════════════════
    // SECCIÓN 6 — VENDEDOR: LEADS (INTERESADOS DE LANDING PAGE)
    //
    // Los leads son registros de personas que completaron el
    // formulario de contacto en el landing page público.
    // El vendedor los ve y los marca como "atendidos" una vez
    // que los contacta. Un contador sin leer aparece como badge
    // en el sidebar del dashboard.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Devuelve todos los leads ordenados por estado (pendientes primero) y fecha.
    /// Todos los vendedores ven todos los leads; no están filtrados por vendedor.
    /// Esto es intencional: cualquier vendedor puede atender cualquier lead.
    /// </summary>
    [HttpGet("GetVendedorLeads")]
    public async Task<IActionResult> GetVendedorLeads()
    {
        // Usamos IsVendedor() en lugar de GetVendedorId() porque aquí no necesitamos
        // filtrar por vendedor; cualquier vendedor autenticado puede ver todos los leads.
        if (!IsVendedor())
            return Forbid();

        var leads = await _vendedorService.GetLeadsAsync();
        return Ok(leads);
    }

    /// <summary>
    /// Marca un lead como atendido y registra qué vendedor lo atendió.
    /// Una vez marcado, el lead deja de aparecer en la lista de pendientes
    /// y el contador del badge se reduce.
    /// </summary>
    [HttpPost("MarcarLeadAtendido")]
    public async Task<IActionResult> MarcarLeadAtendido(Guid id)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        // El nombre del vendedor se registra en el lead para saber quién lo atendió.
        var vendedorNombre = User.Identity?.Name ?? "Vendedor";
        var (success, message) = await _vendedorService.MarcarLeadAtendidoAsync(
            id, vendedorId.Value, vendedorNombre);

        if (!success)
            return BadRequest(new { success = false, message });

        return Ok(new { success = true, message });
    }

    /// <summary>
    /// Devuelve el número de leads que aún no han sido atendidos.
    /// Se usa para mostrar un badge de notificación en el sidebar del dashboard.
    /// Al ser un número pequeño, es más eficiente pedirlo por separado
    /// que cargar todos los leads solo para contar.
    /// </summary>
    [HttpGet("GetVendedorLeadsCount")]
    public async Task<IActionResult> GetVendedorLeadsCount()
    {
        if (!IsVendedor())
            return Forbid();

        var count = await _vendedorService.GetLeadsCountAsync();
        return Ok(new { count });
    }

    // ═══════════════════════════════════════════════════════════
    // SECCIÓN 7 — VENDEDOR: MÉTODOS DE PAGO DEL ADMIN
    //
    // El vendedor necesita ver los métodos de pago que el admin
    // acepta para las suscripciones, y también sus propios datos
    // bancarios que el admin usará para pagarle comisiones.
    //
    // Los métodos de pago del admin se almacenan con
    // NegocioId = Guid.Empty (00000000-...-000000000000).
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Devuelve la lista de métodos de pago del administrador.
    /// Los vendedores necesitan esta información para indicar a los
    /// dueños de negocio cómo pagar sus suscripciones.
    /// Los métodos del admin usan NegocioId = Guid.Empty.
    /// </summary>
    [HttpGet("GetAdminMetodosPagoParaVendedor")]
    [Authorize(Roles = "Vendedor,Admin")]
    public async Task<IActionResult> GetAdminMetodosPagoParaVendedor()
    {
        var metodos = await _metodoPagoService.GetMetodosPagoAsync(Guid.Empty);
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
            m.Orden
        }));
    }

    /// <summary>
    /// Devuelve los datos completos de un método de pago del admin,
    /// incluyendo la imagen QR en Base64. Se usa para mostrar el detalle
    /// en un modal del dashboard del vendedor.
    /// </summary>
    [HttpGet("GetAdminMetodoPagoDetalle")]
    [Authorize(Roles = "Vendedor,Admin")]
    public async Task<IActionResult> GetAdminMetodoPagoDetalle(Guid metodoPagoId)
    {
        var metodo = await _metodoPagoService.GetMetodoPagoAsync(Guid.Empty, metodoPagoId);
        if (metodo == null)
            return NotFound(new { success = false, message = "Método de pago no encontrado" });

        return Ok(new
        {
            metodo.MetodoPagoId,
            metodo.Nombre,
            metodo.NumeroCuenta,
            metodo.Cedula,
            metodo.NombreTitular,
            metodo.ImagenQR,
            metodo.Instrucciones
        });
    }

    /// <summary>
    /// Devuelve los datos bancarios del vendedor autenticado.
    /// Estos datos son los que el admin usará para pagarle comisiones.
    /// El vendedor puede verlos como referencia en su panel.
    /// </summary>
    [HttpGet("GetVendedorDatosBancarios")]
    public async Task<IActionResult> GetVendedorDatosBancarios()
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var vendedor = await _vendedorService.GetVendedorAsync(vendedorId.Value);
        if (vendedor == null)
            return NotFound(new { success = false, message = "Vendedor no encontrado" });

        return Ok(new
        {
            vendedor.NombreBanco,
            vendedor.NumeroCedula,
            vendedor.NumeroCuenta,
            NombreCompleto = $"{vendedor.Nombre} {vendedor.Apellido}"
        });
    }

    [HttpPost("ActualizarDatosBancarios")]
    public async Task<IActionResult> ActualizarDatosBancarios(string nombreBanco, string numeroCuenta, string numeroCedula)
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        var result = await _vendedorService.ActualizarDatosBancariosAsync(
            vendedorId.Value, nombreBanco, numeroCuenta, numeroCedula);
        if (!result.success)
            return BadRequest(new { result.success, result.message });
        return Ok(new { result.success, result.message });
    }

    // ═══════════════════════════════════════════════════════════
    // SECCIÓN 8 — VENDEDOR: MIS COMISIONES
    //
    // El vendedor puede ver sus comisiones pendientes y pagadas,
    // incluyendo el método de pago usado por el admin.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Devuelve todas las comisiones del vendedor autenticado,
    /// separadas en pendientes y pagadas, con estadísticas de resumen.
    /// Incluye el campo MetodoPagoPago para las comisiones ya pagadas.
    /// </summary>
    [HttpGet("GetVendedorMisComisiones")]
    public async Task<IActionResult> GetVendedorMisComisiones()
    {
        var vendedorId = GetVendedorId();
        if (!vendedorId.HasValue)
            return Forbid();

        try
        {
            var comisiones = await _comisionService.GetComisionesVendedorAsync(vendedorId.Value);
            return Ok(comisiones);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error al cargar comisiones: " + ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PRIVADOS DE APOYO (HELPERS)
    //
    // Estos métodos encapsulan lógica repetida de extracción de
    // claims para mantener el código de los endpoints limpio.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Verifica si el usuario de la sesión actual tiene el rol "Vendedor".
    /// Devuelve true tanto para vendedores que hicieron login directamente
    /// como para admins que están impersonando a un vendedor.
    /// </summary>
    private bool IsVendedor()
    {
        // Se busca el claim de rol en la colección de claims de la cookie actual.
        return User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Vendedor");
    }

    /// <summary>
    /// Extrae el VendedorId de los claims de la sesión actual y lo convierte a Guid.
    /// Devuelve null si el claim no existe o si el valor no es un Guid válido,
    /// lo que indica que el usuario no es un vendedor o su sesión es inválida.
    /// </summary>
    private Guid? GetVendedorId()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "VendedorId");

        // Guid.TryParse evita una excepción si el claim tiene un valor malformado.
        if (claim != null && Guid.TryParse(claim.Value, out var id))
            return id;

        return null; // el llamador debe verificar .HasValue antes de usar el resultado
    }
}
