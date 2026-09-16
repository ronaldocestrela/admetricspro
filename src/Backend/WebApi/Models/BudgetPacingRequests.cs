using System.ComponentModel.DataAnnotations;

namespace WebApi.Models;

/// <summary>
/// Modelo de requisição para simulação sob demanda de velocidade de consumo (pacing) e previsão de fim de mês.
/// </summary>
public sealed record SimulateBudgetPacingApiRequest
{
    /// <summary>
    /// Orçamento total contratado ou planejado.
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "O orçamento deve ser maior que zero.")]
    public decimal TargetBudget { get; init; }

    /// <summary>
    /// Gasto acumulado realizado até o momento.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "O gasto acumulado não pode ser negativo.")]
    public decimal CurrentSpend { get; init; }

    /// <summary>
    /// Data inicial do ciclo de faturamento.
    /// </summary>
    [Required]
    public DateTime StartDateUtc { get; init; }

    /// <summary>
    /// Data de encerramento do ciclo de faturamento.
    /// </summary>
    [Required]
    public DateTime EndDateUtc { get; init; }

    /// <summary>
    /// Data de referência/corte para o cálculo (opcional, padrão: data atual UTC).
    /// </summary>
    public DateTime? AsOfDateUtc { get; init; }

    /// <summary>
    /// Margem de tolerância percentual para o status No Ritmo (opcional, padrão 0.10 para +/- 10%).
    /// </summary>
    [Range(0, 1.0, ErrorMessage = "A tolerância deve estar entre 0 e 100%.")]
    public decimal? TolerancePercentage { get; init; }
}
