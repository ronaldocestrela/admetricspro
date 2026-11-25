using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Infrastructure.Persistence.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SocialPublishing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeia a outbox compartilhada do schema <c>messaging</c> sem gerar migrações duplicadas no módulo social.
/// </summary>
public sealed class SocialPublishingIntegrationOutboxEntityTypeConfiguration : IEntityTypeConfiguration<IntegrationOutboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IntegrationOutboxMessage> builder)
    {
        new IntegrationOutboxMessageEntityTypeConfiguration().Configure(builder);
        builder.ToTable("IntegrationOutboxMessages", "messaging", table => table.ExcludeFromMigrations());
    }
}