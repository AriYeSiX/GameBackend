using GameBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameBackend.Infrastructure.Persistence.Configurations;

public class LeaderboardConfiguration : IEntityTypeConfiguration<Leaderboard>
{
    public static readonly Guid ClassicId = Guid.Parse("0b6f2a3e-6c1f-4f4e-9a51-6d2a1f3c9e01");
    public static readonly Guid EndlessId = Guid.Parse("0b6f2a3e-6c1f-4f4e-9a51-6d2a1f3c9e02");

    public void Configure(EntityTypeBuilder<Leaderboard> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Key).HasMaxLength(64).IsRequired();
        builder.HasIndex(b => b.Key).IsUnique();
        builder.Property(b => b.Name).HasMaxLength(128).IsRequired();

        builder.HasData(
            new Leaderboard { Id = ClassicId, Key = "classic", Name = "Classic" },
            new Leaderboard { Id = EndlessId, Key = "endless", Name = "Endless" });
    }
}