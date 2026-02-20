// ═══════════════════════════════════════════════════════════
// NegocioService.cs — Implementación del servicio de negocios
//
// Contiene la lógica de negocio para gestionar los "tenants"
// de la plataforma SaaS. Cada negocio (gimnasio, barbería,
// taller, etc.) es un tenant independiente con su propia
// base de datos lógica filtrada por NegocioId.
//
// IMPORTANTE: La clase del modelo se llama "Gym" internamente
// (herencia histórica del nombre original del proyecto),
// pero conceptualmente representa cualquier tipo de negocio.
//
// RESPONSABILIDADES DE ESTE SERVICIO:
//   1. CRUD de negocios (crear, leer, editar, eliminar)
//   2. Cambio de estado (Activo <-> Prueba)
//   3. Estadísticas del dashboard del superadmin
//   4. Registro de acciones administrativas (AdminLogs)
//   5. Datos financieros para la pestaña de Ventas admin
//   6. Exportación a Excel
// ═══════════════════════════════════════════════════════════

using ClosedXML.Excel;
using Gimnasio.Data;
using Gimnasio.Helpers;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Implementación concreta de INegocioService.
/// Depende de ApplicationDbContext para acceso a datos
/// y de IAuthService para hashear contraseñas con BCrypt.
/// Ambas son inyectadas por el sistema de DI de ASP.NET Core.
/// </summary>
public class NegocioService : INegocioService
{
    // DbContext de Entity Framework Core — acceso a todas las tablas
    private readonly ApplicationDbContext _context;

