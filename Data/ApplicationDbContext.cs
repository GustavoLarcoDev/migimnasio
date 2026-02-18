// ═══════════════════════════════════════════════════════════
// ApplicationDbContext.cs — Contexto de base de datos (EF Core)
// Define las tablas de la base de datos SQL Server.
// Cada DbSet corresponde a una tabla en la BD.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // ═══════════════════════════════════════════════════════════
    // TABLAS DE LA BASE DE DATOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>Tabla de clientes de todos los negocios</summary>
    public DbSet<Cliente> Clientes { get; set; }

    /// <summary>Tabla de negocios registrados en la plataforma</summary>
    public DbSet<Gym> Negocios { get; set; }

    /// <summary>Tabla de logs/registros financieros (fuente inmutable)</summary>
    public DbSet<Logs> Logs { get; set; }

    /// <summary>Tabla de notificaciones automáticas de vencimiento</summary>
    public DbSet<Notificacion> Notificaciones { get; set; }

    /// <summary>Tabla de sugerencias/feedback de los negocios</summary>
    public DbSet<Sugerencia> Sugerencias { get; set; }

    /// <summary>Tabla de logs de acciones administrativas</summary>
    public DbSet<AdminLog> AdminLogs { get; set; }

    /// <summary>Tabla de productos en inventario</summary>
    public DbSet<Producto> Productos { get; set; }

    /// <summary>Tabla de movimientos de inventario (audit trail inmutable)</summary>
    public DbSet<MovimientoInventario> MovimientosInventario { get; set; }

    /// <summary>Tabla de vendedores del sistema</summary>
    public DbSet<Vendedor> Vendedores { get; set; }

    /// <summary>Tabla de leads/interesados desde la landing page</summary>
    public DbSet<LeadVendedor> LeadsVendedor { get; set; }

    // ═══════════════════════════════════════════════════════════
    // TABLAS MODELO ARTESANAL (citas, servicios, empleados)
    // ═══════════════════════════════════════════════════════════

    public DbSet<ServicioNegocio> ServiciosNegocio { get; set; }
    public DbSet<Empleado> Empleados { get; set; }
    public DbSet<HorarioEmpleado> HorariosEmpleado { get; set; }
    public DbSet<HorarioExcepcion> HorariosExcepcion { get; set; }
    public DbSet<Cita> Citas { get; set; }
    public DbSet<PagoCita> PagosCita { get; set; }

    // ═══════════════════════════════════════════════════════════
    // CONFIGURACIÓN DE MODELO (índices y constraints)
    // ═══════════════════════════════════════════════════════════

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Citas — índices para calendario, doble-booking, filtros y recordatorios
        modelBuilder.Entity<Cita>(entity =>
        {
            entity.HasIndex(c => new { c.NegocioId, c.FechaHoraInicio })
                .HasDatabaseName("IX_Citas_NegocioId_FechaHoraInicio");

            entity.HasIndex(c => new { c.EmpleadoId, c.FechaHoraInicio, c.FechaHoraFin })
                .HasDatabaseName("IX_Citas_EmpleadoId_Horario");

            entity.HasIndex(c => new { c.NegocioId, c.Estado })
                .HasDatabaseName("IX_Citas_NegocioId_Estado");

            entity.HasIndex(c => new { c.RecordatorioEnviado, c.Estado, c.FechaHoraInicio })
                .HasDatabaseName("IX_Citas_Recordatorio");
        });

        // PagoCita — 1 pago por cita (índice único)
        modelBuilder.Entity<PagoCita>(entity =>
        {
            entity.HasIndex(p => p.CitaId)
                .IsUnique()
                .HasDatabaseName("IX_PagosCita_CitaId_Unique");

            entity.HasOne(p => p.Cita)
                .WithOne(c => c.Pago)
                .HasForeignKey<PagoCita>(p => p.CitaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // HorarioEmpleado — UNIQUE por empleado/dia
        modelBuilder.Entity<HorarioEmpleado>(entity =>
        {
            entity.HasIndex(h => new { h.EmpleadoId, h.DiaSemana })
                .IsUnique()
                .HasDatabaseName("IX_HorariosEmpleado_EmpleadoId_Dia");
        });

        // HorarioExcepcion — UNIQUE por empleado/fecha
        modelBuilder.Entity<HorarioExcepcion>(entity =>
        {
            entity.HasIndex(h => new { h.EmpleadoId, h.Fecha })
                .IsUnique()
                .HasDatabaseName("IX_HorariosExcepcion_EmpleadoId_Fecha");
        });

        // Evitar cascade delete múltiple en Cita
        modelBuilder.Entity<Cita>(entity =>
        {
            entity.HasOne(c => c.Cliente)
                .WithMany(cl => cl.Citas)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(c => c.Empleado)
                .WithMany(e => e.Citas)
                .HasForeignKey(c => c.EmpleadoId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(c => c.Servicio)
                .WithMany(s => s.Citas)
                .HasForeignKey(c => c.ServicioId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(c => c.Negocio)
                .WithMany(n => n.Citas)
                .HasForeignKey(c => c.NegocioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
