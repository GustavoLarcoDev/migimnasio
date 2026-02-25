// ═══════════════════════════════════════════════════════════
// VendedorService.cs — Implementación del servicio de vendedores
//
// Gestiona el ciclo de vida completo de los vendedores:
//   - CRUD con soft delete (eliminación lógica, no física)
//   - Login con verificación BCrypt
//   - Vista de negocios asignados con estadísticas
//   - Cola de leads comerciales para gestionar prospectos
//
// SOFT DELETE:
//   Los vendedores no se borran físicamente de la BD.
//   Solo se pone IsActive = false. Esto preserva el vínculo
//   histórico con los negocios que crearon (VendedorId en Gym).
//
// AISLAMIENTO DE DATOS:
//   Un vendedor solo ve los negocios donde n.VendedorId == suyo.
//   El filtrado se hace siempre en la consulta (no en el cliente).
// ═══════════════════════════════════════════════════════════

#nullable enable
using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Implementación concreta de IVendedorService.
/// Recibe ApplicationDbContext para acceso a BD e IAuthService
/// para verificar y hashear contraseñas con BCrypt.
/// </summary>
public class VendedorService : IVendedorService
{
    // DbContext de Entity Framework Core — acceso a todas las tablas
    private readonly ApplicationDbContext _context;

