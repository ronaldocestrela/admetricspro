using BuildingBlocks.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core fluente para a entidade associativa <see cref="SquadWorkspace"/>.
/// </summary>
public sealed class SquadWorkspaceEntityTypeConfiguration : IEntityTypeConfiguration<SquadWorkspace>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SquadWorkspace> builder)
    {
        builder.ToTable("SquadWorkspaces");

        builder.HasKey(workspace => workspace.Id);

        builder.Property(workspace => workspace.SquadId)
            .IsRequired();

        builder.Property(workspace => workspace.WorkspaceId)
            .IsRequired();

        builder.HasIndex(workspace => new { workspace.SquadId, workspace.WorkspaceId })
            .IsUnique();

        builder.HasIndex(workspace => workspace.WorkspaceId);

        builder.Property(workspace => workspace.AssignedAtUtc)
            .IsRequired();
    }
}
