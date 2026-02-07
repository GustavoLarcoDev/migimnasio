using Gimnasio.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Gimnasio.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Controllers
{
    public class GimnasiosController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        // Constantes para el administrador
        private const string ADMIN_EMAIL = "migimnasio10@gmail.com";
        private const string ADMIN_PASSWORD = "DanielMaurizio2025!";

        public GimnasiosController(ApplicationDbContext context)
        {
            _context = context;
        }
        #region  Administradores de Gimnasios
        // Método helper para verificar si el usuario es admin
        private bool IsAdmin()
        {
            if (!User.Identity.IsAuthenticated)
                return false;
                
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email);
            return emailClaim != null && emailClaim.Value == ADMIN_EMAIL;
        }
        
        // GET: Gimnasios
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult Index()
        {
            // Verificar si es admin
            if (!IsAdmin())
            {
                TempData["Error"] = "No tiene permisos para acceder a esta página";
                return RedirectToAction("Login");
            }
            
            return View();
        }
        
        // GET: Gimnasios/GetGimnasios
        [HttpGet]
        public async Task<IActionResult> GetGimnasios()
        {
            try
            {
                // Verificar si es admin
                if (!IsAdmin())
                {
                    return Forbid();
                }
                
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
                        TotalClientes = _context.Clientes.Count(c => c.GimnasioId == g.GimnasioId)
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

        // GET: Gimnasios/GetGimnasio/id
        [HttpGet]
        public async Task<IActionResult> GetGimnasio(Guid id)
        {
            try
            {
                // Verificar si es admin
                if (!IsAdmin())
                {
                    return Forbid();
                }
                
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

        // GET: Gimnasios/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            // Verificar si es admin
            if (!IsAdmin())
            {
                TempData["Error"] = "No tiene permisos para acceder a esta página";
                return RedirectToAction("Login");
            }
            
            return View("Create");
        }

        // POST: Gimnasios/Create
        [HttpPost]
        public async Task<IActionResult> Create(string NombreGimnasio, string duenoGimnasio, string telefono, string EmailGimnasio, 
                                                string passwordGimnasio, bool isActive, bool esPrueba)
        {
            try
            {
                // Verificar si es admin
                if (!IsAdmin())
                {
                    return Forbid();
                }
                
                // Validación: No pueden ser ambos true o ambos false
                if (isActive && esPrueba)
                {
                    return BadRequest("Un gimnasio no puede ser de pago y de prueba al mismo tiempo");
                }

                if (!isActive && !esPrueba)
                {
                    return BadRequest("Debe seleccionar si el gimnasio es de pago o de prueba");
                }

                // Validaciones existentes
                if (string.IsNullOrWhiteSpace(EmailGimnasio))
                {
                    return BadRequest("Correo del Gimnasio es necesario");
                }

                bool existing = await _context.Gimnasios.AnyAsync(c => c.Email == EmailGimnasio);
                if (existing)
                {
                    return BadRequest("Gimnasio con este email ya existe");
                }

                if (string.IsNullOrWhiteSpace(NombreGimnasio))
                {
                    return BadRequest("Nombre del Gimnasio es necesario");
                }

                if (string.IsNullOrWhiteSpace(duenoGimnasio))
                {
                    return BadRequest("Dueño Gimnasio es necesario");
                }

                if (string.IsNullOrWhiteSpace(telefono))
                {
                    return BadRequest("Teléfono es necesario");
                }

                if (string.IsNullOrWhiteSpace(passwordGimnasio))
                {
                    return BadRequest("Password es necesario");
                }

                // Crear el gimnasio con la lógica correcta
                var gimnasio = new Gym
                {
                    GimnasioId = Guid.NewGuid(),
                    GimnasioNombre = NombreGimnasio,
                    DuenoGimnasio = duenoGimnasio,
                    Telefono = telefono,
                    Email = EmailGimnasio,
                    Password = passwordGimnasio,
                    IsActive = isActive,
                    EsPrueba = esPrueba,
                    FechaCreacion = DateTime.Now,
                    FechaDeActualizacion = DateTime.Now,
                };

                _context.Gimnasios.Add(gimnasio);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Gimnasio creado exitosamente" });
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
                // Verificar si es admin
                if (!IsAdmin())
                {
                    return Forbid();
                }
                
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Datos inválidos" });
                }

                // Validación: No pueden ser ambos true o ambos false
                if (gimnasio.IsActive && gimnasio.EsPrueba)
                {
                    return BadRequest(new { success = false, message = "Un gimnasio no puede ser de pago y de prueba al mismo tiempo" });
                }

                if (!gimnasio.IsActive && !gimnasio.EsPrueba)
                {
                    return BadRequest(new { success = false, message = "Debe seleccionar si el gimnasio es de pago o de prueba" });
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
                // Verificar si es admin
                if (!IsAdmin())
                {
                    return Forbid();
                }
                
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
                // Verificar si es admin
                if (!IsAdmin())
                {
                    return Forbid();
                }
                
                var gimnasio = await _context.Gimnasios.FindAsync(id);

                if (gimnasio == null)
                {
                    return NotFound(new { success = false, message = "Gimnasio no encontrado" });
                }

                if (gimnasio.IsActive)
                {
                    gimnasio.IsActive = false;
                    gimnasio.EsPrueba = true;
                }
                else
                {
                    gimnasio.IsActive = true;
                    gimnasio.EsPrueba = false;
                }

                gimnasio.FechaDeActualizacion = DateTime.Now;

                _context.Update(gimnasio);
                await _context.SaveChangesAsync();

                string tipoActual = gimnasio.IsActive ? "Pago (Activo)" : "Prueba";
        
                return Ok(new 
                { 
                    success = true, 
                    message = $"Gimnasio cambiado a modo {tipoActual} exitosamente",
                    isActive = gimnasio.IsActive,
                    esPrueba = gimnasio.EsPrueba
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
                // Verificar si es admin
                if (!IsAdmin())
                {
                    return Forbid();
                }
                
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
        #endregion
        
        #region Autenticacion de Gimnasios

        // GET: Gimnasios/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Si es admin, redirigir al índice de gimnasios
                if (IsAdmin())
                {
                    return RedirectToAction("Index");
                }
                
                // Si es un gimnasio regular, redirigir a su dashboard
                var gimnasioIdClaim = User.Claims.FirstOrDefault(c => c.Type == "GimnasioId");
                if (gimnasioIdClaim != null)
                {
                   return RedirectToAction("Dashboard", new { id = gimnasioIdClaim.Value });
                }
            }
            return View();
        }

        // POST: Gimnasios/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try 
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    ViewBag.Error = "Email y contraseña son obligatorios";
                    return View();
                }

                // Verificar si es el administrador
                if (email == ADMIN_EMAIL && password == ADMIN_PASSWORD)
                {
                    var adminClaims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Administrador"),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, ADMIN_EMAIL),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")
                    };

                    var adminIdentity = new System.Security.Claims.ClaimsIdentity(adminClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var adminAuthProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new System.Security.Claims.ClaimsPrincipal(adminIdentity),
                        adminAuthProperties);

                    return RedirectToAction("Index");
                }

                // Si no es admin, buscar en gimnasios
                var gimnasio = await _context.Gimnasios
                    .FirstOrDefaultAsync(g => g.Email == email && g.Password == password);

                if (gimnasio == null)
                {
                    ViewBag.Error = "Credenciales inválidas";
                    return View();
                }

                if (!gimnasio.IsActive && !gimnasio.EsPrueba)
                {
                    ViewBag.Error = "Su cuenta no está activa. Contacte al administrador.";
                    return View();
                }

                // Crear Claims para gimnasio
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, gimnasio.GimnasioNombre),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, gimnasio.Email),
                    new System.Security.Claims.Claim("GimnasioId", gimnasio.GimnasioId.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Gimnasio")
                };

                var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Dashboard", new { id = gimnasio.GimnasioId });
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Ocurrió un error al intentar iniciar sesión: " + ex.Message;
                return View();
            }
        }

        // GET: Gimnasios/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            return RedirectToAction("Login");
        }
        #endregion

        #region Datos y tablas del dashboard

        // GET: Gimnasios/Dashboard/5
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("Gimnasios/{id}/Dashboard")]
        public async Task<IActionResult> Dashboard(Guid id)
        {
            try
            {
                // Verificar que el usuario logueado sea el dueño del dashboard
                var gimnasioIdClaim = User.Claims.FirstOrDefault(c => c.Type == "GimnasioId");
                if (gimnasioIdClaim == null || gimnasioIdClaim.Value != id.ToString())
                {
                    return Forbid();
                }

                var gimnasio = await _context.Gimnasios
                    .Include(g => g.Clientes)
                    .FirstOrDefaultAsync(g => g.GimnasioId == id);

                if (gimnasio == null)
                {
                    return NotFound();
                }

                return View(gimnasio);
            }
            catch (Exception ex)
            {
                 return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        
        // GET: Gimnasios/GetDashboardStats
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpGet]
         public async Task<IActionResult> GetDashboardStats(Guid gimnasioId)
        {
            try
            {
                var clientes = await _context.Clientes
                    .Where(c => c.GimnasioId == gimnasioId)
                    .ToListAsync();

                var totalClientes = clientes.Count;
                
                // Cliente activo si FechaQueTermina >= Hoy
                var clientesActivos = clientes.Count(c => c.FechaQueTermina.Date >= DateTime.Now.Date);
                
                // Clientes vencidos
                var clientesVencidos = clientes.Count(c => c.FechaQueTermina.Date < DateTime.Now.Date);

                // Nuevos hoy
                var clientesNuevosHoy = clientes.Count(c => c.FechaDeCreacion.Date == DateTime.Now.Date);
                
                // Nuevos mes
                var clientesNuevosMes = clientes.Count(c => c.FechaDeCreacion.Month == DateTime.Now.Month && c.FechaDeCreacion.Year == DateTime.Now.Year);

                // Ingresos hoy (suma de precio de clientes creados hoy? O renovaciones hoy? 
                // Simplificación: usaremos FechaDeActualizacion para renovaciones y FechaDeCreacion para nuevos.
                // Como no tenemos tabla de transacciones separada, sumaremos el precio de los clientes creados o actualizados hoy
                // NOTA: Esto es una aproximación. Lo ideal sería una tabla de Pagos.
                // Para este ejemplo, sumaremos Precio de clientes creados ESTE MES para "Ingresos Mes"
                var ingresosMes = clientes
                    .Where(c => c.FechaDeActualizacion.Month == DateTime.Now.Month && c.FechaDeActualizacion.Year == DateTime.Now.Year)
                    .Sum(c => c.Precio);

                var ingresosHoy = clientes
                    .Where(c => c.FechaDeActualizacion.Date == DateTime.Now.Date)
                    .Sum(c => c.Precio);

                // Próximos a vencer (en los próximos 5 días) y que NO estén ya vencidos
                var proximosVencer = clientes
                    .Where(c => c.FechaQueTermina.Date >= DateTime.Now.Date && c.FechaQueTermina.Date <= DateTime.Now.AddDays(5).Date)
                    .Select(c => new 
                    {
                        c.Nombre,
                        c.Apellido,
                        NombreCompleto = $"{c.Nombre} {c.Apellido}",
                        c.Telefono,
                        c.FechaQueTermina,
                        DiasRestantes = (c.FechaQueTermina.Date - DateTime.Now.Date).Days
                    })
                    .OrderBy(c => c.DiasRestantes)
                    .ToList();

                return Ok(new
                {
                    totalClientes,
                    clientesActivos,
                    clientesVencidos,
                    clientesNuevosHoy,
                    clientesNuevosMes,
                    ingresosMes,
                    ingresosHoy,
                    proximosVencer
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: Gimnasios/GetClientes
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpGet]
         public async Task<IActionResult> GetClientes(Guid gimnasioId)
        {
            try
            {
                var clientes = await _context.Clientes
                    .Where(c => c.GimnasioId == gimnasioId)
                    .OrderByDescending(c => c.FechaQueTermina) // Mostrar los que vencen más lejos primero, o al revés?
                    // Mejor los activos primero? O por fecha creación? Ordenemos por Vencimiento ascendente (los que vencen pronto primero)
                    // que sean >= hoy. Y luego los vencidos.
                    // Para simplificar, devolvemos todos y el front ordena.
                    .Select(c => new 
                    {
                        c.ClienteId,
                        c.Nombre,
                        c.Apellido,
                        NombreCompleto = $"{c.Nombre} {c.Apellido}",
                        c.Email,
                        c.Telefono,
                        c.Direccion,
                        c.Dias,
                        c.Precio,
                        c.FechaDeCreacion,
                        c.FechaQueTermina,
                        DiasRestantes = (c.FechaQueTermina.Date - DateTime.Now.Date).Days,
                        EstaActivo = c.FechaQueTermina.Date >= DateTime.Now.Date
                    })
                    .ToListAsync();

                return Ok(clientes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
         #endregion
         
         #region Todo relacionado con CRUD clientes de los gimnasios
         // POST: Gimnasios/CrearCliente
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpPost]
         public async Task<IActionResult> CrearCliente([FromForm] ClienteCreateModel model)
         {
             try
             {
                 // Verificar autorización
                 var gimnasioIdClaim = User.Claims.FirstOrDefault(c => c.Type == "GimnasioId");
                 if (gimnasioIdClaim == null || model.GimnasioId.ToString() != gimnasioIdClaim.Value)
                 {
                      return Forbid();
                 }

                 // Validaciones básicas
                 if (string.IsNullOrWhiteSpace(model.Nombre) || string.IsNullOrWhiteSpace(model.Apellido))
                 {
                     return BadRequest(new { success = false, message = "Nombre y Apellido son obligatorios" });
                 }

                 if (string.IsNullOrWhiteSpace(model.Telefono))
                 {
                     return BadRequest(new { success = false, message = "Teléfono es obligatorio" });
                 }

                 if (model.Dias <= 0)
                 {
                     return BadRequest(new { success = false, message = "Los días deben ser mayor a 0" });
                 }

                 if (model.Precio <= 0)
                 {
                     return BadRequest(new { success = false, message = "El precio debe ser mayor a 0" });
                 }

                 var cliente = new Cliente
                 {
                     ClienteId = Guid.NewGuid(),
                     GimnasioId = model.GimnasioId,
                     Nombre = model.Nombre,
                     Apellido = model.Apellido,
                     Email = model.Email,
                     Telefono = model.Telefono,
                     Direccion = model.Direccion,
                     Dias = model.Dias,
                     Precio = model.Precio,
                     EsDiario = model.EsDiario,
                     FechaDeCreacion = DateTime.Now,
                     FechaDeActualizacion = DateTime.Now,
                     FechaQueTermina = DateTime.Now.AddDays(model.Dias)
                 };

                 _context.Clientes.Add(cliente);
                 await _context.SaveChangesAsync();

                 // Create automatic log for client creation
                 var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
                 await CreateLogAsync(
                     gimnasioId: model.GimnasioId,
                     tipo: "cliente_creado",
                     message: $"Nuevo cliente creado: {nombreCompleto}",
                     monto: model.Precio,
                     clienteId: cliente.ClienteId,
                     nombreCliente: nombreCompleto
                 );

                 return Ok(new { success = true, message = "Cliente creado exitosamente" });
             }
              catch (Exception ex)
             {
                 return StatusCode(500, new { success = false, message = ex.Message });
             }
         }

        // GET: Gimnasios/GetCliente
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpGet]
         public async Task<IActionResult> GetCliente(Guid id, Guid gimnasioId)
        {
            try
            {
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.ClienteId == id && c.GimnasioId == gimnasioId);

                if (cliente == null)
                {
                    return NotFound(new { success = false, message = "Cliente no encontrado" });
                }

                return Ok(cliente);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: Gimnasios/EditarCliente
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpPost]
         public async Task<IActionResult> EditarCliente([FromForm] ClienteCreateModel model)
         {
             try
             {
                 // Verificar autorización
                 var gimnasioIdClaim = User.Claims.FirstOrDefault(c => c.Type == "GimnasioId");
                 if (gimnasioIdClaim == null || model.GimnasioId.ToString() != gimnasioIdClaim.Value)
                 {
                      return Forbid();
                 }

                 var cliente = await _context.Clientes
                     .FirstOrDefaultAsync(c => c.ClienteId == model.ClienteId && c.GimnasioId == model.GimnasioId);

                 if (cliente == null)
                 {
                     return NotFound(new { success = false, message = "Cliente no encontrado" });
                 }

                 // Validaciones básicas
                 if (string.IsNullOrWhiteSpace(model.Nombre) || string.IsNullOrWhiteSpace(model.Apellido))
                 {
                     return BadRequest(new { success = false, message = "Nombre y Apellido son obligatorios" });
                 }

                 if (string.IsNullOrWhiteSpace(model.Telefono))
                 {
                     return BadRequest(new { success = false, message = "Teléfono es obligatorio" });
                 }

                 if (model.Dias <= 0)
                 {
                     return BadRequest(new { success = false, message = "Los días deben ser mayor a 0" });
                 }

                 if (model.Precio <= 0)
                 {
                     return BadRequest(new { success = false, message = "El precio debe ser mayor a 0" });
                 }

                 cliente.Nombre = model.Nombre;
                 cliente.Apellido = model.Apellido;
                 cliente.Email = model.Email;
                 cliente.Telefono = model.Telefono;
                 cliente.Direccion = model.Direccion;
                 // Si cambiamos los días y precio en edición, ¿Debería afectar la fecha de término?
                 // Asumiremos que edición es solo CORRECCIÓN de datos.
                 // Si quiere renovar, usará la función Renovar.
                 // PERO permitiremos actualizar Dias y Precio como referencia (último pago).
                 cliente.Dias = model.Dias;
                 cliente.Precio = model.Precio;
                 cliente.EsDiario = model.EsDiario;
                 cliente.FechaDeActualizacion = DateTime.Now;

                 _context.Update(cliente);
                 await _context.SaveChangesAsync();

                 // Create automatic log for client update
                 var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
                 await CreateLogAsync(
                     gimnasioId: model.GimnasioId,
                     tipo: "cliente_editado",
                     message: $"Cliente editado: {nombreCompleto}",
                     monto: 0,
                     clienteId: cliente.ClienteId,
                     nombreCliente: nombreCompleto
                 );

                 return Ok(new { success = true, message = "Cliente actualizado exitosamente" });
             }
             catch (Exception ex)
             {
                 return StatusCode(500, new { success = false, message = ex.Message });
             }
         }

        // POST: Gimnasios/EliminarCliente
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpPost]
         public async Task<IActionResult> EliminarCliente(Guid id, Guid gimnasioId)
        {
            try
            {
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.ClienteId == id && c.GimnasioId == gimnasioId);

                if (cliente == null)
                {
                    return NotFound(new { success = false, message = "Cliente no encontrado" });
                }

                var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
                
                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();

                // Create automatic log for client deletion
                await CreateLogAsync(
                    gimnasioId: gimnasioId,
                    tipo: "cliente_eliminado",
                    message: $"Cliente eliminado: {nombreCompleto}",
                    monto: 0,
                    clienteId: cliente.ClienteId,
                    nombreCliente: nombreCompleto
                );

                return Ok(new { success = true, message = "Cliente eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: Gimnasios/RenovarCliente
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpPost]
         public async Task<IActionResult> RenovarCliente(Guid id, Guid gimnasioId, int dias, decimal precio)
        {
            try
            {
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.ClienteId == id && c.GimnasioId == gimnasioId);

                if (cliente == null)
                {
                    return NotFound(new { success = false, message = "Cliente no encontrado" });
                }

                if (dias <= 0)
                {
                     return BadRequest(new { success = false, message = "Días debe ser mayor a 0" });
                }

                // Lógica de renovación:
                // Si ya estaba vencido, empieza a contar desde HOY.
                // Si NO estaba vencido, se suman los días a la fecha que terminaba.
                
                if (cliente.FechaQueTermina < DateTime.Now)
                {
                    cliente.FechaQueTermina = DateTime.Now.AddDays(dias);
                }
                else
                {
                    cliente.FechaQueTermina = cliente.FechaQueTermina.AddDays(dias);
                }

                cliente.Dias = dias; // Guardamos el último paquete comprado
                cliente.Precio = precio; // Guardamos el último precio pagado
                cliente.FechaDeActualizacion = DateTime.Now;

                _context.Update(cliente);
                await _context.SaveChangesAsync();

                // Create automatic log for client renewal
                var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
                await CreateLogAsync(
                    gimnasioId: gimnasioId,
                    tipo: "cliente_renovado",
                    message: $"Renovación: {nombreCompleto} ({dias} días)",
                    monto: precio,
                    clienteId: cliente.ClienteId,
                    nombreCliente: nombreCompleto
                );

                return Ok(new { success = true, message = "Membresía renovada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: Gimnasios/ExportClientesExcel
         [Microsoft.AspNetCore.Authorization.Authorize]
         public async Task<IActionResult> ExportClientesExcel(Guid gimnasioId)
        {
            try
            {
                var clientes = await _context.Clientes
                    .Where(c => c.GimnasioId == gimnasioId)
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Clientes");

                // Encabezados
                worksheet.Cell(1, 1).Value = "Nombre";
                worksheet.Cell(1, 2).Value = "Apellido";
                worksheet.Cell(1, 3).Value = "Email";
                worksheet.Cell(1, 4).Value = "Teléfono";
                worksheet.Cell(1, 5).Value = "Fecha Inicio"; // Fecha Creación
                worksheet.Cell(1, 6).Value = "Fecha Vencimiento";
                worksheet.Cell(1, 7).Value = "Estado";
                worksheet.Cell(1, 8).Value = "Tipo";
                worksheet.Cell(1, 9).Value = "Último Precio";

                // Estilo
                var headerRange = worksheet.Range(1, 1, 1, 9);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
                headerRange.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var cliente in clientes)
                {
                    worksheet.Cell(row, 1).Value = cliente.Nombre;
                    worksheet.Cell(row, 2).Value = cliente.Apellido;
                    worksheet.Cell(row, 3).Value = cliente.Email;
                    worksheet.Cell(row, 4).Value = cliente.Telefono;
                    worksheet.Cell(row, 5).Value = cliente.FechaDeCreacion.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 6).Value = cliente.FechaQueTermina.ToString("dd/MM/yyyy");
                    
                    bool activo = cliente.FechaQueTermina.Date >= DateTime.Now.Date;
                    worksheet.Cell(row, 7).Value = activo ? "Activo" : "Vencido";
                    
                    worksheet.Cell(row, 8).Value = cliente.EsDiario ? "Diario" : "Regular";
                    worksheet.Cell(row, 9).Value = cliente.Precio;
                    
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                return File(
                    content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Clientes_{DateTime.Now:yyyyMMdd}.xlsx"
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

         // GET: Gimnasios/GetClientesDiarios
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpGet]
         public async Task<IActionResult> GetClientesDiarios(Guid gimnasioId)
         {
             try
             {
                 var clientes = await _context.Clientes
                     .Where(c => c.GimnasioId == gimnasioId && c.EsDiario)
                     .OrderByDescending(c => c.FechaDeCreacion)
                     .Select(c => new 
                     {
                         c.ClienteId,
                         c.Nombre,
                         c.Apellido,
                         NombreCompleto = $"{c.Nombre} {c.Apellido}",
                         c.Telefono,
                         c.FechaDeCreacion
                     })
                     .ToListAsync();

                 return Ok(clientes);
             }
             catch (Exception ex)
             {
                 return StatusCode(500, new { success = false, message = ex.Message });
             }
         }



        // POST: Gimnasios/ImportarClientesExcel
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost]
        public async Task<IActionResult> ImportarClientesExcel(Guid gimnasioId, IFormFile file)
        {
            try
            {
                // Verificar autorización
                var gimnasioIdClaim = User.Claims.FirstOrDefault(c => c.Type == "GimnasioId");
                if (gimnasioIdClaim == null || gimnasioId.ToString() != gimnasioIdClaim.Value)
                {
                    return Forbid();
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No se ha proporcionado ningún archivo" });
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    return BadRequest(new { success = false, message = "El archivo debe ser un Excel (.xlsx o .xls)" });
                }

                var clientesCreados = new List<string>();
                var clientesOmitidos = new List<string>();
                var errores = new List<string>();

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // Saltar encabezado

                if (rows == null)
                {
                    return BadRequest(new { success = false, message = "El archivo está vacío" });
                }

                // Obtener clientes existentes para validación de duplicados
                var clientesExistentes = await _context.Clientes
                    .Where(c => c.GimnasioId == gimnasioId)
                    .Select(c => new { c.Nombre, c.Apellido })
                    .ToListAsync();

                int rowNumber = 2;
                foreach (var row in rows)
                {
                    try
                    {
                        var nombre = row.Cell(1).GetValue<string>()?.Trim();
                        var apellido = row.Cell(2).GetValue<string>()?.Trim();

                        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
                        {
                            errores.Add($"Fila {rowNumber}: Nombre y Apellido son obligatorios");
                            rowNumber++;
                            continue;
                        }

                        // Validación case-sensitive de duplicados
                        var existeDuplicado = clientesExistentes.Any(c => 
                            c.Nombre == nombre && c.Apellido == apellido);

                        if (existeDuplicado)
                        {
                            clientesOmitidos.Add($"{nombre} {apellido}");
                            rowNumber++;
                            continue;
                        }

                        // Leer campos opcionales
                        var email = row.Cell(3).GetValue<string>()?.Trim();
                        var telefono = row.Cell(4).GetValue<string>()?.Trim();
                        var direccion = row.Cell(5).GetValue<string>()?.Trim();
                        
                        int dias = 30; // Por defecto 30 días
                        decimal precio = 0;
                        
                        try { dias = row.Cell(6).GetValue<int>(); } catch { }
                        try { precio = row.Cell(7).GetValue<decimal>(); } catch { }
                        if (dias <= 0) dias = 30;

                        var cliente = new Cliente
                        {
                            ClienteId = Guid.NewGuid(),
                            GimnasioId = gimnasioId,
                            Nombre = nombre,
                            Apellido = apellido,
                            Email = email,
                            Telefono = telefono ?? "",
                            Direccion = direccion,
                            Dias = dias,
                            Precio = precio,
                            EsDiario = false,
                            FechaDeCreacion = DateTime.Now,
                            FechaDeActualizacion = DateTime.Now,
                            FechaQueTermina = DateTime.Now.AddDays(dias)
                        };

                        _context.Clientes.Add(cliente);
                        clientesCreados.Add($"{nombre} {apellido}");
                        
                        // Agregar a la lista de existentes para evitar duplicados dentro del mismo archivo
                        clientesExistentes.Add(new { Nombre = nombre, Apellido = apellido });
                    }
                    catch (Exception ex)
                    {
                        errores.Add($"Fila {rowNumber}: {ex.Message}");
                    }
                    rowNumber++;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Importación completada",
                    clientesCreados = clientesCreados.Count,
                    clientesOmitidos = clientesOmitidos.Count,
                    erroresCount = errores.Count,
                    detalleCreados = clientesCreados,
                    detalleOmitidos = clientesOmitidos,
                    detalleErrores = errores
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
         #endregion
         
         private async Task CreateLogAsync(Guid gimnasioId, string tipo, string message, decimal monto = 0, Guid? clienteId = null, string nombreCliente = null)
        {
            try
            {
                var log = new Logs
                {
                    Id = Guid.NewGuid(),
                    GimnasioId = gimnasioId,
                    Message = message,
                    Monto = monto,
                    Tipo = tipo,
                    ClienteId = clienteId,
                    NombreCliente = nombreCliente,
                    Fecha = DateTime.Now
                };

                _context.Logs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking main operations
                Console.WriteLine($"Error creating log: {ex.Message}");
            }
        }

        #region CRUD de Logs
         
         // POST: Gimnasios/CrearLog
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpPost]
         public async Task<IActionResult> CrearLog([FromForm] LogCreateModel model)
         {
             try
             {
                 // Verificar autorización
                 var gimnasioIdClaim = User.Claims.FirstOrDefault(c => c.Type == "GimnasioId");
                 if (gimnasioIdClaim == null || model.GimnasioId.ToString() != gimnasioIdClaim.Value)
                 {
                     return Forbid();
                 }

                 if (string.IsNullOrWhiteSpace(model.Message))
                 {
                     return BadRequest(new { success = false, message = "La descripción es obligatoria" });
                 }

                 var log = new Logs
                 {
                     Id = Guid.NewGuid(),
                     GimnasioId = model.GimnasioId,
                     Message = model.Message,
                     Monto = model.Monto,
                     Tipo = model.Monto >= 0 ? "ingreso" : "gasto",
                     Fecha = DateTime.Now
                 };

                 _context.Logs.Add(log);
                 await _context.SaveChangesAsync();

                 return Ok(new { success = true, message = "Log registrado exitosamente" });
             }
             catch (Exception ex)
             {
                 return StatusCode(500, new { success = false, message = ex.Message });
             }
         }

         // GET: Gimnasios/GetLogs
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpGet]
         public async Task<IActionResult> GetLogs(Guid gimnasioId)
         {
             try
             {
                 // Verificar autorización
                 var gimnasioIdClaim = User.Claims.FirstOrDefault(c => c.Type == "GimnasioId");
                 if (gimnasioIdClaim == null || gimnasioId.ToString() != gimnasioIdClaim.Value)
                 {
                     return Forbid();
                 }

                 var logs = await _context.Logs
                     .Where(l => l.GimnasioId == gimnasioId)
                     .OrderByDescending(l => l.Fecha)
                     .Select(l => new
                     {
                         l.Id,
                         l.Message,
                         l.Monto,
                         l.Tipo,
                         l.ClienteId,
                         l.NombreCliente,
                         l.Fecha
                     })
                     .ToListAsync();

                 return Ok(logs);
             }
             catch (Exception ex)
             {
                 return StatusCode(500, new { success = false, message = ex.Message });
             }
         }

         // GET: Gimnasios/GetLog
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpGet]
         public async Task<IActionResult> GetLog(Guid id, Guid gimnasioId)
         {
             try
             {
                 var log = await _context.Logs
                     .FirstOrDefaultAsync(l => l.Id == id && l.GimnasioId == gimnasioId);

                 if (log == null)
                 {
                     return NotFound(new { success = false, message = "Log no encontrado" });
                 }

                 return Ok(log);
             }
             catch (Exception ex)
             {
                 return StatusCode(500, new { success = false, message = ex.Message });
             }
         }

         // POST: Gimnasios/EditarLog
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpPost]
         public async Task<IActionResult> EditarLog(Guid id, Guid gimnasioId, string message, decimal monto)
         {
             try
             {
                 var log = await _context.Logs
                     .FirstOrDefaultAsync(l => l.Id == id && l.GimnasioId == gimnasioId);

                 if (log == null)
                 {
                     return NotFound(new { success = false, message = "Log no encontrado" });
                 }

                 log.Message = message;
                 log.Monto = monto;
                 log.Tipo = monto >= 0 ? "ingreso" : "gasto";

                 _context.Update(log);
                 await _context.SaveChangesAsync();

                 return Ok(new { success = true, message = "Log actualizado exitosamente" });
             }
             catch (Exception ex)
             {
                 return StatusCode(500, new { success = false, message = ex.Message });
             }
         }

         // POST: Gimnasios/EliminarLog
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpPost]
         public async Task<IActionResult> EliminarLog(Guid id, Guid gimnasioId)
         {
             try
             {
                 var log = await _context.Logs
                     .FirstOrDefaultAsync(l => l.Id == id && l.GimnasioId == gimnasioId);

                 if (log == null)
                 {
                     return NotFound(new { success = false, message = "Log no encontrado" });
                 }

                 _context.Logs.Remove(log);
                 await _context.SaveChangesAsync();

                 return Ok(new { success = true, message = "Log eliminado exitosamente" });
             }
             catch (Exception ex)
             {
                 return StatusCode(500, new { success = false, message = ex.Message });
             }
         }

         // GET: Gimnasios/ExportLogsExcel
         [Microsoft.AspNetCore.Authorization.Authorize]
         public async Task<IActionResult> ExportLogsExcel(Guid gimnasioId)
         {
             try
             {
                 // Verificar autorización
                 var gimnasioIdClaim = User.Claims.FirstOrDefault(c => c.Type == "GimnasioId");
                 if (gimnasioIdClaim == null || gimnasioId.ToString() != gimnasioIdClaim.Value)
                 {
                     return Forbid();
                 }

                 var logs = await _context.Logs
                     .Where(l => l.GimnasioId == gimnasioId)
                     .OrderByDescending(l => l.Fecha)
                     .ToListAsync();

                 using var workbook = new XLWorkbook();
                 var worksheet = workbook.Worksheets.Add("Logs");

                 // Encabezados
                 worksheet.Cell(1, 1).Value = "Descripción";
                 worksheet.Cell(1, 2).Value = "Monto";
                 worksheet.Cell(1, 3).Value = "Tipo";
                 worksheet.Cell(1, 4).Value = "Cliente";
                 worksheet.Cell(1, 5).Value = "Fecha";

                 var headerRange = worksheet.Range(1, 1, 1, 5);
                 headerRange.Style.Font.Bold = true;
                 headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
                 headerRange.Style.Font.FontColor = XLColor.White;

                 int row = 2;
                 decimal totalIngresos = 0;
                 decimal totalGastos = 0;

                 foreach (var log in logs)
                 {
                     worksheet.Cell(row, 1).Value = log.Message;
                     worksheet.Cell(row, 2).Value = log.Monto;
                     worksheet.Cell(row, 3).Value = log.Tipo;
                     worksheet.Cell(row, 4).Value = log.NombreCliente ?? "-";
                     worksheet.Cell(row, 5).Value = log.Fecha.ToString("dd/MM/yyyy HH:mm");

                     if (log.Monto >= 0)
                     {
                         worksheet.Cell(row, 2).Style.Font.FontColor = XLColor.Green;
                         totalIngresos += log.Monto;
                     }
                     else
                     {
                         worksheet.Cell(row, 2).Style.Font.FontColor = XLColor.Red;
                         totalGastos += Math.Abs(log.Monto);
                     }
                     row++;
                 }

                 // Totales
                 row++;
                 worksheet.Cell(row, 1).Value = "Total Ingresos:";
                 worksheet.Cell(row, 2).Value = totalIngresos;
                 worksheet.Cell(row, 2).Style.Font.FontColor = XLColor.Green;
                 row++;
                 worksheet.Cell(row, 1).Value = "Total Gastos:";
                 worksheet.Cell(row, 2).Value = totalGastos;
                 worksheet.Cell(row, 2).Style.Font.FontColor = XLColor.Red;
                 row++;
                 worksheet.Cell(row, 1).Value = "Balance:";
                 worksheet.Cell(row, 2).Value = totalIngresos - totalGastos;
                 worksheet.Cell(row, 1).Style.Font.Bold = true;
                 worksheet.Cell(row, 2).Style.Font.Bold = true;

                 worksheet.Columns().AdjustToContents();

                 using var stream = new MemoryStream();
                 workbook.SaveAs(stream);
                 var content = stream.ToArray();

                 return File(
                    content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Logs_{DateTime.Now:yyyyMMdd}.xlsx"
                );
             }
             catch (Exception ex)
             {
                 return StatusCode(500, new { success = false, message = ex.Message });
             }
         }
         #endregion
         
         #region Estadísticas de Ventas
         
         // GET: Gimnasios/GetVentasStats
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpGet]
         public async Task<IActionResult> GetVentasStats(Guid gimnasioId)
         {
             try
             {
                 var hoy = DateTime.Now.Date;
                 var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
                 var inicioAnio = new DateTime(hoy.Year, 1, 1);

                 // Obtener clientes y logs
                 var clientes = await _context.Clientes
                     .Where(c => c.GimnasioId == gimnasioId)
                     .ToListAsync();

                 var logs = await _context.Logs
                     .Where(l => l.GimnasioId == gimnasioId)
                     .ToListAsync();

                 // Membresías de 30 días vendidas este mes
                 var membresias30Dias = clientes
                     .Where(c => c.Dias >= 30 && c.FechaDeCreacion >= inicioMes)
                     .Sum(c => c.Precio);

                 // Clientes diarios de hoy
                 var clientesDiariosHoy = clientes
                     .Count(c => c.EsDiario && c.FechaDeCreacion.Date == hoy);

                 // Ingresos del día (clientes + logs positivos)
                 var ingresosClientesHoy = clientes
                     .Where(c => c.FechaDeCreacion.Date == hoy)
                     .Sum(c => c.Precio);
                 var ingresosLogsHoy = logs
                     .Where(l => l.Fecha.Date == hoy && l.Monto > 0)
                     .Sum(l => l.Monto);
                 var ingresosDia = ingresosClientesHoy + ingresosLogsHoy;

                 // Ingresos del mes
                 var ingresosClientesMes = clientes
                     .Where(c => c.FechaDeCreacion >= inicioMes)
                     .Sum(c => c.Precio);
                 var ingresosLogsMes = logs
                     .Where(l => l.Fecha >= inicioMes && l.Monto > 0)
                     .Sum(l => l.Monto);
                 var ingresosMes = ingresosClientesMes + ingresosLogsMes;

                 // Ingresos del año
                 var ingresosClientesAnio = clientes
                     .Where(c => c.FechaDeCreacion >= inicioAnio)
                     .Sum(c => c.Precio);
                 var ingresosLogsAnio = logs
                     .Where(l => l.Fecha >= inicioAnio && l.Monto > 0)
                     .Sum(l => l.Monto);
                 var ingresosAnio = ingresosClientesAnio + ingresosLogsAnio;

                 // Gastos (logs negativos)
                 var gastosDia = logs
                     .Where(l => l.Fecha.Date == hoy && l.Monto < 0)
                     .Sum(l => Math.Abs(l.Monto));
                 var gastosMes = logs
                     .Where(l => l.Fecha >= inicioMes && l.Monto < 0)
                     .Sum(l => Math.Abs(l.Monto));
                 var gastosAnio = logs
                     .Where(l => l.Fecha >= inicioAnio && l.Monto < 0)
                     .Sum(l => Math.Abs(l.Monto));

                 // Ganancia neta
                 var gananciaDia = ingresosDia - gastosDia;
                 var gananciaMes = ingresosMes - gastosMes;
                 var gananciaAnio = ingresosAnio - gastosAnio;

                 return Ok(new
                 {
                     membresias30Dias,
                     clientesDiariosHoy,
                     
                     ingresosDia,
                     ingresosMes,
                     ingresosAnio,
                     
                     gastosDia,
                     gastosMes,
                     gastosAnio,
                     
                     gananciaDia,
                     gananciaMes,
                     gananciaAnio
                 });
             }
             catch (Exception ex)
             {
                 return StatusCode(500, new { success = false, message = ex.Message });
             }
         }

         // GET: Gimnasios/GetChartData
         [Microsoft.AspNetCore.Authorization.Authorize]
         [HttpGet]
         public async Task<IActionResult> GetChartData(Guid gimnasioId, string periodo = "semana")
         {
             try
             {
                 var hoy = DateTime.Now.Date;
                 var clientes = await _context.Clientes
                     .Where(c => c.GimnasioId == gimnasioId)
                     .ToListAsync();

                 var logs = await _context.Logs
                     .Where(l => l.GimnasioId == gimnasioId)
                     .ToListAsync();

                 var labels = new List<string>();
                 var ingresos = new List<decimal>();
                 var gastos = new List<decimal>();

                 switch (periodo.ToLower())
                 {
                     case "semana":
                         // Últimos 7 días
                         for (int i = 6; i >= 0; i--)
                         {
                             var fecha = hoy.AddDays(-i);
                             labels.Add(fecha.ToString("dd/MM"));

                             var ingresoDia = clientes
                                 .Where(c => c.FechaDeActualizacion.Date == fecha)
                                 .Sum(c => c.Precio)
                                 + logs.Where(l => l.Fecha.Date == fecha && l.Monto > 0)
                                     .Sum(l => l.Monto);

                             var gastoDia = logs
                                 .Where(l => l.Fecha.Date == fecha && l.Monto < 0)
                                 .Sum(l => Math.Abs(l.Monto));

                             ingresos.Add(ingresoDia);
                             gastos.Add(gastoDia);
                         }
                         break;

                     case "mes":
                         // Últimas 4 semanas
                         for (int i = 3; i >= 0; i--)
                         {
                             var inicioSemana = hoy.AddDays(-7 * i - (int)hoy.DayOfWeek);
                             var finSemana = inicioSemana.AddDays(6);
                             labels.Add($"Sem {4 - i}");

                             var ingresoSemana = clientes
                                 .Where(c => c.FechaDeActualizacion.Date >= inicioSemana && c.FechaDeActualizacion.Date <= finSemana)
                                 .Sum(c => c.Precio)
                                 + logs.Where(l => l.Fecha.Date >= inicioSemana && l.Fecha.Date <= finSemana && l.Monto > 0)
                                     .Sum(l => l.Monto);

                             var gastoSemana = logs
                                 .Where(l => l.Fecha.Date >= inicioSemana && l.Fecha.Date <= finSemana && l.Monto < 0)
                                 .Sum(l => Math.Abs(l.Monto));

                             ingresos.Add(ingresoSemana);
                             gastos.Add(gastoSemana);
                         }
                         break;

                     case "anio":
                         // Últimos 12 meses
                         for (int i = 11; i >= 0; i--)
                         {
                             var fecha = hoy.AddMonths(-i);
                             var inicioMes = new DateTime(fecha.Year, fecha.Month, 1);
                             var finMes = inicioMes.AddMonths(1).AddDays(-1);
                             labels.Add(fecha.ToString("MMM"));

                             var ingresoMes = clientes
                                 .Where(c => c.FechaDeActualizacion.Date >= inicioMes && c.FechaDeActualizacion.Date <= finMes)
                                 .Sum(c => c.Precio)
                                 + logs.Where(l => l.Fecha.Date >= inicioMes && l.Fecha.Date <= finMes && l.Monto > 0)
                                     .Sum(l => l.Monto);

                             var gastoMes = logs
                                 .Where(l => l.Fecha.Date >= inicioMes && l.Fecha.Date <= finMes && l.Monto < 0)
                                 .Sum(l => Math.Abs(l.Monto));

                             ingresos.Add(ingresoMes);
                             gastos.Add(gastoMes);
                         }
                         break;
                 }

                 return Ok(new
                 {
                     labels,
                     ingresos,
                     gastos
                 });
             }
             catch (Exception ex)
             {
                 return StatusCode(500, new { success = false, message = ex.Message });
             }
         }
         #endregion
    }
}