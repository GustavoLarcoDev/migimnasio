using Gimnasio.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Gimnasio.Data;

namespace Gimnasio.Controllers
{
    public class GimnasiosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GimnasiosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Gimnasios
        public IActionResult Index()
        {
            return View();
        }

        // GET: Gimnasios/GetGimnasios
        [HttpGet]
        public async Task<IActionResult> GetGimnasios()
        {
            try
            {
                var gimnasios = await _context.Gimnasios
                    .Select(g => new
                    {
                        g.GimnasioId,
                        g.GimnasioNombre,
                        g.DuenoGimnasio,
                        g.Telefono,
                        g.Email,
                        g.IsActive,
                        g.EsPrueba,
                        g.FechaCreacion,
                        TotalClientes = g.Clientes.Count
                    })
                    .OrderByDescending(g => g.FechaCreacion)
                    .ToListAsync();

                return Ok(gimnasios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: Gimnasios/GetGimnasio/5
        [HttpGet]
        public async Task<IActionResult> GetGimnasio(Guid id)
        {
            try
            {
                var gimnasio = await _context.Gimnasios.FindAsync(id);

                if (gimnasio == null)
                {
                    return NotFound(new { success = false, message = "Gimnasio no encontrado" });
                }

                return Ok(new
                {
                    gimnasio.GimnasioId,
                    gimnasio.GimnasioNombre,
                    gimnasio.DuenoGimnasio,
                    gimnasio.Telefono,
                    gimnasio.Email,
                    gimnasio.Password,
                    gimnasio.IsActive,
                    gimnasio.EsPrueba
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: Gimnasios/Crear
        [HttpPost]
        public async Task<IActionResult> Crear([FromForm] Gym gimnasio)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Datos inválidos" });
                }

                // Verificar si ya existe un gimnasio con ese email
                var existeEmail = await _context.Gimnasios
                    .AnyAsync(g => g.Email == gimnasio.Email);

                if (existeEmail)
                {
                    return BadRequest(new { success = false, message = "Ya existe un gimnasio con ese email" });
                }

                gimnasio.GimnasioId = Guid.NewGuid();
                gimnasio.FechaCreacion = DateTime.Now;
                gimnasio.FechaDeActualizacion = DateTime.Now;
                gimnasio.IsActive = true;

                // Hash de password (deberías usar BCrypt o similar en producción)
                // gimnasio.Password = HashPassword(gimnasio.Password);

                _context.Gimnasios.Add(gimnasio);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Gimnasio creado exitosamente", id = gimnasio.GimnasioId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PUT: Gimnasios/Editar
        [HttpPost]
        public async Task<IActionResult> Editar([FromForm] Gym gimnasio)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Datos inválidos" });
                }

                var gimnasioExistente = await _context.Gimnasios.FindAsync(gimnasio.GimnasioId);

                if (gimnasioExistente == null)
                {
                    return NotFound(new { success = false, message = "Gimnasio no encontrado" });
                }

                // Verificar si el email ya existe en otro gimnasio
                var existeEmail = await _context.Gimnasios
                    .AnyAsync(g => g.Email == gimnasio.Email && g.GimnasioId != gimnasio.GimnasioId);

                if (existeEmail)
                {
                    return BadRequest(new { success = false, message = "Ya existe otro gimnasio con ese email" });
                }

                gimnasioExistente.GimnasioNombre = gimnasio.GimnasioNombre;
                gimnasioExistente.DuenoGimnasio = gimnasio.DuenoGimnasio;
                gimnasioExistente.Telefono = gimnasio.Telefono;
                gimnasioExistente.Email = gimnasio.Email;
                gimnasioExistente.IsActive = gimnasio.IsActive;
                gimnasioExistente.EsPrueba = gimnasio.EsPrueba;
                gimnasioExistente.FechaDeActualizacion = DateTime.Now;

                // Solo actualizar password si se proporcionó uno nuevo
                if (!string.IsNullOrEmpty(gimnasio.Password))
                {
                    gimnasioExistente.Password = gimnasio.Password;
                }

                _context.Update(gimnasioExistente);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Gimnasio actualizado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: Gimnasios/Eliminar/5
        [HttpPost]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            try
            {
                var gimnasio = await _context.Gimnasios
                    .Include(g => g.Clientes)
                    .FirstOrDefaultAsync(g => g.GimnasioId == id);

                if (gimnasio == null)
                {
                    return NotFound(new { success = false, message = "Gimnasio no encontrado" });
                }

                // Verificar si tiene clientes
                if (gimnasio.Clientes.Any())
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = $"No se puede eliminar. El gimnasio tiene {gimnasio.Clientes.Count} cliente(s) registrado(s)" 
                    });
                }

                _context.Gimnasios.Remove(gimnasio);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Gimnasio eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: Gimnasios/CambiarEstado
        [HttpPost]
        public async Task<IActionResult> CambiarEstado(Guid id)
        {
            try
            {
                var gimnasio = await _context.Gimnasios.FindAsync(id);

                if (gimnasio == null)
                {
                    return NotFound(new { success = false, message = "Gimnasio no encontrado" });
                }

                gimnasio.IsActive = !gimnasio.IsActive;
                gimnasio.FechaDeActualizacion = DateTime.Now;

                _context.Update(gimnasio);
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    success = true, 
                    message = $"Gimnasio {(gimnasio.IsActive ? "activado" : "desactivado")} exitosamente",
                    isActive = gimnasio.IsActive
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: Gimnasios/ExportExcel
        public async Task<IActionResult> ExportExcel()
        {
            try
            {
                var gimnasios = await _context.Gimnasios
                    .Include(g => g.Clientes)
                    .OrderByDescending(g => g.FechaCreacion)
                    .ToListAsync();

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Gimnasios");

                // Encabezados
                worksheet.Cell(1, 1).Value = "Nombre del Gimnasio";
                worksheet.Cell(1, 2).Value = "Dueño";
                worksheet.Cell(1, 3).Value = "Email";
                worksheet.Cell(1, 4).Value = "Teléfono";
                worksheet.Cell(1, 5).Value = "Estado";
                worksheet.Cell(1, 6).Value = "Es Prueba";
                worksheet.Cell(1, 7).Value = "Total Clientes";
                worksheet.Cell(1, 8).Value = "Fecha Creación";

                // Estilo encabezados
                var headerRange = worksheet.Range(1, 1, 1, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
                headerRange.Style.Font.FontColor = XLColor.White;

                // Datos
                int row = 2;
                foreach (var gimnasio in gimnasios)
                {
                    worksheet.Cell(row, 1).Value = gimnasio.GimnasioNombre;
                    worksheet.Cell(row, 2).Value = gimnasio.DuenoGimnasio;
                    worksheet.Cell(row, 3).Value = gimnasio.Email;
                    worksheet.Cell(row, 4).Value = gimnasio.Telefono;
                    worksheet.Cell(row, 5).Value = gimnasio.IsActive ? "Activo" : "Inactivo";
                    worksheet.Cell(row, 6).Value = gimnasio.EsPrueba ? "Sí" : "No";
                    worksheet.Cell(row, 7).Value = gimnasio.Clientes.Count;
                    worksheet.Cell(row, 8).Value = gimnasio.FechaCreacion.ToString("dd/MM/yyyy");
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                return File(
                    content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Gimnasios_{DateTime.Now:yyyyMMdd}.xlsx"
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}