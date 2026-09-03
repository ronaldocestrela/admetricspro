using Master.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// EF Core mapping for <see cref="ImpersonationSession"/> aggregate.
/// </summary>
public sealed class ImpersonationSessionEntityTypeConfiguration : IEntityTypeConfiguration<ImpersonationSession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ImpersonationSession> builder)
    {
        builder.ToTable("ImpersonationSessions");

        builder.HasKey(session => session.Id);

        builder
            .Property(session => session.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder
            .Property(session => session.SuperAdminId)
            .IsRequired();

        builder
            .Property(session => session.SupportTicketId)
            .HasMaxLength(50)
            .IsRequired();

        builder
            .Property(session => session.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder
            .Property(session => session.CreatedAtUtc)
            .IsRequired();

        builder
            .Property(session => session.ExpiresAtUtc)
            .IsRequired();

        builder
            .Property(session => session.RevokedAtUtc);

        builder
            .Property(session => session.RevokeReason)
            .HasMaxLength(500);

        builder.HasIndex(session => session.TenantId);
        builder.HasIndex(session => session.SuperAdminId);
        builder.HasIndex(session => session.SupportTicketId);
    }
}