    // Servicio de autenticación — usado para hashear y verificar contraseñas BCrypt
    private readonly IAuthService _authService;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// ASP.NET Core resuelve las dependencias automáticamente
    /// según el registro en Program.cs (AddScoped).
    /// </summary>
    public VendedorService(ApplicationDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los vendedores activos con el conteo de negocios que manejan.
    ///
    /// Estrategia de dos consultas para mejor rendimiento:
    ///   1. Traer todos los vendedores activos.
    ///   2. Traer los conteos de negocios agrupados por vendedor en una sola consulta.
    ///   3. Combinar en memoria con un Dictionary para búsqueda O(1).
    ///
    /// Este enfoque es más eficiente que una subconsulta por vendedor (N+1 queries).
    ///
    /// negociosCreados cuenta SOLO negocios que pagan (IsActive=true, EsPrueba=false)
    /// porque ese es el indicador real del rendimiento del vendedor.
    /// </summary>
    public async Task<List<object>> GetAllVendedoresAsync()
    {
        // Primera consulta: todos los vendedores activos
        var vendedores = await _context.Vendedores.AsNoTracking()
            .Where(v => v.IsActive)
            .OrderByDescending(v => v.FechaCreacion)
            .ToListAsync();

        // Extraer IDs para filtrar en la segunda consulta
        var vendedorIds = vendedores.Select(v => v.VendedorId).ToHashSet();

        // Segunda consulta: conteo de negocios pagantes agrupado por vendedor.
        // Una sola query en lugar de N queries (una por vendedor).
        var negociosPorVendedor = await _context.Negocios
            .Where(n => n.VendedorId.HasValue
                && vendedorIds.Contains(n.VendedorId.Value)
                && n.IsActive
                && !n.EsPrueba)   // Solo negocios que pagan, no los que están en prueba
            .GroupBy(n => n.VendedorId!.Value)
            .Select(g => new { VendedorId = g.Key, Count = g.Count() })
            .ToListAsync();

        // Convertir a Dictionary para acceso O(1) al combinar con la lista de vendedores
        var countMap = negociosPorVendedor.ToDictionary(x => x.VendedorId, x => x.Count);

        // Combinar en memoria: para cada vendedor, buscar su conteo en el mapa
        return vendedores.Select(v => (object)new
        {
            v.VendedorId,
            v.Nombre,
            v.Apellido,
            nombreCompleto = $"{v.Nombre} {v.Apellido}",
            v.Correo,
            v.Telefono,
            v.NombreBanco,
            v.NumeroCedula,
            v.NumeroCuenta,
            v.IsActive,
            fechaCreacion = v.FechaCreacion.ToString("yyyy-MM-dd"),
            // GetValueOrDefault retorna 0 si el vendedor no tiene negocios asignados
            negociosCreados = countMap.GetValueOrDefault(v.VendedorId, 0)
        }).ToList();
    }

    /// <summary>
    /// Obtiene un vendedor por ID, verificando que esté activo.
    /// Los vendedores con IsActive=false (eliminados lógicamente) retornan null.
    /// </summary>
    public async Task<Vendedor?> GetVendedorAsync(Guid id)
    {
        return await _context.Vendedores.FirstOrDefaultAsync(v => v.VendedorId == id && v.IsActive);
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE VENDEDORES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo vendedor con contraseña hasheada.
    ///
    /// Flujo:
    ///   1. Validar campos obligatorios (nombre, apellido, correo, password).
    ///   2. Verificar unicidad de correo entre vendedores ACTIVOS.
    ///      Si un vendedor fue eliminado (soft delete) con el mismo correo,
    ///      se podría crear uno nuevo (el anterior ya no está activo).
    ///   3. Hashear la contraseña con BCrypt.
    ///   4. Normalizar el correo a minúsculas para consistencia en el login.
    ///   5. Guardar en la BD.
    /// </summary>
    public async Task<(bool success, string message)> CrearVendedorAsync(
        string nombre, string apellido, string correo, string telefono, string password,
        string? nombreBanco = null, string? numeroCedula = null, string? numeroCuenta = null)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
            return (false, "Nombre y apellido son obligatorios");

        if (string.IsNullOrWhiteSpace(correo))
            return (false, "El correo es obligatorio");

        // Contraseña mínimo 6 caracteres: regla de seguridad básica
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "La contraseña debe tener al menos 6 caracteres");

        // Verificar unicidad solo entre vendedores activos (IsActive = true).
        // Un correo de un vendedor eliminado puede reutilizarse.
        var existeCorreo = await _context.Vendedores.AnyAsync(v => v.Correo == correo && v.IsActive);
        if (existeCorreo)
            return (false, "Ya existe un vendedor con ese correo");

        var vendedor = new Vendedor
        {
            VendedorId = Guid.NewGuid(),
            // Trim() para eliminar espacios accidentales al inicio/fin
            Nombre = nombre.Trim(),
            Apellido = apellido.Trim(),
            // Normalizar correo a minúsculas para evitar duplicados por capitalización
            Correo = correo.Trim().ToLower(),
            Telefono = PhoneHelper.NormalizeEcuador(telefono?.Trim() ?? ""),
            // Hashear la contraseña antes de guardar (nunca guardar texto plano)
            Password = _authService.HashPassword(password),
            // Datos bancarios opcionales: null si vienen vacíos
            NombreBanco = string.IsNullOrWhiteSpace(nombreBanco) ? null : nombreBanco.Trim(),
            NumeroCedula = string.IsNullOrWhiteSpace(numeroCedula) ? null : numeroCedula.Trim(),
            NumeroCuenta = string.IsNullOrWhiteSpace(numeroCuenta) ? null : numeroCuenta.Trim(),
            IsActive = true,
            FechaCreacion = TimeHelper.Now
        };

        _context.Vendedores.Add(vendedor);
        await _context.SaveChangesAsync();

        return (true, $"Vendedor '{nombre} {apellido}' creado exitosamente");
    }

