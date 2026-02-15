// ═══════════════════════════════════════════════════════════
// HomeController.cs — Controlador de la página de inicio (landing page)
// Muestra la página principal pública y maneja errores generales
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Gimnasio.Models;
using Gimnasio.Data;
using Gimnasio.Services;

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
    [EnableRateLimiting("lead")]
    public async Task<IActionResult> Lead(string Name, string GymName, string Email, string? Phone, string? Message)
    {
        try
        {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(GymName) || string.IsNullOrWhiteSpace(Email))
            return BadRequest();
        if (!new EmailAddressAttribute().IsValid(Email.Trim()))
            return BadRequest();

        var lead = new LeadVendedor
        {
            Id = Guid.NewGuid(),
            Nombre = Name.Trim(),
            NombreNegocio = GymName.Trim(),
            Email = Email.Trim(),
            Telefono = Phone?.Trim(),
            Mensaje = Message?.Trim(),
            FechaCreacion = TimeHelper.Now
        };

        _context.LeadsVendedor.Add(lead);
        await _context.SaveChangesAsync();

        return Ok();
        }
        catch (Exception) { return StatusCode(500); }
    }

// ═══════════════════════════════════════════════════════════
    // SEED — Endpoint temporal para datos de prueba (ELIMINAR DESPUÉS DE AUDITORÍA)
    // ═══════════════════════════════════════════════════════════

    [HttpPost]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Seed()
    {
        // Solo admin puede ejecutar seed
        var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        if (roleClaim?.Value != "Admin")
            return Forbid();

        try
        {
            var now = TimeHelper.Now;
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Test1234!");

            // ═══════════════════════════════════════════════════════
            // PASO 1: Limpiar todas las tablas (hijos primero por FK)
            // ═══════════════════════════════════════════════════════

            _context.MovimientosInventario.RemoveRange(_context.MovimientosInventario);
            _context.Productos.RemoveRange(_context.Productos);
            _context.Notificaciones.RemoveRange(_context.Notificaciones);
            _context.Sugerencias.RemoveRange(_context.Sugerencias);
            _context.Logs.RemoveRange(_context.Logs);
            _context.Clientes.RemoveRange(_context.Clientes);
            _context.AdminLogs.RemoveRange(_context.AdminLogs);
            _context.LeadsVendedor.RemoveRange(_context.LeadsVendedor);
            _context.Negocios.RemoveRange(_context.Negocios);
            _context.Vendedores.RemoveRange(_context.Vendedores);
            await _context.SaveChangesAsync();

            // ═══════════════════════════════════════════════════════
            // PASO 2: Vendedores (3)
            // ═══════════════════════════════════════════════════════

            var v1Id = Guid.NewGuid();
            var v2Id = Guid.NewGuid();
            var v3Id = Guid.NewGuid();

            _context.Vendedores.AddRange(
                new Vendedor { VendedorId = v1Id, Nombre = "Carlos", Apellido = "Mendoza", Correo = "carlos@mynegocio.com", Telefono = "0991234567", Password = passwordHash, IsActive = true, FechaCreacion = now.AddDays(-60), NegociosCreados = 2 },
                new Vendedor { VendedorId = v2Id, Nombre = "María", Apellido = "López", Correo = "maria@mynegocio.com", Telefono = "0997654321", Password = passwordHash, IsActive = true, FechaCreacion = now.AddDays(-45), NegociosCreados = 2 },
                new Vendedor { VendedorId = v3Id, Nombre = "Pedro", Apellido = "Ramos", Correo = "pedro@mynegocio.com", Telefono = "0993456789", Password = passwordHash, IsActive = false, FechaCreacion = now.AddDays(-90), NegociosCreados = 0 }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 3: Negocios (5)
            // ═══════════════════════════════════════════════════════

            var n1Id = Guid.NewGuid();
            var n2Id = Guid.NewGuid();
            var n3Id = Guid.NewGuid();
            var n4Id = Guid.NewGuid();
            var n5Id = Guid.NewGuid();

            _context.Negocios.AddRange(
                // N1: Gym PowerFit — activo pagado, expira en 25 días
                new Gym { NegocioId = n1Id, NegocioNombre = "Gym PowerFit", DuenoNegocio = "Juan Pérez", Email = "juan@powerfit.com", Password = passwordHash, Telefono = "0991001001", IsActive = true, EsPrueba = false, DiasPagados = 30, PrecioSuscripcion = 25, FechaPago = now.AddDays(-5), FechaExpiracion = now.AddDays(25), FechaCreacion = now.AddDays(-60), FechaDeActualizacion = now, VendedorId = v1Id },
                // N2: Barbería Elite — activo pagado, expira en 15 días
                new Gym { NegocioId = n2Id, NegocioNombre = "Barbería Elite", DuenoNegocio = "Roberto Silva", Email = "roberto@elite.com", Password = passwordHash, Telefono = "0992002002", IsActive = true, EsPrueba = false, DiasPagados = 30, PrecioSuscripcion = 20, FechaPago = now.AddDays(-15), FechaExpiracion = now.AddDays(15), FechaCreacion = now.AddDays(-45), FechaDeActualizacion = now, VendedorId = v1Id },
                // N3: Yoga Zen — prueba, 5 días restantes
                new Gym { NegocioId = n3Id, NegocioNombre = "Yoga Zen", DuenoNegocio = "Ana Torres", Email = "ana@yogazen.com", Password = passwordHash, Telefono = "0993003003", IsActive = false, EsPrueba = true, DiasPagados = 7, PrecioSuscripcion = 0, FechaExpiracion = now.AddDays(5), FechaCreacion = now.AddDays(-2), FechaDeActualizacion = now, VendedorId = v2Id },
                // N4: CrossFit Max — expirado hace 3 días (login bloqueado)
                new Gym { NegocioId = n4Id, NegocioNombre = "CrossFit Max", DuenoNegocio = "Diego Ruiz", Email = "diego@crossfit.com", Password = passwordHash, Telefono = "0994004004", IsActive = true, EsPrueba = false, DiasPagados = 30, PrecioSuscripcion = 25, FechaPago = now.AddDays(-33), FechaExpiracion = now.AddDays(-3), FechaCreacion = now.AddDays(-60), FechaDeActualizacion = now, VendedorId = v2Id },
                // N5: Spa Relax — activo pagado, 20 días, sin vendedor
                new Gym { NegocioId = n5Id, NegocioNombre = "Spa Relax", DuenoNegocio = "Laura Gómez", Email = "laura@sparelax.com", Password = passwordHash, Telefono = "0995005005", IsActive = true, EsPrueba = false, DiasPagados = 30, PrecioSuscripcion = 30, FechaPago = now.AddDays(-10), FechaExpiracion = now.AddDays(20), FechaCreacion = now.AddDays(-30), FechaDeActualizacion = now, VendedorId = null }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 4: Clientes N1 — Gym PowerFit (12 escenarios)
            // ═══════════════════════════════════════════════════════

            var c1Id = Guid.NewGuid();
            var c2Id = Guid.NewGuid();
            var c3Id = Guid.NewGuid();
            var c4Id = Guid.NewGuid();
            var c5Id = Guid.NewGuid();
            var c6Id = Guid.NewGuid();
            var c7Id = Guid.NewGuid();
            var c8Id = Guid.NewGuid();
            var c9Id = Guid.NewGuid();
            var c10Id = Guid.NewGuid();
            var c11Id = Guid.NewGuid();
            var c12Id = Guid.NewGuid();

            _context.Clientes.AddRange(
                // C1: 30d, empezó hace 15d → 15 días restantes, badge verde
                new Cliente { ClienteId = c1Id, NegocioId = n1Id, Nombre = "Ana", Apellido = "García", Email = "ana.garcia@email.com", Telefono = "0991111001", Direccion = "Av. Principal 123", EsDiario = false, Dias = 30, Precio = 30, FechaDeCreacion = now.AddDays(-15), FechaQueTermina = now.AddDays(15), FechaDeActualizacion = now.AddDays(-15) },
                // C2: 90d, empieza 20/02/2026 → "Por empezar", 90 días
                new Cliente { ClienteId = c2Id, NegocioId = n1Id, Nombre = "Luis", Apellido = "Martínez", Email = "luis.martinez@email.com", Telefono = "0991111002", Direccion = "Calle 10 Norte", EsDiario = false, Dias = 90, Precio = 75, FechaDeCreacion = new DateTime(2026, 2, 20), FechaQueTermina = new DateTime(2026, 5, 21), FechaDeActualizacion = now },
                // C3: 30d, venció hace 5d → "Vencido", badge rojo
                new Cliente { ClienteId = c3Id, NegocioId = n1Id, Nombre = "María", Apellido = "Rodríguez", Email = "maria.rodriguez@email.com", Telefono = "0991111003", Direccion = "Urbanización Los Ceibos", EsDiario = false, Dias = 30, Precio = 30, FechaDeCreacion = now.AddDays(-35), FechaQueTermina = now.AddDays(-5), FechaDeActualizacion = now.AddDays(-35) },
                // C4: 30d, vence mañana → 1 día, badge amarillo
                new Cliente { ClienteId = c4Id, NegocioId = n1Id, Nombre = "Carlos", Apellido = "Fernández", Email = "carlos.fernandez@email.com", Telefono = "0991111004", Direccion = "Cdla. Kennedy", EsDiario = false, Dias = 30, Precio = 30, FechaDeCreacion = now.AddDays(-29), FechaQueTermina = now.AddDays(1), FechaDeActualizacion = now.AddDays(-29) },
                // C5: 30d, vence en 3 días → warning + notificación
                new Cliente { ClienteId = c5Id, NegocioId = n1Id, Nombre = "Patricia", Apellido = "López", Email = "patricia.lopez@email.com", Telefono = "0991111005", Direccion = "Av. 9 de Octubre", EsDiario = false, Dias = 30, Precio = 30, FechaDeCreacion = now.AddDays(-27), FechaQueTermina = now.AddDays(3), FechaDeActualizacion = now.AddDays(-27) },
                // C6: 60d, empezó hace 10d → 50 días, verde
                new Cliente { ClienteId = c6Id, NegocioId = n1Id, Nombre = "Jorge", Apellido = "Sánchez", Email = "jorge.sanchez@email.com", Telefono = "0991111006", Direccion = "Samborondón", EsDiario = false, Dias = 60, Precio = 50, FechaDeCreacion = now.AddDays(-10), FechaQueTermina = now.AddDays(50), FechaDeActualizacion = now.AddDays(-10) },
                // C7: 7d, venció hace 2d → "Vencido"
                new Cliente { ClienteId = c7Id, NegocioId = n1Id, Nombre = "Valentina", Apellido = "Cruz", Email = "valentina.cruz@email.com", Telefono = "0991111007", Direccion = "Urdesa Central", EsDiario = false, Dias = 7, Precio = 10, FechaDeCreacion = now.AddDays(-9), FechaQueTermina = now.AddDays(-2), FechaDeActualizacion = now.AddDays(-9) },
                // C8: Diario (EsDiario = true)
                new Cliente { ClienteId = c8Id, NegocioId = n1Id, Nombre = "Diego", Apellido = "Morales", Email = "diego.morales@email.com", Telefono = "0991111008", Direccion = "Centro", EsDiario = true, Dias = 1, Precio = 5, FechaDeCreacion = now, FechaQueTermina = now.AddDays(1), FechaDeActualizacion = now },
                // C9: 365d, empezó hace 100d → 265 días, verde
                new Cliente { ClienteId = c9Id, NegocioId = n1Id, Nombre = "Sofía", Apellido = "Herrera", Email = "sofia.herrera@email.com", Telefono = "0991111009", Direccion = "Ceibos Norte", EsDiario = false, Dias = 365, Precio = 250, FechaDeCreacion = now.AddDays(-100), FechaQueTermina = now.AddDays(265), FechaDeActualizacion = now.AddDays(-100) },
                // C10: 30d, vence en 5 días → warning
                new Cliente { ClienteId = c10Id, NegocioId = n1Id, Nombre = "Ricardo", Apellido = "Blanco", Email = "ricardo.blanco@email.com", Telefono = "0991111010", Direccion = "Alborada", EsDiario = false, Dias = 30, Precio = 30, FechaDeCreacion = now.AddDays(-25), FechaQueTermina = now.AddDays(5), FechaDeActualizacion = now.AddDays(-25) },
                // C11: 15d, empieza 25/02/2026 → "Por empezar", 15 días
                new Cliente { ClienteId = c11Id, NegocioId = n1Id, Nombre = "Elena", Apellido = "Vargas", Email = "elena.vargas@email.com", Telefono = "0991111011", Direccion = "Sauces 8", EsDiario = false, Dias = 15, Precio = 20, FechaDeCreacion = new DateTime(2026, 2, 25), FechaQueTermina = new DateTime(2026, 3, 12), FechaDeActualizacion = now },
                // C12: 30d, empezó hoy → 30 días, verde
                new Cliente { ClienteId = c12Id, NegocioId = n1Id, Nombre = "Fernando", Apellido = "Ruiz", Email = "fernando.ruiz@email.com", Telefono = "0991111012", Direccion = "Garzota", EsDiario = false, Dias = 30, Precio = 30, FechaDeCreacion = now, FechaQueTermina = now.AddDays(30), FechaDeActualizacion = now }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 5: Clientes N2 — Barbería Elite (4)
            // ═══════════════════════════════════════════════════════

            _context.Clientes.AddRange(
                // Activo 1
                new Cliente { ClienteId = Guid.NewGuid(), NegocioId = n2Id, Nombre = "Andrés", Apellido = "Mora", Email = "andres@email.com", Telefono = "0992222001", EsDiario = false, Dias = 30, Precio = 15, FechaDeCreacion = now.AddDays(-10), FechaQueTermina = now.AddDays(20), FechaDeActualizacion = now.AddDays(-10) },
                // Activo 2
                new Cliente { ClienteId = Guid.NewGuid(), NegocioId = n2Id, Nombre = "Camila", Apellido = "Reyes", Email = "camila@email.com", Telefono = "0992222002", EsDiario = false, Dias = 30, Precio = 15, FechaDeCreacion = now.AddDays(-5), FechaQueTermina = now.AddDays(25), FechaDeActualizacion = now.AddDays(-5) },
                // Vencido
                new Cliente { ClienteId = Guid.NewGuid(), NegocioId = n2Id, Nombre = "Héctor", Apellido = "Delgado", Email = "hector@email.com", Telefono = "0992222003", EsDiario = false, Dias = 30, Precio = 15, FechaDeCreacion = now.AddDays(-40), FechaQueTermina = now.AddDays(-10), FechaDeActualizacion = now.AddDays(-40) },
                // Diario
                new Cliente { ClienteId = Guid.NewGuid(), NegocioId = n2Id, Nombre = "Isabel", Apellido = "Paredes", Email = "isabel@email.com", Telefono = "0992222004", EsDiario = true, Dias = 1, Precio = 5, FechaDeCreacion = now, FechaQueTermina = now.AddDays(1), FechaDeActualizacion = now }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 6: Clientes N3 — Yoga Zen (2)
            // ═══════════════════════════════════════════════════════

            _context.Clientes.AddRange(
                // Activo
                new Cliente { ClienteId = Guid.NewGuid(), NegocioId = n3Id, Nombre = "Gabriela", Apellido = "Mendoza", Email = "gabriela@email.com", Telefono = "0993333001", EsDiario = false, Dias = 30, Precio = 20, FechaDeCreacion = now.AddDays(-5), FechaQueTermina = now.AddDays(25), FechaDeActualizacion = now.AddDays(-5) },
                // Por empezar
                new Cliente { ClienteId = Guid.NewGuid(), NegocioId = n3Id, Nombre = "Tomás", Apellido = "Vega", Email = "tomas@email.com", Telefono = "0993333002", EsDiario = false, Dias = 30, Precio = 20, FechaDeCreacion = new DateTime(2026, 2, 20), FechaQueTermina = new DateTime(2026, 3, 22), FechaDeActualizacion = now }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 7: Productos N1 — Gym PowerFit (5)
            // ═══════════════════════════════════════════════════════

            var p1Id = Guid.NewGuid();
            var p2Id = Guid.NewGuid();
            var p3Id = Guid.NewGuid();
            var p4Id = Guid.NewGuid();
            var p5Id = Guid.NewGuid();

            _context.Productos.AddRange(
                // Stock normal (25 > min 5)
                new Producto { ProductoId = p1Id, NegocioId = n1Id, Nombre = "Proteína Whey 1kg", PrecioVenta = 35.00m, CostoCompra = 22.00m, Stock = 25, StockMinimo = 5, IsActive = true, FechaCreacion = now.AddDays(-30), FechaDeActualizacion = now },
                // Stock bajo (3 < min 5)
                new Producto { ProductoId = p2Id, NegocioId = n1Id, Nombre = "Creatina 300g", PrecioVenta = 18.50m, CostoCompra = 10.00m, Stock = 3, StockMinimo = 5, IsActive = true, FechaCreacion = now.AddDays(-30), FechaDeActualizacion = now },
                // Sin stock (0)
                new Producto { ProductoId = p3Id, NegocioId = n1Id, Nombre = "Guantes de gym", PrecioVenta = 12.00m, CostoCompra = 6.50m, Stock = 0, StockMinimo = 3, IsActive = true, FechaCreacion = now.AddDays(-20), FechaDeActualizacion = now },
                // Stock alto (50 > min 10)
                new Producto { ProductoId = p4Id, NegocioId = n1Id, Nombre = "Botella shaker", PrecioVenta = 8.00m, CostoCompra = 3.50m, Stock = 50, StockMinimo = 10, IsActive = true, FechaCreacion = now.AddDays(-25), FechaDeActualizacion = now },
                // Inactivo (soft delete)
                new Producto { ProductoId = p5Id, NegocioId = n1Id, Nombre = "Barra energética", PrecioVenta = 25.00m, CostoCompra = 15.00m, Stock = 10, StockMinimo = 5, IsActive = false, FechaCreacion = now.AddDays(-15), FechaDeActualizacion = now }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 8: Productos N2 — Barbería Elite (3)
            // ═══════════════════════════════════════════════════════

            var p6Id = Guid.NewGuid();
            var p7Id = Guid.NewGuid();
            var p8Id = Guid.NewGuid();

            _context.Productos.AddRange(
                new Producto { ProductoId = p6Id, NegocioId = n2Id, Nombre = "Gel para cabello", PrecioVenta = 8.00m, CostoCompra = 3.50m, Stock = 20, StockMinimo = 5, IsActive = true, FechaCreacion = now.AddDays(-20), FechaDeActualizacion = now },
                new Producto { ProductoId = p7Id, NegocioId = n2Id, Nombre = "Cera mate", PrecioVenta = 12.00m, CostoCompra = 6.00m, Stock = 15, StockMinimo = 3, IsActive = true, FechaCreacion = now.AddDays(-20), FechaDeActualizacion = now },
                new Producto { ProductoId = p8Id, NegocioId = n2Id, Nombre = "Aftershave premium", PrecioVenta = 15.00m, CostoCompra = 8.00m, Stock = 8, StockMinimo = 3, IsActive = true, FechaCreacion = now.AddDays(-15), FechaDeActualizacion = now }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 9: Movimientos de inventario N1 (8)
            // ═══════════════════════════════════════════════════════

            _context.MovimientosInventario.AddRange(
                // Restock (primero cronológicamente)
                new MovimientoInventario { MovimientoId = Guid.NewGuid(), NegocioId = n1Id, ProductoId = p2Id, NombreProducto = "Creatina 300g", Tipo = "restock", Cantidad = 5, PrecioUnitario = 10.00m, Total = 50.00m, StockAnterior = 0, StockNuevo = 5, Nota = "Reposición de stock", Fecha = now.AddDays(-10) },
                new MovimientoInventario { MovimientoId = Guid.NewGuid(), NegocioId = n1Id, ProductoId = p1Id, NombreProducto = "Proteína Whey 1kg", Tipo = "restock", Cantidad = 10, PrecioUnitario = 22.00m, Total = 220.00m, StockAnterior = 17, StockNuevo = 27, Nota = "Compra mayorista", Fecha = now.AddDays(-7) },
                // Ventas
                new MovimientoInventario { MovimientoId = Guid.NewGuid(), NegocioId = n1Id, ProductoId = p1Id, NombreProducto = "Proteína Whey 1kg", Tipo = "venta", Cantidad = 2, PrecioUnitario = 35.00m, Total = 70.00m, StockAnterior = 27, StockNuevo = 25, Fecha = now.AddDays(-5) },
                new MovimientoInventario { MovimientoId = Guid.NewGuid(), NegocioId = n1Id, ProductoId = p2Id, NombreProducto = "Creatina 300g", Tipo = "venta", Cantidad = 1, PrecioUnitario = 18.50m, Total = 18.50m, StockAnterior = 4, StockNuevo = 3, Fecha = now.AddDays(-3) },
                new MovimientoInventario { MovimientoId = Guid.NewGuid(), NegocioId = n1Id, ProductoId = p4Id, NombreProducto = "Botella shaker", Tipo = "venta", Cantidad = 3, PrecioUnitario = 8.00m, Total = 24.00m, StockAnterior = 53, StockNuevo = 50, Fecha = now.AddDays(-1) },
                // Devolución
                new MovimientoInventario { MovimientoId = Guid.NewGuid(), NegocioId = n1Id, ProductoId = p3Id, NombreProducto = "Guantes de gym", Tipo = "devolucion", Cantidad = 1, PrecioUnitario = 12.00m, Total = 12.00m, StockAnterior = 1, StockNuevo = 2, Nota = "Cliente devolvió por talla incorrecta", Fecha = now.AddDays(-4) },
                // Ajustes
                new MovimientoInventario { MovimientoId = Guid.NewGuid(), NegocioId = n1Id, ProductoId = p3Id, NombreProducto = "Guantes de gym", Tipo = "ajuste", Cantidad = 2, PrecioUnitario = 6.50m, Total = 13.00m, StockAnterior = 2, StockNuevo = 0, Nota = "Inventario dañado por humedad", Fecha = now.AddDays(-2) },
                new MovimientoInventario { MovimientoId = Guid.NewGuid(), NegocioId = n1Id, ProductoId = p2Id, NombreProducto = "Creatina 300g", Tipo = "ajuste", Cantidad = 1, PrecioUnitario = 10.00m, Total = 10.00m, StockAnterior = 4, StockNuevo = 3, Nota = "Corrección de conteo físico", Fecha = now.AddDays(-2) }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 10: Logs N1 — Gym PowerFit (22 registros, 30 días)
            // ═══════════════════════════════════════════════════════

            _context.Logs.AddRange(
                // Clientes creados (ingresos de membresías)
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Nuevo cliente registrado: Sofía Herrera (anual)", Monto = 250, Tipo = "cliente_creado", ClienteId = c9Id, NombreCliente = "Sofía Herrera", Fecha = now.AddDays(-100) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Nuevo cliente registrado: María Rodríguez", Monto = 30, Tipo = "cliente_creado", ClienteId = c3Id, NombreCliente = "María Rodríguez", Fecha = now.AddDays(-35) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Nuevo cliente registrado: Carlos Fernández", Monto = 30, Tipo = "cliente_creado", ClienteId = c4Id, NombreCliente = "Carlos Fernández", Fecha = now.AddDays(-29) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Nuevo cliente registrado: Patricia López", Monto = 30, Tipo = "cliente_creado", ClienteId = c5Id, NombreCliente = "Patricia López", Fecha = now.AddDays(-27) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Nuevo cliente registrado: Ricardo Blanco", Monto = 30, Tipo = "cliente_creado", ClienteId = c10Id, NombreCliente = "Ricardo Blanco", Fecha = now.AddDays(-25) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Nuevo cliente registrado: Ana García", Monto = 30, Tipo = "cliente_creado", ClienteId = c1Id, NombreCliente = "Ana García", Fecha = now.AddDays(-15) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Membresía renovada: Ana García (30 días)", Monto = 30, Tipo = "cliente_renovado", ClienteId = c1Id, NombreCliente = "Ana García", Fecha = now.AddDays(-15) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Nuevo cliente registrado: Jorge Sánchez", Monto = 50, Tipo = "cliente_creado", ClienteId = c6Id, NombreCliente = "Jorge Sánchez", Fecha = now.AddDays(-10) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Nuevo cliente registrado: Luis Martínez", Monto = 75, Tipo = "cliente_creado", ClienteId = c2Id, NombreCliente = "Luis Martínez", Fecha = now.AddDays(-10) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Nuevo cliente registrado: Valentina Cruz", Monto = 10, Tipo = "cliente_creado", ClienteId = c7Id, NombreCliente = "Valentina Cruz", Fecha = now.AddDays(-9) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Nuevo cliente registrado: Fernando Ruiz", Monto = 30, Tipo = "cliente_creado", ClienteId = c12Id, NombreCliente = "Fernando Ruiz", Fecha = now },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Nuevo cliente registrado: Diego Morales (diario)", Monto = 5, Tipo = "cliente_creado", ClienteId = c8Id, NombreCliente = "Diego Morales", Fecha = now },
                // Gastos operativos (distribuidos en el mes)
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Pago de agua", Monto = -25, Tipo = "gasto", Fecha = now.AddDays(-20) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Pago de luz eléctrica", Monto = -45, Tipo = "gasto", Fecha = now.AddDays(-18) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Compra de equipo (mancuernas)", Monto = -120, Tipo = "gasto", Fecha = now.AddDays(-12) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Mantenimiento aire acondicionado", Monto = -60, Tipo = "gasto", Fecha = now.AddDays(-8) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Internet mensual", Monto = -35, Tipo = "gasto", Fecha = now.AddDays(-5) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Alquiler mensual del local", Monto = -500, Tipo = "gasto", Fecha = now.AddDays(-1) },
                // Ingresos extras
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Entrenamiento personal (extra)", Monto = 40, Tipo = "ingreso", Fecha = now.AddDays(-7) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Venta de suplementos", Monto = 85, Tipo = "ingreso", Fecha = now.AddDays(-3) },
                // Sesiones
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Inicio de sesión", Monto = 0, Tipo = "sesion_inicio", Fecha = now.AddDays(-2) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n1Id, Message = "Inicio de sesión", Monto = 0, Tipo = "sesion_inicio", Fecha = now }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 11: Logs N2 — Barbería Elite (5)
            // ═══════════════════════════════════════════════════════

            _context.Logs.AddRange(
                new Logs { Id = Guid.NewGuid(), NegocioId = n2Id, Message = "Nuevo cliente registrado: Andrés Mora", Monto = 15, Tipo = "cliente_creado", NombreCliente = "Andrés Mora", Fecha = now.AddDays(-10) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n2Id, Message = "Nuevo cliente registrado: Camila Reyes", Monto = 15, Tipo = "cliente_creado", NombreCliente = "Camila Reyes", Fecha = now.AddDays(-5) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n2Id, Message = "Pago de alquiler", Monto = -300, Tipo = "gasto", Fecha = now.AddDays(-15) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n2Id, Message = "Venta de productos capilares", Monto = 45, Tipo = "ingreso", Fecha = now.AddDays(-3) },
                new Logs { Id = Guid.NewGuid(), NegocioId = n2Id, Message = "Inicio de sesión", Monto = 0, Tipo = "sesion_inicio", Fecha = now }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 12: Notificaciones N1 (5: 3 no leídas, 2 leídas)
            // ═══════════════════════════════════════════════════════

            _context.Notificaciones.AddRange(
                new Notificacion { Id = Guid.NewGuid(), NegocioId = n1Id, Mensaje = "La membresía de Patricia López vence en 3 día(s)", Tipo = "vencimiento", ClienteId = c5Id, NombreCliente = "Patricia López", Leida = false, FechaCreacion = now },
                new Notificacion { Id = Guid.NewGuid(), NegocioId = n1Id, Mensaje = "La membresía de Carlos Fernández vence en 1 día(s)", Tipo = "vencimiento", ClienteId = c4Id, NombreCliente = "Carlos Fernández", Leida = false, FechaCreacion = now },
                new Notificacion { Id = Guid.NewGuid(), NegocioId = n1Id, Mensaje = "La membresía de Ricardo Blanco vence en 5 día(s)", Tipo = "vencimiento", ClienteId = c10Id, NombreCliente = "Ricardo Blanco", Leida = false, FechaCreacion = now },
                new Notificacion { Id = Guid.NewGuid(), NegocioId = n1Id, Mensaje = "La membresía de María Rodríguez ha vencido", Tipo = "vencimiento", ClienteId = c3Id, NombreCliente = "María Rodríguez", Leida = true, FechaCreacion = now.AddDays(-5) },
                new Notificacion { Id = Guid.NewGuid(), NegocioId = n1Id, Mensaje = "La membresía de Valentina Cruz ha vencido", Tipo = "vencimiento", ClienteId = c7Id, NombreCliente = "Valentina Cruz", Leida = true, FechaCreacion = now.AddDays(-2) }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 13: Sugerencias (3: de N1, N2, N5)
            // ═══════════════════════════════════════════════════════

            _context.Sugerencias.AddRange(
                new Sugerencia { Id = Guid.NewGuid(), NegocioId = n1Id, NegocioNombre = "Gym PowerFit", Mensaje = "Sería genial poder agregar fotos de los clientes para identificarlos más fácil.", FechaCreacion = now.AddDays(-5), Leida = false },
                new Sugerencia { Id = Guid.NewGuid(), NegocioId = n2Id, NegocioNombre = "Barbería Elite", Mensaje = "¿Podrían agregar un calendario para agendar citas? Nos ayudaría mucho.", FechaCreacion = now.AddDays(-3), Leida = true },
                new Sugerencia { Id = Guid.NewGuid(), NegocioId = n5Id, NegocioNombre = "Spa Relax", Mensaje = "El sistema está muy completo, solo faltaría poder enviar recordatorios por email.", FechaCreacion = now.AddDays(-1), Leida = false }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 14: AdminLogs (5)
            // ═══════════════════════════════════════════════════════

            _context.AdminLogs.AddRange(
                new AdminLog { Id = Guid.NewGuid(), Accion = "Crear", Detalle = "Negocio 'Gym PowerFit' creado por vendedor Carlos Mendoza", Fecha = now.AddDays(-60), NegocioAfectado = "Gym PowerFit" },
                new AdminLog { Id = Guid.NewGuid(), Accion = "Crear", Detalle = "Negocio 'Barbería Elite' creado por vendedor Carlos Mendoza", Fecha = now.AddDays(-45), NegocioAfectado = "Barbería Elite" },
                new AdminLog { Id = Guid.NewGuid(), Accion = "Editar", Detalle = "Precio de suscripción actualizado de $20 a $25 para 'Gym PowerFit'", Fecha = now.AddDays(-30), NegocioAfectado = "Gym PowerFit" },
                new AdminLog { Id = Guid.NewGuid(), Accion = "CambiarEstado", Detalle = "Negocio 'CrossFit Max' marcado como expirado", Fecha = now.AddDays(-3), NegocioAfectado = "CrossFit Max" },
                new AdminLog { Id = Guid.NewGuid(), Accion = "Impersonar", Detalle = "Admin ingresó como 'Gym PowerFit' para verificar datos", Fecha = now.AddDays(-1), NegocioAfectado = "Gym PowerFit" }
            );

            // ═══════════════════════════════════════════════════════
            // PASO 15: LeadsVendedor (4: 2 sin atender, 2 atendidos)
            // ═══════════════════════════════════════════════════════

            _context.LeadsVendedor.AddRange(
                new LeadVendedor { Id = Guid.NewGuid(), Nombre = "Roberto Gómez", NombreNegocio = "Gym Fitness Total", Email = "roberto.gomez@email.com", Telefono = "0998887766", Mensaje = "Me interesa el sistema para mi gimnasio de 50 clientes", Atendido = false, FechaCreacion = now.AddDays(-2) },
                new LeadVendedor { Id = Guid.NewGuid(), Nombre = "Lucía Mejía", NombreNegocio = "Estética Bella", Email = "lucia.mejia@email.com", Telefono = "0997776655", Mensaje = "Quisiera una demo del sistema", Atendido = false, FechaCreacion = now.AddDays(-1) },
                new LeadVendedor { Id = Guid.NewGuid(), Nombre = "Marco Salazar", NombreNegocio = "Box CrossFit Fuego", Email = "marco.salazar@email.com", Telefono = "0996665544", Mensaje = "¿Cuánto cuesta el plan mensual?", Atendido = true, AtendidoPorId = v1Id, AtendidoPorNombre = "Carlos Mendoza", FechaCreacion = now.AddDays(-5) },
                new LeadVendedor { Id = Guid.NewGuid(), Nombre = "Verónica Paz", NombreNegocio = "Pilates Studio VP", Email = "veronica.paz@email.com", Telefono = "0995554433", Mensaje = "Necesito controlar membresías de 30 alumnas", Atendido = true, AtendidoPorId = v2Id, AtendidoPorNombre = "María López", FechaCreacion = now.AddDays(-7) }
            );

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Seed completado exitosamente",
                data = new
                {
                    vendedores = 3,
                    negocios = 5,
                    clientes = 18,
                    productos = 8,
                    movimientos = 8,
                    logs = 27,
                    notificaciones = 5,
                    sugerencias = 3,
                    adminLogs = 5,
                    leads = 4
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
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
