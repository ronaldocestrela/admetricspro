using Automations.Domain.SafetyGuards;
using BuildingBlocks.Domain.Automations.SafetyGuards;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Automations.Infrastructure.Persistence;

/// <summary>
/// Implementação concreta do repositório de incidentes de segurança com EF Core.
/// Consulta o banco de dados dedicado do inquilino (TenantDbContext).
/// </summary>
public sealed class SafetyGuardIncidentRepository : ISafetyGuardIncidentRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SafetyGuardIncidentRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor do contexto do banco do inquilino.</param>
    public SafetyGuardIncidentRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task AddAsync(SafetyGuardIncident incident, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);

        var dbContextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbContextResult.IsFailure)
        {
            return;
        }

        var db = dbContextResult.Value;
        await db.SafetyGuardIncidents.AddAsync(incident, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SafetyGuardIncident>> GetRecentByWorkspaceIdAsync(
        Guid workspaceId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var dbContextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbContextResult.IsFailure)
        {
            return Array.Empty<SafetyGuardIncident>();
        }

        var db = dbContextResult.Value;
        var safeLimit = Math.Clamp(limit, 1, 100);

        return await db.SafetyGuardIncidents
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId)
            .OrderByDescending(i => i.DetectedAtUtc)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);
    }
}
