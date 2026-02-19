// ═══════════════════════════════════════════════════════════
// ServicioNegocioService.cs — Implementación del servicio de gestión del catálogo
//
// RESPONSABILIDAD DE ESTA CLASE:
//   Gestionar el catálogo de servicios que ofrece el negocio (peluquería, spa, etc.).
//   Un "servicio" aquí es una oferta comercial: tiene nombre, precio y duración.
//
// RELACIÓN CON LAS CITAS:
//   Al crear una cita, se selecciona un servicio de este catálogo.
//   El sistema copia el nombre, precio y duración del servicio a la cita
//   (desnormalización). Esto significa que si después se cambia el precio de un
//   servicio, las citas pasadas siguen mostrando el precio original que se cobró.
//
// DESNORMALIZACIÓN (por qué guardamos datos duplicados):
//   La cita almacena: NombreServicio, PrecioServicio, DuracionMinutos
//   (copiados del ServicioNegocio en el momento de la creación de la cita)
//   Razón: el catálogo de servicios puede cambiar (precios, nombres, se eliminan),
//   pero el historial de citas debe ser inmutable y consistente con lo que ocurrió.
//
// COMBOS:
//   Un servicio puede ser un "combo" (EsCombo=true): un paquete que agrupa
//   varios servicios. Los ítems del combo se guardan como texto en ItemsIncluidos
//   (ej: "Corte de cabello, Lavado, Peinado"). No hay una tabla separada para
//   los ítems del combo porque es información puramente descriptiva.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Implementación concreta del servicio de gestión del catálogo de servicios del negocio.
/// Maneja el CRUD de servicios con validación de precios, duración y eliminación lógica.
/// </summary>
public class ServicioNegocioService : IServicioNegocioService
{
    // _context: acceso a la base de datos a través de Entity Framework Core.
    // Nos permite hacer consultas LINQ que EF Core traduce automáticamente a SQL.
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// ASP.NET Core inyecta el ApplicationDbContext automáticamente (configurado en Program.cs).
    /// </summary>
    public ServicioNegocioService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los servicios activos del negocio ordenados por nombre.
    /// Solo devuelve servicios con IsActive=true; los dados de baja son invisibles para el usuario.
    /// Proyectamos un objeto anónimo con solo los campos necesarios para reducir el tamaño del JSON.
    /// </summary>
    public async Task<object> GetServiciosAsync(Guid negocioId)
    {
        return await _context.ServiciosNegocio
            .Where(s => s.NegocioId == negocioId && s.IsActive)
            .OrderBy(s => s.Nombre)
            .Select(s => new
            {
                s.ServicioId,
                s.Nombre,
                s.Descripcion,       // Descripción opcional del servicio para el cliente
                s.ItemsIncluidos,    // Lista de ítems para combos (texto libre)
                s.DuracionMinutos,   // Duración estimada en minutos (bloquea este tiempo en el calendario)
                s.Precio,            // Precio base del servicio
                s.EsCombo,           // true si es un paquete de servicios
                s.FechaCreacion
            })
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un servicio específico por su ID dentro de un negocio.
    /// Devuelve la entidad <see cref="ServicioNegocio"/> completa porque el controlador
    /// necesita todos los campos para prellenar el formulario de edición.
    /// Siempre filtra por negocioId para garantizar el aislamiento multi-tenant.
    /// </summary>
    public async Task<ServicioNegocio> GetServicioAsync(Guid servicioId, Guid negocioId)
    {
        return await _context.ServiciosNegocio
            .FirstOrDefaultAsync(s => s.ServicioId == servicioId && s.NegocioId == negocioId && s.IsActive);
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD DE SERVICIOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo servicio en el catálogo del negocio.
    ///
    /// VALIDACIONES (el orden importa: validamos antes de consultar la DB):
    ///   1. Nombre no vacío: es el identificador principal que ven los usuarios.
    ///   2. Precio > 0: un precio de 0 o negativo no tiene sentido comercial.
    ///      Las cortesías se manejan con la opción "regalo" al cobrar la cita.
    ///   3. Duración > 0: si fuera 0, la cita empezaría y terminaría al mismo tiempo,
    ///      lo que haría imposible detectar conflictos de horario correctamente.
    ///
    /// CAMPOS DE AUDITORÍA:
    ///   FechaCreacion y FechaDeActualizacion se inicializan con TimeHelper.Now
    ///   para tener un registro de cuándo se creó el servicio.
    /// </summary>
    public async Task<(bool success, string message)> CrearServicioAsync(ServicioCreateDto dto)
    {
        // Validación del nombre: es el campo principal y obligatorio
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return (false, "El nombre del servicio es obligatorio");

        // El precio debe ser positivo. Los servicios "gratis" se gestionan
        // a nivel de pago (opción regalo), no a nivel de catálogo.
        if (dto.Precio <= 0)
            return (false, "El precio debe ser mayor a 0");

        // La duración determina cuánto tiempo bloquea el calendario del empleado.
        // Una duración de 0 causaría bugs en la detección de conflictos de horario.
        if (dto.DuracionMinutos <= 0)
            return (false, "La duración debe ser mayor a 0 minutos");

        var servicio = new ServicioNegocio
        {
            ServicioId = Guid.NewGuid(),
            NegocioId = dto.NegocioId,
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,            // Puede ser null (campo opcional)
            ItemsIncluidos = dto.ItemsIncluidos,      // Puede ser null (solo relevante para combos)
            DuracionMinutos = dto.DuracionMinutos,
            Precio = dto.Precio,
            EsCombo = dto.EsCombo,                    // Marca si es un paquete de servicios
            IsActive = true,                          // Activo desde su creación
            FechaCreacion = TimeHelper.Now,
            FechaDeActualizacion = TimeHelper.Now
        };

        _context.ServiciosNegocio.Add(servicio);
        await _context.SaveChangesAsync();

        return (true, "Servicio creado exitosamente");
    }

    /// <summary>
    /// Edita un servicio existente en el catálogo.
    /// Aplica las mismas validaciones que <see cref="CrearServicioAsync"/>.
    ///
    /// EFECTO EN CITAS EXISTENTES:
    ///   Ninguno. Las citas ya creadas tienen copias desnormalizadas del nombre,
    ///   precio y duración, así que los cambios aquí solo afectan citas futuras.
    ///   Esto es un comportamiento intencional: no queremos alterar registros pasados.
    ///
    /// CAMPOS ACTUALIZABLES:
    ///   Nombre, Descripcion, ItemsIncluidos, DuracionMinutos, Precio, EsCombo.
    ///   No se actualiza ServicioId, NegocioId, IsActive ni FechaCreacion.
    /// </summary>
    public async Task<(bool success, string message)> EditarServicioAsync(ServicioCreateDto dto)
    {
        // Buscamos el servicio verificando que pertenezca al negocio y esté activo.
        // No se puede editar un servicio que ya fue dado de baja.
        var servicio = await _context.ServiciosNegocio
            .FirstOrDefaultAsync(s => s.ServicioId == dto.ServicioId && s.NegocioId == dto.NegocioId && s.IsActive);

        if (servicio == null)
            return (false, "Servicio no encontrado");

        // Las mismas validaciones que en la creación se aplican también en la edición.
        // Esto garantiza que el catálogo siempre tenga datos consistentes.
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return (false, "El nombre del servicio es obligatorio");
        if (dto.Precio <= 0)
            return (false, "El precio debe ser mayor a 0");
        if (dto.DuracionMinutos <= 0)
            return (false, "La duración debe ser mayor a 0 minutos");

        // Actualizar los campos del servicio.
        // EF Core detecta el cambio en la entidad tracked y genera el SQL UPDATE automáticamente.
        servicio.Nombre = dto.Nombre;
        servicio.Descripcion = dto.Descripcion;
        servicio.ItemsIncluidos = dto.ItemsIncluidos;
        servicio.DuracionMinutos = dto.DuracionMinutos;
        servicio.Precio = dto.Precio;
        servicio.EsCombo = dto.EsCombo;
        // Actualizamos la fecha de modificación para tener auditoría de cuándo se editó
        servicio.FechaDeActualizacion = TimeHelper.Now;

        await _context.SaveChangesAsync();

        return (true, "Servicio actualizado exitosamente");
    }

    /// <summary>
    /// Da de baja un servicio del catálogo de forma lógica (soft delete: IsActive = false).
    ///
    /// POR QUÉ SOFT DELETE Y NO BORRADO FÍSICO:
    ///   Si borráramos el registro, las citas pasadas que referencian este ServicioId
    ///   tendrían una clave foránea inválida. Con el soft delete, el registro queda
    ///   en la base de datos y las citas históricas siguen siendo válidas.
    ///   Además, gracias a la desnormalización (NombreServicio/PrecioServicio en la cita),
    ///   el historial se puede consultar sin necesidad de hacer JOIN con ServiciosNegocio.
    ///
    /// DIFERENCIA CON EMPLEADO:
    ///   Al dar de baja un empleado, verificamos si tiene citas pendientes (porque
    ///   esas citas quedarían sin atención). Para servicios NO hacemos esa verificación
    ///   porque el servicio ya no es "actor" de la cita una vez creada: solo fue una
    ///   referencia para copiar precio y duración al momento de la reserva.
    /// </summary>
    public async Task<(bool success, string message)> EliminarServicioAsync(Guid servicioId, Guid negocioId)
    {
        // Buscamos el servicio activo. No se puede eliminar algo que ya está inactivo.
        var servicio = await _context.ServiciosNegocio
            .FirstOrDefaultAsync(s => s.ServicioId == servicioId && s.NegocioId == negocioId && s.IsActive);

        if (servicio == null)
            return (false, "Servicio no encontrado");

        // Soft delete: marcar como inactivo en vez de borrar el registro.
        // El servicio desaparecerá del catálogo activo pero seguirá en la base de datos.
        servicio.IsActive = false;
        // Actualizamos la fecha para saber cuándo fue dado de baja
        servicio.FechaDeActualizacion = TimeHelper.Now;

        await _context.SaveChangesAsync();

        return (true, "Servicio eliminado exitosamente");
    }
}
