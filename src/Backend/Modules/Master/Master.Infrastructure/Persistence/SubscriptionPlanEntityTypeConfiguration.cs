using Master.Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// EF Core entity mapping configuration for <see cref="SubscriptionPlan"/> in the Master database.
/// </summary>
public sealed class SubscriptionPlanEntityTypeConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasKey(plan => plan.Id);

        builder
            .Property(plan => plan.Id)
            .HasConversion(id => id.Value, value => new PlanId(value));

        builder
            .Property(plan => plan.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder
            .HasIndex(plan => plan.Name)
            .IsUnique();

        builder
            .Property(plan => plan.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder
            .Property(plan => plan.Tier)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder
            .Property(plan => plan.MonthlyPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder
            .Property(plan => plan.AnnualDiscountPercentage)
            .IsRequired();

        builder.OwnsOne(plan => plan.Limits, limits =>
        {
            limits.Property(l => l.MaxSeats)
                .HasColumnName("MaxSeats")
                .IsRequired();

            limits.Property(l => l.MaxWorkspaces)
                .HasColumnName("MaxWorkspaces")
                .IsRequired();

            limits.Property(l => l.MonthlyAdSpendCap)
                .HasColumnName("MonthlyAdSpendCap")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        builder.OwnsOne(plan => plan.Features, features =>
        {
            features.Property(f => f.HasWhiteLabel)
                .HasColumnName("HasWhiteLabel")
                .IsRequired();

            features.Property(f => f.HasCustomCname)
                .HasColumnName("HasCustomCname")
                .IsRequired();

            features.Property(f => f.HasAiCopilot)
                .HasColumnName("HasAiCopilot")
                .IsRequired();

            features.Property(f => f.HasCrossNetworkAutomations)
                .HasColumnName("HasCrossNetworkAutomations")
                .IsRequired();
        });

        builder
            .Property(plan => plan.IsActive)
            .IsRequired();

        builder
            .Property(plan => plan.CreatedAtUtc)
            .IsRequired();

        builder
            .Property(plan => plan.UpdatedAtUtc)
            .IsRequired(false);
    }
}
