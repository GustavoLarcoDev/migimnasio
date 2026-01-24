using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;
public class GimnasiosController : Controller
{
    private readonly ApplicationDbContext _context;
    public GimnasiosController(ApplicationDbContext context)
    {
        _context = context;
    }
    [HttpGet]
    [Route("/gimnasios")]
    public async Task <IActionResult>  Index()
    {
        return View();
    }
    [HttpGet]
    public async Task<IActionResult> GetClientes()
    {
        var gimnasio = _context.Gimnasios.ToListAsync();
        return Json(await gimnasio);
    }
}