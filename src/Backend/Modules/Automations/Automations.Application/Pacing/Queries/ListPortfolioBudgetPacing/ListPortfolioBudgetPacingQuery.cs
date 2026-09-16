using Automations.Application.Pacing.DTOs;
using Automations.Application.Pacing.Services;
using Automations.Domain.Pacing;
using BuildingBlocks.Domain.Automations.Pacing;
using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Automations.Application.Pacing.Queries.ListPortfolioBudgetPacing;

/// <summary>
/// Consulta para consolidar o status de pacing de toda a carteira de clientes do inquilino.
/// </summary>
/// <param name="SquadId">Filtro opcional por Squad.</param>
/// <param name="Year">Ano de referência (opcional, padrão: ano atual).</param>
/// <param name="Month">Mês de referência (1 a 12, opcional, padrão: mês atual).</param>
/// <param name="AsOfDateUtc">Data de corte opcional (padrão: data/hora atual UTC).</param>
public sealed record ListPortfolioBudgetPacingQuery(
    Guid? SquadId = null,
    int? Year = null,
    int? Month = null,
    DateTime? AsOfDateUtc = null) : IRequest<Result<PortfolioPacingSummaryDto>>;

/// <summary>
/// Manipulador da consulta <see cref="ListPortfolioBudgetPacingQuery"/>.
/// </summary>
public sealed class ListPortfolioBudgetPacingQueryHandler : IRequestHandler<ListPortfolioBudgetPacingQuery, Result<PortfolioPacingSummaryDto>>
{
    private readonly IBudgetPacingDataProvider _dataProvider;
    private readonly IBudgetPacingCalculator _calculator;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ListPortfolioBudgetPacingQueryHandler"/>.
    /// </summary>
    public ListPortfolioBudgetPacingQueryHandler(
        IBudgetPacingDataProvider dataProvider,
        IBudgetPacingCalculator calculator)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    /// <inheritdoc />
    public async Task<Result<PortfolioPacingSummaryDto>> Handle(ListPortfolioBudgetPacingQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var asOfDate = request.AsOfDateUtc ?? now;
        var year = request.Year ?? asOfDate.Year;
        var month = request.Month ?? asOfDate.Month;

        if (month < 1 || month > 12)
        {
            return Result<PortfolioPacingSummaryDto>.Failure(
                Error.Validation("Pacing.InvalidMonth", "O mês deve estar compreendido entre 1 e 12."));
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var cycleStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var cycleEnd = new DateTime(year, month, daysInMonth, 23, 59, 59, DateTimeKind.Utc);

        var dataResult = await _dataProvider.GetPortfolioPacingDataAsync(request.SquadId, cycleStart, cycleEnd, cancellationToken);
        if (dataResult.IsFailure)
        {
            return Result<PortfolioPacingSummaryDto>.Failure(dataResult.Error);
        }

        var rawList = dataResult.Value;
        var workspaceDtos = new List<WorkspaceBudgetPacingDto>();

        foreach (var raw in rawList)
        {
            var calcResult = _calculator.Calculate(
                raw.MonthlyAdSpendBudget,
                raw.CurrentSpend,
                cycleStart,
                cycleEnd,
                asOfDate);

            if (calcResult.IsFailure)
            {
                continue;
            }

            var p = calcResult.Value;

            var campaignBreakdown = raw.Campaigns.Select(c =>
            {
                var campIdeal = (c.DailyBudget ?? (p.TotalDaysInCycle > 0 ? c.CurrentSpend / p.ElapsedDays : 0m)) * p.ElapsedDays;
                var campRatio = campIdeal > 0 ? Math.Round(c.CurrentSpend / campIdeal, 4) : 1.0m;
                PacingStatus campStatus = campRatio > 1.10m ? PacingStatus.Over : (campRatio < 0.90m ? PacingStatus.Under : PacingStatus.OnTrack);

                return new CampaignPacingDto
                {
                    CampaignId = c.CampaignId,
                    CampaignName = c.CampaignName,
                    Platform = c.Platform,
                    DailyBudget = c.DailyBudget,
                    CurrentSpend = c.CurrentSpend,
                    Status = campStatus,
                    PacingRatio = campRatio
                };
            }).ToList();

            workspaceDtos.Add(new WorkspaceBudgetPacingDto
            {
                WorkspaceId = raw.WorkspaceId,
                WorkspaceName = raw.WorkspaceName,
                TargetBudget = p.TargetBudget,
                CurrentSpend = p.CurrentSpend,
                RemainingBudget = p.RemainingBudget,
                TotalDaysInCycle = p.TotalDaysInCycle,
                ElapsedDays = p.ElapsedDays,
                RemainingDays = p.RemainingDays,
                ExpectedSpendToDate = p.ExpectedSpendToDate,
                PacingRatio = p.PacingRatio,
                PacingPercentage = p.PacingPercentage,
                ActualDailyRunRate = p.ActualDailyRunRate,
                IdealDailyRunRate = p.IdealDailyRunRate,
                RequiredDailyRunRate = p.RequiredDailyRunRate,
                ProjectedMonthEndSpend = p.ProjectedMonthEndSpend,
                ProjectedVariance = p.ProjectedVariance,
                ProjectedVariancePercentage = p.ProjectedVariancePercentage,
                Status = p.Status,
                Recommendation = p.Recommendation,
                Currency = "BRL",
                CycleStartDateUtc = cycleStart,
                CycleEndDateUtc = cycleEnd,
                AsOfDateUtc = asOfDate,
                CampaignBreakdown = campaignBreakdown
            });
        }

        var summary = new PortfolioPacingSummaryDto
        {
            TotalWorkspaces = workspaceDtos.Count,
            OnTrackCount = workspaceDtos.Count(w => w.Status == PacingStatus.OnTrack),
            OverCount = workspaceDtos.Count(w => w.Status == PacingStatus.Over),
            UnderCount = workspaceDtos.Count(w => w.Status == PacingStatus.Under),
            TotalContractedBudget = workspaceDtos.Sum(w => w.TargetBudget),
            TotalCurrentSpend = workspaceDtos.Sum(w => w.CurrentSpend),
            TotalProjectedSpend = workspaceDtos.Sum(w => w.ProjectedMonthEndSpend),
            Currency = "BRL",
            Workspaces = workspaceDtos
        };

        return Result<PortfolioPacingSummaryDto>.Success(summary);
    }
}
