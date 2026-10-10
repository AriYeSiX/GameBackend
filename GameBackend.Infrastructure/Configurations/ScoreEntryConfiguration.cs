using GameBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameBackend.Infrastructure.Persistence.Configurations;

public class ScoreEntryConfiguration : IEntityTypeConfiguration<ScoreEntry>
{
    public void Configure(EntityTypeBuilder<ScoreEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.LeaderboardId, e.PlayerId }).IsUnique();
        builder.HasIndex(e => new { e.LeaderboardId, e.Score }).IsDescending(false, true);

        builder.HasOne<Leaderboard>()
            .WithMany()
            .HasForeignKey(e => e.LeaderboardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(e => e.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}