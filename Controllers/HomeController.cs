// ═══════════════════════════════════════════════════════════
// HomeController.cs — Controlador de la página de inicio (landing page)
// Muestra la página principal pública y maneja errores generales
// ═══════════════════════════════════════════════════════════

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Gimnasio.Models;

namespace Gimnasio.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Muestra la landing page pública del sistema
    /// </summary>
    public IActionResult Index()
    {
        return View();
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
