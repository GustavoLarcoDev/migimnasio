// ═══════════════════════════════════════════════════════════
// SugerenciaService.cs — Servicio de sugerencias y feedback
//
// FUNCIÓN: Permite a los dueños de negocios enviar sugerencias,
// reportar errores o solicitar nuevas funcionalidades al administrador
// del sistema directamente desde el dashboard.
//
// FLUJO:
//   1. El dueño escribe un mensaje en el modal de sugerencias del dashboard.
//   2. CrearSugerenciaAsync guarda la sugerencia con Leida = false.
//   3. El admin ve las sugerencias en su panel con un badge de no leídas.
//   4. Al abrir cada sugerencia, el admin la marca como leída.
//
// LÍMITES:
//   - Mensaje vacío: no se permite.
//   - Máximo 1000 caracteres por sugerencia.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Implementación del servicio de sugerencias. Permite a los negocios
/// enviar mensajes de feedback al administrador del sistema, y al admin
/// consultar y marcar como leídas las sugerencias recibidas.
/// </summary>
public class SugerenciaService : ISugerenciaService
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor: recibe el DbContext por inyección de dependencias.
    /// </summary>
    public SugerenciaService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // CREAR SUGERENCIA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea una nueva sugerencia enviada por el dueño de un negocio.
    ///
    /// Validaciones:
    ///   - El mensaje no puede estar vacío.
    ///   - El mensaje no puede exceder 1000 caracteres (para evitar spam o abusos).
    ///
    /// La sugerencia se guarda con Leida = false para que el admin
    /// sepa que tiene mensajes nuevos pendientes de revisar.
    /// </summary>
    /// <param name="negocioId">ID del negocio que envía la sugerencia.</param>
    /// <param name="negocioNombre">Nombre del negocio (se guarda para que el admin sepa quién la envió).</param>
    /// <param name="mensaje">Texto de la sugerencia o feedback.</param>
    public async Task<(bool success, string message)> CrearSugerenciaAsync(Guid negocioId, string negocioNombre, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return (false, "El mensaje es obligatorio");

        if (mensaje.Length > 1000)
            return (false, "El mensaje no puede exceder 1000 caracteres");

        var sugerencia = new Sugerencia
        {
            Id = Guid.NewGuid(),
            NegocioId = negocioId,
            // Se guarda el nombre del negocio desnormalizado para que aparezca
            // en el panel del admin aunque después se cambie el nombre del negocio.
            NegocioNombre = negocioNombre,
            Mensaje = mensaje.Trim(), // Limpiar espacios al inicio y al final
            FechaCreacion = TimeHelper.Now
            // Leida se inicializa en false por defecto en el modelo
        };

        _context.Sugerencias.Add(sugerencia);
        await _context.SaveChangesAsync();

        return (true, "Sugerencia enviada exitosamente. Gracias por tu feedback.");
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS (SOLO PARA EL ADMIN)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todas las sugerencias de todos los negocios, ordenadas de
    /// más reciente a más antigua. Solo el admin puede ver esta lista.
    ///
    /// Retorna un objeto anónimo (sin exponer el modelo completo) con los
    /// campos necesarios para la tabla del panel de administración.
    /// </summary>
    public async Task<object> GetSugerenciasAsync()
    {
        return await _context.Sugerencias
            .OrderByDescending(s => s.FechaCreacion)
            .Select(s => new
            {
                s.Id,
                s.NegocioId,
                s.NegocioNombre,
                s.Mensaje,
                s.FechaCreacion,
                s.Leida
            })
            .ToListAsync();
    }

    /// <summary>
    /// Marca una sugerencia como leída. El admin llama a esto cuando
    /// abre o revisa el contenido de la sugerencia en el panel.
    ///
    /// Una vez marcada como leída, desaparece del contador de no leídas
    /// en el badge del sidebar del panel admin.
    /// </summary>
    /// <param name="id">ID de la sugerencia a marcar como leída.</param>
    public async Task<(bool success, string message)> MarcarLeidaAsync(Guid id)
    {
        var sugerencia = await _context.Sugerencias.FindAsync(id);
        if (sugerencia == null)
            return (false, "Sugerencia no encontrada");

        sugerencia.Leida = true;
        await _context.SaveChangesAsync();

        return (true, "Marcada como leída");
    }
}
