// ═══════════════════════════════════════════════════════════
// ClienteService.cs — Implementación del servicio de clientes
//
// Contiene toda la lógica de negocio para gestionar clientes:
//   - CRUD completo con auditoría vía logs
//   - Renovación de membresías
//   - Estadísticas del dashboard
//   - Importación y exportación de datos en Excel
//   - Soporte para dos modelos: membresías y citas artesanales
//
// PATRÓN DE LOGS INMUTABLES:
//   Cada acción que involucra dinero (crear, renovar, eliminar)
//   genera un registro en la tabla Logs. Esto garantiza que aunque
//   un cliente sea eliminado, sus ingresos históricos no desaparecen.
//
// MULTI-TENANCY:
//   Todas las consultas filtran por NegocioId para asegurar que
//   un negocio nunca pueda ver ni modificar datos de otro negocio.
// ═══════════════════════════════════════════════════════════

using ClosedXML.Excel;
using Gimnasio.Data;
using Gimnasio.Helpers;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Implementación concreta de IClienteService.
/// Recibe ApplicationDbContext e ILogService por inyección de dependencias
/// (configurados en Program.cs con AddScoped).
/// </summary>
public class ClienteService : IClienteService
{
    // DbContext de Entity Framework Core para acceder a la base de datos
    private readonly ApplicationDbContext _context;

    // Servicio de logs para registrar cada acción importante (pagos, cambios, etc.)
    private readonly ILogService _logService;

    /// <summary>
    /// Constructor: ASP.NET Core inyecta las dependencias automáticamente
    /// gracias al sistema de DI registrado en Program.cs.
    /// </summary>
    public ClienteService(ApplicationDbContext context, ILogService logService)
    {
        _context = context;
        _logService = logService;
    }

