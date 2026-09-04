using Master.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core para a entidade de usuário corporativo <see cref="MasterUser"/>.
/// </summary>
public sealed class MasterUserEntityTypeConfiguration : IEntityTypeConfiguration<MasterUser>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MasterUser> builder)
    {
        builder.Property(u => u.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();
    }
}
