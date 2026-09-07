using BuildingBlocks.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core fluente para a entidade de domínio <see cref="ConnectedAdAccount"/>.
/// </summary>
public sealed class ConnectedAdAccountEntityTypeConfiguration : IEntityTypeConfiguration<ConnectedAdAccount>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConnectedAdAccount> builder)
    {
        builder.ToTable("ConnectedAdAccounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.WorkspaceId)
            .IsRequired();

        builder.HasIndex(account => account.WorkspaceId);

        builder.Property(account => account.Platform)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(account => new { account.WorkspaceId, account.Platform });

        builder.Property(account => account.ExternalAccountId)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(account => account.AccountName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(account => account.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("BRL")
            .IsRequired();

        builder.Property(account => account.Status)
            .HasMaxLength(50)
            .HasDefaultValue("Connected")
            .IsRequired();

        builder.Property(account => account.IsDemo)
            .IsRequired();

        builder.HasIndex(account => account.IsDemo);

        builder.Property(account => account.CreatedAtUtc)
            .IsRequired();

        builder.Property(account => account.UpdatedAtUtc)
            .IsRequired(false);
    }
}
