using Master.Application.Auditing;
using Master.Domain.Auditing;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Repositories;

/// <summary>
/// Repositório EF Core para persistência e consulta da trilha de auditoria global na tabela MasterAuditLogs.
/// </summary>
public sealed class MasterAuditRepository : IMasterAuditRepository
{
    private readonly MasterDbContext _context;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="MasterAuditRepository"/>.
    /// </summary>
    /// <param name="context">Contexto de catálogo Master.</param>
    public MasterAuditRepository(MasterDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(MasterAuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await _context.AuditLogs.AddAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MasterAuditEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MasterAuditEntry>> GetByTenantIdAsync(Guid tenantId, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MasterAuditEntry>> GetImpersonatedLogsAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(e => e.IsImpersonated)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
