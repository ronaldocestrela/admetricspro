using System.Text.Json;
using Master.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// Configuração de mapeamento do EF Core para a entidade agregada imutável <see cref="MasterAuditEntry"/>.
/// Define a tabela 'MasterAuditLogs' no Catálogo Master, com índices de auditoria e conversão de tags.
/// </summary>
public sealed class MasterAuditEntryEntityTypeConfiguration : IEntityTypeConfiguration<MasterAuditEntry>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MasterAuditEntry> builder)
    {
        builder.ToTable("MasterAuditLogs");

        builder.HasKey(entry => entry.Id);

        builder
            .Property(entry => entry.TenantId);

        builder
            .Property(entry => entry.Action)
            .HasMaxLength(150)
            .IsRequired();

        builder
            .Property(entry => entry.Resource)
            .HasMaxLength(100)
            .IsRequired();

        builder
            .Property(entry => entry.ResourceId)
            .HasMaxLength(200);

        builder
            .Property(entry => entry.Details)
            .HasMaxLength(4000);

        builder
            .Property(entry => entry.IsImpersonated)
            .IsRequired();

        builder
            .Property(entry => entry.SuperAdminId);

        builder
            .Property(entry => entry.SupportTicketId)
            .HasMaxLength(50);

        builder
            .Property(entry => entry.ImpersonationSessionId);

        builder
            .Property(entry => entry.IpAddress)
            .HasMaxLength(45);

        builder
            .Property(entry => entry.CreatedAtUtc)
            .IsRequired();

        var tagsComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<string>>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder
            .Property(entry => entry.Tags)
            .HasConversion(
                tags => JsonSerializer.Serialize(tags, JsonOptions),
                json => string.IsNullOrWhiteSpace(json)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>())
            .Metadata.SetValueComparer(tagsComparer);

        builder
            .Property(entry => entry.Tags)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasIndex(entry => new { entry.TenantId, entry.CreatedAtUtc });
        builder.HasIndex(entry => new { entry.SuperAdminId, entry.CreatedAtUtc });
        builder.HasIndex(entry => new { entry.IsImpersonated, entry.CreatedAtUtc });
        builder.HasIndex(entry => entry.Action);
    }
}
