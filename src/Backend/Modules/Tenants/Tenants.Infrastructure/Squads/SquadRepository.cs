using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tenants.Application.Squads.Repositories;

namespace Tenants.Infrastructure.Squads;

/// <summary>
/// Implementação de <see cref="ISquadRepository"/> para persistência e consulta do agregado <see cref="Squad"/>
/// no banco de dados operacional dedicado do inquilino.
/// </summary>
public sealed class SquadRepository : ISquadRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SquadRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor de contexto para obter a instância resolvida de <see cref="TenantDbContext"/>.</param>
    public SquadRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<Squad?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.Squads
            .Include(s => s.Members)
            .Include(s => s.Workspaces)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Squad>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<Squad>();
        }

        var query = contextResult.Value.Squads
            .Include(s => s.Members)
            .Include(s => s.Workspaces)
            .AsNoTracking();

        if (activeOnly.HasValue)
        {
            query = query.Where(s => s.IsActive == activeOnly.Value);
        }

        return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return false;
        }

        var normalizedName = name.Trim().ToUpper();
        var query = contextResult.Value.Squads.AsNoTracking()
            .Where(s => s.Name.ToUpper() == normalizedName);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Squad>> GetSquadsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<Squad>();
        }

        return await contextResult.Value.Squads
            .Include(s => s.Members)
            .Include(s => s.Workspaces)
            .Where(s => s.IsActive && s.Members.Any(m => m.UserId == userId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Squad>> GetSquadsByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<Squad>();
        }

        return await contextResult.Value.Squads
            .Include(s => s.Members)
            .Include(s => s.Workspaces)
            .Where(s => s.IsActive && s.Workspaces.Any(w => w.WorkspaceId == workspaceId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Squad squad, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(squad);
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsSuccess)
        {
            await contextResult.Value.Squads.AddAsync(squad, cancellationToken);
        }
    }

    /// <inheritdoc />
    public void Update(Squad squad)
    {
        ArgumentNullException.ThrowIfNull(squad);
        var contextResult = _contextAccessor.GetDbContextAsync().GetAwaiter().GetResult();
        if (contextResult.IsSuccess)
        {
            contextResult.Value.Squads.Update(squad);
        }
    }

    /// <inheritdoc />
    public void Remove(Squad squad)
    {
        ArgumentNullException.ThrowIfNull(squad);
        var contextResult = _contextAccessor.GetDbContextAsync().GetAwaiter().GetResult();
        if (contextResult.IsSuccess)
        {
            contextResult.Value.Squads.Remove(squad);
        }
    }
}
