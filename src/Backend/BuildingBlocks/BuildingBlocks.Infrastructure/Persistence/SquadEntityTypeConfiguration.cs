using BuildingBlocks.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core fluente para o agregado <see cref="Squad"/>.
/// </summary>
public sealed class SquadEntityTypeConfiguration : IEntityTypeConfiguration<Squad>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Squad> builder)
    {
        builder.ToTable("Squads");

        builder.HasKey(squad => squad.Id);

        builder.Property(squad => squad.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(squad => squad.Name);

        builder.Property(squad => squad.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(squad => squad.IsActive)
            .IsRequired();

        builder.HasIndex(squad => squad.IsActive);

        builder.Property(squad => squad.CreatedAtUtc)
            .IsRequired();

        builder.Property(squad => squad.UpdatedAtUtc)
            .IsRequired(false);

        builder.HasMany(squad => squad.Members)
            .WithOne()
            .HasForeignKey(member => member.SquadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(squad => squad.Workspaces)
            .WithOne()
            .HasForeignKey(workspace => workspace.SquadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(squad => squad.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(squad => squad.Workspaces)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
