using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="OrganicPost"/> e mídias filhas no schema social.
/// </summary>
public sealed class OrganicPostEntityTypeConfiguration : IEntityTypeConfiguration<OrganicPost>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrganicPost> builder)
    {
        builder.ToTable("OrganicPosts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.WorkspaceId).IsRequired();
        builder.Property(p => p.ConnectionId).IsRequired();
        builder.Property(p => p.Platform)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(p => p.Format)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(p => p.Pillar)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(p => p.Caption).HasMaxLength(2200).IsRequired();
        builder.Property(p => p.ScheduledAtUtc).IsRequired();
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.HasIndex(p => new { p.WorkspaceId, p.ScheduledAtUtc });
        builder.HasIndex(p => p.WorkspaceId);

        builder.Ignore(p => p.Media);

        builder.HasMany<OrganicMediaAsset>("_media")
            .WithOne()
            .HasForeignKey("OrganicPostId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_media")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
