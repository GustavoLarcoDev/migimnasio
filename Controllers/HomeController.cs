// ═══════════════════════════════════════════════════════════
// HomeController.cs — Controlador de la página de inicio (landing page)
// Muestra la página principal pública y maneja errores generales
// ═══════════════════════════════════════════════════════════

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Gimnasio.Models;
using Gimnasio.Data;

#nullable enable
namespace Gimnasio.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Muestra la landing page pública del sistema
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lead(string Name, string GymName, string Email, string? Phone, string? Message)
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(GymName) || string.IsNullOrWhiteSpace(Email))
            return BadRequest();

        var lead = new LeadVendedor
        {
            Id = Guid.NewGuid(),
            Nombre = Name.Trim(),
            NombreNegocio = GymName.Trim(),
            Email = Email.Trim(),
            Telefono = Phone?.Trim(),
            Mensaje = Message?.Trim(),
            FechaCreacion = DateTime.Now
        };

        _context.LeadsVendedor.Add(lead);
        await _context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Muestra la página de error genérica con el ID de seguimiento
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
