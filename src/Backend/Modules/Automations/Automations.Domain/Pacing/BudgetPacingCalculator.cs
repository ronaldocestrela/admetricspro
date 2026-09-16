using BuildingBlocks.Domain.Automations.Pacing;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Domain.Pacing;

/// <summary>
/// Implementação concreta do serviço de domínio para cálculo de projeção de consumo de orçamento e pacing.
/// </summary>
public sealed class BudgetPacingCalculator : IBudgetPacingCalculator
{
    /// <inheritdoc />
    public Result<BudgetPacingCalculationResult> Calculate(
        decimal targetBudget,
        decimal currentSpend,
        DateTime cycleStartDateUtc,
        DateTime cycleEndDateUtc,
        DateTime currentDateUtc,
        decimal tolerancePercentage = 0.10m)
    {
        if (cycleEndDateUtc < cycleStartDateUtc)
        {
            return Result<BudgetPacingCalculationResult>.Failure(
                Error.Validation("Pacing.InvalidDateRange", "A data final do ciclo não pode ser anterior à data inicial."));
        }

        if (targetBudget <= 0)
        {
            return Result<BudgetPacingCalculationResult>.Failure(
                Error.Validation("Pacing.InvalidTargetBudget", "O orçamento alvo deve ser maior que zero."));
        }

        if (currentSpend < 0)
        {
            return Result<BudgetPacingCalculationResult>.Failure(
                Error.Validation("Pacing.InvalidCurrentSpend", "O gasto acumulado não pode ser negativo."));
        }

        if (tolerancePercentage < 0 || tolerancePercentage > 1)
        {
            return Result<BudgetPacingCalculationResult>.Failure(
                Error.Validation("Pacing.InvalidTolerance", "A tolerância de pacing deve estar entre 0 e 100%."));
        }

        var startDate = cycleStartDateUtc.Date;
        var endDate = cycleEndDateUtc.Date;
        var currDate = currentDateUtc.Date;

        var totalDaysInCycle = (endDate - startDate).Days + 1;
        if (totalDaysInCycle <= 0)
        {
            totalDaysInCycle = 1;
        }

        int elapsedDays;
        if (currDate < startDate)
        {
            elapsedDays = 1;
        }
        else if (currDate > endDate)
        {
            elapsedDays = totalDaysInCycle;
        }
        else
        {
            elapsedDays = (currDate - startDate).Days + 1;
        }

        var remainingDays = Math.Max(0, totalDaysInCycle - elapsedDays);

        var idealDailyRateExact = targetBudget / totalDaysInCycle;
        var actualDailyRateExact = currentSpend / elapsedDays;

        var idealDailyRunRate = Math.Round(idealDailyRateExact, 2);
        var expectedSpendToDate = Math.Round(idealDailyRateExact * elapsedDays, 2);
        var actualDailyRunRate = Math.Round(actualDailyRateExact, 2);

        var pacingRatio = expectedSpendToDate > 0
            ? Math.Round(currentSpend / expectedSpendToDate, 4)
            : 1.0m;

        var pacingPercentage = Math.Round(pacingRatio * 100m, 2);

        decimal projectedMonthEndSpend;
        if (remainingDays == 0)
        {
            projectedMonthEndSpend = currentSpend;
        }
        else
        {
            projectedMonthEndSpend = Math.Round(currentSpend + (actualDailyRateExact * remainingDays), 2);
        }

        var remainingBudget = targetBudget - currentSpend;
        var requiredDailyRunRate = remainingDays > 0
            ? Math.Round(Math.Max(0, remainingBudget) / remainingDays, 2)
            : 0m;

        var projectedVariance = projectedMonthEndSpend - targetBudget;
        var projectedVariancePercentage = Math.Round((projectedVariance / targetBudget) * 100m, 2);

        var lowerThreshold = 1.0m - tolerancePercentage;
        var upperThreshold = 1.0m + tolerancePercentage;

        PacingStatus status;
        string recommendation;

        if (pacingRatio > upperThreshold)
        {
            status = PacingStatus.Over;
            recommendation = $"Ritmo Sobreaquecido (+{projectedVariancePercentage:F1}% projetado). Recomenda-se reduzir o ritmo diário para R$ {requiredDailyRunRate:N2}/dia para evitar esgotamento prematuro do orçamento.";
        }
        else if (pacingRatio < lowerThreshold)
        {
            status = PacingStatus.Under;
            recommendation = $"Ritmo Subinvestido ({projectedVariancePercentage:F1}% projetado). Recomenda-se elevar o investimento diário para R$ {requiredDailyRunRate:N2}/dia ou realocar saldo ocioso para campanhas de maior tração.";
        }
        else
        {
            status = PacingStatus.OnTrack;
            recommendation = "Ritmo No Ritmo. O consumo está equilibrado com a previsão temporal de fechamento do mês.";
        }

        var result = new BudgetPacingCalculationResult(
            targetBudget,
            currentSpend,
            remainingBudget,
            totalDaysInCycle,
            elapsedDays,
            remainingDays,
            expectedSpendToDate,
            pacingRatio,
            pacingPercentage,
            actualDailyRunRate,
            idealDailyRunRate,
            requiredDailyRunRate,
            projectedMonthEndSpend,
            projectedVariance,
            projectedVariancePercentage,
            status,
            recommendation);

        return Result<BudgetPacingCalculationResult>.Success(result);
    }
}
