using BuildingBlocks.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Mapeamento EF da outbox transacional no schema <c>messaging</c>.
/// </summary>
public sealed class IntegrationOutboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<IntegrationOutboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IntegrationOutboxMessage> builder)
    {
        builder.ToTable("IntegrationOutboxMessages", "messaging");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.MessageType).HasMaxLength(200).IsRequired();
        builder.Property(m => m.PayloadJson).IsRequired();
        builder.Property(m => m.LastErrorCode).HasMaxLength(100);
        builder.Property(m => m.RowVersion).IsRowVersion();
        builder.HasIndex(m => m.EventId).IsUnique();
        builder.HasIndex(m => new { m.Status, m.NextAttemptAtUtc });
    }
}