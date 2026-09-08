using Master.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento EF Core para a entidade de auditoria e idempotência <see cref="TenantNotificationLog"/>.
/// </summary>
public sealed class TenantNotificationLogEntityTypeConfiguration : IEntityTypeConfiguration<TenantNotificationLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TenantNotificationLog> builder)
    {
        builder.ToTable("TenantNotificationLogs");

        builder.HasKey(log => log.Id);

        builder
            .Property(log => log.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder
            .Property(log => log.Type)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder
            .Property(log => log.RecipientEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(log => log.Subject)
            .HasMaxLength(250)
            .IsRequired();

        builder
            .Property(log => log.SentAtUtc)
            .IsRequired();

        builder
            .Property(log => log.IsSuccess)
            .IsRequired();

        builder
            .Property(log => log.ErrorMessage)
            .HasMaxLength(2000)
            .IsRequired(false);

        // Índice para buscas rápidas de histórico e validação de deduplicação por tenant e tipo de aviso
        builder
            .HasIndex(log => new { log.TenantId, log.Type });
    }
}
