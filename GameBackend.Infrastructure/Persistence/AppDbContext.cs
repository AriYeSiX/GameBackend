using GameBackend.Application.Abstractions;
using GameBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SaveSlot> SaveSlots => Set<SaveSlot>();
    public DbSet<Leaderboard> Leaderboards => Set<Leaderboard>();
    public DbSet<ScoreEntry> ScoreEntries => Set<ScoreEntry>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}