    /// <summary>
    /// Edita los datos de un vendedor existente.
    ///
    /// La contraseña es opcional en la edición:
    ///   - Si password es null o vacío: no se modifica el hash actual.
    ///   - Si viene con valor: se valida largo mínimo y se re-hashea.
    ///
    /// Validación de unicidad de correo excluye al propio vendedor
    /// para evitar que "editar sin cambiar el correo" falle.
    /// </summary>
    public async Task<(bool success, string message)> EditarVendedorAsync(
        Guid id, string nombre, string apellido, string correo, string telefono, string? password,
        string? nombreBanco = null, string? numeroCedula = null, string? numeroCuenta = null)
    {
        // Buscar solo entre activos para no editar vendedores eliminados
        var vendedor = await _context.Vendedores.FirstOrDefaultAsync(v => v.VendedorId == id && v.IsActive);
        if (vendedor == null)
            return (false, "Vendedor no encontrado");

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
            return (false, "Nombre y apellido son obligatorios");

        // Excluir el propio vendedor de la verificación de unicidad
        var existeCorreo = await _context.Vendedores.AnyAsync(v => v.Correo == correo && v.IsActive && v.VendedorId != id);
        if (existeCorreo)
            return (false, "Ya existe otro vendedor con ese correo");

        vendedor.Nombre = nombre.Trim();
        vendedor.Apellido = apellido.Trim();
        vendedor.Correo = correo.Trim().ToLower();
        vendedor.Telefono = PhoneHelper.NormalizeEcuador(telefono?.Trim() ?? "");
        // Datos bancarios: null si vienen vacíos (el campo se limpia si se borra en el form)
        vendedor.NombreBanco = string.IsNullOrWhiteSpace(nombreBanco) ? null : nombreBanco.Trim();
        vendedor.NumeroCedula = string.IsNullOrWhiteSpace(numeroCedula) ? null : numeroCedula.Trim();
        vendedor.NumeroCuenta = string.IsNullOrWhiteSpace(numeroCuenta) ? null : numeroCuenta.Trim();

        // Solo actualizar la contraseña si se envió una nueva
        if (!string.IsNullOrWhiteSpace(password))
        {
            if (password.Length < 6)
                return (false, "La contraseña debe tener al menos 6 caracteres");
            vendedor.Password = _authService.HashPassword(password);
        }

        _context.Update(vendedor);
        await _context.SaveChangesAsync();

        return (true, $"Vendedor '{nombre} {apellido}' actualizado");
    }

    /// <summary>
    /// Elimina un vendedor de forma lógica: pone IsActive = false.
    ///
    /// No se borra el registro físicamente porque:
    ///   1. Los negocios creados por este vendedor tienen VendedorId = su ID.
    ///      Si se borrara el vendedor, ese campo quedaría huérfano.
    ///   2. El historial de actividad del vendedor quedaría incompleto.
    ///   3. Si vuelve al equipo, se puede reactivar sin perder datos.
    ///
    /// El vendedor eliminado ya no puede hacer login (filtro IsActive = true en Login).
    /// </summary>
    public async Task<(bool success, string message)> EliminarVendedorAsync(Guid id)
    {
        // Aquí NO filtramos por IsActive porque podría intentarse eliminar
        // un vendedor ya inactivo (aunque sería un no-op)
        var vendedor = await _context.Vendedores.FirstOrDefaultAsync(v => v.VendedorId == id);
        if (vendedor == null)
            return (false, "Vendedor no encontrado");

        // Soft delete: marcar como inactivo en lugar de eliminar físicamente
        vendedor.IsActive = false;
        _context.Update(vendedor);
        await _context.SaveChangesAsync();

        return (true, $"Vendedor '{vendedor.Nombre} {vendedor.Apellido}' eliminado");
    }

    // ═══════════════════════════════════════════════════════════
    // LOGIN Y SESIÓN
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Autentica a un vendedor verificando correo y contraseña.
    ///
    /// Proceso de autenticación:
    ///   1. Si correo o password son vacíos, retorna null inmediatamente.
    ///   2. Busca en la BD un vendedor ACTIVO con ese correo (normalizado a minúsculas).
    ///   3. Si no existe, retorna null (no revelar si el correo existe o no).
    ///   4. Verifica el password contra el hash BCrypt con VerifyPassword.
    ///   5. Si el hash no coincide, retorna null.
    ///   6. Solo si todo es correcto, retorna el objeto Vendedor.
    ///
    /// La práctica de retornar null en todos los casos de fallo (en lugar de
    /// mensajes específicos) es una medida de seguridad: evita que un atacante
    /// descubra si un correo existe en el sistema.
    /// </summary>
    public async Task<Vendedor?> LoginVendedorAsync(string correo, string password)
    {
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
            return null;

        // Normalizar correo y buscar solo entre vendedores activos
        var vendedor = await _context.Vendedores
            .FirstOrDefaultAsync(v => v.Correo == correo.Trim().ToLower() && v.IsActive);

        // Si no existe el correo, retornar null sin indicar el motivo
        if (vendedor == null)
            return null;

        // VerifyPassword usa BCrypt internamente: extrae el salt del hash guardado
        // y hashea el password ingresado con ese mismo salt para comparar.
        if (!_authService.VerifyPassword(password, vendedor.Password))
            return null;

        return vendedor;
    }

