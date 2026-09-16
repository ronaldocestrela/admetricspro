namespace BuildingBlocks.Domain.Automations.Pacing;

/// <summary>
/// Resultado da projeção e cálculo analítico de consumo de verba publicitária (Pacing).
/// </summary>
public sealed record BudgetPacingCalculationResult
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="BudgetPacingCalculationResult"/>.
    /// </summary>
    public BudgetPacingCalculationResult(
        decimal targetBudget,
        decimal currentSpend,
        decimal remainingBudget,
        int totalDaysInCycle,
        int elapsedDays,
        int remainingDays,
        decimal expectedSpendToDate,
        decimal pacingRatio,
        decimal pacingPercentage,
        decimal actualDailyRunRate,
        decimal idealDailyRunRate,
        decimal requiredDailyRunRate,
        decimal projectedMonthEndSpend,
        decimal projectedVariance,
        decimal projectedVariancePercentage,
        PacingStatus status,
        string recommendation)
    {
        TargetBudget = targetBudget;
        CurrentSpend = currentSpend;
        RemainingBudget = remainingBudget;
        TotalDaysInCycle = totalDaysInCycle;
        ElapsedDays = elapsedDays;
        RemainingDays = remainingDays;
        ExpectedSpendToDate = expectedSpendToDate;
        PacingRatio = pacingRatio;
        PacingPercentage = pacingPercentage;
        ActualDailyRunRate = actualDailyRunRate;
        IdealDailyRunRate = idealDailyRunRate;
        RequiredDailyRunRate = requiredDailyRunRate;
        ProjectedMonthEndSpend = projectedMonthEndSpend;
        ProjectedVariance = projectedVariance;
        ProjectedVariancePercentage = projectedVariancePercentage;
        Status = status;
        Recommendation = recommendation;
    }

    /// <summary>
    /// Orçamento total contratado/programado para o ciclo de faturamento.
    /// </summary>
    public decimal TargetBudget { get; init; }

    /// <summary>
    /// Total de gasto acumulado realizado até o momento de referência.
    /// </summary>
    public decimal CurrentSpend { get; init; }

    /// <summary>
    /// Saldo orçamentário restante a ser consumido no ciclo (TargetBudget - CurrentSpend).
    /// </summary>
    public decimal RemainingBudget { get; init; }

    /// <summary>
    /// Quantidade total de dias que compõem o ciclo de investimento.
    /// </summary>
    public int TotalDaysInCycle { get; init; }

    /// <summary>
    /// Quantidade de dias já decorridos desde o início do ciclo até a data de referência.
    /// </summary>
    public int ElapsedDays { get; init; }

    /// <summary>
    /// Quantidade de dias restantes até o encerramento do ciclo.
    /// </summary>
    public int RemainingDays { get; init; }

    /// <summary>
    /// Gasto ideal linear que deveria ter sido consumido até o momento.
    /// </summary>
    public decimal ExpectedSpendToDate { get; init; }

    /// <summary>
    /// Razão matemática entre o gasto realizado e o gasto esperado (CurrentSpend / ExpectedSpendToDate).
    /// </summary>
    public decimal PacingRatio { get; init; }

    /// <summary>
    /// Percentual de velocidade de consumo em relação ao ideal (PacingRatio * 100%).
    /// </summary>
    public decimal PacingPercentage { get; init; }

    /// <summary>
    /// Ritmo médio diário de gasto verificado até o momento (CurrentSpend / ElapsedDays).
    /// </summary>
    public decimal ActualDailyRunRate { get; init; }

    /// <summary>
    /// Ritmo diário linear planejado originalmente para o ciclo completo (TargetBudget / TotalDaysInCycle).
    /// </summary>
    public decimal IdealDailyRunRate { get; init; }

    /// <summary>
    /// Ritmo diário necessário nos dias restantes para que o gasto feche exatamente em 100% da verba contratada.
    /// </summary>
    public decimal RequiredDailyRunRate { get; init; }

    /// <summary>
    /// Estimativa de gasto acumulado projetado para o encerramento do ciclo mantendo a velocidade atual.
    /// </summary>
    public decimal ProjectedMonthEndSpend { get; init; }

    /// <summary>
    /// Diferença monetária projetada entre a estimativa de fechamento e a meta orçamentária (ProjectedMonthEndSpend - TargetBudget).
    /// </summary>
    public decimal ProjectedVariance { get; init; }

    /// <summary>
    /// Percentual de desvio projetado em relação à meta contratada (ProjectedVariance / TargetBudget * 100%).
    /// </summary>
    public decimal ProjectedVariancePercentage { get; init; }

    /// <summary>
    /// Classificação de status operacional do ritmo de consumo (OnTrack, Over, Under).
    /// </summary>
    public PacingStatus Status { get; init; }

    /// <summary>
    /// Recomendação tática orientada a dados para adequar o ritmo aos objetivos da conta.
    /// </summary>
    public string Recommendation { get; init; }
}
