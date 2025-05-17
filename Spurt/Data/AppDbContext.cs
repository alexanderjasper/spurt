using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Categories;
using Spurt.Domain.Games;
using Spurt.Domain.Players;
using Spurt.Domain.Users;

namespace Spurt.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players { get; set; } = null!;
    public DbSet<Game> Games { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Clue> Clues { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Player>()
            .Property(p => p.IsCreator)
            .HasDefaultValue(false);
        modelBuilder.Entity<Player>()
            .HasOne(p => p.User)
            .WithMany(u => u.Players)
            .HasForeignKey(p => p.UserId)
            .IsRequired();
        modelBuilder.Entity<Player>()
            .HasOne(p => p.Game)
            .WithMany(g => g.Players)
            .HasForeignKey(p => p.GameId)
            .IsRequired();
        modelBuilder.Entity<Player>()
            .HasOne(p => p.Category)
            .WithOne(c => c.Player)
            .HasForeignKey<Category>(c => c.PlayerId);

        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);
        modelBuilder.Entity<User>()
            .Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<Game>()
            .HasKey(g => g.Id);
        modelBuilder.Entity<Game>()
            .Property(g => g.Code)
            .IsRequired()
            .HasMaxLength(6);
        modelBuilder.Entity<Game>()
            .HasMany(g => g.Players)
            .WithOne(p => p.Game)
            .HasForeignKey(p => p.GameId);
        modelBuilder.Entity<Game>()
            .HasOne(g => g.SelectedClue)
            .WithMany()
            .HasForeignKey(g => g.SelectedClueId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);


        modelBuilder.Entity<Category>()
            .HasKey(c => c.Id);
        modelBuilder.Entity<Category>()
            .Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(100);
        modelBuilder.Entity<Category>()
            .HasMany(c => c.Clues)
            .WithOne(q => q.Category)
            .HasForeignKey(q => q.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Clue>()
            .HasKey(q => q.Id);
        modelBuilder.Entity<Clue>()
            .Property(q => q.Answer)
            .IsRequired();
        modelBuilder.Entity<Clue>()
            .Property(q => q.Question)
            .IsRequired();
        modelBuilder.Entity<Clue>()
            .Property(q => q.PointValue)
            .IsRequired();

        base.OnModelCreating(modelBuilder);
    }
}