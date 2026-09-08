using Master.Domain.Billing;
using Master.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core para a entidade <see cref="TenantPaymentTransaction"/>.
/// </summary>
public sealed class TenantPaymentTransactionEntityTypeConfiguration : IEntityTypeConfiguration<TenantPaymentTransaction>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TenantPaymentTransaction> builder)
    {
        builder.ToTable("TenantPaymentTransactions");

        builder.HasKey(tx => tx.Id);

        builder
            .Property(tx => tx.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder
            .Property(tx => tx.Tier)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder
            .Property(tx => tx.BillingCycle)
            .HasMaxLength(20)
            .IsRequired();

        builder
            .Property(tx => tx.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder
            .Property(tx => tx.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder
            .Property(tx => tx.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder
            .Property(tx => tx.GatewayProvider)
            .HasMaxLength(50)
            .IsRequired();

        builder
            .Property(tx => tx.GatewayTransactionId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder
            .Property(tx => tx.PixQrCode)
            .IsRequired(false);

        builder
            .Property(tx => tx.PixCopiaECola)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder
            .Property(tx => tx.PixExpiresAtUtc)
            .IsRequired(false);

        builder
            .Property(tx => tx.CreatedAtUtc)
            .IsRequired();

        builder
            .Property(tx => tx.PaidAtUtc)
            .IsRequired(false);

        builder
            .Property(tx => tx.FailureReason)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.HasIndex(tx => tx.TenantId);
        builder.HasIndex(tx => tx.GatewayTransactionId);
        builder.HasIndex(tx => tx.Status);
    }
}
