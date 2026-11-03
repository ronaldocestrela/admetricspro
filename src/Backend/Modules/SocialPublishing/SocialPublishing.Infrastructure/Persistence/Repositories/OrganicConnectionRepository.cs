using Microsoft.EntityFrameworkCore;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core de <see cref="IOrganicConnectionRepository"/>.
/// </summary>
public sealed class OrganicConnectionRepository : IOrganicConnectionRepository
{
    private readonly SocialPublishingDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OrganicConnectionRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor scoped do contexto social.</param>
    public OrganicConnectionRepository(SocialPublishingDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<OrganicConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.OrganicConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrganicConnection>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<OrganicConnection>();
        }

        return await contextResult.Value.OrganicConnections
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(OrganicConnection connection, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        await contextResult.Value.OrganicConnections.AddAsync(connection, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(OrganicConnection connection, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        contextResult.Value.OrganicConnections.Update(connection);
        await Task.CompletedTask;
    }
}
