// ═══════════════════════════════════════════════════════════════════════════
// NotificationService.cs — Servicio de notificaciones automáticas del dashboard
//
// PROPÓSITO:
//   Genera y gestiona alertas internas para el dueño del negocio cuando
//   membresías de clientes están próximas a vencer.
//
//   IMPORTANTE: Estas notificaciones NO son mensajes de WhatsApp ni emails.
//   Son alertas dentro del panel de control de la plataforma, visibles solo
//   para el dueño del negocio. Se muestran en el panel lateral del dashboard
//   con un badge (número rojo) que indica cuántas están sin leer.
//
// FLUJO DE GENERACIÓN AUTOMÁTICA:
//   1. El dueño abre su dashboard (HomeController o NegocioController)
//   2. El controlador llama a GenerarNotificacionesAsync()
//   3. El servicio busca clientes con membresía que vence en 0-3 días
//   4. Por cada cliente, verifica si ya tiene notificación de HOY (anti-duplicado)
//   5. Si no tiene → crea una nueva notificación
//   6. Las notificaciones nuevas aparecen como "no leídas" (badge rojo)
//   7. El dueño puede marcarlas como leídas individualmente o todas a la vez
//
// DISEÑO DE LA TABLA Notificaciones:
//   - Id (Guid): identificador único
//   - NegocioId (Guid): a qué negocio pertenece
//   - ClienteId (Guid): a qué cliente hace referencia
//   - NombreCliente (string): snapshot del nombre (en caso de que cambie)
//   - Mensaje (string): texto descriptivo de la alerta
//   - Tipo (string): "vencimiento" (puede haber más tipos en el futuro)
//   - Leida (bool): false = no leída (badge rojo), true = leída (sin badge)
//   - FechaCreacion (DateTime): para ordenar y para el anti-duplicado diario
// ═══════════════════════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Servicio de notificaciones automáticas del dashboard.
/// Se encarga de crear alertas de vencimiento de membresía y de gestionar
/// el estado leído/no leído de cada notificación.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// El constructor solo necesita el DbContext porque este servicio no envía
    /// mensajes externos — solo escribe y lee de la base de datos local.
    /// Los mensajes externos (WhatsApp, email) son responsabilidad de otros servicios.
    /// </summary>
    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las últimas 50 notificaciones del negocio.
    ///
    /// Por qué limitamos a 50:
    ///   Un negocio activo puede acumular cientos de notificaciones en meses.
    ///   Cargar todas en cada request del dashboard sería innecesariamente lento.
    ///   Las 50 más recientes cubren toda la información relevante para el dueño.
    ///
    /// La proyección a tipo anónimo evita serializar propiedades de navegación
    /// de EF Core que podrían causar referencias circulares en el JSON.
    /// </summary>
    public async Task<object> GetNotificacionesAsync(Guid negocioId)
    {
        return await _context.Notificaciones
            .Where(n => n.NegocioId == negocioId)
            .OrderByDescending(n => n.FechaCreacion)  // Las más recientes primero
            .Select(n => new
            {
                n.Id,
                n.Mensaje,
                n.Tipo,
                n.ClienteId,
                n.NombreCliente,
                n.Leida,
                n.FechaCreacion
            })
            .Take(50)  // Limitamos a las 50 más recientes para eficiencia
            .ToListAsync();
    }

    /// <summary>
    /// Cuenta las notificaciones NO leídas del negocio.
    /// Solo cuenta las no leídas (Leida == false) porque el badge del dashboard
    /// debe mostrar únicamente las alertas que el dueño aún no ha visto.
    /// Cuando el dueño marca una como leída, este contador baja.
    /// </summary>
    public async Task<int> GetNotificacionesCountAsync(Guid negocioId)
    {
        return await _context.Notificaciones
            .CountAsync(n => n.NegocioId == negocioId && !n.Leida);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MARCAR COMO LEÍDA
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Marca una notificación específica como leída (Leida = true).
    /// El filtro doble (Id + NegocioId) garantiza que un negocio no pueda
    /// marcar notificaciones de otro negocio como leídas (seguridad multi-tenant).
    /// </summary>
    public async Task<(bool success, string message)> MarcarLeidaAsync(Guid id, Guid negocioId)
    {
        var notificacion = await _context.Notificaciones
            .FirstOrDefaultAsync(n => n.Id == id && n.NegocioId == negocioId);

        if (notificacion == null)
            return (false, "Notificación no encontrada");

        notificacion.Leida = true;
        _context.Update(notificacion);
        await _context.SaveChangesAsync();

        return (true, "Notificación marcada como leída");
    }

    /// <summary>
    /// Marca TODAS las notificaciones no leídas del negocio como leídas.
    ///
    /// ESTRATEGIA DE ACTUALIZACIÓN:
    ///   Cargamos solo las notificaciones no leídas en memoria y las actualizamos
    ///   una por una en el loop. Para el volumen típico de notificaciones (decenas,
    ///   no miles), esto es más simple y legible que hacer un UPDATE masivo con SQL.
    ///
    ///   Si en el futuro el volumen creciera mucho, se podría optimizar con:
    ///   await _context.Notificaciones
    ///       .Where(n => n.NegocioId == negocioId && !n.Leida)
    ///       .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true));
    ///   (EF Core 7+ ExecuteUpdateAsync para UPDATE masivo sin cargar en memoria)
    /// </summary>
    public async Task<(bool success, string message)> MarcarTodasLeidasAsync(Guid negocioId)
    {
        // Cargar solo las no leídas (evitar actualizar registros que ya están leídos)
        var notificaciones = await _context.Notificaciones
            .Where(n => n.NegocioId == negocioId && !n.Leida)
            .ToListAsync();

        foreach (var n in notificaciones)
            n.Leida = true;

        // Un solo SaveChangesAsync para todas las actualizaciones (una transacción)
        await _context.SaveChangesAsync();

        return (true, $"{notificaciones.Count} notificaciones marcadas como leídas");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GENERACIÓN AUTOMÁTICA DE NOTIFICACIONES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Analiza las membresías del negocio y genera notificaciones automáticas
    /// para alertar al dueño sobre clientes en riesgo de perder acceso.
    ///
    /// LÓGICA DE VENTANA TEMPORAL:
    ///   Se buscan clientes cuya FechaQueTermina está entre hoy y hoy+3 días.
    ///   Ejemplo: si hoy es 18 de febrero:
    ///     - Se detectan membresías que vencen el 18, 19, 20 y 21 de febrero
    ///     - diasRestantes = 0 → "vence HOY"
    ///     - diasRestantes = 1 → "vence en 1 día"
    ///     - diasRestantes = 2 → "vence en 2 días"
    ///     - diasRestantes = 3 → "vence en 3 días"
    ///
    /// SISTEMA ANTI-DUPLICADOS:
    ///   Para cada cliente, se verifica si ya existe una notificación para ese
    ///   cliente con fecha de creación igual a HOY (comparando solo la parte Date).
    ///   Si ya existe → continue (skip)
    ///   Si no existe → crear nueva notificación
    ///
    ///   Esto permite que el método sea idempotente: llamarlo 10 veces en el mismo
    ///   día produce el mismo resultado que llamarlo 1 vez.
    ///
    /// OPTIMIZACIÓN PENDIENTE:
    ///   El loop actual hace N consultas a la base de datos (una por cliente) para
    ///   verificar duplicados con AnyAsync(). Para negocios con muchos clientes
    ///   en la ventana de vencimiento, podría optimizarse precargando todos los IDs
    ///   de notificaciones de hoy en un HashSet antes del loop.
    ///
    /// NEGOCIOS ARTESANALES:
    ///   Los negocios de tipo "artesanal" (peluquerías, barberías, spas) no tienen
    ///   el concepto de "membresía" con fecha de vencimiento — manejan citas individuales.
    ///   Por eso retornamos inmediatamente para no hacer consultas innecesarias.
    /// </summary>
    public async Task<(bool success, string message, int count)> GenerarNotificacionesAsync(Guid negocioId)
    {
        // Guard: los negocios artesanales no tienen membresías — no hay nada que revisar
        var negocio = await _context.Negocios.FindAsync(negocioId);
        if (negocio?.TipoNegocio == "artesanal")
            return (true, "Negocio artesanal — sin notificaciones de membresía", 0);

        // Definir la ventana de tiempo: desde hoy hasta 3 días en el futuro
        var hoy     = TimeHelper.Now.Date;
        var en3Dias = hoy.AddDays(3);

        // Buscar todos los clientes cuya membresía vence dentro de la ventana
        var clientesProximos = await _context.Clientes
            .Where(c => c.NegocioId == negocioId
                && c.FechaQueTermina.Date >= hoy     // Incluir los que vencen hoy
                && c.FechaQueTermina.Date <= en3Dias) // Hasta 3 días en el futuro
            .ToListAsync();

        // Pre-cargar ClienteIds ya notificados hoy (elimina N+1)
        var notificadosHoy = (await _context.Notificaciones
            .Where(n => n.NegocioId == negocioId && n.FechaCreacion.Date == hoy)
            .Select(n => n.ClienteId)
            .ToListAsync()).ToHashSet();

        int count = 0;
        foreach (var cliente in clientesProximos)
        {
            // Anti-duplicado: verificar si ya existe una notificación para este cliente HOY
            // Un cliente puede aparecer varios días seguidos (ej. 3 días, luego 2, luego 1)
            // pero solo generamos UNA notificación por día por cliente
            if (notificadosHoy.Contains(cliente.ClienteId)) continue;  // Ya fue notificado hoy → skip

            // Calcular cuántos días exactos faltan para el vencimiento
            var diasRestantes  = (cliente.FechaQueTermina.Date - hoy).Days;
            var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";

            // Construir el mensaje descriptivo según urgencia
            // diasRestantes == 0 significa que vence HOY (el mismo día de la consulta)
            var notificacion = new Notificacion
            {
                Id            = Guid.NewGuid(),
                NegocioId     = negocioId,
                Mensaje       = diasRestantes == 0
                    ? $"La membresía de {nombreCompleto} vence HOY"
                    : $"La membresía de {nombreCompleto} vence en {diasRestantes} día(s)",
                Tipo          = "vencimiento",  // Tipo para filtrado y estilos visuales en frontend
                ClienteId     = cliente.ClienteId,
                NombreCliente = nombreCompleto,   // Guardamos el nombre como snapshot (puede cambiar)
                Leida         = false,            // Nueva → no leída → aparece en el badge
                FechaCreacion = TimeHelper.Now
            };

            _context.Notificaciones.Add(notificacion);
            count++;
        }

        // Guardar todas las nuevas notificaciones en una sola transacción
        await _context.SaveChangesAsync();

        return (true, $"Se generaron {count} notificaciones", count);
    }
}
