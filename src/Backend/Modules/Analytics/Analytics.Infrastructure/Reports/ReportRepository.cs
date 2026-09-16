using Analytics.Domain.Reports;
using BuildingBlocks.Domain.Reports;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Infrastructure.Reports;

/// <summary>
/// Implementação concreta do repositório de relatórios, agendamentos e auditoria com EF Core no TenantDbContext.
/// </summary>
public sealed class ReportRepository : IReportRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ReportRepository"/>.
    /// </summary>
    public ReportRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<ReportSchedule?> GetScheduleByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return null;

        return await dbResult.Value.ReportSchedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReportSchedule>> GetSchedulesByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return Array.Empty<ReportSchedule>();

        return await dbResult.Value.ReportSchedules
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReportSchedule>> GetPendingSchedulesAsync(DateTime dueLimitUtc, CancellationToken cancellationToken = default)
    {
        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return Array.Empty<ReportSchedule>();

        return await dbResult.Value.ReportSchedules
            .Where(s => s.IsActive && s.NextExecutionAtUtc.HasValue && s.NextExecutionAtUtc.Value <= dueLimitUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddScheduleAsync(ReportSchedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return;

        var db = dbResult.Value;
        await db.ReportSchedules.AddAsync(schedule, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateScheduleAsync(ReportSchedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return;

        var db = dbResult.Value;
        db.ReportSchedules.Update(schedule);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteScheduleAsync(ReportSchedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return;

        var db = dbResult.Value;
        db.ReportSchedules.Remove(schedule);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GeneratedReport?> GetReportByShareTokenAsync(string shareToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shareToken)) return null;

        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return null;

        return await dbResult.Value.GeneratedReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ShareToken == shareToken.Trim(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GeneratedReport?> GetReportByIdAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return null;

        return await dbResult.Value.GeneratedReports
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GeneratedReport>> GetReportsByWorkspaceIdAsync(Guid workspaceId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return Array.Empty<GeneratedReport>();

        return await dbResult.Value.GeneratedReports
            .AsNoTracking()
            .Where(r => r.WorkspaceId == workspaceId)
            .OrderByDescending(r => r.GeneratedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddGeneratedReportAsync(GeneratedReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return;

        var db = dbResult.Value;
        await db.GeneratedReports.AddAsync(report, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateGeneratedReportAsync(GeneratedReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return;

        var db = dbResult.Value;
        db.GeneratedReports.Update(report);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddDispatchLogsAsync(IEnumerable<ReportDispatchLog> logs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(logs);
        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return;

        var db = dbResult.Value;
        await db.ReportDispatchLogs.AddRangeAsync(logs, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReportDispatchLog>> GetDispatchLogsByReportIdAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var dbResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbResult.IsFailure) return Array.Empty<ReportDispatchLog>();

        return await dbResult.Value.ReportDispatchLogs
            .AsNoTracking()
            .Where(l => l.GeneratedReportId == reportId)
            .OrderByDescending(l => l.DispatchedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
