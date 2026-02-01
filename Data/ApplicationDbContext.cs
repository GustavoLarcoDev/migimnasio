using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;
namespace Gimnasio.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Gym> Gimnasios { get; set; }
    public DbSet<Logs> Logs { get; set; }
}