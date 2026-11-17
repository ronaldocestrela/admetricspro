using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tenants.Application.Users.Repositories;

namespace Tenants.Infrastructure.Users;

/// <summary>
/// Implementação EF Core de <see cref="ITenantUserInvitationRepository"/>.
/// </summary>
public sealed class TenantUserInvitationRepository : ITenantUserInvitationRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantUserInvitationRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor do contexto do inquilino.</param>
    public TenantUserInvitationRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<TenantUserInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        var normalized = tokenHash.Trim();
        return await contextResult.Value.TenantUserInvitations
            .FirstOrDefaultAsync(i => i.TokenHash == normalized, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TenantUserInvitation?> GetOpenByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        return await contextResult.Value.TenantUserInvitations
            .Where(i => i.UserId == userId &&
                        i.AcceptedAtUtc == null &&
                        i.RevokedAtUtc == null &&
                        i.ExpiresAtUtc > now)
            .OrderByDescending(i => i.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantUserInvitation>> GetOpenByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return Array.Empty<TenantUserInvitation>();
        }

        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<TenantUserInvitation>();
        }

        var now = DateTime.UtcNow;
        return await contextResult.Value.TenantUserInvitations
            .AsNoTracking()
            .Where(i => userIds.Contains(i.UserId) &&
                        i.AcceptedAtUtc == null &&
                        i.RevokedAtUtc == null &&
                        i.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(TenantUserInvitation invitation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsSuccess)
        {
            await contextResult.Value.TenantUserInvitations.AddAsync(invitation, cancellationToken);
        }
    }

    /// <inheritdoc />
    public void Update(TenantUserInvitation invitation)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        var contextResult = _contextAccessor.GetDbContextAsync().GetAwaiter().GetResult();
        if (contextResult.IsSuccess)
        {
            contextResult.Value.TenantUserInvitations.Update(invitation);
        }
    }
}