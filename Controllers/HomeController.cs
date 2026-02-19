// ═══════════════════════════════════════════════════════════════════════════
// HomeController.cs — Controlador de la página de inicio (landing page pública)
//
// Responsabilidades:
//   1. Mostrar la landing page principal que ven los visitantes anónimos.
//   2. Recibir y guardar leads (solicitudes de interés) enviados desde el
//      formulario de contacto de la landing page.
//   3. Mostrar la página de error genérica cuando algo falla en la app.
//
// Este controlador NO requiere autenticación. Es la puerta de entrada pública
// del sistema antes de que el usuario inicie sesión.
// ═══════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Gimnasio.Models;
using Gimnasio.Data;

#nullable enable
namespace Gimnasio.Controllers;

public class HomeController : Controller
{
    // ───────────────────────────────────────────────────────────────────────
    // Dependencias inyectadas
    // ASP.NET Core inyecta estas dependencias automáticamente en el constructor
    // gracias al sistema de Dependency Injection (DI) configurado en Program.cs.
    // ───────────────────────────────────────────────────────────────────────

    // Logger tipado: registra mensajes de diagnóstico asociados a este controlador
    private readonly ILogger<HomeController> _logger;

    // Contexto de base de datos EF Core: permite leer y escribir en SQL Server
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor del controlador. ASP.NET Core lo llama automáticamente
    /// e inyecta las dependencias registradas en Program.cs (DI container).
    /// </summary>
    /// <param name="logger">Logger tipado para este controlador.</param>
    /// <param name="context">Contexto de EF Core para acceso a la base de datos.</param>
    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LANDING PAGE
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Muestra la landing page pública del sistema (página de inicio).
    /// No requiere autenticación: cualquier visitante puede verla.
    /// La vista correspondiente está en Views/Home/Index.cshtml.
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LEADS — Formulario de contacto / interés desde la landing page
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Recibe y persiste un "lead": un visitante que llenó el formulario de
    /// contacto en la landing page para expresar interés en el producto.
    ///
    /// Este endpoint está protegido con tres capas de seguridad:
    ///   - [ValidateAntiForgeryToken]: previene ataques CSRF (falsificación de
    ///     solicitudes entre sitios). El token se genera en el formulario HTML
    ///     con @Html.AntiForgeryToken() y ASP.NET lo valida automáticamente.
    ///   - [EnableRateLimiting("lead")]: limita a 3 envíos por IP por minuto en
    ///     producción (500 en desarrollo) para frenar bots y spam. La política
    ///     "lead" se define en Program.cs con FixedWindowRateLimiter.
    ///   - Validación de formato de email antes de guardar.
    /// </summary>
    /// <param name="Name">Nombre completo del interesado.</param>
    /// <param name="GymName">Nombre del negocio que quiere usar el sistema.</param>
    /// <param name="Email">Correo electrónico de contacto.</param>
    /// <param name="Phone">Teléfono (opcional).</param>
    /// <param name="Message">Mensaje libre (opcional).</param>
    /// <returns>
    ///   200 OK si el lead se guardó correctamente.
    ///   400 Bad Request si faltan campos obligatorios o el email es inválido.
    ///   500 Internal Server Error si ocurre un error inesperado al guardar.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("lead")]
    public async Task<IActionResult> Lead(string Name, string GymName, string Email, string? Phone, string? Message)
    {
        try
        {
            // ── Validación de campos obligatorios ──────────────────────────
            // Se rechazan envíos vacíos antes de llegar a la base de datos.
            // IsNullOrWhiteSpace también captura cadenas con solo espacios.
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(GymName) || string.IsNullOrWhiteSpace(Email))
                return BadRequest();

            // ── Validación de formato de email ─────────────────────────────
            // Se reutiliza el atributo de Data Annotations de manera programática
            // para validar el formato sin necesidad de un modelo con [Required].
            // Trim() elimina espacios accidentales que el usuario pudo haber ingresado.
            if (!new EmailAddressAttribute().IsValid(Email.Trim()))
                return BadRequest();

            // ── Construcción del lead ──────────────────────────────────────
            // Se crea el objeto con un GUID único generado en la aplicación
            // (no delegado a la DB), lo que permite consistencia entre capas.
            // TimeHelper.Now centraliza la obtención de la fecha actual para
            // garantizar que toda la app use la misma zona horaria configurada.
            var lead = new LeadVendedor
            {
                Id = Guid.NewGuid(),
                Nombre = Name.Trim(),
                NombreNegocio = GymName.Trim(),
                Email = Email.Trim(),
                Telefono = Phone?.Trim(),    // ?. evita NullReferenceException si Phone es null
                Mensaje = Message?.Trim(),   // ídem para Message
                FechaCreacion = TimeHelper.Now
            };

            // ── Persistencia en base de datos ──────────────────────────────
            // Add() rastrea el objeto como "nuevo" en EF Core (estado Added).
            // SaveChangesAsync() ejecuta el INSERT de forma asíncrona para no
            // bloquear el hilo del servidor mientras espera la respuesta de SQL.
            _context.LeadsVendedor.Add(lead);
            await _context.SaveChangesAsync();

            // 200 OK le indica al JavaScript del formulario que fue exitoso,
            // permitiéndole mostrar un mensaje de confirmación al usuario.
            return Ok();
        }
        catch (Exception)
        {
            // Se captura cualquier excepción inesperada (ej. timeout de DB,
            // violación de constraint) y se retorna 500 sin exponer detalles
            // internos al cliente por razones de seguridad.
            return StatusCode(500);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MANEJO DE ERRORES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Muestra la página de error genérica cuando ocurre una excepción no
    /// controlada en cualquier parte de la aplicación.
    ///
    /// ASP.NET Core redirige aquí automáticamente si se configura en Program.cs
    /// con: app.UseExceptionHandler("/Home/Error")
    ///
    /// El atributo [ResponseCache] con NoStore=true es obligatorio aquí:
    /// si el navegador cacheara esta página, podría mostrar el error de una
    /// solicitud anterior para una solicitud exitosa posterior, confundiendo
    /// al usuario. Se fuerza a que siempre se obtenga una respuesta fresca.
    /// </summary>
    /// <returns>Vista de error con el ID de seguimiento de la solicitud fallida.</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Activity.Current?.Id es el ID de traza del sistema de diagnóstico
        // de .NET (OpenTelemetry / Activity API). Si no existe (ej. en
        // solicitudes simples), se usa HttpContext.TraceIdentifier como
        // fallback, que es el ID único que ASP.NET asigna a cada request.
        // Ambos sirven para correlacionar el error en los logs del servidor.
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
