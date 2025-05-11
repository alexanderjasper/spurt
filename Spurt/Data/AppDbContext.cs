using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Player;

namespace Spurt.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Player>()
            .Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        base.OnModelCreating(modelBuilder);
    }
} 