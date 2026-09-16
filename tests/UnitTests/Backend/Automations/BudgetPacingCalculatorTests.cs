using BuildingBlocks.Domain.Automations.Pacing;
using FluentAssertions;
using global::Automations.Domain.Pacing;

namespace UnitTests.Backend.Automations;

/// <summary>
/// Suíte de testes unitários para o calculador de pacing e projeção de consumo de orçamento (Subfase 4.3.1 e 4.3.2).
/// Valida cálculo de projeção linear, dias decorridos, ritmo diário realizado vs. ideal, desvio projetado e os 3 status.
/// </summary>
public sealed class BudgetPacingCalculatorTests
{
    private readonly BudgetPacingCalculator _calculator = new();

    /// <summary>
    /// Valida que quando o gasto realizado está perfeitamente alinhado com o tempo decorrido,
    /// o status deve ser OnTrack (No Ritmo), com PacingRatio próximo de 1.0 (100%).
    /// </summary>
    [Fact]
    public void Calculate_WhenSpendIsExactlyOnPace_ShouldReturnOnTrackStatus()
    {
        // Arrange
        // Ciclo de 30 dias (ex: 1 a 30 de setembro). No 15º dia (50% do mês), gastou R$ 5.000 de R$ 10.000.
        var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);
        var currentDate = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc); // 15º dia
        decimal targetBudget = 10_000m;
        decimal currentSpend = 5_000m;

        // Act
        var result = _calculator.Calculate(targetBudget, currentSpend, startDate, endDate, currentDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pacing = result.Value;

        pacing.TargetBudget.Should().Be(10_000m);
        pacing.CurrentSpend.Should().Be(5_000m);
        pacing.RemainingBudget.Should().Be(5_000m);
        pacing.TotalDaysInCycle.Should().Be(30);
        pacing.ElapsedDays.Should().Be(15);
        pacing.RemainingDays.Should().Be(15);

        pacing.ExpectedSpendToDate.Should().Be(5_000m);
        pacing.PacingRatio.Should().Be(1.0m);
        pacing.PacingPercentage.Should().Be(100.0m);

        pacing.ActualDailyRunRate.Should().BeApproximately(333.33m, 0.01m);
        pacing.IdealDailyRunRate.Should().BeApproximately(333.33m, 0.01m);
        pacing.RequiredDailyRunRate.Should().BeApproximately(333.33m, 0.01m);

        pacing.ProjectedMonthEndSpend.Should().Be(10_000m);
        pacing.ProjectedVariance.Should().Be(0m);
        pacing.ProjectedVariancePercentage.Should().Be(0m);

        pacing.Status.Should().Be(PacingStatus.OnTrack);
    }

    /// <summary>
    /// Valida que quando o consumo está acelerado acima do limiar de tolerância (+10%),
    /// o status deve ser Over (Sobreaquecido) e a projeção deve indicar estouro de verba.
    /// </summary>
    [Fact]
    public void Calculate_WhenSpendIsSignificantlyHigherThanExpected_ShouldReturnOverStatus()
    {
        // Arrange
        // No 10º dia de 30 dias (33.33% do mês), o gasto ideal seria ~R$ 3.333,33.
        // O cliente já gastou R$ 5.000 (50% do orçamento total -> ratio ~1.50).
        var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);
        var currentDate = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
        decimal targetBudget = 10_000m;
        decimal currentSpend = 5_000m;

        // Act
        var result = _calculator.Calculate(targetBudget, currentSpend, startDate, endDate, currentDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pacing = result.Value;

        pacing.ElapsedDays.Should().Be(10);
        pacing.RemainingDays.Should().Be(20);
        pacing.ExpectedSpendToDate.Should().BeApproximately(3333.33m, 0.01m);
        pacing.PacingRatio.Should().BeApproximately(1.50m, 0.01m);
        pacing.PacingPercentage.Should().BeApproximately(150.0m, 0.1m);

        // Run rate atual: 5000 / 10 = 500/dia.
        pacing.ActualDailyRunRate.Should().Be(500m);
        // Projeção final: 500 * 30 = 15.000.
        pacing.ProjectedMonthEndSpend.Should().Be(15_000m);
        pacing.ProjectedVariance.Should().Be(5_000m);
        pacing.ProjectedVariancePercentage.Should().Be(50.0m);

        // Para não estourar os 10k nos 20 dias restantes (5k restantes), o ritmo deve cair para 250/dia.
        pacing.RequiredDailyRunRate.Should().Be(250m);

        pacing.Status.Should().Be(PacingStatus.Over);
        pacing.Recommendation.Should().Contain("Sobreaquecido");
    }

    /// <summary>
    /// Valida que quando o consumo está desacelerado abaixo do limiar de tolerância (-10%),
    /// o status deve ser Under (Subinvestido) e a projeção deve indicar sobra de verba.
    /// </summary>
    [Fact]
    public void Calculate_WhenSpendIsSignificantlyLowerThanExpected_ShouldReturnUnderStatus()
    {
        // Arrange
        // No 20º dia de 30 dias (66.67% do mês), o gasto ideal seria ~R$ 6.666,67.
        // O cliente só gastou R$ 3.000 (30% do orçamento total -> ratio ~0.45).
        var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);
        var currentDate = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);
        decimal targetBudget = 10_000m;
        decimal currentSpend = 3_000m;

        // Act
        var result = _calculator.Calculate(targetBudget, currentSpend, startDate, endDate, currentDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pacing = result.Value;

        pacing.ElapsedDays.Should().Be(20);
        pacing.RemainingDays.Should().Be(10);
        pacing.ExpectedSpendToDate.Should().BeApproximately(6666.67m, 0.01m);
        pacing.PacingRatio.Should().BeApproximately(0.45m, 0.01m);
        pacing.PacingPercentage.Should().BeApproximately(45.0m, 0.1m);

        // Run rate atual: 3000 / 20 = 150/dia.
        pacing.ActualDailyRunRate.Should().Be(150m);
        // Projeção final: 150 * 30 = 4.500.
        pacing.ProjectedMonthEndSpend.Should().Be(4_500m);
        pacing.ProjectedVariance.Should().Be(-5_500m);
        pacing.ProjectedVariancePercentage.Should().Be(-55.0m);

        // Para atingir os 10k nos 10 dias restantes (7k restantes), o ritmo deve subir para 700/dia.
        pacing.RequiredDailyRunRate.Should().Be(700m);

        pacing.Status.Should().Be(PacingStatus.Under);
        pacing.Recommendation.Should().Contain("Subinvestido");
    }

    /// <summary>
    /// Valida que pequenas flutuações dentro da tolerância (ex: ratio de 1.05 ou 0.95 com tolerância de 10%)
    /// continuam classificadas como OnTrack (No Ritmo).
    /// </summary>
    [Theory]
    [InlineData(5_400, PacingStatus.OnTrack)] // Ratio = 1.08 (+8%, dentro de +/- 10%)
    [InlineData(4_600, PacingStatus.OnTrack)] // Ratio = 0.92 (-8%, dentro de +/- 10%)
    [InlineData(5_600, PacingStatus.Over)]    // Ratio = 1.12 (+12%, fora de 10%)
    [InlineData(4_400, PacingStatus.Under)]   // Ratio = 0.88 (-12%, fora de 10%)
    public void Calculate_ToleranceBand_ShouldClassifyCorrectly(decimal currentSpend, PacingStatus expectedStatus)
    {
        // Arrange
        // Dia 15 de 30 dias -> Gasto esperado = R$ 5.000.
        var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);
        var currentDate = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
        decimal targetBudget = 10_000m;

        // Act
        var result = _calculator.Calculate(targetBudget, currentSpend, startDate, endDate, currentDate, tolerancePercentage: 0.10m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(expectedStatus);
    }

    /// <summary>
    /// Valida comportamento no primeiro dia do ciclo (elapsed = 1 dia).
    /// </summary>
    [Fact]
    public void Calculate_OnFirstDayOfCycle_ShouldHandleZeroDivisionAndProduceSensibleForecast()
    {
        // Arrange
        var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);
        var currentDate = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        decimal targetBudget = 3_000m; // 100/dia ideal
        decimal currentSpend = 100m;

        // Act
        var result = _calculator.Calculate(targetBudget, currentSpend, startDate, endDate, currentDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pacing = result.Value;

        pacing.TotalDaysInCycle.Should().Be(30);
        pacing.ElapsedDays.Should().Be(1);
        pacing.RemainingDays.Should().Be(29);
        pacing.ExpectedSpendToDate.Should().Be(100m);
        pacing.ActualDailyRunRate.Should().Be(100m);
        pacing.ProjectedMonthEndSpend.Should().Be(3_000m);
        pacing.Status.Should().Be(PacingStatus.OnTrack);
    }

    /// <summary>
    /// Valida comportamento no último dia do ciclo (remaining = 0 dias).
    /// </summary>
    [Fact]
    public void Calculate_OnLastDayOfCycle_ShouldHandleZeroRemainingDaysGracefully()
    {
        // Arrange
        var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);
        var currentDate = new DateTime(2026, 9, 30, 22, 0, 0, DateTimeKind.Utc);
        decimal targetBudget = 10_000m;
        decimal currentSpend = 9_950m;

        // Act
        var result = _calculator.Calculate(targetBudget, currentSpend, startDate, endDate, currentDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pacing = result.Value;

        pacing.TotalDaysInCycle.Should().Be(30);
        pacing.ElapsedDays.Should().Be(30);
        pacing.RemainingDays.Should().Be(0);
        pacing.ProjectedMonthEndSpend.Should().Be(9_950m);
        pacing.Status.Should().Be(PacingStatus.OnTrack);
    }

    /// <summary>
    /// Valida que se o orçamento ou gasto tiver valores inválidos (negativos ou datas invertidas),
    /// a operação retorna falha Result com código semântico sem lançar exceções.
    /// </summary>
    [Fact]
    public void Calculate_WithInvalidParameters_ShouldReturnFailureResult()
    {
        // Arrange
        var startDate = new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc); // Invertida
        var currentDate = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var invalidDatesResult = _calculator.Calculate(1000m, 500m, startDate, endDate, currentDate);
        var negativeBudgetResult = _calculator.Calculate(-100m, 50m, endDate, startDate, currentDate);
        var negativeSpendResult = _calculator.Calculate(1000m, -50m, endDate, startDate, currentDate);

        // Assert
        invalidDatesResult.IsFailure.Should().BeTrue();
        invalidDatesResult.Error.Code.Should().Be("Pacing.InvalidDateRange");

        negativeBudgetResult.IsFailure.Should().BeTrue();
        negativeBudgetResult.Error.Code.Should().Be("Pacing.InvalidTargetBudget");

        negativeSpendResult.IsFailure.Should().BeTrue();
        negativeSpendResult.Error.Code.Should().Be("Pacing.InvalidCurrentSpend");
    }
}
