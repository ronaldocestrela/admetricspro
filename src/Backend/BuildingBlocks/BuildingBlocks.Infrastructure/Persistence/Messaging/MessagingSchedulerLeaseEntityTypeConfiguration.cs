using BuildingBlocks.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Mapeamento EF do lease de scheduler no schema <c>messaging</c>.
/// </summary>
public sealed class MessagingSchedulerLeaseEntityTypeConfiguration : IEntityTypeConfiguration<MessagingSchedulerLease>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MessagingSchedulerLease> builder)
    {
        builder.ToTable("MessagingSchedulerLeases", "messaging");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.LeaseKey).HasMaxLength(100).IsRequired();
        builder.Property(l => l.HolderInstanceId).HasMaxLength(200).IsRequired();
        builder.HasIndex(l => l.LeaseKey).IsUnique();
    }
}