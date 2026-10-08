using GameBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameBackend.Infrastructure.Persistence.Configurations;

public class SaveSlotConfiguration : IEntityTypeConfiguration<SaveSlot>
{
    public void Configure(EntityTypeBuilder<SaveSlot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.PlayerId, s.SlotNumber }).IsUnique();
        builder.Property(s => s.Data).HasColumnType("jsonb").IsRequired();
        builder.Property(s => s.Version).IsConcurrencyToken();

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}