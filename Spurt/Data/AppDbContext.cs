using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Games;
using Spurt.Domain.Players;

namespace Spurt.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players { get; set; } = null!;
    public DbSet<Game> Games { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Player>()
            .Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);
        modelBuilder.Entity<Player>()
            .Property(p => p.IsCreator)
            .HasDefaultValue(false);

        modelBuilder.Entity<Game>()
            .HasKey(g => g.Id);
        modelBuilder.Entity<Game>()
            .Property(g => g.Code)
            .IsRequired()
            .HasMaxLength(6);
        modelBuilder.Entity<Game>()
            .HasMany(g => g.Players)
            .WithMany();

        base.OnModelCreating(modelBuilder);
    }
}