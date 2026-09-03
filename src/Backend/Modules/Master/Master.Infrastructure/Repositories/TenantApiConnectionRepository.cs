using Master.Application.Integrations.Repositories;
using Master.Domain.Integrations;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITenantApiConnectionRepository"/> on the Master database.
/// </summary>
public sealed class TenantApiConnectionRepository : ITenantApiConnectionRepository
{
    private readonly MasterDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantApiConnectionRepository"/> class.
    /// </summary>
    /// <param name="context">Master database context.</param>
    public TenantApiConnectionRepository(MasterDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<TenantApiConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApiConnections
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantApiConnection>> GetConnectionsAsync(
        AdPlatform? platform = null,
        ApiConnectionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TenantApiConnections.AsNoTracking().AsQueryable();

        if (platform.HasValue)
        {
            query = query.Where(c => c.Platform == platform.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        return await query
            .OrderByDescending(c => c.Status == ApiConnectionStatus.Expired || c.Status == ApiConnectionStatus.Revoked)
            .ThenByDescending(c => c.Status == ApiConnectionStatus.ExpiringSoon)
            .ThenBy(c => c.TenantName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByStatusAsync(ApiConnectionStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApiConnections
            .CountAsync(c => c.Status == status, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TenantApiConnections.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(TenantApiConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        await _context.TenantApiConnections.AddAsync(connection, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TenantApiConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _context.TenantApiConnections.Update(connection);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
