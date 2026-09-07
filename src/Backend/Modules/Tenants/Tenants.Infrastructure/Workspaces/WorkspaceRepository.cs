using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Infrastructure.Workspaces;

/// <summary>
/// Implementação de <see cref="IWorkspaceRepository"/> para persistência e consulta no <see cref="TenantDbContext"/> do inquilino ativo.
/// </summary>
public sealed class WorkspaceRepository : IWorkspaceRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="WorkspaceRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor de contexto para obter o TenantDbContext resolvido.</param>
    public WorkspaceRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.Workspaces
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Workspace>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<Workspace>();
        }

        var query = contextResult.Value.Workspaces.AsNoTracking();
        if (activeOnly.HasValue)
        {
            query = query.Where(w => w.IsActive == activeOnly.Value);
        }

        return await query.OrderByDescending(w => w.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByCnpjOrCpfAsync(string cnpjOrCpf, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return false;
        }

        var query = contextResult.Value.Workspaces.AsNoTracking()
            .Where(w => w.CnpjOrCpf == cnpjOrCpf);

        if (excludeId.HasValue)
        {
            query = query.Where(w => w.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return 0;
        }

        return await contextResult.Value.Workspaces
            .AsNoTracking()
            .CountAsync(w => w.IsActive, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException($"Não foi possível resolver o contexto de banco do inquilino: {contextResult.Error.Description}");
        }

        await contextResult.Value.Workspaces.AddAsync(workspace, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Workspace workspace)
    {
        var contextResult = _contextAccessor.GetDbContextAsync().GetAwaiter().GetResult();
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException($"Não foi possível resolver o contexto de banco do inquilino: {contextResult.Error.Description}");
        }

        contextResult.Value.Workspaces.Update(workspace);
    }
}
