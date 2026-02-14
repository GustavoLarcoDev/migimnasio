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
}
