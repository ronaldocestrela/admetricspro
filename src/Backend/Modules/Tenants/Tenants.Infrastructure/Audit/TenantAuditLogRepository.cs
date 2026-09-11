using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tenants.Application.Audit.Repositories;

namespace Tenants.Infrastructure.Audit;

/// <summary>
/// Implementação de repositório de auditoria imutável do inquilino baseado em <see cref="TenantDbContext"/>.
/// </summary>
public sealed class TenantAuditLogRepository : ITenantAuditLogRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantAuditLogRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor de contexto para obter o DbContext do inquilino ativo.</param>
    public TenantAuditLogRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task AddAsync(TenantAuditLog entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException($"Não foi possível obter o banco do inquilino: {contextResult.Error.Description}");
        }

        await contextResult.Value.TenantAuditLogs.AddAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantAuditLog>> GetLogsAsync(
        Guid? userId = null,
        string? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return [];
        }

        var query = contextResult.Value.TenantAuditLogs.AsNoTracking().AsQueryable();

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            query = query.Where(log => log.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var trimmedAction = action.Trim();
            query = query.Where(log => log.Action == trimmedAction);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(log => log.CreatedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(log => log.CreatedAtUtc <= toUtc.Value);
        }

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        return await query
            .OrderByDescending(log => log.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(
        Guid? userId = null,
        string? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return 0;
        }

        var query = contextResult.Value.TenantAuditLogs.AsNoTracking().AsQueryable();

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            query = query.Where(log => log.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var trimmedAction = action.Trim();
            query = query.Where(log => log.Action == trimmedAction);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(log => log.CreatedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(log => log.CreatedAtUtc <= toUtc.Value);
        }

        return await query.CountAsync(cancellationToken);
    }
}
