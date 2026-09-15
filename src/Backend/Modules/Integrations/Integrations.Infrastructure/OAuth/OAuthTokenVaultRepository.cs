using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Infrastructure.Persistence;
using Integrations.Domain.OAuth;
using Microsoft.EntityFrameworkCore;

namespace Integrations.Infrastructure.OAuth;

/// <summary>
/// Implementação de <see cref="IOAuthTokenVaultRepository"/> que persiste credenciais cifradas
/// no banco de dados dedicado do inquilino utilizando <see cref="ITenantDbContextAccessor"/>.
/// </summary>
public sealed class OAuthTokenVaultRepository : IOAuthTokenVaultRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OAuthTokenVaultRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor de contexto de banco do inquilino corrente.</param>
    public OAuthTokenVaultRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<OAuthTokenVault?> GetByWorkspaceAndPlatformAsync(
        Guid workspaceId,
        string platform,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        var normalizedPlatform = OAuthPlatform.Normalize(platform);
        return await contextResult.Value.OAuthTokenVaults
            .FirstOrDefaultAsync(v => v.WorkspaceId == workspaceId && v.Platform == normalizedPlatform, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OAuthTokenVault>> GetByWorkspaceIdAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<OAuthTokenVault>();
        }

        return await contextResult.Value.OAuthTokenVaults
            .Where(v => v.WorkspaceId == workspaceId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OAuthTokenVault>> GetExpiringTokensAsync(
        TimeSpan threshold,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<OAuthTokenVault>();
        }

        var deadline = DateTime.UtcNow.Add(threshold);
        return await contextResult.Value.OAuthTokenVaults
            .Where(v => v.Status == OAuthConnectionStatus.Active &&
                        v.AccessTokenExpiresAtUtc.HasValue &&
                        v.AccessTokenExpiresAtUtc.Value <= deadline)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(OAuthTokenVault vault, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vault);

        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return;
        }

        await contextResult.Value.OAuthTokenVaults.AddAsync(vault, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(OAuthTokenVault vault)
    {
        ArgumentNullException.ThrowIfNull(vault);

        var contextResult = _contextAccessor.GetDbContextAsync().GetAwaiter().GetResult();
        if (contextResult.IsFailure)
        {
            return;
        }

        contextResult.Value.OAuthTokenVaults.Update(vault);
    }

    /// <inheritdoc />
    public void Remove(OAuthTokenVault vault)
    {
        ArgumentNullException.ThrowIfNull(vault);

        var contextResult = _contextAccessor.GetDbContextAsync().GetAwaiter().GetResult();
        if (contextResult.IsFailure)
        {
            return;
        }

        contextResult.Value.OAuthTokenVaults.Remove(vault);
    }
}
