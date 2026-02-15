// ═══════════════════════════════════════════════════════════
// NegocioService.cs — Servicio principal para gestión de negocios
// Maneja CRUD de negocios, exportación Excel, estadísticas
// del panel admin y registro de acciones administrativas
// ═══════════════════════════════════════════════════════════

using ClosedXML.Excel;
using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class NegocioService : INegocioService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;

    public NegocioService(ApplicationDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los negocios con sus estadísticas de suscripción
    /// </summary>
    public async Task<object> GetAllNegociosAsync()
    {
        var now = DateTime.Now;
        return await _context.Negocios
            .Select(g => new
            {
                g.NegocioId,
                g.NegocioNombre,
                g.DuenoNegocio,
                g.Telefono,
                g.Email,
                g.IsActive,
                g.EsPrueba,
                g.FechaCreacion,
                g.DiasPagados,
                g.PrecioSuscripcion,
                g.FechaPago,
                g.FechaExpiracion,
                // Calcular días restantes de suscripción basado en FechaExpiracion
                DiasRestantesSuscripcion = g.FechaExpiracion.HasValue
                    ? (int)(g.FechaExpiracion.Value.Date - now.Date).TotalDays
                    : (int?)null,
                // Indica si la suscripción aún no ha comenzado (fecha de inicio en el futuro)
                PorEmpezar = g.FechaPago.HasValue && g.FechaPago.Value.Date > now.Date,
                TotalClientes = _context.Clientes.Count(c => c.NegocioId == g.NegocioId),
                g.VendedorId,
                VendedorNombre = g.VendedorId.HasValue
                    ? _context.Vendedores
                        .Where(v => v.VendedorId == g.VendedorId.Value)
                        .Select(v => v.Nombre + " " + v.Apellido)
                        .FirstOrDefault()
                    : null
            })
            .OrderByDescending(g => g.FechaCreacion)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene los datos de un negocio específico (sin clientes)
    /// </summary>
    public async Task<object> GetNegocioAsync(Guid id)
    {
        var negocio = await _context.Negocios.FindAsync(id);
        if (negocio == null) return null;

        return new
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
        };
    }

    /// <summary>
    /// Obtiene el objeto Gym completo para impersonación
    /// </summary>
    public async Task<Gym> GetNegocioForImpersonationAsync(Guid id)
    {
        return await _context.Negocios.FindAsync(id);
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo negocio con campos de suscripción opcionales.
    /// Si no se proporcionan fechas, se calculan según el tipo (prueba = 7 días, pago = 30 días).
    /// </summary>
    public async Task<(bool success, string message)> CreateNegocioAsync(string nombre, string dueno, string telefono, string email, string password, bool isActive, bool esPrueba,
        DateTime? fechaPago = null, DateTime? fechaExpiracion = null, decimal? precioSuscripcion = null, int? diasPagados = null, Guid? vendedorId = null)
    {
        // Validar que no sea pago Y prueba al mismo tiempo
        if (isActive && esPrueba)
            return (false, "Un negocio no puede ser de pago y de prueba al mismo tiempo");
        if (!isActive && !esPrueba)
            return (false, "Debe seleccionar si el negocio es de pago o de prueba");

        // Validaciones de campos obligatorios
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Correo del Negocio es necesario");

        bool existing = await _context.Negocios.AnyAsync(c => c.Email == email);
        if (existing)
            return (false, "Negocio con este email ya existe");
        if (string.IsNullOrWhiteSpace(nombre))
            return (false, "Nombre del Negocio es necesario");
        if (string.IsNullOrWhiteSpace(dueno))
            return (false, "Dueño del Negocio es necesario");
        if (string.IsNullOrWhiteSpace(telefono))
            return (false, "Teléfono es necesario");
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password es necesario");

        var now = DateTime.Now;

        // Usar valores proporcionados o valores por defecto según tipo
        var fechaPagoFinal = fechaPago ?? now;
        var fechaExpiracionFinal = fechaExpiracion ?? (esPrueba ? now.AddDays(7) : now.AddDays(30));
        var diasPagadosFinal = diasPagados ?? (esPrueba ? 7 : 30);
        var precioFinal = precioSuscripcion ?? (esPrueba ? 0m : 0m);

        var negocio = new Gym
        {
            NegocioId = Guid.NewGuid(),
            NegocioNombre = nombre,
            DuenoNegocio = dueno,
            Telefono = telefono,
            Email = email,
            Password = _authService.HashPassword(password),
            IsActive = isActive,
            EsPrueba = esPrueba,
            FechaCreacion = now,
            FechaDeActualizacion = now,
            DiasPagados = diasPagadosFinal,
            PrecioSuscripcion = precioFinal,
            FechaPago = fechaPagoFinal,
            FechaExpiracion = fechaExpiracionFinal,
            VendedorId = vendedorId,
        };

        _context.Negocios.Add(negocio);
        await _context.SaveChangesAsync();

        return (true, "Negocio creado exitosamente");
    }

    /// <summary>
    /// Edita un negocio existente. Si se envía contraseña nueva, se re-hashea con BCrypt.
    /// Valida que no exista otro negocio con el mismo email.
    /// </summary>
    public async Task<(bool success, string message)> EditarNegocioAsync(Gym negocio)
    {
        if (negocio.IsActive && negocio.EsPrueba)
            return (false, "Un negocio no puede ser de pago y de prueba al mismo tiempo");
        if (!negocio.IsActive && !negocio.EsPrueba)
            return (false, "Debe seleccionar si el negocio es de pago o de prueba");

        var existente = await _context.Negocios.FindAsync(negocio.NegocioId);
        if (existente == null)
            return (false, "Negocio no encontrado");

        // Validar email único (excluyendo el negocio actual)
        var existeEmail = await _context.Negocios
            .AnyAsync(g => g.Email == negocio.Email && g.NegocioId != negocio.NegocioId);
        if (existeEmail)
            return (false, "Ya existe otro negocio con ese email");

        // Actualizar campos
        existente.NegocioNombre = negocio.NegocioNombre;
        existente.DuenoNegocio = negocio.DuenoNegocio;
        existente.Telefono = negocio.Telefono;
        existente.Email = negocio.Email;
        existente.IsActive = negocio.IsActive;
        existente.EsPrueba = negocio.EsPrueba;
        existente.DiasPagados = negocio.DiasPagados;
        existente.PrecioSuscripcion = negocio.PrecioSuscripcion;
        existente.FechaExpiracion = negocio.FechaExpiracion;

        // Si se establece fecha de expiración pero no hay fecha de pago, asignar ahora
        if (negocio.FechaExpiracion.HasValue && existente.FechaPago == null)
            existente.FechaPago = DateTime.Now;
        existente.FechaDeActualizacion = DateTime.Now;

        // Solo re-hashear si se envió una nueva contraseña
        if (!string.IsNullOrEmpty(negocio.Password))
        {
            existente.Password = _authService.HashPassword(negocio.Password);
        }

        _context.Update(existente);
        await _context.SaveChangesAsync();

        return (true, "Negocio actualizado exitosamente");
    }

    /// <summary>
    /// Elimina un negocio. Falla si tiene clientes registrados.
    /// </summary>
    public async Task<(bool success, string message)> EliminarNegocioAsync(Guid id)
    {
        var negocio = await _context.Negocios
            .Include(g => g.Clientes)
            .FirstOrDefaultAsync(g => g.NegocioId == id);

        if (negocio == null)
            return (false, "Negocio no encontrado");

        if (negocio.Clientes.Any())
            return (false, $"No se puede eliminar. El negocio tiene {negocio.Clientes.Count} cliente(s) registrado(s)");

        _context.Negocios.Remove(negocio);
        await _context.SaveChangesAsync();

        return (true, "Negocio eliminado exitosamente");
    }

    /// <summary>
    /// Alterna el estado entre Pago (Activo) y Prueba.
    /// Si estaba activo pasa a prueba, y viceversa.
    /// </summary>
    public async Task<(bool success, string message, bool? isActive, bool? esPrueba)> CambiarEstadoAsync(Guid id)
    {
        var negocio = await _context.Negocios.FindAsync(id);
        if (negocio == null)
            return (false, "Negocio no encontrado", null, null);

        if (negocio.IsActive)
        {
            negocio.IsActive = false;
            negocio.EsPrueba = true;
        }
        else
        {
            negocio.IsActive = true;
            negocio.EsPrueba = false;
        }

        negocio.FechaDeActualizacion = DateTime.Now;
        _context.Update(negocio);
        await _context.SaveChangesAsync();

        string tipoActual = negocio.IsActive ? "Pago (Activo)" : "Prueba";
        return (true, $"Negocio cambiado a modo {tipoActual} exitosamente", negocio.IsActive, negocio.EsPrueba);
    }

    // ═══════════════════════════════════════════════════════════
    // ESTADÍSTICAS DEL DASHBOARD ADMIN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula estadísticas generales para el panel admin:
    /// - Total de negocios, activos, en prueba
    /// - MRR (Monthly Recurring Revenue) de negocios activos que pagan
    /// - Total de clientes en la plataforma
    /// - Ingresos por mes de los últimos 12 meses
    /// </summary>
    public async Task<object> GetAdminDashboardStatsAsync()
    {
        var negocios = await _context.Negocios.ToListAsync();
        var totalNegocios = negocios.Count;
        var activos = negocios.Count(n => n.IsActive);
        var prueba = negocios.Count(n => n.EsPrueba);
        // Contar solo clientes que pertenecen a negocios existentes
        var negocioIds = negocios.Select(n => n.NegocioId).ToHashSet();
        var totalClientes = await _context.Clientes
            .Where(c => negocioIds.Contains(c.NegocioId))
            .CountAsync();

        // MRR: suma de PrecioSuscripcion de negocios activos que pagan
        var mrr = negocios
            .Where(n => n.IsActive && !n.EsPrueba && n.PrecioSuscripcion > 0)
            .Sum(n => n.PrecioSuscripcion);

        // Ingresos por mes (basado en FechaPago de los últimos 12 meses)
        var hace12Meses = DateTime.Now.AddMonths(-12);
        var ingresosporMes = negocios
            .Where(n => n.FechaPago.HasValue && n.FechaPago.Value >= hace12Meses && n.PrecioSuscripcion > 0)
            .GroupBy(n => new { n.FechaPago!.Value.Year, n.FechaPago.Value.Month })
            .Select(g => new
            {
                Mes = $"{g.Key.Year}-{g.Key.Month:D2}",
                Total = g.Sum(n => n.PrecioSuscripcion)
            })
            .OrderBy(g => g.Mes)
            .ToList();

        // Completar meses faltantes con $0 para el gráfico
        var revenuePorMes = new List<object>();
        for (int i = 11; i >= 0; i--)
        {
            var fecha = DateTime.Now.AddMonths(-i);
            var mesKey = $"{fecha.Year}-{fecha.Month:D2}";
            var mesNombre = fecha.ToString("MMM yyyy");
            var ingreso = ingresosporMes.FirstOrDefault(x => x.Mes == mesKey);
            revenuePorMes.Add(new { mes = mesNombre, total = ingreso?.Total ?? 0m });
        }

        return new
        {
            totalNegocios,
            activos,
            prueba,
            mrr,
            totalClientes,
            revenuePorMes
        };
    }

    // ═══════════════════════════════════════════════════════════
    // LOGS DE ADMINISTRACIÓN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Registra una acción del admin en la tabla AdminLogs
    /// </summary>
    public async Task RegistrarAdminLogAsync(string accion, string detalle, string negocioAfectado = null)
    {
        var log = new AdminLog
        {
            Id = Guid.NewGuid(),
            Accion = accion,
            Detalle = detalle,
            Fecha = DateTime.Now,
            NegocioAfectado = negocioAfectado
        };
        _context.AdminLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Obtiene las últimas 200 acciones administrativas
    /// </summary>
    public async Task<object> GetAdminLogsAsync()
    {
        return await _context.AdminLogs
            .OrderByDescending(l => l.Fecha)
            .Take(200)
            .Select(l => new
            {
                l.Id,
                l.Accion,
                l.Detalle,
                l.Fecha,
                l.NegocioAfectado
            })
            .ToListAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // DATOS FINANCIEROS PARA PESTAÑA DE VENTAS ADMIN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene datos financieros para la pestaña de Ventas del panel admin:
    /// - Ingresos totales (suma de PrecioSuscripcion de todos los negocios que han pagado)
    /// - Promedio de ingreso por negocio
    /// - Cantidad de negocios pagando activamente
    /// - Lista detallada de negocios que pagan
    /// </summary>
    public async Task<object> GetVentasAdminAsync()
    {
        var now = DateTime.Now;

        // Negocios activos que pagan (no en prueba, con precio > 0)
        var negociosPagando = await _context.Negocios
            .Where(n => n.IsActive && !n.EsPrueba && n.PrecioSuscripcion > 0)
            .ToListAsync();

        var totalRevenue = negociosPagando.Sum(n => n.PrecioSuscripcion);
        var businessesPaying = negociosPagando.Count;
        var averageRevenuePerBusiness = businessesPaying > 0
            ? totalRevenue / businessesPaying
            : 0m;

        // Lista detallada de negocios que pagan
        var payingBusinessesList = negociosPagando
            .OrderByDescending(n => n.PrecioSuscripcion)
            .Select(n => new
            {
                nombre = n.NegocioNombre,
                precio = n.PrecioSuscripcion,
                fechaPago = n.FechaPago,
                fechaExpiracion = n.FechaExpiracion,
                diasRestantes = n.FechaExpiracion.HasValue
                    ? (int)(n.FechaExpiracion.Value.Date - now.Date).TotalDays
                    : (int?)null,
                porEmpezar = n.FechaPago.HasValue && n.FechaPago.Value.Date > now.Date
            })
            .ToList();

        return new
        {
            totalRevenue,
            averageRevenuePerBusiness,
            businessesPaying,
            payingBusinessesList
        };
    }

    // ═══════════════════════════════════════════════════════════
    // EXPORTACIÓN EXCEL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Genera un archivo Excel con todos los negocios y su información básica
    /// </summary>
    public async Task<byte[]> ExportExcelAsync()
    {
        var negocios = await _context.Negocios
            .Include(g => g.Clientes)
            .OrderByDescending(g => g.FechaCreacion)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Negocios");

        // Encabezados
        worksheet.Cell(1, 1).Value = "Nombre del Negocio";
        worksheet.Cell(1, 2).Value = "Dueño";
        worksheet.Cell(1, 3).Value = "Email";
        worksheet.Cell(1, 4).Value = "Teléfono";
        worksheet.Cell(1, 5).Value = "Estado";
        worksheet.Cell(1, 6).Value = "Es Prueba";
        worksheet.Cell(1, 7).Value = "Total Clientes";
        worksheet.Cell(1, 8).Value = "Fecha Creación";

        // Estilo de encabezados
        var headerRange = worksheet.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
        headerRange.Style.Font.FontColor = XLColor.White;

        // Datos
        int row = 2;
        foreach (var negocio in negocios)
        {
            worksheet.Cell(row, 1).Value = negocio.NegocioNombre;
            worksheet.Cell(row, 2).Value = negocio.DuenoNegocio;
            worksheet.Cell(row, 3).Value = negocio.Email;
            worksheet.Cell(row, 4).Value = negocio.Telefono;
            worksheet.Cell(row, 5).Value = negocio.IsActive ? "Activo" : "Inactivo";
            worksheet.Cell(row, 6).Value = negocio.EsPrueba ? "Sí" : "No";
            worksheet.Cell(row, 7).Value = negocio.Clientes.Count;
            worksheet.Cell(row, 8).Value = negocio.FechaCreacion.ToString("dd/MM/yyyy");
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
