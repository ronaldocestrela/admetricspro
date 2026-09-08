using Master.Application.Repositories;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Repositories;

/// <summary>
/// Implementação EF Core do repositório de logs de notificações transacionais de inquilinos.
/// </summary>
public sealed class TenantNotificationLogRepository : ITenantNotificationLogRepository
{
    private readonly MasterDbContext _context;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantNotificationLogRepository"/>.
    /// </summary>
    /// <param name="context">Contexto central MasterDbContext.</param>
    public TenantNotificationLogRepository(MasterDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task AddAsync(TenantNotificationLog log, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);
        await _context.TenantNotificationLogs.AddAsync(log, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<TrialNoticeType>> GetSentNoticeTypesForTenantAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        var sentTypes = await _context.TenantNotificationLogs
            .AsNoTracking()
            .Where(log => log.TenantId == tenantId && log.IsSuccess)
            .Select(log => log.Type)
            .Distinct()
            .ToListAsync(cancellationToken);

        return sentTypes.ToHashSet();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantNotificationLog>> GetLogsByTenantIdAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        return await _context.TenantNotificationLogs
            .AsNoTracking()
            .Where(log => log.TenantId == tenantId)
            .OrderByDescending(log => log.SentAtUtc)
            .ToListAsync(cancellationToken);
    }
}
