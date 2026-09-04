using Master.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core para a entidade de perfil corporativo <see cref="MasterRole"/>.
/// </summary>
public sealed class MasterRoleEntityTypeConfiguration : IEntityTypeConfiguration<MasterRole>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MasterRole> builder)
    {
        builder.Property(r => r.Description)
            .HasMaxLength(500)
            .IsRequired();
    }
}