    // ═══════════════════════════════════════════════════════════
    // ESTADÍSTICAS DEL DASHBOARD
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula todas las métricas visibles en el dashboard del negocio.
    /// Los ingresos se calculan desde los Logs (no desde los clientes actuales)
    /// para garantizar que los datos financieros sean inmutables e históricos.
    /// </summary>
    /// <param name="negocioId">
    /// ID del negocio. Filtramos por este campo en TODAS las consultas
    /// para garantizar el aislamiento multi-tenant: un negocio nunca
    /// puede ver los clientes o ingresos de otro negocio.
    /// </param>
    public async Task<object> GetDashboardStatsAsync(Guid negocioId)
    {
        // Traemos todos los clientes del negocio a memoria para calcular
        // múltiples métricas sin volver a la base de datos cada vez.
        // Si hubiera millones de clientes, habría que calcular en la BD;
        // para el volumen actual esto es más simple y legible.
        var clientes = await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
            .ToListAsync();

        var totalClientes = clientes.Count;

        // Un cliente está activo si su fecha de vencimiento es hoy o en el futuro.
        // Comparamos .Date para ignorar la parte de la hora y evitar bugs de zona horaria.
        var clientesActivos = clientes.Count(c => c.FechaQueTermina.Date >= TimeHelper.Now.Date);
        var clientesVencidos = clientes.Count(c => c.FechaQueTermina.Date < TimeHelper.Now.Date);

        // Clientes nuevos: comparamos por fecha de creación
        var clientesNuevosHoy = clientes.Count(c => c.FechaDeCreacion.Date == TimeHelper.Now.Date);
        var clientesNuevosMes = clientes.Count(c =>
            c.FechaDeCreacion.Month == TimeHelper.Now.Month &&
            c.FechaDeCreacion.Year == TimeHelper.Now.Year);

        // ─── INGRESOS: Fuente inmutable ───────────────────────────────
        // Los ingresos NO se calculan sumando los precios de clientes actuales,
        // sino desde la tabla Logs. Razón: si un cliente es eliminado, su pago
        // sigue registrado. Los logs son el registro financiero verdadero.
        var logs = await _context.Logs
            .Where(l => l.NegocioId == negocioId && l.Monto > 0)
            .ToListAsync();

        // Ingresos del mes actual y de hoy, calculados en memoria sobre los logs filtrados
        var ingresosMes = logs
            .Where(l => l.Fecha.Month == TimeHelper.Now.Month && l.Fecha.Year == TimeHelper.Now.Year)
            .Sum(l => l.Monto);

        var ingresosHoy = logs
            .Where(l => l.Fecha.Date == TimeHelper.Now.Date)
            .Sum(l => l.Monto);

        // ─── PRÓXIMOS A VENCER ─────────────────────────────────────────
        // Clientes cuya membresía vence entre hoy y los próximos 5 días.
        // Ordenados por DiasRestantes para mostrar los más urgentes primero.
        // Esta lista se usa para mostrar alertas en el dashboard.
        var proximosVencer = clientes
            .Where(c =>
                c.FechaQueTermina.Date >= TimeHelper.Now.Date &&
                c.FechaQueTermina.Date <= TimeHelper.Now.AddDays(5).Date)
            .Select(c => new
            {
                c.Nombre,
                c.Apellido,
                NombreCompleto = $"{c.Nombre} {c.Apellido}",
                c.Telefono,
                c.FechaQueTermina,
                // Calculamos días restantes para que el frontend los muestre directamente
                DiasRestantes = (c.FechaQueTermina.Date - TimeHelper.Now.Date).Days
            })
            .OrderBy(c => c.DiasRestantes)
            .ToList();

        // Retornamos un objeto anónimo con todas las métricas.
        // El controller lo serializa a JSON automáticamente.
        return new
        {
            totalClientes,
            clientesActivos,
            clientesVencidos,
            clientesNuevosHoy,
            clientesNuevosMes,
            ingresosMes,
            ingresosHoy,
            proximosVencer
        };
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los clientes del negocio con campos calculados para el frontend.
    /// Usamos Select() para proyectar solo los campos necesarios y evitar traer
    /// datos innecesarios como hashes de contraseña u otros campos pesados.
    /// </summary>
    public async Task<object> GetClientesAsync(Guid negocioId)
    {
        return await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
            // Ordenamos por FechaQueTermina desc para que los activos aparezcan primero
            .OrderByDescending(c => c.FechaQueTermina)
            .Select(c => new
            {
                c.ClienteId,
                c.Nombre,
                c.Apellido,
                // Campo calculado en el servidor para evitar concatenación en el frontend
                NombreCompleto = $"{c.Nombre} {c.Apellido}",
                c.Email,
                c.Telefono,
                c.Direccion,
                c.Dias,
                c.Precio,
                c.FechaDeCreacion,
                c.FechaQueTermina,
                // PorEmpezar = true si la membresía aún no comenzó (fecha inicio en el futuro).
                // Ejemplo: se registra un cliente hoy para que empiece mañana.
                PorEmpezar = c.FechaDeCreacion.Date > TimeHelper.Now.Date,
                // DiasRestantes tiene lógica especial:
                //   - Si la membresía aún no empezó: días totales de la membresía
                //   - Si ya empezó: días que quedan desde hoy (puede ser negativo si venció)
                DiasRestantes = c.FechaDeCreacion.Date > TimeHelper.Now.Date
                    ? (c.FechaQueTermina.Date - c.FechaDeCreacion.Date).Days
                    : (c.FechaQueTermina.Date - TimeHelper.Now.Date).Days,
                // EstaActivo = true si la membresía vence hoy o en el futuro
                EstaActivo = c.FechaQueTermina.Date >= TimeHelper.Now.Date
            })
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un único cliente por ID, con validación de pertenencia al negocio.
    /// El doble filtro (ClienteId AND NegocioId) es clave: sin el filtro de NegocioId,
    /// un usuario malintencionado podría adivinar un GUID y ver datos de otro negocio.
    /// </summary>
    public async Task<Cliente> GetClienteAsync(Guid id, Guid negocioId)
    {
        return await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == id && c.NegocioId == negocioId);
    }

    /// <summary>
    /// Obtiene solo los clientes con EsDiario = true.
    /// Retorna un subconjunto de campos porque la vista de clientes diarios
    /// solo necesita nombre y teléfono para generar los links de WhatsApp.
    /// </summary>
    public async Task<object> GetClientesDiariosAsync(Guid negocioId)
    {
        return await _context.Clientes
            .Where(c => c.NegocioId == negocioId && c.EsDiario)
            // Los más recientes primero: el dueño suele querer ver los del día de hoy
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
    }

    // ═══════════════════════════════════════════════════════════
    // CREAR CLIENTE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo cliente con membresía periódica.
    ///
    /// Lógica de cálculo de fecha de fin (en orden de prioridad):
    ///   1. Si el DTO trae FechaFin explícita, se usa esa fecha.
    ///      Los días se calculan como (FechaFin - FechaInicio).
    ///   2. Si el DTO trae Dias, la fecha de fin = FechaInicio + Dias.
    ///   3. Si no trae ninguno, retorna error.
    ///
    /// Al final siempre se registra un log con el monto pagado.
    /// Esto garantiza integridad financiera: si el cliente es borrado
    /// después, el ingreso ya quedó registrado de forma inmutable.
    /// </summary>
    public async Task<(bool success, string message)> CrearClienteAsync(ClienteCreateDto model)
    {
        // ─── Validaciones de campos obligatorios ──────────────────────
        if (string.IsNullOrWhiteSpace(model.Nombre) || string.IsNullOrWhiteSpace(model.Apellido))
            return (false, "Nombre y Apellido son obligatorios");
        if (string.IsNullOrWhiteSpace(model.Telefono))
            return (false, "Teléfono es obligatorio");
        if (model.Precio <= 0)
            return (false, "El precio debe ser mayor a 0");

        // Si no se especifica fecha de inicio, se usa el momento actual
        var fechaInicio = model.FechaInicio ?? TimeHelper.Now;
        DateTime fechaFin;
        int dias;

        // ─── Calcular fecha de fin: FechaFin tiene prioridad sobre Dias ──
        // Esto permite flexibilidad: el dueño puede indicar "hasta el 31 de marzo"
        // o puede indicar "30 días", según lo que sea más conveniente en cada caso.
        if (model.FechaFin.HasValue)
        {
            fechaFin = model.FechaFin.Value;
            // Calculamos los días para guardar el dato y no perder información
            dias = (fechaFin - fechaInicio).Days;
        }
        else if (model.Dias > 0)
        {
            dias = model.Dias;
            fechaFin = fechaInicio.AddDays(dias);
        }
        else
        {
            return (false, "Debe indicar la fecha de finalización o los días");
        }

        // Validar que la fecha de fin sea posterior a la de inicio
        if (dias <= 0)
            return (false, "La fecha de finalización debe ser posterior a la fecha de inicio");

        // ─── Crear el cliente ────────────────────────────────────────
        var cliente = new Cliente
        {
            ClienteId = Guid.NewGuid(),
            NegocioId = model.NegocioId,
            Nombre = model.Nombre,
            Apellido = model.Apellido,
            Email = model.Email,
            Telefono = PhoneHelper.NormalizeEcuador(model.Telefono),
            Direccion = model.Direccion,
            Dias = dias,
            Precio = model.Precio,
            EsDiario = model.EsDiario,
            FechaDeCreacion = fechaInicio,
            FechaDeActualizacion = TimeHelper.Now,
            FechaQueTermina = fechaFin
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        // ─── Registrar log inmutable con el pago ──────────────────────
        // Este log se guarda SIEMPRE, incluso si después el cliente es eliminado.
        // Así el dueño siempre puede ver cuánto ingresó históricamente.
        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
        await _logService.CreateLogAsync(
            model.NegocioId,
            "cliente_creado",
            $"Nuevo cliente registrado: {nombreCompleto}, {dias} días, ${model.Precio:F2}, vence {fechaFin:dd/MM/yyyy}",
            model.Precio,
            cliente.ClienteId,
            nombreCompleto);

        return (true, "Cliente creado exitosamente");
    }

    // ═══════════════════════════════════════════════════════════
    // EDITAR CLIENTE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Edita un cliente existente con auditoría de cambios.
    ///
    /// El proceso es:
    ///   1. Buscar el cliente (con doble filtro de seguridad: id + negocioId).
    ///   2. Validar los nuevos datos.
    ///   3. Comparar campo por campo para detectar qué cambió.
    ///   4. Aplicar los cambios al objeto de la base de datos.
    ///   5. Guardar y registrar en logs con detalle de todos los campos que cambiaron.
    ///
    /// El log de auditoría le permite al dueño ver: "el 15/02 se cambió el precio
    /// de $500 a $600 y se extendió 30 días más", lo cual es muy útil en disputas.
    /// </summary>
    public async Task<(bool success, string message)> EditarClienteAsync(ClienteCreateDto model)
    {
        // Buscamos con doble filtro para garantizar que el cliente pertenezca al negocio
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == model.ClienteId && c.NegocioId == model.NegocioId);

        if (cliente == null)
            return (false, "Cliente no encontrado");
        if (string.IsNullOrWhiteSpace(model.Nombre) || string.IsNullOrWhiteSpace(model.Apellido))
            return (false, "Nombre y Apellido son obligatorios");
        if (string.IsNullOrWhiteSpace(model.Telefono))
            return (false, "Teléfono es obligatorio");
        if (model.Precio <= 0)
            return (false, "El precio debe ser mayor a 0");

        // Si no se especifica fecha de inicio, conservamos la original del cliente
        var fechaInicio = model.FechaInicio ?? cliente.FechaDeCreacion;
        DateTime fechaFin;
        int dias;

        // Misma lógica de prioridad que en CrearClienteAsync
        if (model.FechaFin.HasValue)
        {
            fechaFin = model.FechaFin.Value;
            dias = (fechaFin - fechaInicio).Days;
        }
        else if (model.Dias > 0)
        {
            dias = model.Dias;
            fechaFin = fechaInicio.AddDays(dias);
        }
        else
        {
            return (false, "Debe indicar la fecha de finalización o los días");
        }

        if (dias <= 0)
            return (false, "La fecha de finalización debe ser posterior a la fecha de inicio");

        // ─── Detectar cambios campo por campo ─────────────────────────
        // Armamos una lista de strings describiendo cada cambio.
        // Si ningún campo cambió, el log dirá "sin cambios detectados".
        // Esto es valioso para auditoría: el dueño puede ver el historial exacto.
        var cambios = new List<string>();
        var nombreAnterior = $"{cliente.Nombre} {cliente.Apellido}";

        if (cliente.Nombre != model.Nombre)
            cambios.Add($"nombre: {cliente.Nombre} → {model.Nombre}");
        if (cliente.Apellido != model.Apellido)
            cambios.Add($"apellido: {cliente.Apellido} → {model.Apellido}");
        if (cliente.Email != model.Email)
            cambios.Add($"email: {cliente.Email ?? "vacío"} → {model.Email ?? "vacío"}");
        if (cliente.Telefono != model.Telefono)
            cambios.Add($"teléfono: {cliente.Telefono} → {model.Telefono}");
        if (cliente.Direccion != model.Direccion)
            cambios.Add($"dirección actualizada");
        if (cliente.Dias != dias)
            cambios.Add($"días: {cliente.Dias} → {dias}");
        if (cliente.Precio != model.Precio)
            cambios.Add($"precio: ${cliente.Precio:F2} → ${model.Precio:F2}");
        if (cliente.EsDiario != model.EsDiario)
            cambios.Add($"tipo: {(cliente.EsDiario ? "Diario" : "Regular")} → {(model.EsDiario ? "Diario" : "Regular")}");
        if (cliente.FechaDeCreacion.Date != fechaInicio.Date)
            cambios.Add($"fecha inicio: {cliente.FechaDeCreacion:dd/MM/yyyy} → {fechaInicio:dd/MM/yyyy}");
        if (cliente.FechaQueTermina.Date != fechaFin.Date)
            cambios.Add($"fecha fin: {cliente.FechaQueTermina:dd/MM/yyyy} → {fechaFin:dd/MM/yyyy}");

        // ─── Aplicar los cambios al objeto tracked por EF Core ────────
        // EF Core rastrea el objeto 'cliente' automáticamente desde que lo
        // trajo de la BD. Al modificar sus propiedades y llamar SaveChanges,
        // EF genera el UPDATE solo con los campos que cambiaron.
        cliente.Nombre = model.Nombre;
        cliente.Apellido = model.Apellido;
        cliente.Email = model.Email;
        cliente.Telefono = PhoneHelper.NormalizeEcuador(model.Telefono);
        cliente.Direccion = model.Direccion;
        cliente.Dias = dias;
        cliente.Precio = model.Precio;
        cliente.EsDiario = model.EsDiario;
        cliente.FechaDeCreacion = fechaInicio;
        cliente.FechaQueTermina = fechaFin;
        cliente.FechaDeActualizacion = TimeHelper.Now;

        _context.Update(cliente);
        await _context.SaveChangesAsync();

        // Guardar en el log el detalle de todos los cambios detectados
        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
        var detalleCambios = cambios.Count > 0
            ? string.Join(", ", cambios)
            : "sin cambios detectados";

        // Monto = 0 porque una edición no implica necesariamente un pago nuevo
        await _logService.CreateLogAsync(
            model.NegocioId,
            "cliente_editado",
            $"Cliente {nombreAnterior} actualizado: {detalleCambios}",
            0,
            cliente.ClienteId,
            nombreCompleto);

        return (true, "Cliente actualizado exitosamente");
    }

    // ═══════════════════════════════════════════════════════════
    // ELIMINAR CLIENTE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Elimina un cliente de la base de datos.
    ///
    /// Antes de eliminar:
    ///   1. Verifica que el cliente exista y pertenezca al negocio (seguridad).
    ///   2. Verifica que no tenga citas asociadas (integridad referencial manual,
    ///      porque el modelo artesanal usa ClienteId en la tabla Citas).
    ///
    /// IMPORTANTE: Los logs de pago del cliente NO se eliminan. La tabla Logs
    /// es el registro financiero inmutable. Eliminar al cliente no afecta
    /// los ingresos reportados en el dashboard histórico.
    /// </summary>
    public async Task<(bool success, string message)> EliminarClienteAsync(Guid id, Guid negocioId)
    {
        // Buscar con doble filtro para garantizar aislamiento multi-tenant
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == id && c.NegocioId == negocioId);

        if (cliente == null)
            return (false, "Cliente no encontrado");

        // Verificar citas asociadas antes de eliminar.
        // Si el negocio usa el modelo artesanal, el cliente puede tener citas
        // futuras agendadas. No permitimos eliminar el cliente en ese caso
        // para evitar citas "huérfanas" sin cliente.
        // SEGURIDAD MULTI-TENANT: Filtramos también por NegocioId para aislamiento entre negocios.
        var tieneCitas = await _context.Citas
            .AnyAsync(c => c.ClienteId == id && c.NegocioId == negocioId);
        if (tieneCitas)
            return (false, "No se puede eliminar. El cliente tiene citas registradas.");

        // Preparar el detalle del log ANTES de eliminar, porque después ya no tenemos acceso al objeto
        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
        var diasRestantes = (cliente.FechaQueTermina.Date - TimeHelper.Now.Date).Days;
        var estadoCliente = diasRestantes >= 0
            ? $"activo, {diasRestantes} días restantes"
            : "vencido";

        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync();

        // Registrar la eliminación en logs para tener registro de quién fue eliminado y cuándo
        await _logService.CreateLogAsync(
            negocioId,
            "cliente_eliminado",
            $"Cliente eliminado: {nombreCompleto} ({estadoCliente}, ${cliente.Precio:F2})",
            0,
            cliente.ClienteId,
            nombreCompleto);

        return (true, "Cliente eliminado exitosamente");
    }

