using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialPublishing.Domain.Publications;

namespace SocialPublishing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="PublicationJob"/> no schema social.
/// </summary>
public sealed class PublicationJobEntityTypeConfiguration : IEntityTypeConfiguration<PublicationJob>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PublicationJob> builder)
    {
        builder.ToTable("PublicationJobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.PostId).IsRequired();
        builder.Property(j => j.WorkspaceId).IsRequired();
        builder.Property(j => j.ScheduledAtUtc).IsRequired();
        builder.Property(j => j.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(j => j.AttemptCount).IsRequired();
        builder.Property(j => j.CreatedAtUtc).IsRequired();
        builder.Property(j => j.RowVersion).IsRowVersion();

        builder.HasIndex(j => j.IdempotencyKey).IsUnique();
        builder.HasIndex(j => new { j.PostId, j.Status });
        builder.HasIndex(j => new { j.WorkspaceId, j.ScheduledAtUtc });
    }
}
