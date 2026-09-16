using Automations.Application.Pacing.Services;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Automations.Infrastructure.Pacing;

/// <summary>
/// Implementação concreta de <see cref="IBudgetPacingDataProvider"/> que consulta o banco de dados dedicado do inquilino.
/// </summary>
public sealed class BudgetPacingDataProvider : IBudgetPacingDataProvider
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BudgetPacingDataProvider"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor do contexto transacional do inquilino atual.</param>
    public BudgetPacingDataProvider(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<WorkspacePacingRawData>> GetWorkspacePacingDataAsync(
        Guid workspaceId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default)
    {
        var dbContextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbContextResult.IsFailure)
        {
            return Result<WorkspacePacingRawData>.Failure(dbContextResult.Error);
        }

        var db = dbContextResult.Value;

        var workspace = await db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace is null)
        {
            return Result<WorkspacePacingRawData>.Failure(
                Error.NotFound("Workspace.NotFound", "Workspace não localizado para este inquilino."));
        }

        var startDate = startDateUtc.Date;
        var endDate = endDateUtc.Date;

        var metrics = await db.CampaignMetrics
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.Date >= startDate && m.Date <= endDate)
            .ToListAsync(cancellationToken);

        var campaigns = await db.Campaigns
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

        var metricsByCampaign = metrics
            .GroupBy(m => m.CampaignId)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Spend));

        var campaignSpends = new List<CampaignSpendRawData>();

        foreach (var camp in campaigns)
        {
            metricsByCampaign.TryGetValue(camp.Id, out var spend);
            campaignSpends.Add(new CampaignSpendRawData(
                camp.Id,
                camp.Name,
                camp.Platform,
                camp.DailyBudget,
                spend));
        }

        // Campanhas que possam ter métricas mas não estejam no catálogo
        foreach (var kvp in metricsByCampaign)
        {
            if (campaignSpends.All(c => c.CampaignId != kvp.Key))
            {
                var metricSample = metrics.First(m => m.CampaignId == kvp.Key);
                campaignSpends.Add(new CampaignSpendRawData(
                    kvp.Key,
                    metricSample.ExternalCampaignId,
                    metricSample.Platform,
                    null,
                    kvp.Value));
            }
        }

        var totalSpend = metrics.Sum(m => m.Spend);

        var rawData = new WorkspacePacingRawData(
            workspace.Id,
            workspace.Name,
            workspace.MonthlyAdSpendBudget,
            totalSpend,
            campaignSpends);

        return Result<WorkspacePacingRawData>.Success(rawData);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<WorkspacePacingRawData>>> GetPortfolioPacingDataAsync(
        Guid? squadId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default)
    {
        var dbContextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbContextResult.IsFailure)
        {
            return Result<IReadOnlyList<WorkspacePacingRawData>>.Failure(dbContextResult.Error);
        }

        var db = dbContextResult.Value;

        var workspacesQuery = db.Workspaces
            .AsNoTracking()
            .Where(w => w.IsActive);

        if (squadId.HasValue && squadId.Value != Guid.Empty)
        {
            var wsIdsInSquad = db.SquadWorkspaces
                .AsNoTracking()
                .Where(sw => sw.SquadId == squadId.Value)
                .Select(sw => sw.WorkspaceId);

            workspacesQuery = workspacesQuery.Where(w => wsIdsInSquad.Contains(w.Id));
        }

        var workspaces = await workspacesQuery.ToListAsync(cancellationToken);
        var workspaceIds = workspaces.Select(w => w.Id).ToList();

        var startDate = startDateUtc.Date;
        var endDate = endDateUtc.Date;

        var allMetrics = await db.CampaignMetrics
            .AsNoTracking()
            .Where(m => workspaceIds.Contains(m.WorkspaceId) && m.Date >= startDate && m.Date <= endDate)
            .ToListAsync(cancellationToken);

        var allCampaigns = await db.Campaigns
            .AsNoTracking()
            .Where(c => workspaceIds.Contains(c.WorkspaceId))
            .ToListAsync(cancellationToken);

        var resultList = new List<WorkspacePacingRawData>();

        foreach (var ws in workspaces)
        {
            var wsMetrics = allMetrics.Where(m => m.WorkspaceId == ws.Id).ToList();
            var wsCampaigns = allCampaigns.Where(c => c.WorkspaceId == ws.Id).ToList();

            var metricsByCamp = wsMetrics
                .GroupBy(m => m.CampaignId)
                .ToDictionary(g => g.Key, g => g.Sum(m => m.Spend));

            var campSpends = new List<CampaignSpendRawData>();
            foreach (var camp in wsCampaigns)
            {
                metricsByCamp.TryGetValue(camp.Id, out var spend);
                campSpends.Add(new CampaignSpendRawData(
                    camp.Id,
                    camp.Name,
                    camp.Platform,
                    camp.DailyBudget,
                    spend));
            }

            var totalSpend = wsMetrics.Sum(m => m.Spend);

            resultList.Add(new WorkspacePacingRawData(
                ws.Id,
                ws.Name,
                ws.MonthlyAdSpendBudget,
                totalSpend,
                campSpends));
        }

        return Result<IReadOnlyList<WorkspacePacingRawData>>.Success(resultList);
    }
}
