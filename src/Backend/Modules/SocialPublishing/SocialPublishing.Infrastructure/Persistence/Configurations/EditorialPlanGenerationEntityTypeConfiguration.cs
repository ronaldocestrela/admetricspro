using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialPublishing.Domain.Planning;

namespace SocialPublishing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="EditorialPlanGeneration"/> no schema social.
/// </summary>
public sealed class EditorialPlanGenerationEntityTypeConfiguration : IEntityTypeConfiguration<EditorialPlanGeneration>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EditorialPlanGeneration> builder)
    {
        builder.ToTable("EditorialPlanGenerations");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.WorkspaceId).IsRequired();
        builder.Property(g => g.RangeStart).IsRequired();
        builder.Property(g => g.RangeEnd).IsRequired();
        builder.Property(g => g.Seed).IsRequired();
        builder.Property(g => g.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(g => g.CreatedAtUtc).IsRequired();

        builder.HasIndex(g => new { g.WorkspaceId, g.RangeStart, g.RangeEnd, g.Seed }).IsUnique();

        builder.Property<List<Guid>>("_createdPostIds")
            .HasField("_createdPostIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("CreatedPostIdsJson")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions) ?? new List<Guid>())
            .IsRequired();

        builder.Property<List<EditorialPlanFailedSlot>>("_failedSlots")
            .HasField("_failedSlots")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("FailedSlotsJson")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<EditorialPlanFailedSlot>>(v, JsonOptions) ?? new List<EditorialPlanFailedSlot>())
            .IsRequired();

        builder.Ignore(g => g.CreatedPostIds);
        builder.Ignore(g => g.FailedSlots);
    }
}
