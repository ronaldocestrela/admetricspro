using Master.Application.FeatureFlags.Repositories;
using Master.Domain.FeatureFlags;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFeatureFlagRepository"/> for the Master database.
/// </summary>
public sealed class FeatureFlagRepository : IFeatureFlagRepository
{
    private readonly MasterDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagRepository"/> class.
    /// </summary>
    /// <param name="context">Master catalog context.</param>
    public FeatureFlagRepository(MasterDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var normalizedKey = key.Trim().ToLowerInvariant();
        return await _context.FeatureFlags
            .FirstOrDefaultAsync(f => f.Key == normalizedKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .AsNoTracking()
            .OrderBy(f => f.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FeatureFlag>> GetKillSwitchesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .AsNoTracking()
            .Where(f => f.IsKillSwitch)
            .OrderBy(f => f.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(flag);
        await _context.FeatureFlags.AddAsync(flag, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(flag);
        _context.FeatureFlags.Update(flag);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
