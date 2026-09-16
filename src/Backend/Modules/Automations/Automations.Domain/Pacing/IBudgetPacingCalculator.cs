using BuildingBlocks.Domain.Automations.Pacing;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Domain.Pacing;

/// <summary>
/// Contrato do calculador analítico de velocidade de consumo de verba publicitária e previsão de fechamento de ciclo (Pacing).
/// </summary>
public interface IBudgetPacingCalculator
{
    /// <summary>
    /// Calcula as métricas de pacing, projeção de fim de mês e classificação em 3 status operacionais.
    /// </summary>
    /// <param name="targetBudget">Orçamento total programado para o ciclo.</param>
    /// <param name="currentSpend">Gasto acumulado realizado até a data de referência.</param>
    /// <param name="cycleStartDateUtc">Data inicial do ciclo em UTC.</param>
    /// <param name="cycleEndDateUtc">Data final do ciclo em UTC.</param>
    /// <param name="currentDateUtc">Data de corte/referência do cálculo em UTC.</param>
    /// <param name="tolerancePercentage">Margem de tolerância percentual para o status No Ritmo (ex: 0.10 para +/-10%). Padrão é 0.10.</param>
    /// <returns>Resultado com o relatório completo de projeção de pacing ou falha de validação.</returns>
    Result<BudgetPacingCalculationResult> Calculate(
        decimal targetBudget,
        decimal currentSpend,
        DateTime cycleStartDateUtc,
        DateTime cycleEndDateUtc,
        DateTime currentDateUtc,
        decimal tolerancePercentage = 0.10m);
}
