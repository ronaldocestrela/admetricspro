using Master.Application.Repositories;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Repositories;

/// <summary>
/// EF Core repository implementation for <see cref="ImpersonationSession"/> aggregate persistence.
/// </summary>
public sealed class ImpersonationSessionRepository : IImpersonationSessionRepository
{
    private readonly MasterDbContext _masterDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImpersonationSessionRepository"/> class.
    /// </summary>
    /// <param name="masterDbContext">Master catalog context.</param>
    public ImpersonationSessionRepository(MasterDbContext masterDbContext)
    {
        _masterDbContext = masterDbContext;
    }

    /// <inheritdoc />
    public async Task AddAsync(ImpersonationSession entity, CancellationToken cancellationToken = default)
    {
        await _masterDbContext.ImpersonationSessions.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ImpersonationSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _masterDbContext.ImpersonationSessions.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(ImpersonationSession entity)
    {
        _masterDbContext.ImpersonationSessions.Update(entity);
    }

    /// <inheritdoc />
    public void Remove(ImpersonationSession entity)
    {
        _masterDbContext.ImpersonationSessions.Remove(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImpersonationSession>> GetActiveByTenantIdAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sessions = await _masterDbContext.ImpersonationSessions
            .Where(s => s.TenantId == tenantId && s.RevokedAtUtc == null && s.ExpiresAtUtc > now)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return sessions.AsReadOnly();
    }

    /// <inheritdoc />
    public Task<ImpersonationSession?> GetActiveSessionByIdAsync(
        Guid sessionId,
        DateTime referenceUtc,
        CancellationToken cancellationToken = default)
    {
        return _masterDbContext.ImpersonationSessions
            .SingleOrDefaultAsync(
                s => s.Id == sessionId && s.RevokedAtUtc == null && s.ExpiresAtUtc > referenceUtc,
                cancellationToken);
    }
}
