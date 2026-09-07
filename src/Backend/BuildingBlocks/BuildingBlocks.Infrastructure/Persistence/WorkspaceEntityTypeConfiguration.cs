using BuildingBlocks.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core fluente para a entidade de domínio <see cref="Workspace"/>.
/// </summary>
public sealed class WorkspaceEntityTypeConfiguration : IEntityTypeConfiguration<Workspace>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");

        builder.HasKey(workspace => workspace.Id);

        builder.Property(workspace => workspace.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(workspace => workspace.CnpjOrCpf)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(workspace => workspace.CnpjOrCpf);

        builder.Property(workspace => workspace.MonthlyAdSpendBudget)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(workspace => workspace.Segment)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(workspace => workspace.IsActive)
            .IsRequired();

        builder.HasIndex(workspace => workspace.IsActive);

        builder.Property(workspace => workspace.CreatedAtUtc)
            .IsRequired();

        builder.Property(workspace => workspace.UpdatedAtUtc)
            .IsRequired(false);
    }
}