    // ═══════════════════════════════════════════════════════════
    // NEGOCIOS ASIGNADOS Y ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene los negocios donde VendedorId coincide con el ID del vendedor autenticado.
    ///
    /// Cada negocio se enriquece con:
    ///   - totalClientes: subconsulta SQL que cuenta clientes de ese negocio.
    ///   - diasRestantesSuscripcion: (FechaExpiracion - Hoy), null si no tiene fecha.
    ///   - porEmpezar: true si FechaPago es en el futuro.
    ///
    /// Cast<object>().ToList() al final es necesario porque List<T anónimo>
    /// no puede convertirse implícitamente a List<object> sin el cast explícito.
    /// </summary>
    public async Task<List<object>> GetNegociosByVendedorAsync(Guid vendedorId)
    {
        var now = TimeHelper.Now;

        return (await _context.Negocios
            .Where(n => n.VendedorId == vendedorId)  // Aislamiento: solo los negocios de este vendedor
            .OrderByDescending(n => n.FechaCreacion)  // Los más recientes primero
            .Select(n => new
            {
                n.NegocioId,
                n.NegocioNombre,
                n.DuenoNegocio,
                n.Email,
                n.Telefono,
                n.IsActive,
                n.EsPrueba,
                n.NegocioBloqueado,
                // Subconsulta: cuántos clientes tiene este negocio
                totalClientes = _context.Clientes.Count(c => c.NegocioId == n.NegocioId),
                fechaCreacion = n.FechaCreacion.ToString("yyyy-MM-dd"),
                // Días restantes de suscripción (puede ser negativo si ya venció)
                diasRestantesSuscripcion = n.FechaExpiracion.HasValue
                    ? (int)(n.FechaExpiracion.Value.Date - now.Date).TotalDays
                    : (int?)null,
                n.FechaExpiracion,
                n.DiasPagados,
                n.PrecioSuscripcion,
                // Negocio "por empezar": el pago fue cargado pero la operación empieza en el futuro
                porEmpezar = n.FechaPago.HasValue && n.FechaPago.Value.Date > now.Date
            })
            .ToListAsync())
            // Cast necesario para convertir List<tipo anónimo> a List<object>
            .Cast<object>()
            .ToList();
    }

    /// <summary>
    /// Calcula estadísticas de rendimiento del vendedor para las tarjetas del dashboard.
    ///
    /// Cargamos todos los negocios del vendedor primero para poder
    /// calcular múltiples métricas en memoria sin múltiples queries.
    ///
    /// totalClientes usa una segunda consulta que filtra por los IDs de negocios
    /// del vendedor usando Contains(), que EF Core traduce a SQL IN(...).
    /// </summary>
    public async Task<object> GetVendedorStatsAsync(Guid vendedorId)
    {
        // Cargar todos los negocios del vendedor para calcular métricas en memoria
        var negocios = await _context.Negocios
            .Where(n => n.VendedorId == vendedorId)
            .ToListAsync();

        return new
        {
            totalNegocios = negocios.Count,
            // Activos = pagan suscripción (no están en prueba)
            activos = negocios.Count(n => n.IsActive && !n.EsPrueba),
            // En prueba = período gratuito, aún no convirtieron
            enPrueba = negocios.Count(n => n.EsPrueba),
            // Contar clientes de todos los negocios del vendedor con una sola query
            // Contains() se traduce a SQL IN(id1, id2, ...) por EF Core
            totalClientes = await _context.Clientes
                .Where(c => negocios.Select(n => n.NegocioId).Contains(c.NegocioId))
                .CountAsync()
        };
    }

