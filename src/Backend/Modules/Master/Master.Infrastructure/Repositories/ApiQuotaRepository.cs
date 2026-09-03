using Master.Application.Integrations.Repositories;
using Master.Domain.Integrations;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IApiQuotaRepository"/> on the Master database.
/// </summary>
public sealed class ApiQuotaRepository : IApiQuotaRepository
{
    private readonly MasterDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiQuotaRepository"/> class.
    /// </summary>
    /// <param name="context">Master database context.</param>
    public ApiQuotaRepository(MasterDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<ApiQuotaTracker?> GetByPlatformAsync(AdPlatform platform, CancellationToken cancellationToken = default)
    {
        return await _context.ApiQuotaTrackers
            .FirstOrDefaultAsync(t => t.Platform == platform, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApiQuotaTracker>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ApiQuotaTrackers
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ApiQuotaTracker tracker, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tracker);
        await _context.ApiQuotaTrackers.AddAsync(tracker, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ApiQuotaTracker tracker, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tracker);
        _context.ApiQuotaTrackers.Update(tracker);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
