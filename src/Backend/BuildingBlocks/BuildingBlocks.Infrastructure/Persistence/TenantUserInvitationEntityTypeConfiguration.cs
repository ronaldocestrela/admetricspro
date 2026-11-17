using BuildingBlocks.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuração EF Core da entidade <see cref="TenantUserInvitation"/> no banco operacional do inquilino.
/// </summary>
public sealed class TenantUserInvitationEntityTypeConfiguration : IEntityTypeConfiguration<TenantUserInvitation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TenantUserInvitation> builder)
    {
        builder.ToTable("TenantUserInvitations");
        builder.HasKey(invitation => invitation.Id);

        builder.Property(invitation => invitation.UserId).IsRequired();
        builder.Property(invitation => invitation.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(invitation => invitation.ExpiresAtUtc).IsRequired();
        builder.Property(invitation => invitation.CreatedAtUtc).IsRequired();
        builder.Property(invitation => invitation.CreatedByUserId);
        builder.Property(invitation => invitation.AcceptedAtUtc);
        builder.Property(invitation => invitation.RevokedAtUtc);

        builder.HasIndex(invitation => invitation.TokenHash).IsUnique();
        builder.HasIndex(invitation => invitation.UserId);
        builder.HasIndex(invitation => new { invitation.UserId, invitation.AcceptedAtUtc, invitation.RevokedAtUtc });
    }
}