    // ═══════════════════════════════════════════════════════════
    // RENOVAR MEMBRESÍA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Extiende la membresía de un cliente hacia el futuro y registra el pago.
    ///
    /// Validación clave: la nueva fecha debe ser POSTERIOR a la fecha de fin actual.
    /// Esto evita el caso donde el dueño ingresa por error una fecha pasada
    /// o una fecha que acortaría la membresía actual del cliente.
    ///
    /// Al renovar se actualiza:
    ///   - FechaQueTermina: la nueva fecha de vencimiento
    ///   - Dias: los días adicionales que se agregaron en esta renovación
    ///   - Precio: el monto cobrado en esta renovación específica
    /// </summary>
    public async Task<(bool success, string message)> RenovarClienteAsync(
        Guid id, Guid negocioId, DateTime nuevaFechaFin, decimal precio)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == id && c.NegocioId == negocioId);

        if (cliente == null)
            return (false, "Cliente no encontrado");

        // La nueva fecha debe ser estrictamente posterior a la fecha de fin actual.
        // Si la nueva fecha es igual o anterior, probablemente hubo un error del usuario.
        if (nuevaFechaFin <= cliente.FechaQueTermina)
            return (false, "La nueva fecha debe ser posterior a la fecha de finalización actual");

        // Calcular cuántos días se agregan en esta renovación
        var diasAgregados = (nuevaFechaFin - cliente.FechaQueTermina).Days;

        cliente.FechaQueTermina = nuevaFechaFin;
        cliente.Dias = diasAgregados;   // Guardamos los días de ESTA renovación, no el total acumulado
        cliente.Precio = precio;        // Precio de esta renovación específica
        cliente.FechaDeActualizacion = TimeHelper.Now;

        _context.Update(cliente);
        await _context.SaveChangesAsync();

        // Registrar el pago en logs con Monto = precio para que aparezca en ingresos
        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
        await _logService.CreateLogAsync(
            negocioId,
            "cliente_renovado",
            $"Cliente {nombreCompleto} renovó: +{diasAgregados} días, ${precio:F2}, nueva fecha fin {nuevaFechaFin:dd/MM/yyyy}",
            precio,
            cliente.ClienteId,
            nombreCompleto);

        return (true, "Membresía renovada exitosamente");
    }

    // ═══════════════════════════════════════════════════════════
    // LIMPIAR CLIENTES DIARIOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Resetea la lista de clientes diarios marcando EsDiario = false en todos.
    ///
    /// Flujo de uso típico:
    ///   1. El dueño abre el panel de "Clientes Diarios".
    ///   2. Ve la lista de quienes vinieron hoy pagando por día.
    ///   3. Hace clic en los links de WhatsApp para confirmarles.
    ///   4. Presiona "Limpiar lista" que llama a este método.
    ///   5. Al día siguiente la lista empieza vacía de nuevo.
    ///
    /// Usamos un forEach en lugar de un UPDATE masivo para que EF Core
    /// rastree los cambios correctamente y actualice FechaDeActualizacion
    /// en cada entidad de forma individualizada.
    /// </summary>
    public async Task<int> LimpiarClientesDiariosAsync(Guid negocioId)
    {
        var clientesDiarios = await _context.Clientes
            .Where(c => c.NegocioId == negocioId && c.EsDiario)
            .ToListAsync();

        // Si no hay clientes diarios, no hay nada que hacer
        if (clientesDiarios.Count == 0)
            return 0;

        // Marcar cada cliente como no-diario y actualizar timestamp
        foreach (var cliente in clientesDiarios)
        {
            cliente.EsDiario = false;
            cliente.FechaDeActualizacion = TimeHelper.Now;
        }

        await _context.SaveChangesAsync();

        // Registrar la acción en logs para que quede registro de cuándo se limpió
        await _logService.CreateLogAsync(
            negocioId,
            "limpiar_diarios",
            $"Lista de clientes diarios limpiada: {clientesDiarios.Count} cliente(s)",
            0,
            null,
            null);

        return clientesDiarios.Count;
    }

    // ═══════════════════════════════════════════════════════════
    // CLIENTES ARTESANAL (negocios con modelo de citas)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Búsqueda de clientes para el autocomplete del formulario de citas.
    ///
    /// El usuario escribe en un campo de texto y se hace una búsqueda
    /// parcial (contains) en nombre, apellido y teléfono simultáneamente.
    /// ToLower() en ambos lados garantiza que la búsqueda no distinga mayúsculas.
    ///
    /// Limitamos a 10 resultados máximo para que el dropdown no sea
    /// demasiado largo y la consulta sea rápida.
    /// </summary>
    public async Task<object> BuscarClientesAsync(Guid negocioId, string query)
    {
        // Mínimo 2 caracteres para evitar traer todos los clientes en búsquedas vacías
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<object>();

        // Normalizar el query a minúsculas para comparación case-insensitive
        var q = query.ToLower();

        return await _context.Clientes
            .Where(c => c.NegocioId == negocioId &&
                // Buscar en nombre, apellido y teléfono con búsqueda parcial
                (c.Nombre.ToLower().Contains(q) ||
                 c.Apellido.ToLower().Contains(q) ||
                 // Teléfono puede ser null, por eso verificamos primero
                 (c.Telefono != null && c.Telefono.Contains(q))))
            .OrderBy(c => c.Nombre)
            .Take(10)
            .Select(c => new
            {
                c.ClienteId,
                c.Nombre,
                c.Apellido,
                NombreCompleto = c.Nombre + " " + c.Apellido,
                c.Telefono,
                c.Email
            })
            .ToListAsync();
    }

    /// <summary>
    /// Crea un cliente sin campos de membresía, para negocios artesanales.
    /// La diferencia con CrearClienteAsync es que estos clientes no pagan
    /// una cuota periódica: cada visita/cita tiene su propio costo.
    ///
    /// Se inicializan Dias=0, Precio=0, FechaQueTermina=Ahora porque la
    /// entidad Cliente los requiere a nivel de base de datos, pero no
    /// tienen significado funcional en el modelo artesanal.
    /// </summary>
    public async Task<(bool success, string message)> CrearClienteArtesanalAsync(ClienteArtesanalCreateDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Nombre) || string.IsNullOrWhiteSpace(model.Apellido))
            return (false, "Nombre y Apellido son obligatorios");

        var cliente = new Cliente
        {
            ClienteId = Guid.NewGuid(),
            NegocioId = model.NegocioId,
            Nombre = model.Nombre,
            Apellido = model.Apellido,
            Email = model.Email,
            Telefono = PhoneHelper.NormalizeEcuador(model.Telefono),
            Direccion = model.Direccion,
            // Campos de membresía en valores neutros: no aplican al modelo artesanal
            Dias = 0,
            Precio = 0,
            EsDiario = false,
            FechaDeCreacion = TimeHelper.Now,
            FechaDeActualizacion = TimeHelper.Now,
            FechaQueTermina = TimeHelper.Now   // Fecha "dummy", no tiene significado aquí
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        // Registrar creación en logs (sin monto porque no hay pago al crear el cliente)
        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
        await _logService.CreateLogAsync(
            model.NegocioId,
            "cliente_creado",
            $"Nuevo cliente registrado (artesanal): {nombreCompleto}",
            0,
            cliente.ClienteId,
            nombreCompleto);

        return (true, "Cliente creado exitosamente");
    }

    /// <summary>
    /// Actualiza los datos de contacto de un cliente artesanal.
    /// Solo modifica nombre, apellido, email, teléfono y dirección.
    /// No toca campos de membresía porque no aplican al modelo artesanal.
    /// </summary>
    public async Task<(bool success, string message)> EditarClienteArtesanalAsync(ClienteArtesanalCreateDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Nombre))
            return (false, "El nombre es obligatorio");

        // Doble filtro para seguridad multi-tenant
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == model.ClienteId && c.NegocioId == model.NegocioId);

        if (cliente == null)
            return (false, "Cliente no encontrado");

        // Actualizar solo los campos de datos personales, no los de membresía
        cliente.Nombre = model.Nombre;
        cliente.Apellido = model.Apellido;
        cliente.Email = model.Email;
        cliente.Telefono = PhoneHelper.NormalizeEcuador(model.Telefono);
        cliente.Direccion = model.Direccion;
        cliente.FechaDeActualizacion = TimeHelper.Now;

        await _context.SaveChangesAsync();

        return (true, "Cliente actualizado exitosamente");
    }

    /// <summary>
    /// Obtiene todos los clientes del negocio artesanal con el total de citas.
    /// TotalCitas se calcula con una subconsulta correlacionada por ClienteId.
    /// EF Core traduce esto a una subconsulta SQL eficiente:
    ///   SELECT ..., (SELECT COUNT(*) FROM Citas WHERE ClienteId = c.ClienteId) AS TotalCitas
    /// </summary>
    public async Task<object> GetClientesArtesanalAsync(Guid negocioId)
    {
        return await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
            .OrderBy(c => c.Nombre)
            .Select(c => new
            {
                c.ClienteId,
                c.Nombre,
                c.Apellido,
                NombreCompleto = c.Nombre + " " + c.Apellido,
                c.Email,
                c.Telefono,
                c.Direccion,
                c.FechaDeCreacion,
                // Subconsulta: contar cuántas citas tiene este cliente en particular
                TotalCitas = _context.Citas.Count(ci => ci.ClienteId == c.ClienteId)
            })
            .ToListAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // EXPORTACIÓN EXCEL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Genera un archivo Excel con todos los clientes del negocio.
    ///
    /// Usa la librería ClosedXML (open source, sin dependencia de Excel instalado).
    /// El archivo se genera en un MemoryStream y se retorna como byte[],
    /// lo que permite enviarlo directamente como respuesta HTTP.
    ///
    /// Formato del archivo:
    ///   - Fila 1: Encabezados en negrita con fondo azul
    ///   - Filas 2+: Un cliente por fila
    ///   - Columnas A-J ajustadas automáticamente al contenido
    /// </summary>
    public async Task<byte[]> ExportClientesExcelAsync(Guid negocioId)
    {
        var clientes = await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Clientes");

        // ─── Encabezados ──────────────────────────────────────────────
        worksheet.Cell(1, 1).Value = "Nombre";
        worksheet.Cell(1, 2).Value = "Apellido";
        worksheet.Cell(1, 3).Value = "Email";
        worksheet.Cell(1, 4).Value = "Teléfono";
        worksheet.Cell(1, 5).Value = "Fecha Inicio";
        worksheet.Cell(1, 6).Value = "Fecha Vencimiento";
        worksheet.Cell(1, 7).Value = "Días";
        worksheet.Cell(1, 8).Value = "Precio";
        worksheet.Cell(1, 9).Value = "Tipo";
        worksheet.Cell(1, 10).Value = "Estado";

        // Estilo visual de la fila de encabezados
        var headerRange = worksheet.Range(1, 1, 1, 10);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
        headerRange.Style.Font.FontColor = XLColor.White;

        // ─── Datos: un cliente por fila ────────────────────────────────
        int row = 2;
        foreach (var cliente in clientes)
        {
            worksheet.Cell(row, 1).Value = cliente.Nombre;
            worksheet.Cell(row, 2).Value = cliente.Apellido;
            worksheet.Cell(row, 3).Value = cliente.Email;
            worksheet.Cell(row, 4).Value = cliente.Telefono;
            worksheet.Cell(row, 5).Value = cliente.FechaDeCreacion;
            worksheet.Cell(row, 5).Style.DateFormat.Format = "dd/MM/yyyy";
            worksheet.Cell(row, 6).Value = cliente.FechaQueTermina;
            worksheet.Cell(row, 6).Style.DateFormat.Format = "dd/MM/yyyy";
            worksheet.Cell(row, 7).Value = cliente.Dias;
            worksheet.Cell(row, 8).Value = cliente.Precio;
            worksheet.Cell(row, 9).Value = cliente.EsDiario ? "Diario" : "Regular";

            // Calculamos el estado en el momento de exportar
            bool activo = cliente.FechaQueTermina.Date >= TimeHelper.Now.Date;
            worksheet.Cell(row, 10).Value = activo ? "Activo" : "Vencido";

            row++;
        }

        // Ajustar el ancho de cada columna al contenido más largo
        worksheet.Columns().AdjustToContents();

        // Serializar a bytes para poder retornar como archivo HTTP
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════════════
    // IMPORTACIÓN EXCEL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Importa clientes desde un archivo Excel (.xlsx) subido por el usuario.
    ///
    /// El importador es tolerante y flexible:
    ///   1. Detecta columnas por nombre de encabezado (normaliza tildes para matching).
    ///   2. Si no detecta encabezados, cae en modo "columnas por posición" (fallback).
    ///   3. Omite filas con nombre+apellido duplicados (no sobreescribe clientes existentes).
    ///   4. Cada fila se procesa con try/catch independiente para que un error en una
    ///      fila no impida importar el resto.
    ///   5. Al final retorna un resumen completo: creados, omitidos, errores.
    /// </summary>
    public async Task<object> ImportarClientesExcelAsync(Guid negocioId, Stream fileStream)
    {
        // Listas para el resumen final del proceso de importación
        var clientesCreados = new List<string>();
        var clientesOmitidos = new List<string>();
        var errores = new List<string>();

        // Copiar el stream a memoria para que ClosedXML pueda leerlo libremente
        // (algunos streams HTTP no soportan seek hacia atrás)
        using var memStream = new MemoryStream();
        await fileStream.CopyToAsync(memStream);
        memStream.Position = 0;

        using var workbook = new XLWorkbook(memStream);
        var worksheet = workbook.Worksheet(1); // Siempre tomamos la primera hoja
        var rangeUsed = worksheet.RangeUsed();  // Solo las celdas con datos

        if (rangeUsed == null)
            return new { success = false, message = "El archivo está vacío" };

        // ─── FASE 1: Detectar columnas por encabezados ─────────────────
        // Leemos la fila 1 y mapeamos cada encabezado a su número de columna.
        // Normalizamos quitando tildes para aceptar variantes: "teléfono" y "telefono".
        var headerRow = rangeUsed.Row(1);
        var colMap = new Dictionary<string, int>();  // Clave = nombre semántico, Valor = número de columna

        for (int col = 1; col <= rangeUsed.ColumnCount(); col++)
        {
            var header = headerRow.Cell(col).GetString()?.Trim().ToLowerInvariant() ?? "";

            // Quitar tildes para hacer el matching flexible sin distinguir acentos
            header = header
                .Replace("á", "a").Replace("é", "e")
                .Replace("í", "i").Replace("ó", "o")
                .Replace("ú", "u");

            // Asignar la columna al campo semántico correspondiente
            if (header.Contains("nombre") && !header.Contains("apellido") && !header.Contains("cliente") && !colMap.ContainsKey("nombre"))
                colMap["nombre"] = col;
            else if (header.Contains("apellido"))
                colMap["apellido"] = col;
            else if ((header.Contains("nombre") && header.Contains("cliente")) || header == "cliente")
                // Columna "Nombre Cliente" o "Cliente": nombre completo en una sola celda
                colMap["nombre_completo"] = col;
            else if (header.Contains("email") || header.Contains("correo"))
                colMap["email"] = col;
            else if (header.Contains("telefono") || header.Contains("tel") || header.Contains("celular") || header.Contains("whatsapp"))
                colMap["telefono"] = col;
            else if (header.Contains("direccion") || header.Contains("address"))
                colMap["direccion"] = col;
            else if (header.Contains("inicio") || header.Contains("creacion"))
                colMap["fecha_inicio"] = col;
            else if (header.Contains("vencimiento") || header.Contains("fecha fin") || header.Contains("fin") || header.Contains("termina"))
                colMap["fecha_fin"] = col;
            else if (header.Contains("dia") && !header.Contains("diario"))
                colMap["dias"] = col;
            else if (header.Contains("precio") || header.Contains("monto") || header.Contains("costo"))
                colMap["precio"] = col;
            else if (header == "tipo")
                colMap["tipo"] = col;
        }

        // ─── FASE 2: Fallback por posición si no se detectaron encabezados ──
        // Si el archivo no tiene encabezados (o no los reconocimos), asumimos
        // el orden más común: Nombre, Apellido, Email, Teléfono, FechaInicio, FechaFin, Días, Precio.
        if (!colMap.ContainsKey("nombre") && !colMap.ContainsKey("nombre_completo"))
        {
            colMap["nombre"] = 1;
            colMap["apellido"] = 2;
            if (rangeUsed.ColumnCount() >= 3) colMap["email"] = 3;
            if (rangeUsed.ColumnCount() >= 4) colMap["telefono"] = 4;
            if (rangeUsed.ColumnCount() >= 5) colMap["fecha_inicio"] = 5;
            if (rangeUsed.ColumnCount() >= 6) colMap["fecha_fin"] = 6;
            if (rangeUsed.ColumnCount() >= 7) colMap["dias"] = 7;
            if (rangeUsed.ColumnCount() >= 8) colMap["precio"] = 8;
        }

        // ─── FASE 3: Cargar clientes existentes para detectar duplicados ──
        // Cargamos en memoria los nombres de clientes existentes para hacer
        // la comparación de duplicados en .NET, evitando una consulta por fila.
        var clientesExistentes = await _context.Clientes
            .Where(c => c.NegocioId == negocioId)
            .Select(c => new { c.Nombre, c.Apellido })
            .ToListAsync();

        // Skip(1) para omitir la fila de encabezados
        var rows = rangeUsed.RowsUsed().Skip(1);
        int rowNumber = 2;

        // ─── FASE 4: Procesar cada fila del Excel ─────────────────────
        foreach (var row in rows)
        {
            try
            {
                // ── Leer nombre y apellido ────────────────────────────────
                string nombre, apellido;

                if (colMap.ContainsKey("nombre_completo"))
                {
                    // Si el Excel tiene nombre completo en una celda, lo separamos por el primer espacio.
                    // Ejemplo: "María Fernández" → nombre="María", apellido="Fernández"
                    var fullName = GetCellString(row.Cell(colMap["nombre_completo"]));
                    if (string.IsNullOrWhiteSpace(fullName))
                    {
                        errores.Add($"Fila {rowNumber}: Nombre es obligatorio");
                        rowNumber++;
                        continue;
                    }
                    // Split con límite 2 para preservar apellidos compuestos en el segundo elemento
                    var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    nombre = parts[0];
                    apellido = parts.Length > 1 ? parts[1] : "-";
                }
                else
                {
                    // Columnas separadas de nombre y apellido
                    nombre = colMap.ContainsKey("nombre") ? GetCellString(row.Cell(colMap["nombre"])) : "";
                    apellido = colMap.ContainsKey("apellido") ? GetCellString(row.Cell(colMap["apellido"])) : "-";
                }

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errores.Add($"Fila {rowNumber}: Nombre es obligatorio");
                    rowNumber++;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(apellido)) apellido = "-";

                // ── Verificar duplicados (nombre + apellido) ──────────────
                // Si ya existe un cliente con el mismo nombre completo, omitimos la fila
                // en lugar de sobreescribir o crear duplicados.
                if (clientesExistentes.Any(c => c.Nombre == nombre && c.Apellido == apellido))
                {
                    clientesOmitidos.Add($"{nombre} {apellido}");
                    rowNumber++;
                    continue;
                }

                // ── Campos opcionales de contacto ─────────────────────────
                var email = colMap.ContainsKey("email") ? GetCellString(row.Cell(colMap["email"])) : "";
                var telefono = colMap.ContainsKey("telefono") ? GetCellString(row.Cell(colMap["telefono"])) : "";
                var direccion = colMap.ContainsKey("direccion") ? GetCellString(row.Cell(colMap["direccion"])) : "";

                // ── Fechas y días ─────────────────────────────────────────
                DateTime fechaInicio = TimeHelper.Now;
                DateTime? fechaFin = null;
                int dias = 30; // Valor por defecto si no se puede determinar

                if (colMap.ContainsKey("fecha_inicio"))
                    fechaInicio = TryParseExcelDate(row.Cell(colMap["fecha_inicio"])) ?? TimeHelper.Now;

                if (colMap.ContainsKey("fecha_fin"))
                    fechaFin = TryParseExcelDate(row.Cell(colMap["fecha_fin"]));

                if (colMap.ContainsKey("dias"))
                {
                    try { dias = (int)row.Cell(colMap["dias"]).GetDouble(); }
                    catch { dias = 30; } // Si la celda no es numérica, usar 30 días por defecto
                }

                // FechaFin tiene prioridad sobre Días (misma lógica que en CrearClienteAsync)
                if (fechaFin.HasValue && fechaFin.Value > fechaInicio)
                {
                    dias = (fechaFin.Value - fechaInicio).Days;
                }
                else if (dias > 0)
                {
                    fechaFin = fechaInicio.AddDays(dias);
                }
                else
                {
                    // Si no se puede determinar, usar 30 días como fallback seguro
                    dias = 30;
                    fechaFin = fechaInicio.AddDays(30);
                }

                // ── Precio ────────────────────────────────────────────────
                decimal precio = 0;
                if (colMap.ContainsKey("precio"))
                {
                    try { precio = (decimal)row.Cell(colMap["precio"]).GetDouble(); }
                    catch { precio = 0; }
                }
                // Sanear precios negativos (puede ocurrir si el Excel tiene errores)
                if (precio < 0) precio = 0;

                // ── Tipo de cliente ───────────────────────────────────────
                bool esDiario = false;
                if (colMap.ContainsKey("tipo"))
                {
                    var tipo = GetCellString(row.Cell(colMap["tipo"]));
                    // Aceptamos "Diario" o "Si/Sí" como indicadores de cliente diario
                    esDiario = tipo.Equals("diario", StringComparison.OrdinalIgnoreCase) ||
                               tipo.Equals("si", StringComparison.OrdinalIgnoreCase);
                }

                // ── Crear el objeto Cliente y agregarlo al contexto ───────
                var cliente = new Cliente
                {
                    ClienteId = Guid.NewGuid(),
                    NegocioId = negocioId,
                    Nombre = nombre,
                    Apellido = apellido,
                    Email = email,
                    Telefono = PhoneHelper.NormalizeEcuador(telefono),
                    Direccion = direccion,
                    Dias = dias,
                    Precio = precio,
                    EsDiario = esDiario,
                    FechaDeCreacion = fechaInicio,
                    FechaDeActualizacion = TimeHelper.Now,
                    FechaQueTermina = fechaFin.Value
                };

                _context.Clientes.Add(cliente);
                clientesCreados.Add($"{nombre} {apellido}");

                // Agregar a la lista en memoria para detectar duplicados dentro del mismo Excel
                // (sin esto, dos filas con el mismo nombre en el Excel se procesarían ambas)
                clientesExistentes.Add(new { Nombre = nombre, Apellido = apellido });
            }
            catch (Exception ex)
            {
                // Un error en una fila no detiene el resto de la importación
                errores.Add($"Fila {rowNumber}: {ex.Message}");
            }
            rowNumber++;
        }

        // Guardar todos los clientes creados en una sola operación de BD
        await _context.SaveChangesAsync();

        // Retornar resumen completo del proceso
        return new
        {
            success = true,
            message = "Importación completada",
            clientesCreados = clientesCreados.Count,
            clientesOmitidos = clientesOmitidos.Count,
            erroresCount = errores.Count,
            detalleCreados = clientesCreados,
            detalleOmitidos = clientesOmitidos,
            detalleErrores = errores
        };
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS PRIVADOS PARA IMPORTACIÓN EXCEL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Intenta interpretar el contenido de una celda de Excel como fecha.
    ///
    /// El problema es que Excel guarda fechas de múltiples formas:
    ///   1. Tipo DateTime nativo: ClosedXML lo lee directamente.
    ///   2. Número serial OLE (ej: 44927.0 = 01/01/2023): Excel internamente
    ///      guarda fechas como días transcurridos desde 01/01/1900.
    ///   3. String con formato variado (dd/MM/yyyy, yyyy-MM-dd, etc.).
    ///
    /// Probamos los tres métodos en orden de prioridad para ser lo más
    /// tolerante posible con diferentes formatos de Excel.
    /// </summary>
    private static DateTime? TryParseExcelDate(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;

        // Intento 1: DateTime nativo (el más confiable si el Excel fue bien formateado)
        try
        {
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();
        }
        catch { }

        // Intento 2: Número serial OLE (fechas guardadas como número en Excel)
        // El rango 1-100000 filtra valores que no tienen sentido como fechas
        try
        {
            if (cell.DataType == XLDataType.Number)
            {
                var num = cell.GetDouble();
                if (num > 1 && num < 100000)
                    return DateTime.FromOADate(num);
            }
        }
        catch { }

        // Intento 3: String con múltiples formatos posibles
        var str = cell.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(str)) return null;

        // Lista de formatos comunes en archivos Excel latinoamericanos y universales
        string[] formats = {
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
            "yyyy-MM-dd", "yyyy/MM/dd",
            "MM/dd/yyyy", "M/d/yyyy",
            "dd/MM/yyyy HH:mm", "yyyy-MM-dd HH:mm:ss"
        };

        // Intentar parseo exacto con los formatos conocidos
        if (DateTime.TryParseExact(str, formats,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var parsed))
            return parsed;

        // Último recurso: parseo libre (depende de la cultura del sistema)
        if (DateTime.TryParse(str, out var fallback))
            return fallback;

        return null; // Si ningún método funcionó, retornamos null
    }

    /// <summary>
    /// Lee el contenido de una celda de Excel como texto limpio.
    ///
    /// Necesitamos este helper porque ClosedXML puede lanzar excepciones
    /// si se intenta leer una celda numérica como string con GetString().
    /// En ese caso, usamos cell.Value.ToString() como fallback.
    /// </summary>
    private static string GetCellString(IXLCell cell)
    {
        if (cell.IsEmpty()) return "";
        try
        {
            return cell.GetString()?.Trim() ?? "";
        }
        catch
        {
            // Fallback: convertir el valor bruto a string
            return cell.Value.ToString()?.Trim() ?? "";
        }
    }
}
