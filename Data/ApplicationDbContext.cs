// ═══════════════════════════════════════════════════════════
// ApplicationDbContext.cs — Contexto de base de datos (EF Core)
//
// Este archivo es el puente entre el código C# y la base de
// datos SQL Server. Entity Framework Core usa este contexto
// para tres cosas fundamentales:
//
//   1. MAPEO: Cada propiedad DbSet<T> corresponde a una tabla
//             en la BD. EF Core traduce consultas LINQ a SQL.
//
//   2. CHANGE TRACKING: EF Core rastrea qué entidades fueron
//             modificadas y genera el UPDATE/INSERT/DELETE
//             correspondiente al llamar SaveChangesAsync().
//
//   3. CONFIGURACIÓN: OnModelCreating() permite definir índices,
//             constraints, relaciones y comportamientos de
//             borrado que no se pueden expresar solo con
//             atributos en los modelos.
//
// ApplicationDbContext se registra como "Scoped" en Program.cs,
// lo que significa que hay UNA instancia por request HTTP.
// Nunca compartas una instancia entre threads o requests.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Data;

/// <summary>
/// Contexto principal de Entity Framework Core para la aplicación My-Negocio.
/// Hereda de DbContext y expone todas las tablas de la base de datos SQL Server
/// como propiedades fuertemente tipadas (DbSet&lt;T&gt;).
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Constructor requerido por ASP.NET Core DI. Recibe las opciones de
    /// configuración (cadena de conexión, proveedor de BD, warnings, etc.)
    /// que fueron definidas en Program.cs al llamar AddDbContext().
    /// El parámetro se pasa al constructor base de DbContext.
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // ═══════════════════════════════════════════════════════════
    // TABLAS PRINCIPALES DEL SISTEMA
    //
    // Cada DbSet<T> representa una tabla. El nombre de la propiedad
    // (ej. "Clientes") se convierte en el nombre de la tabla en SQL
    // Server, salvo que se configure explícitamente con [Table("...")].
    //
    // Al hacer consultas LINQ sobre estas propiedades, EF Core
    // genera el SQL optimizado automáticamente. Ejemplo:
    //   var activos = await _db.Clientes
    //       .Where(c => c.Activo)
    //       .ToListAsync();
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Tabla de clientes de todos los negocios registrados en la plataforma.
    /// Cada cliente pertenece a UN negocio (relación N:1 con Negocios).
    /// Almacena datos personales, estado de membresía y fechas de vencimiento.
    /// </summary>
    public DbSet<Cliente> Clientes { get; set; }

    /// <summary>
    /// Tabla de negocios (clase C# llamada "Gym" por razones históricas,
    /// renombrada a "Negocios" en la BD). Cada fila es un negocio suscrito
    /// a la plataforma con sus credenciales, estado de pago y configuración.
    /// </summary>
    public DbSet<Gym> Negocios { get; set; }

    /// <summary>
    /// Tabla de logs financieros: registro inmutable de cada transacción
    /// (cobro de membresía, venta, etc.). Inmutable significa que nunca
    /// se editan ni eliminan filas, solo se insertan (audit trail contable).
    /// </summary>
    public DbSet<Logs> Logs { get; set; }

    /// <summary>
    /// Tabla de notificaciones automáticas generadas por el sistema cuando
    /// detecta membresías próximas a vencer. Sirven como bandeja de entrada
    /// para que el negocio tome acción (llamar al cliente, renovar, etc.).
    /// </summary>
    public DbSet<Notificacion> Notificaciones { get; set; }

    /// <summary>
    /// Tabla de sugerencias y feedback enviado por los negocios hacia
    /// la plataforma My-Negocio. Permite recopilar mejoras y reportar bugs.
    /// </summary>
    public DbSet<Sugerencia> Sugerencias { get; set; }

    /// <summary>
    /// Tabla de logs de acciones administrativas del súper-admin
    /// (ej. activar/desactivar un negocio, cambiar precios, etc.).
    /// Separado de Logs financieros por tener distinto propósito y audiencia.
    /// </summary>
    public DbSet<AdminLog> AdminLogs { get; set; }

    /// <summary>
    /// Tabla de productos en inventario de cada negocio.
    /// Almacena nombre, precio, stock actual y categoría.
    /// </summary>
    public DbSet<Producto> Productos { get; set; }

    /// <summary>
    /// Tabla de movimientos de inventario: cada fila registra un cambio
    /// de stock (entrada, salida, ajuste). También es inmutable (audit trail)
    /// para poder reconstruir el historial completo de stock en cualquier fecha.
    /// </summary>
    public DbSet<MovimientoInventario> MovimientosInventario { get; set; }

    /// <summary>
    /// Tabla de vendedores del sistema: personas que consiguen nuevos
    /// negocios para la plataforma y reciben comisión por ello.
    /// </summary>
    public DbSet<Vendedor> Vendedores { get; set; }

    /// <summary>
    /// Tabla de leads/prospectos capturados desde la landing page pública.
    /// Un lead es un negocio interesado que aún no se ha registrado.
    /// Los vendedores dan seguimiento a estos leads para convertirlos en clientes.
    /// </summary>
    public DbSet<LeadVendedor> LeadsVendedor { get; set; }

    /// <summary>
    /// Tabla de comisiones generadas para los vendedores.
    /// Cada registro representa una comisión ganada al crear un negocio
    /// que cumple los requisitos mínimos.
    /// El admin marca las comisiones como pagadas al transferir el dinero.
    /// </summary>
    public DbSet<ComisionVendedor> ComisionesVendedor { get; set; }

    public DbSet<Recibo> Recibos { get; set; }

    /// <summary>
    /// Categorías para agrupar productos en la interfaz "Tienda" (agrupación visual en pestañas).
    /// </summary>
    public DbSet<CategoriaProducto> CategoriasProducto { get; set; }

    /// <summary>
    /// Órdenes de Venta desde el Punto de Venta.
    /// Registra el proceso de venta con los totales y opcionalmente asocia un recibo.
    /// </summary>
    public DbSet<OrdenVenta> OrdenesVenta { get; set; }

    /// <summary>
    /// Lista de productos vendidos en cada Orden de Venta.
    /// </summary>
    public DbSet<DetalleOrdenVenta> DetallesOrdenVenta { get; set; }

    // ═══════════════════════════════════════════════════════════
    // TABLAS DEL MODELO RESTAURANTE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Mesas fisicas del restaurante con estado visual (libre/ocupada/reservada).
    /// </summary>
    public DbSet<Mesa> Mesas { get; set; }

    /// <summary>
    /// Reservas de mesas del restaurante con datos del cliente y horario.
    /// </summary>
    public DbSet<Reserva> Reservas { get; set; }

    /// <summary>
    /// Menus guardados por el restaurante (desayuno, almuerzo, cena, general)
    /// con HTML renderizado y JSON de items para edicion.
    /// </summary>
    public DbSet<MenuRestaurante> MenusRestaurante { get; set; }

    // ═══════════════════════════════════════════════════════════
    // TABLAS DEL MODELO ARTESANAL
    //
    // El "modelo artesanal" es la modalidad para negocios basados
    // en citas (peluquerías, spas, estudios de tatuaje, etc.).
    // Estas tablas forman un subsistema de agenda con empleados,
    // horarios, servicios, citas y pagos asociados.
    //
    // Flujo típico:
    //   Negocio define Servicios → registra Empleados →
    //   configura Horarios (regulares + excepciones) → cliente agenda una Cita →
    //   al completarse se registra un PagoCita
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Catálogo de servicios que ofrece el negocio (ej. "Corte de cabello",
    /// "Masaje 60 min"). Cada servicio tiene nombre, precio y duración en minutos.
    /// La duración se usa para calcular disponibilidad en el calendario.
    /// </summary>
    public DbSet<ServicioNegocio> ServiciosNegocio { get; set; }

    /// <summary>
    /// Empleados del negocio que pueden ser asignados a citas.
    /// Cada empleado tiene su propio horario de disponibilidad semanal.
    /// </summary>
    public DbSet<Empleado> Empleados { get; set; }

    /// <summary>
    /// Tabla unificada de horarios que combina horarios regulares semanales
    /// y excepciones de horario en una sola tabla con discriminador TipoHorario.
    /// TipoHorario="regular": horario semanal recurrente (un registro por día de semana).
    /// TipoHorario="excepcion": override puntual por fecha (vacaciones, horario especial).
    /// UNIQUE filtrado: (EmpleadoId, DiaSemana) para regulares, (EmpleadoId, Fecha) para excepciones.
    /// </summary>
    public DbSet<Horario> Horarios { get; set; }

    /// <summary>
    /// Tabla central de citas agendadas. Cada cita relaciona un cliente,
    /// un empleado, un servicio y un negocio con su rango horario.
    /// Tiene estados: Pendiente → Confirmada → Completada / Cancelada.
    /// </summary>
    public DbSet<Cita> Citas { get; set; }

    /// <summary>
    /// Registro del pago asociado a una cita completada.
    /// Relación 1:1 con Cita (una cita tiene como máximo un pago).
    /// Almacena monto, método de pago y fecha de cobro.
    /// </summary>
    public DbSet<PagoCita> PagosCita { get; set; }

    /// <summary>
    /// Métodos de pago configurados por cada negocio (transferencia, efectivo, QR, etc.).
    /// Cada negocio puede tener múltiples métodos de pago con sus datos bancarios e instrucciones.
    /// </summary>
    public DbSet<MetodoPago> MetodosPago { get; set; }

    /// <summary>
    /// Facturas electrónicas emitidas ante el SRI Ecuador.
    /// Cada factura tiene clave de acceso, XML firmado, estado de autorización y RIDE PDF.
    /// </summary>
    public DbSet<FacturaElectronica> FacturasElectronicas { get; set; }

    // ═══════════════════════════════════════════════════════════
    // CONFIGURACIÓN DEL MODELO (OnModelCreating)
    //
    // Este método se ejecuta UNA SOLA VEZ al arrancar la app,
    // cuando EF Core construye el modelo interno de la BD.
    //
    // Aquí configuramos cosas que NO se pueden expresar con
    // atributos en los modelos:
    //   - Índices compuestos (múltiples columnas)
    //   - Restricciones UNIQUE compuestas
    //   - Comportamiento de borrado en cascada (DeleteBehavior)
    //   - Relaciones 1:1 con foreign key explícita
    //
    // Nombrar los índices con convención IX_Tabla_Columnas
    // facilita identificarlos en el servidor de BD.
    // ═══════════════════════════════════════════════════════════

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Siempre llamar al base para que EF Core aplique sus
        // configuraciones internas antes de las nuestras
        base.OnModelCreating(modelBuilder);

        // ── Índices de la tabla Citas ──────────────────────────
        // La tabla Citas es la más consultada del modelo artesanal.
        // Los índices aceleran las consultas más frecuentes y
        // evitan full table scans en tablas con miles de filas.
        modelBuilder.Entity<Cita>(entity =>
        {
            // Índice para el calendario: "dame todas las citas de
            // este negocio en este rango de fechas". Es la consulta
            // más común al cargar la vista de agenda del negocio.
            entity.HasIndex(c => new { c.NegocioId, c.FechaHoraInicio })
                .HasDatabaseName("IX_Citas_NegocioId_FechaHoraInicio");

            // Índice para detección de doble-booking: "¿está este
            // empleado libre entre FechaHoraInicio y FechaHoraFin?"
            // Se consulta cada vez que se intenta crear una cita nueva.
            entity.HasIndex(c => new { c.EmpleadoId, c.FechaHoraInicio, c.FechaHoraFin })
                .HasDatabaseName("IX_Citas_EmpleadoId_Horario");

            // Índice para filtros de estado en el dashboard:
            // "dame todas las citas pendientes de este negocio"
            entity.HasIndex(c => new { c.NegocioId, c.Estado })
                .HasDatabaseName("IX_Citas_NegocioId_Estado");

            // Índice para el servicio de recordatorios automáticos
            // (AppointmentReminderService): "dame todas las citas
            // confirmadas sin recordatorio enviado que ocurren pronto"
            entity.HasIndex(c => new { c.RecordatorioEnviado, c.Estado, c.FechaHoraInicio })
                .HasDatabaseName("IX_Citas_Recordatorio");
        });

        // ── Configuración de PagoCita (relación 1:1 con Cita) ──
        // Una cita solo puede tener un pago. Lo garantizamos con
        // un índice UNIQUE en la foreign key CitaId. Intentar insertar
        // un segundo pago para la misma cita lanzará una excepción de BD.
        modelBuilder.Entity<PagoCita>(entity =>
        {
            // UNIQUE en CitaId: asegura la cardinalidad 1:1 a nivel de BD
            entity.HasIndex(p => p.CitaId)
                .IsUnique()
                .HasDatabaseName("IX_PagosCita_CitaId_Unique");

            // Configuración explícita de la relación 1:1 con su FK.
            // Cascade: si se borra una Cita, su PagoCita se borra también.
            // Tiene sentido porque un pago sin cita asociada es basura.
            entity.HasOne(p => p.Cita)
                .WithOne(c => c.Pago)
                .HasForeignKey<PagoCita>(p => p.CitaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Configuración de la tabla unificada Horarios ──
        // Reemplaza las antiguas tablas HorariosEmpleado y HorariosExcepcion.
        modelBuilder.Entity<Horario>(entity =>
        {
            // UNIQUE para horarios regulares: un empleado tiene máximo un horario por día de semana.
            // El filtro SQL asegura que solo aplique a registros de tipo "regular" con DiaSemana no null.
            entity.HasIndex(h => new { h.EmpleadoId, h.DiaSemana })
                .IsUnique()
                .HasFilter("[TipoHorario] = 'regular' AND [DiaSemana] IS NOT NULL")
                .HasDatabaseName("IX_Horarios_EmpleadoId_DiaSemana");

            // UNIQUE para excepciones: un empleado tiene máximo una excepción por fecha.
            // El filtro SQL asegura que solo aplique a registros de tipo "excepcion" con Fecha no null.
            entity.HasIndex(h => new { h.EmpleadoId, h.Fecha })
                .IsUnique()
                .HasFilter("[TipoHorario] = 'excepcion' AND [Fecha] IS NOT NULL")
                .HasDatabaseName("IX_Horarios_EmpleadoId_Fecha");
        });

        // ── Configuración de relaciones de Cita (evitar multiple cascade) ──
        // EF Core no permite que múltiples foreign keys en la misma tabla
        // usen DeleteBehavior.Cascade si apuntan a la misma tabla raíz,
        // porque SQL Server no puede garantizar el orden de borrado.
        //
        // Solución: usamos NoAction en las FK hacia Cliente, Empleado y
        // Servicio, y Cascade SOLO hacia Negocio (la relación principal).
        // Esto significa:
        //   - Borrar un Negocio → borra todas sus Citas (y sus PagosCita)
        //   - Borrar un Cliente → NO borra sus citas automáticamente
        //     (hay que borrarlas manualmente o dejar la FK en null)
        //   - Borrar un Empleado → idem
        //   - Borrar un Servicio → idem
        modelBuilder.Entity<Cita>(entity =>
        {
            // NoAction: el borrado de un Cliente no afecta sus Citas.
            // La aplicación debe manejar esto explícitamente en código
            // (ej. verificar si tiene citas antes de permitir el borrado).
            entity.HasOne(c => c.Cliente)
                .WithMany(cl => cl.Citas)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.NoAction);

            // NoAction: mismo razonamiento para Empleado
            entity.HasOne(c => c.Empleado)
                .WithMany(e => e.Citas)
                .HasForeignKey(c => c.EmpleadoId)
                .OnDelete(DeleteBehavior.NoAction);

            // NoAction: mismo razonamiento para Servicio
            entity.HasOne(c => c.Servicio)
                .WithMany(s => s.Citas)
                .HasForeignKey(c => c.ServicioId)
                .OnDelete(DeleteBehavior.NoAction);

            // Cascade: borrar un Negocio elimina TODAS sus citas.
            // Es el comportamiento esperado al dar de baja un negocio
            // de la plataforma (limpieza total de sus datos).
            entity.HasOne(c => c.Negocio)
                .WithMany(n => n.Citas)
                .HasForeignKey(c => c.NegocioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ═══════════════════════════════════════════════════════════
        // C4: Indexes for multi-tenant query performance
        // ═══════════════════════════════════════════════════════════
        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.NegocioId)
            .HasDatabaseName("IX_Clientes_NegocioId");

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => new { c.NegocioId, c.FechaQueTermina })
            .HasDatabaseName("IX_Clientes_NegocioId_FechaQueTermina");

        modelBuilder.Entity<Logs>()
            .HasIndex(l => l.NegocioId)
            .HasDatabaseName("IX_Logs_NegocioId");

        modelBuilder.Entity<Logs>()
            .HasIndex(l => new { l.NegocioId, l.Fecha })
            .HasDatabaseName("IX_Logs_NegocioId_Fecha");

        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.NegocioId)
            .HasDatabaseName("IX_Productos_NegocioId");

        modelBuilder.Entity<Notificacion>()
            .HasIndex(n => n.NegocioId)
            .HasDatabaseName("IX_Notificaciones_NegocioId");

        modelBuilder.Entity<Notificacion>()
            .HasIndex(n => new { n.NegocioId, n.ClienteId, n.FechaCreacion })
            .HasDatabaseName("IX_Notificaciones_NegocioId_ClienteId_Fecha");

        modelBuilder.Entity<Sugerencia>()
            .HasIndex(s => s.NegocioId)
            .HasDatabaseName("IX_Sugerencias_NegocioId");

        modelBuilder.Entity<MovimientoInventario>()
            .HasIndex(m => m.NegocioId)
            .HasDatabaseName("IX_MovimientosInventario_NegocioId");

        modelBuilder.Entity<ServicioNegocio>()
            .HasIndex(s => s.NegocioId)
            .HasDatabaseName("IX_ServiciosNegocio_NegocioId");

        modelBuilder.Entity<Empleado>()
            .HasIndex(e => e.NegocioId)
            .HasDatabaseName("IX_Empleados_NegocioId");

        modelBuilder.Entity<Gym>()
            .HasIndex(g => g.Email)
            .IsUnique()
            .HasDatabaseName("IX_Negocios_Email_Unique");

        modelBuilder.Entity<Recibo>(entity =>
        {
            entity.HasIndex(r => new { r.NegocioId, r.NumeroRecibo })
                .HasDatabaseName("IX_Recibos_NegocioId_NumeroRecibo");

            entity.HasIndex(r => new { r.NegocioId, r.FechaCreacion })
                .HasDatabaseName("IX_Recibos_NegocioId_FechaCreacion");
        });

        // ── Configuración Tienda y OrdenVenta ──
        modelBuilder.Entity<CategoriaProducto>(entity =>
        {
            entity.HasOne(c => c.Negocio)
                .WithMany()
                .HasForeignKey(c => c.NegocioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrdenVenta>(entity =>
        {
            entity.HasOne(o => o.Negocio)
                .WithMany()
                .HasForeignKey(o => o.NegocioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(o => o.Recibo)
                .WithMany()
                .HasForeignKey(o => o.ReciboId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DetalleOrdenVenta>(entity =>
        {
            entity.HasOne(d => d.OrdenVenta)
                .WithMany(o => o.Detalles)
                .HasForeignKey(d => d.OrdenVentaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Si borramos un producto de inventario (IsActive = false - Soft Delete), 
            // el detalle de la orden igual no debe romperse a nivel BD si llegara a borrarse (DeleteBehavior.NoAction).
            entity.HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── Configuración Restaurante (Mesas y Menús) ──
        modelBuilder.Entity<Mesa>(entity =>
        {
            entity.HasIndex(m => m.NegocioId)
                .HasDatabaseName("IX_Mesas_NegocioId");

            entity.HasOne(m => m.Negocio)
                .WithMany()
                .HasForeignKey(m => m.NegocioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasIndex(r => r.NegocioId)
                .HasDatabaseName("IX_Reservas_NegocioId");

            entity.HasIndex(r => new { r.NegocioId, r.FechaHoraReserva })
                .HasDatabaseName("IX_Reservas_NegocioId_FechaHora");

            entity.HasOne(r => r.Negocio)
                .WithMany()
                .HasForeignKey(r => r.NegocioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Mesa)
                .WithMany()
                .HasForeignKey(r => r.MesaId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<MenuRestaurante>(entity =>
        {
            entity.HasIndex(m => m.NegocioId)
                .HasDatabaseName("IX_MenusRestaurante_NegocioId");

            entity.HasOne(m => m.Negocio)
                .WithMany()
                .HasForeignKey(m => m.NegocioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrdenVenta → Mesa (nullable FK, NoAction para evitar multiple cascade)
        modelBuilder.Entity<OrdenVenta>(entity =>
        {
            entity.HasOne(o => o.Mesa)
                .WithMany()
                .HasForeignKey(o => o.MesaId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(o => o.Repartidor)
                .WithMany()
                .HasForeignKey(o => o.EmpleadoId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── Configuración MetodoPago ──
        modelBuilder.Entity<MetodoPago>(entity =>
        {
            entity.HasIndex(e => e.NegocioId)
                .HasDatabaseName("IX_MetodosPago_NegocioId");

            entity.HasIndex(e => new { e.NegocioId, e.IsActive })
                .HasDatabaseName("IX_MetodosPago_NegocioId_IsActive");

            entity.HasOne(e => e.Negocio)
                .WithMany(g => g.MetodosPago)
                .HasForeignKey(e => e.NegocioId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Configuración FacturaElectronica ──
        modelBuilder.Entity<FacturaElectronica>(entity =>
        {
            // Índice único: clave de acceso por negocio
            entity.HasIndex(f => new { f.NegocioId, f.ClaveAcceso })
                .IsUnique()
                .HasDatabaseName("IX_FacturasElectronicas_NegocioId_ClaveAcceso");

            // Índice único: numeración secuencial por negocio + establecimiento + punto emisión
            entity.HasIndex(f => new { f.NegocioId, f.Establecimiento, f.PuntoEmision, f.Secuencial })
                .IsUnique()
                .HasDatabaseName("IX_FacturasElectronicas_Numeracion");

            // Índice para consultas por estado
            entity.HasIndex(f => new { f.NegocioId, f.EstadoSri })
                .HasDatabaseName("IX_FacturasElectronicas_NegocioId_EstadoSri");

            // Índice para consultas por fecha de emisión
            entity.HasIndex(f => new { f.NegocioId, f.FechaEmision })
                .HasDatabaseName("IX_FacturasElectronicas_NegocioId_FechaEmision");

            // FK → Negocio (Cascade: borrar negocio borra sus facturas)
            entity.HasOne(f => f.Negocio)
                .WithMany()
                .HasForeignKey(f => f.NegocioId)
                .OnDelete(DeleteBehavior.Cascade);

            // FK → Recibo (SetNull: borrar recibo no borra la factura)
            entity.HasOne(f => f.Recibo)
                .WithMany()
                .HasForeignKey(f => f.ReciboId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ═══════════════════════════════════════════════════════════
        // H8: Decimal precision for money fields
        // ═══════════════════════════════════════════════════════════
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            }
        }
    }
}
