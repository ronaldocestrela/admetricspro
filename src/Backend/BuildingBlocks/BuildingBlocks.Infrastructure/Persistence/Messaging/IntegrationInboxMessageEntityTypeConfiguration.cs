using BuildingBlocks.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Mapeamento EF da inbox de idempotência no schema <c>messaging</c>.
/// </summary>
public sealed class IntegrationInboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<IntegrationInboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IntegrationInboxMessage> builder)
    {
        builder.ToTable("IntegrationInboxMessages", "messaging");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ConsumerName).HasMaxLength(200).IsRequired();
        builder.HasIndex(m => new { m.EventId, m.ConsumerName }).IsUnique();
    }
}