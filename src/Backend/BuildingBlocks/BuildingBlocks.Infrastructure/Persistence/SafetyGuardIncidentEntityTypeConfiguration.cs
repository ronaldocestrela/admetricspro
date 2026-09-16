using BuildingBlocks.Domain.Automations.SafetyGuards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core para a entidade <see cref="SafetyGuardIncident"/>.
/// Mapeia o histórico e auditoria de travas acionadas (Overspending &amp; BrokenLandingPage) no banco do inquilino.
/// </summary>
public sealed class SafetyGuardIncidentEntityTypeConfiguration : IEntityTypeConfiguration<SafetyGuardIncident>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SafetyGuardIncident> builder)
    {
        builder.ToTable("SafetyGuardIncidents");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.WorkspaceId)
            .IsRequired();

        builder.HasIndex(i => i.WorkspaceId);
        builder.HasIndex(i => new { i.WorkspaceId, i.DetectedAtUtc });

        builder.Property(i => i.GuardType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.Severity)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.TargetEntityName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(i => i.TargetEntityId)
            .IsRequired();

        builder.Property(i => i.Platform)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.ActionTaken)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(i => i.CurrentSpend)
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(i => i.DailyBudget)
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(i => i.HttpStatusCode)
            .IsRequired(false);

        builder.Property(i => i.TargetUrl)
            .HasMaxLength(2048)
            .IsRequired(false);

        builder.Property(i => i.DetectedAtUtc)
            .IsRequired();

        builder.Property(i => i.ResolvedAtUtc)
            .IsRequired(false);
    }
}
