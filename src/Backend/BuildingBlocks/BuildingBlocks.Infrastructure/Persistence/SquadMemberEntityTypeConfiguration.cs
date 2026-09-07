using BuildingBlocks.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core fluente para a entidade associativa <see cref="SquadMember"/>.
/// </summary>
public sealed class SquadMemberEntityTypeConfiguration : IEntityTypeConfiguration<SquadMember>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SquadMember> builder)
    {
        builder.ToTable("SquadMembers");

        builder.HasKey(member => member.Id);

        builder.Property(member => member.SquadId)
            .IsRequired();

        builder.Property(member => member.UserId)
            .IsRequired();

        builder.HasIndex(member => new { member.SquadId, member.UserId })
            .IsUnique();

        builder.HasIndex(member => member.UserId);

        builder.Property(member => member.JoinedAtUtc)
            .IsRequired();
    }
}
