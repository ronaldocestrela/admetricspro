using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tenants.Application.Integrations.Repositories;

namespace Tenants.Infrastructure.Integrations;

/// <summary>
/// Implementação de <see cref="IConnectedAdAccountRepository"/> para persistência e consulta no <see cref="TenantDbContext"/> do inquilino ativo.
/// </summary>
public sealed class ConnectedAdAccountRepository : IConnectedAdAccountRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ConnectedAdAccountRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor de contexto para obter o TenantDbContext resolvido.</param>
    public ConnectedAdAccountRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<ConnectedAdAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.ConnectedAdAccounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConnectedAdAccount>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<ConnectedAdAccount>();
        }

        return await contextResult.Value.ConnectedAdAccounts
            .AsNoTracking()
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConnectedAdAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<ConnectedAdAccount>();
        }

        return await contextResult.Value.ConnectedAdAccounts
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return 0;
        }

        return await contextResult.Value.ConnectedAdAccounts.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ConnectedAdAccount account, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsSuccess)
        {
            await contextResult.Value.ConnectedAdAccounts.AddAsync(account, cancellationToken);
        }
    }

    /// <inheritdoc />
    public void Update(ConnectedAdAccount account)
    {
        var syncContext = _contextAccessor.GetDbContextAsync().GetAwaiter().GetResult();
        if (syncContext.IsSuccess)
        {
            syncContext.Value.ConnectedAdAccounts.Update(account);
        }
    }
}