    // ═══════════════════════════════════════════════════════════
    // GESTIÓN DE LEADS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los leads de la plataforma para la cola de trabajo de vendedores.
    ///
    /// Ordenamiento deliberado:
    ///   - OrderBy(l => l.Atendido): false(0) < true(1), entonces los no atendidos van primero.
    ///   - ThenByDescending(l.FechaCreacion): dentro de cada grupo, los más nuevos primero.
    ///
    /// La fecha se formatea como string "yyyy-MM-dd HH:mm" para que el frontend
    /// pueda mostrarla directamente sin procesamiento adicional.
    ///
    /// Todos los vendedores ven TODOS los leads (no hay aislamiento por vendedor aquí),
    /// para que cualquiera pueda tomar un lead libre.
    /// </summary>
    public async Task<List<object>> GetLeadsAsync()
    {
        var leads = await _context.LeadsVendedor.AsNoTracking()
            // No atendidos primero, luego por fecha descendente
            .OrderBy(l => l.Atendido)
            .ThenByDescending(l => l.FechaCreacion)
            .ToListAsync();

        return leads.Select(l => (object)new
        {
            l.Id,
            l.Nombre,
            l.NombreNegocio,
            l.Email,
            l.Telefono,
            l.Mensaje,
            l.Atendido,
            l.AtendidoPorNombre,
            l.Origen,
            // Formato legible: "2026-02-18 14:30" en lugar de DateTime completo
            fechaCreacion = l.FechaCreacion.ToString("yyyy-MM-dd HH:mm")
        }).ToList();
    }

    /// <summary>
    /// Marca un lead como atendido por un vendedor específico.
    ///
    /// Reglas de negocio:
    ///   - Un lead solo puede ser atendido una vez (estado final, no reversible).
    ///   - Si ya fue atendido, se retorna error con el nombre del vendedor que lo tomó.
    ///     Esto evita que dos vendedores trabajen el mismo lead sin saberlo.
    ///
    /// Guardamos AtendidoPorNombre (además de AtendidoPorId) para poder mostrar
    /// el nombre del vendedor sin necesidad de JOIN en consultas futuras.
    /// Esta es la técnica de "desnormalización selectiva" para optimizar lecturas.
    /// </summary>
    public async Task<(bool success, string message)> MarcarLeadAtendidoAsync(
        Guid leadId, Guid vendedorId, string vendedorNombre)
    {
        var lead = await _context.LeadsVendedor.FirstOrDefaultAsync(l => l.Id == leadId);
        if (lead == null)
            return (false, "Lead no encontrado");

        // Verificar si ya fue tomado por otro vendedor
        if (lead.Atendido)
            return (false, $"Este lead ya fue atendido por {lead.AtendidoPorNombre}");

        // Marcar como atendido y registrar qué vendedor lo tomó
        lead.Atendido = true;
        lead.AtendidoPorId = vendedorId;
        // Guardamos el nombre directamente para evitar JOIN al mostrar el historial
        lead.AtendidoPorNombre = vendedorNombre;

        _context.Update(lead);
        await _context.SaveChangesAsync();

        return (true, "Lead marcado como atendido");
    }

    /// <summary>
    /// Cuenta los leads pendientes para el badge de notificaciones del vendedor.
    ///
    /// CountAsync con un predicado es muy eficiente: EF Core lo traduce a
    ///   SELECT COUNT(*) FROM LeadsVendedor WHERE Atendido = 0
    /// que es la consulta más rápida posible para este propósito.
    ///
    /// Esta operación se llama en cada carga del dashboard para mantener
    /// el badge actualizado en tiempo real.
    /// </summary>
    public async Task<int> GetLeadsCountAsync()
    {
        return await _context.LeadsVendedor.CountAsync(l => !l.Atendido);
    }

    public async Task<(bool success, string message)> ActualizarDatosBancariosAsync(
        Guid vendedorId, string nombreBanco, string numeroCuenta, string numeroCedula)
    {
        try
        {
            var vendedor = await _context.Vendedores.FindAsync(vendedorId);
            if (vendedor == null)
                return (false, "Vendedor no encontrado");

            vendedor.NombreBanco = nombreBanco?.Trim();
            vendedor.NumeroCuenta = numeroCuenta?.Trim();
            vendedor.NumeroCedula = numeroCedula?.Trim();

            _context.Update(vendedor);
            await _context.SaveChangesAsync();

            return (true, "Datos bancarios actualizados exitosamente");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error al actualizar datos bancarios del vendedor {vendedorId}: {ex.Message}");
            return (false, "Error al actualizar los datos bancarios");
        }
    }
}
