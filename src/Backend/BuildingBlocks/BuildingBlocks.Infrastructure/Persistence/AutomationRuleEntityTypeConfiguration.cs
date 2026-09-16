using System.Text.Json;
using BuildingBlocks.Domain.Automations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core para a entidade <see cref="AutomationRule"/>.
/// Armazena a árvore de predicados e as ações em colunas JSON no banco do inquilino.
/// </summary>
public sealed class AutomationRuleEntityTypeConfiguration : IEntityTypeConfiguration<AutomationRule>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AutomationRule> builder)
    {
        builder.ToTable("AutomationRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.WorkspaceId)
            .IsRequired();

        builder.HasIndex(r => r.WorkspaceId);

        builder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(r => r.IsEnabled)
            .IsRequired();

        builder.Property(r => r.ExecutionCount)
            .IsRequired();

        builder.Property(r => r.LastTriggeredAtUtc)
            .IsRequired(false);

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .IsRequired(false);

        builder.Property(r => r.ConditionTree)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<RuleConditionGroup>(v, JsonOptions) ?? new RuleConditionGroup())
            .IsRequired();

        builder.Property(r => r.Actions)
            .HasField("_actions")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<RuleAction>>(v, JsonOptions) ?? new List<RuleAction>())
            .IsRequired();
    }
}
