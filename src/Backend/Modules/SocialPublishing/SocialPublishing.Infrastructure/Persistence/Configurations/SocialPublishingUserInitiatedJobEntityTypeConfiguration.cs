using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Infrastructure.Persistence.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SocialPublishing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeia jobs de usuário compartilhados sem gerar migração duplicada no módulo social.
/// </summary>
public sealed class SocialPublishingUserInitiatedJobEntityTypeConfiguration : IEntityTypeConfiguration<UserInitiatedJob>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserInitiatedJob> builder)
    {
        new UserInitiatedJobEntityTypeConfiguration().Configure(builder);
        builder.ToTable("UserInitiatedJobs", "messaging", table => table.ExcludeFromMigrations());
    }
}