using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;
public class ClientesController : Controller
{
    private readonly ApplicationDbContext _context;
    public ClientesController(ApplicationDbContext context)
    {
        _context = context;
    }
    [HttpGet]
    [Route("/clientes")]
    public async Task <IActionResult>  Index()
    {
        return View();
    }
    [HttpGet]
    public async Task<IActionResult> GetClientes()
    {
        var cliente = _context.Clientes.ToListAsync();
        return Json(await cliente);
    }
}