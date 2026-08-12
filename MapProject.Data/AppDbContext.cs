using Microsoft.EntityFrameworkCore;
using MapProject.Entities;

namespace MapProject.Data;

public class AppDbContext : DbContext
{

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    public DbSet<Location> Locations { get; set; } = null!;

}