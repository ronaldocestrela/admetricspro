using BuildingBlocks.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Mapeamento EF de <see cref="UserInitiatedJob"/> no schema <c>messaging</c>.
/// </summary>
public sealed class UserInitiatedJobEntityTypeConfiguration : IEntityTypeConfiguration<UserInitiatedJob>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserInitiatedJob> builder)
    {
        builder.ToTable("UserInitiatedJobs", "messaging");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.ParametersJson).IsRequired();
        builder.Property(j => j.ResultJson);
        builder.Property(j => j.ErrorCode).HasMaxLength(100);
        builder.Property(j => j.ErrorDescription).HasMaxLength(2000);
        builder.Property(j => j.EventId);
        builder.Property(j => j.RowVersion).IsRowVersion();
        builder.HasIndex(j => j.EventId).IsUnique();
        builder.HasIndex(j => new { j.WorkspaceId, j.CreatedAtUtc });
    }
}