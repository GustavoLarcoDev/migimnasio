using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;
namespace Gimnasio.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    // Here we add the DbSet later
    public DbSet<Cliente>  Clientes { get; set; }
    public DbSet<Gym> Gimnasios { get; set; }
}