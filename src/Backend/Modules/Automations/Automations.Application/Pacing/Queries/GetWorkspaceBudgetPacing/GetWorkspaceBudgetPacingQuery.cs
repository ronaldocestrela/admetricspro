using Automations.Application.Pacing.DTOs;
using Automations.Application.Pacing.Services;
using Automations.Domain.Pacing;
using BuildingBlocks.Domain.Automations.Pacing;
using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Automations.Application.Pacing.Queries.GetWorkspaceBudgetPacing;

/// <summary>
/// Consulta para obtenção do cálculo de pacing e previsão de fim de mês para um workspace específico.
/// </summary>
/// <param name="WorkspaceId">Identificador único do Workspace.</param>
/// <param name="Year">Ano de referência (opcional, padrão: ano atual).</param>
/// <param name="Month">Mês de referência (1 a 12, opcional, padrão: mês atual).</param>
/// <param name="AsOfDateUtc">Data de corte opcional (padrão: data/hora atual UTC).</param>
public sealed record GetWorkspaceBudgetPacingQuery(
    Guid WorkspaceId,
    int? Year = null,
    int? Month = null,
    DateTime? AsOfDateUtc = null) : IRequest<Result<WorkspaceBudgetPacingDto>>;

/// <summary>
/// Manipulador da consulta <see cref="GetWorkspaceBudgetPacingQuery"/>.
/// </summary>
public sealed class GetWorkspaceBudgetPacingQueryHandler : IRequestHandler<GetWorkspaceBudgetPacingQuery, Result<WorkspaceBudgetPacingDto>>
{
    private readonly IBudgetPacingDataProvider _dataProvider;
    private readonly IBudgetPacingCalculator _calculator;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetWorkspaceBudgetPacingQueryHandler"/>.
    /// </summary>
    public GetWorkspaceBudgetPacingQueryHandler(
        IBudgetPacingDataProvider dataProvider,
        IBudgetPacingCalculator calculator)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    /// <inheritdoc />
    public async Task<Result<WorkspaceBudgetPacingDto>> Handle(GetWorkspaceBudgetPacingQuery request, CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<WorkspaceBudgetPacingDto>.Failure(
                Error.Validation("Pacing.InvalidWorkspaceId", "O identificador do Workspace é obrigatório."));
        }

        var now = DateTime.UtcNow;
        var asOfDate = request.AsOfDateUtc ?? now;
        var year = request.Year ?? asOfDate.Year;
        var month = request.Month ?? asOfDate.Month;

        if (month < 1 || month > 12)
        {
            return Result<WorkspaceBudgetPacingDto>.Failure(
                Error.Validation("Pacing.InvalidMonth", "O mês deve estar compreendido entre 1 e 12."));
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var cycleStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var cycleEnd = new DateTime(year, month, daysInMonth, 23, 59, 59, DateTimeKind.Utc);

        var dataResult = await _dataProvider.GetWorkspacePacingDataAsync(request.WorkspaceId, cycleStart, cycleEnd, cancellationToken);
        if (dataResult.IsFailure)
        {
            return Result<WorkspaceBudgetPacingDto>.Failure(dataResult.Error);
        }

        var raw = dataResult.Value;
        var calcResult = _calculator.Calculate(
            raw.MonthlyAdSpendBudget,
            raw.CurrentSpend,
            cycleStart,
            cycleEnd,
            asOfDate);

        if (calcResult.IsFailure)
        {
            return Result<WorkspaceBudgetPacingDto>.Failure(calcResult.Error);
        }

        var p = calcResult.Value;

        var campaignBreakdown = raw.Campaigns.Select(c =>
        {
            var campIdealToDate = (c.DailyBudget ?? (p.TotalDaysInCycle > 0 ? c.CurrentSpend / p.ElapsedDays : 0m)) * p.ElapsedDays;
            var campRatio = campIdealToDate > 0 ? Math.Round(c.CurrentSpend / campIdealToDate, 4) : 1.0m;
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

        var dto = new WorkspaceBudgetPacingDto
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
        };

        return Result<WorkspaceBudgetPacingDto>.Success(dto);
    }
}
