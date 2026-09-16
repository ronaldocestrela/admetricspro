using Automations.Application.Pacing.DTOs;
using Automations.Domain.Pacing;
using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Automations.Application.Pacing.Queries.SimulateBudgetPacing;

/// <summary>
/// Consulta para simulação dinâmica de ritmo e projeção de consumo de orçamento sob demanda.
/// </summary>
/// <param name="TargetBudget">Orçamento total planejado.</param>
/// <param name="CurrentSpend">Gasto acumulado realizado até o momento.</param>
/// <param name="StartDateUtc">Data inicial do ciclo.</param>
/// <param name="EndDateUtc">Data final do ciclo.</param>
/// <param name="AsOfDateUtc">Data de referência da simulação.</param>
/// <param name="TolerancePercentage">Margem de tolerância percentual opcional (padrão 0.10).</param>
public sealed record SimulateBudgetPacingQuery(
    decimal TargetBudget,
    decimal CurrentSpend,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    DateTime AsOfDateUtc,
    decimal? TolerancePercentage = null) : IRequest<Result<WorkspaceBudgetPacingDto>>;

/// <summary>
/// Manipulador da consulta <see cref="SimulateBudgetPacingQuery"/>.
/// </summary>
public sealed class SimulateBudgetPacingQueryHandler : IRequestHandler<SimulateBudgetPacingQuery, Result<WorkspaceBudgetPacingDto>>
{
    private readonly IBudgetPacingCalculator _calculator;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SimulateBudgetPacingQueryHandler"/>.
    /// </summary>
    public SimulateBudgetPacingQueryHandler(IBudgetPacingCalculator calculator)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    /// <inheritdoc />
    public Task<Result<WorkspaceBudgetPacingDto>> Handle(SimulateBudgetPacingQuery request, CancellationToken cancellationToken)
    {
        var tolerance = request.TolerancePercentage ?? 0.10m;
        var calcResult = _calculator.Calculate(
            request.TargetBudget,
            request.CurrentSpend,
            request.StartDateUtc,
            request.EndDateUtc,
            request.AsOfDateUtc,
            tolerance);

        if (calcResult.IsFailure)
        {
            return Task.FromResult(Result<WorkspaceBudgetPacingDto>.Failure(calcResult.Error));
        }

        var p = calcResult.Value;

        var dto = new WorkspaceBudgetPacingDto
        {
            WorkspaceId = Guid.Empty,
            WorkspaceName = "Simulação Sob Demanda",
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
            CycleStartDateUtc = request.StartDateUtc,
            CycleEndDateUtc = request.EndDateUtc,
            AsOfDateUtc = request.AsOfDateUtc,
            CampaignBreakdown = Array.Empty<CampaignPacingDto>()
        };

        return Task.FromResult(Result<WorkspaceBudgetPacingDto>.Success(dto));
    }
}
