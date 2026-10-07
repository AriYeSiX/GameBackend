using GameBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameBackend.Infrastructure.Persistence.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Username).HasMaxLength(32).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(p => p.Email).IsUnique();
        builder.HasIndex(p => p.Username).IsUnique();
    } 
}