    // Servicio de autenticación — usado aquí exclusivamente para hashear contraseñas
    private readonly IAuthService _authService;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// ASP.NET Core resuelve estas dependencias automáticamente
    /// según el registro en Program.cs (AddScoped).
    /// </summary>
    public NegocioService(ApplicationDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los negocios con datos enriquecidos para el panel admin.
    ///
    /// Campos calculados en la proyección Select():
    ///   - DiasRestantesSuscripcion: (FechaExpiracion - Hoy). Negativo si venció.
    ///   - PorEmpezar: true si FechaPago es en el futuro (suscripción prepagada).
    ///   - TotalClientes: subconsulta SQL que cuenta clientes de cada negocio.
    ///   - VendedorNombre: subconsulta SQL que busca el nombre del vendedor asignado.
    ///
    /// EF Core traduce las subconsultas en el Select a SQL correlacionado eficiente.
    /// </summary>
    public async Task<object> GetAllNegociosAsync()
    {
        var now = TimeHelper.Now;

        return await _context.Negocios.AsNoTracking()
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
                // Calcular días restantes: negativo = suscripción vencida.
                // Null si FechaExpiracion no está asignada (negocios en prueba sin fecha).
                DiasRestantesSuscripcion = g.FechaExpiracion.HasValue
                    ? (int)(g.FechaExpiracion.Value.Date - now.Date).TotalDays
                    : (int?)null,
                // PorEmpezar = true cuando el pago fue registrado pero la fecha de inicio es futura.
                // Ejemplo: vendedor carga hoy un negocio que empieza a operar el lunes.
                PorEmpezar = g.FechaPago.HasValue && g.FechaPago.Value.Date > now.Date,
                // Subconsulta: cuántos clientes tiene este negocio
                TotalClientes = _context.Clientes.Count(c => c.NegocioId == g.NegocioId),
                g.TipoNegocio,
                g.VendedorId,
                // Subconsulta: nombre del vendedor asignado (null si no tiene)
                VendedorNombre = g.VendedorId.HasValue
                    ? _context.Vendedores
                        .Where(v => v.VendedorId == g.VendedorId.Value)
                        .Select(v => v.Nombre + " " + v.Apellido)
                        .FirstOrDefault()
                    : null
            })
            // Los más recientes primero: el admin normalmente quiere ver los últimos que ingresaron
            .OrderByDescending(g => g.FechaCreacion)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene los datos básicos de un negocio para prellenar el modal de edición.
    /// NO incluye la contraseña en el objeto retornado: el hash BCrypt no se expone
    /// aunque sea en el panel admin, ya que el admin nunca necesita verlo.
    /// </summary>
    public async Task<object> GetNegocioAsync(Guid id)
    {
        var negocio = await _context.Negocios.FindAsync(id);
        if (negocio == null) return null;

        // Proyectar solo los campos necesarios para el formulario de edición.
        // Excluimos explícitamente Password para no exponer el hash BCrypt.
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
            negocio.FechaExpiracion,
            negocio.TipoNegocio
        };
    }

    /// <summary>
    /// Obtiene el objeto Gym completo (con Password) para el flujo de impersonación.
    /// La impersonación permite al admin iniciar sesión como ese negocio para diagnosticar
    /// problemas sin necesitar la contraseña del dueño.
    ///
    /// FindAsync usa la clave primaria directamente (más eficiente que FirstOrDefault).
    /// ADVERTENCIA: el objeto retornado contiene Password hasheado; no serializar a JSON.
    /// </summary>
    public async Task<Gym> GetNegocioForImpersonationAsync(Guid id)
    {
        return await _context.Negocios.FindAsync(id);
    }

    /// <summary>
    /// Obtiene un negocio por su email. Usado para recuperar el ID del negocio
    /// recién creado cuando CreateNegocioAsync solo retorna (success, message).
    /// El email es único en la plataforma, por lo que la búsqueda es segura.
    /// </summary>
    public async Task<Gym> GetNegocioByEmailAsync(string email)
    {
        return await _context.Negocios.FirstOrDefaultAsync(n => n.Email == email);
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo negocio (tenant) en la plataforma.
    ///
    /// Flujo de validaciones:
    ///   1. Verificar que IsActive y EsPrueba no sean ambos true ni ambos false.
    ///      El negocio debe ser uno u otro, nunca ambos ni ninguno.
    ///   2. Validar campos obligatorios: email, nombre, dueño, teléfono, password.
    ///   3. Verificar que el email no esté ya registrado en la plataforma.
    ///   4. Calcular fechas y valores por defecto si no se proporcionaron.
    ///   5. Hashear la contraseña con BCrypt y persistir el negocio.
    ///
    /// Valores por defecto aplicados si los parámetros son null:
    ///   - FechaPago: hoy
    ///   - FechaExpiracion: hoy + 7 días (prueba) o hoy + 30 días (activo)
    ///   - DiasPagados: 7 (prueba) o 30 (activo)
    ///   - PrecioSuscripcion: $0 (se actualiza después al acordar precio)
    /// </summary>
    public async Task<(bool success, string message)> CreateNegocioAsync(
        string nombre, string dueno, string telefono, string email,
        string password, bool isActive, bool esPrueba,
        DateTime? fechaPago = null, DateTime? fechaExpiracion = null,
        decimal? precioSuscripcion = null, int? diasPagados = null,
        Guid? vendedorId = null, string tipoNegocio = "membresias")
    {
        // ─── Validación de estado: debe ser uno de los dos, no ambos ─────
        // IsActive=true = negocio paga suscripción
        // EsPrueba=true = negocio está en período de prueba gratuita
        if (isActive && esPrueba)
            return (false, "Un negocio no puede ser de pago y de prueba al mismo tiempo");
        if (!isActive && !esPrueba)
            return (false, "Debe seleccionar si el negocio es de pago o de prueba");

        // ─── Validaciones de campos obligatorios ──────────────────────────
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Correo del Negocio es necesario");

        // El email es el identificador único de login en toda la plataforma
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

        var now = TimeHelper.Now;

        // ─── Calcular fechas y valores por defecto con el operador ?? ────
        // Si el parámetro es null, se usa el valor por defecto.
        // Prueba = 7 días gratis | Activo = empieza con 30 días
        var fechaPagoFinal = fechaPago ?? now;
        var fechaExpiracionFinal = fechaExpiracion ?? (esPrueba ? now.AddDays(7) : now.AddDays(30));
        var diasPagadosFinal = diasPagados ?? (esPrueba ? 7 : 30);
        var precioFinal = precioSuscripcion ?? 0m; // Precio $0 por defecto, se actualiza al acordar

        var negocio = new Gym
        {
            NegocioId = Guid.NewGuid(),
            NegocioNombre = nombre,
            DuenoNegocio = dueno,
            Telefono = PhoneHelper.NormalizeEcuador(telefono),
            Email = email,
            // BCrypt incluye salt aleatorio: el mismo password produce hashes distintos,
            // lo que hace imposible atacar con tablas rainbow.
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
            // Fallback a "membresias" si no se especifica, para evitar null en BD
            TipoNegocio = tipoNegocio ?? "membresias",
        };

        _context.Negocios.Add(negocio);
        await _context.SaveChangesAsync();

        return (true, "Negocio creado exitosamente");
    }

    /// <summary>
    /// Edita un negocio existente con soporte para cambio opcional de contraseña.
    ///
    /// Comportamiento especial con contraseña:
    ///   - Si negocio.Password viene en blanco: se conserva el hash actual (no se modifica).
    ///   - Si viene con valor: se re-hashea con BCrypt antes de guardar.
    ///   Esto permite editar nombre/email/teléfono sin obligar al admin a ingresar
    ///   una contraseña nueva cada vez.
    ///
    /// Si se asigna FechaExpiracion pero el negocio no tenía FechaPago,
    /// asignamos FechaPago = hoy para mantener consistencia del registro.
    /// </summary>
    public async Task<(bool success, string message)> EditarNegocioAsync(NegocioEditDto negocio)
    {
        if (negocio.IsActive && negocio.EsPrueba)
            return (false, "Un negocio no puede ser de pago y de prueba al mismo tiempo");
        if (!negocio.IsActive && !negocio.EsPrueba)
            return (false, "Debe seleccionar si el negocio es de pago o de prueba");

        // Buscar el negocio existente por clave primaria (más eficiente)
        var existente = await _context.Negocios.FindAsync(negocio.NegocioId);
        if (existente == null)
            return (false, "Negocio no encontrado");

        // Verificar unicidad de email excluyendo el negocio actual.
        // Sin la cláusula g.NegocioId != negocio.NegocioId, el propio negocio
        // jamás podría guardarse con su mismo email (falso positivo de duplicado).
        var existeEmail = await _context.Negocios
            .AnyAsync(g => g.Email == negocio.Email && g.NegocioId != negocio.NegocioId);
        if (existeEmail)
            return (false, "Ya existe otro negocio con ese email");

        // ─── Actualizar todos los campos editables ────────────────────
        existente.NegocioNombre = negocio.NegocioNombre;
        existente.DuenoNegocio = negocio.DuenoNegocio;
        existente.Telefono = PhoneHelper.NormalizeEcuador(negocio.Telefono);
        existente.Email = negocio.Email;
        existente.IsActive = negocio.IsActive;
        existente.EsPrueba = negocio.EsPrueba;
        if (negocio.DiasPagados.HasValue)
            existente.DiasPagados = negocio.DiasPagados.Value;
        if (negocio.PrecioSuscripcion.HasValue)
            existente.PrecioSuscripcion = negocio.PrecioSuscripcion.Value;
        existente.FechaExpiracion = negocio.FechaExpiracion;

        // Si se establece fecha de expiración pero no había fecha de pago registrada,
        // asignamos hoy como fecha de pago para mantener coherencia del registro.
        if (negocio.FechaExpiracion.HasValue && existente.FechaPago == null)
            existente.FechaPago = TimeHelper.Now;

        existente.FechaDeActualizacion = TimeHelper.Now;

        // ─── Contraseña: solo actualizar si se envió una nueva ───────
        if (!string.IsNullOrEmpty(negocio.Password))
        {
            existente.Password = _authService.HashPassword(negocio.Password);
        }

        _context.Update(existente);
        await _context.SaveChangesAsync();

        return (true, "Negocio actualizado exitosamente");
    }

    /// <summary>
    /// Elimina un negocio con verificación de integridad referencial manual.
    ///
    /// Usamos Include(g => g.Clientes) para cargar los clientes en el mismo query
    /// y poder verificar si hay alguno y cuántos son, sin una segunda consulta.
    ///
    /// No usamos eliminación en cascada automática porque queremos que el admin
    /// sepa explícitamente que está intentando eliminar un negocio con datos.
    /// El mensaje de error con el conteo de clientes es intencional para evitar
    /// eliminaciones accidentales.
    /// </summary>
    public async Task<(bool success, string message)> EliminarNegocioAsync(Guid id)
    {
        // Include para cargar la colección de clientes en el mismo JOIN
        var negocio = await _context.Negocios
            .Include(g => g.Clientes)
            .FirstOrDefaultAsync(g => g.NegocioId == id);

        if (negocio == null)
            return (false, "Negocio no encontrado");

        // Bloquear eliminación si tiene clientes para evitar datos huérfanos.
        // El mensaje muestra cuántos hay para que el admin sepa la magnitud.
        if (negocio.Clientes.Any())
            return (false, $"No se puede eliminar. El negocio tiene {negocio.Clientes.Count} cliente(s) registrado(s)");

        _context.Negocios.Remove(negocio);
        await _context.SaveChangesAsync();

        return (true, "Negocio eliminado exitosamente");
    }

    /// <summary>
    /// Alterna el estado del negocio entre "Pago (Activo)" y "Prueba".
    ///
    /// Funciona como un interruptor (toggle):
    ///   - Activo → Prueba:  IsActive=false, EsPrueba=true
    ///   - Prueba → Activo:  IsActive=true,  EsPrueba=false
    ///
    /// El admin usa esto para degradar un negocio cuando no paga su suscripción,
    /// o para activarlo cuando regulariza. Es más rápido que editar el formulario completo.
    ///
    /// Retornamos isActive y esPrueba en la tupla para que el controller pueda
    /// enviarlos al frontend y este actualice el badge de estado sin recargar la página.
    /// </summary>
    public async Task<(bool success, string message, bool? isActive, bool? esPrueba)> CambiarEstadoAsync(Guid id)
    {
        var negocio = await _context.Negocios.FindAsync(id);
        if (negocio == null)
            return (false, "Negocio no encontrado", null, null);

        // Toggle: si estaba activo pasa a prueba, y viceversa
        if (negocio.IsActive)
        {
            // Activo → Prueba: el negocio ya no paga, se degrada a modo gratuito
            negocio.IsActive = false;
            negocio.EsPrueba = true;
        }
        else
        {
            // Prueba → Activo: el negocio regularizó su pago
            negocio.IsActive = true;
            negocio.EsPrueba = false;
        }

        negocio.FechaDeActualizacion = TimeHelper.Now;
        _context.Update(negocio);
        await _context.SaveChangesAsync();

        string tipoActual = negocio.IsActive ? "Pago (Activo)" : "Prueba";
        return (true, $"Negocio cambiado a modo {tipoActual} exitosamente", negocio.IsActive, negocio.EsPrueba);
    }

    // ═══════════════════════════════════════════════════════════
    // ESTADÍSTICAS DEL DASHBOARD ADMIN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula todas las métricas del dashboard del superadmin.
    ///
    /// Estrategia de cálculo en dos pasos para mayor legibilidad:
    ///   1. Cargar todos los negocios a memoria (son pocos comparado con clientes).
    ///   2. Calcular métricas con LINQ en memoria.
    ///
    /// MRR (Monthly Recurring Revenue):
    ///   Solo cuenta negocios IsActive=true, EsPrueba=false y PrecioSuscripcion > 0.
    ///   Es la métrica financiera más importante de un SaaS.
    ///
    /// Para el gráfico de ingresos por mes (revenuePorMes):
    ///   - Se generan los 12 meses del período aunque no haya ingresos en algunos.
    ///   - Meses sin ingresos se insertan con $0 para que el gráfico sea continuo.
    ///   - Basado en FechaPago del negocio (no en pagos de clientes individuales).
    /// </summary>
    public async Task<object> GetAdminDashboardStatsAsync()
    {
        // Cargar todos los negocios en una sola consulta
        var negocios = await _context.Negocios.ToListAsync();
        var totalNegocios = negocios.Count;
        var activos = negocios.Count(n => n.IsActive);
        var prueba = negocios.Count(n => n.EsPrueba);

        // Contar clientes usando HashSet para búsqueda O(1) en el Where
        var negocioIds = negocios.Select(n => n.NegocioId).ToHashSet();
        var totalClientes = await _context.Clientes
            .Where(c => negocioIds.Contains(c.NegocioId))
            .CountAsync();

        // ─── MRR: suma de suscripciones activas que pagan ────────────
        var mrr = negocios
            .Where(n => n.IsActive && !n.EsPrueba && n.PrecioSuscripcion > 0)
            .Sum(n => n.PrecioSuscripcion);

        // ─── Ingresos por mes (basados en FechaPago, últimos 12 meses) ──
        var hace12Meses = TimeHelper.Now.AddMonths(-12);
        var ingresosporMes = negocios
            .Where(n => n.FechaPago.HasValue && n.FechaPago.Value >= hace12Meses && n.PrecioSuscripcion > 0)
            .GroupBy(n => new { n.FechaPago!.Value.Year, n.FechaPago.Value.Month })
            .Select(g => new
            {
                // Formato "YYYY-MM" para que el ordenamiento lexicográfico sea correcto
                Mes = $"{g.Key.Year}-{g.Key.Month:D2}",
                Total = g.Sum(n => n.PrecioSuscripcion)
            })
            .OrderBy(g => g.Mes)
            .ToList();

        // ─── Completar meses faltantes con $0 para el gráfico ─────────
        // Iteramos de más antiguo a más reciente (i=11 → i=0)
        // y si un mes no tuvo ingresos, lo añadimos con total=0
        var revenuePorMes = new List<object>();
        for (int i = 11; i >= 0; i--)
        {
            var fecha = TimeHelper.Now.AddMonths(-i);
            var mesKey = $"{fecha.Year}-{fecha.Month:D2}";
            var mesNombre = fecha.ToString("MMM yyyy"); // Ej: "Feb 2026" para la etiqueta del gráfico
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
    /// Registra una acción del superadmin en la tabla AdminLogs.
    ///
    /// Se llama después de cada operación exitosa del admin para mantener
    /// un rastro auditable de todas las acciones administrativas.
    ///
    /// Ejemplo de uso desde un controller:
    ///   await _negocioService.RegistrarAdminLogAsync(
    ///       "negocio_creado",
    ///       $"Admin creó el negocio '{nombre}' ({email})",
    ///       nombre);
    /// </summary>
    public async Task RegistrarAdminLogAsync(string accion, string detalle, string negocioAfectado = null)
    {
        var log = new AdminLog
        {
            Id = Guid.NewGuid(),
            Accion = accion,
            Detalle = detalle,
            Fecha = TimeHelper.Now,
            NegocioAfectado = negocioAfectado
        };
        _context.AdminLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Obtiene las últimas 200 acciones del admin, de más reciente a más antiguo.
    /// El límite de 200 es pragmático: evita sobrecargar la UI.
    /// Si el historial crece mucho, la solución es agregar paginación con Skip/Take.
    /// </summary>
    public async Task<object> GetAdminLogsAsync()
    {
        return await _context.AdminLogs.AsNoTracking()
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
    /// Calcula métricas financieras para la pestaña "Ventas" del panel del superadmin.
    ///
    /// Define "negocio que paga" como:
    ///   IsActive = true AND EsPrueba = false AND PrecioSuscripcion > 0
    ///
    /// Métricas calculadas:
    ///   - totalRevenue: suma de todas las suscripciones activas.
    ///   - averageRevenuePerBusiness: totalRevenue / cantidad de pagantes.
    ///     Si no hay pagantes, retorna 0 para evitar división por cero.
    ///   - businessesPaying: cuántos negocios están pagando actualmente.
    ///   - payingBusinessesList: lista detallada ordenada por precio descendente
    ///     para identificar los clientes más rentables (los de mayor ticket arriba).
    /// </summary>
    public async Task<object> GetVentasAdminAsync()
    {
        var now = TimeHelper.Now;

        // Filtrar solo negocios que efectivamente tienen suscripción paga
        var negociosPagando = await _context.Negocios
            .Where(n => n.IsActive && !n.EsPrueba && n.PrecioSuscripcion > 0)
            .ToListAsync();

        var totalRevenue = negociosPagando.Sum(n => n.PrecioSuscripcion);
        var businessesPaying = negociosPagando.Count;

        // Proteger contra división por cero si no hay negocios pagantes aún
        var averageRevenuePerBusiness = businessesPaying > 0
            ? totalRevenue / businessesPaying
            : 0m;

        // Lista detallada: los más caros primero para identificar clientes premium
        var payingBusinessesList = negociosPagando
            .OrderByDescending(n => n.PrecioSuscripcion)
            .Select(n => new
            {
                nombre = n.NegocioNombre,
                precio = n.PrecioSuscripcion,
                fechaPago = n.FechaPago,
                fechaExpiracion = n.FechaExpiracion,
                // Negativo = suscripción vencida (no debería ocurrir en activos, pero se muestra por completitud)
                diasRestantes = n.FechaExpiracion.HasValue
                    ? (int)(n.FechaExpiracion.Value.Date - now.Date).TotalDays
                    : (int?)null,
                // PorEmpezar: el vendedor cargó el negocio pero aún no empezó a operar
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
    /// Genera un archivo Excel con todos los negocios de la plataforma.
    ///
    /// Usa ClosedXML (librería open source, no requiere Office instalado en el servidor).
    /// El archivo se construye en un MemoryStream y se retorna como byte[],
    /// lo que permite enviarlo directamente como respuesta HTTP con el Content-Type correcto.
    ///
    /// Include(g => g.Clientes) carga las colecciones de clientes en la misma consulta
    /// para poder calcular negocio.Clientes.Count sin N+1 queries adicionales.
    /// </summary>
    public async Task<byte[]> ExportExcelAsync()
    {
        // Include para evitar el problema N+1: sin esto, cada acceso a negocio.Clientes.Count
        // generaría una consulta SQL adicional por negocio
        var negocios = await _context.Negocios
            .Include(g => g.Clientes)
            .OrderByDescending(g => g.FechaCreacion)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Negocios");

        // ─── Encabezados con estilo visual ────────────────────────────
        worksheet.Cell(1, 1).Value = "Nombre del Negocio";
        worksheet.Cell(1, 2).Value = "Dueño";
        worksheet.Cell(1, 3).Value = "Email";
        worksheet.Cell(1, 4).Value = "Teléfono";
        worksheet.Cell(1, 5).Value = "Estado";
        worksheet.Cell(1, 6).Value = "Es Prueba";
        worksheet.Cell(1, 7).Value = "Total Clientes";
        worksheet.Cell(1, 8).Value = "Fecha Creación";

        // Mismo color azul que en el Excel de clientes para consistencia visual
        var headerRange = worksheet.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
        headerRange.Style.Font.FontColor = XLColor.White;

        // ─── Datos: un negocio por fila ────────────────────────────────
        int row = 2;
        foreach (var negocio in negocios)
        {
            worksheet.Cell(row, 1).Value = negocio.NegocioNombre;
            worksheet.Cell(row, 2).Value = negocio.DuenoNegocio;
            worksheet.Cell(row, 3).Value = negocio.Email;
            worksheet.Cell(row, 4).Value = negocio.Telefono;
            worksheet.Cell(row, 5).Value = negocio.IsActive ? "Activo" : "Inactivo";
            worksheet.Cell(row, 6).Value = negocio.EsPrueba ? "Sí" : "No";
            // Clientes ya están en memoria gracias al Include, no hay query adicional
            worksheet.Cell(row, 7).Value = negocio.Clientes.Count;
            worksheet.Cell(row, 8).Value = negocio.FechaCreacion.ToString("dd/MM/yyyy");
            row++;
        }

        // Ajustar el ancho de cada columna al texto más largo que contenga
        worksheet.Columns().AdjustToContents();

        // Serializar a bytes para retornar como archivo HTTP descargable
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
