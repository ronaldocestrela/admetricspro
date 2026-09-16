using BuildingBlocks.Domain.Automations.Pacing;

namespace Automations.Application.Pacing.DTOs;

/// <summary>
/// DTO que representa o detalhamento do ritmo de investimento de uma campanha individual.
/// </summary>
public sealed record CampaignPacingDto
{
    /// <summary>
    /// Identificador único da campanha.
    /// </summary>
    public Guid CampaignId { get; init; }

    /// <summary>
    /// Nome descritivo da campanha na rede de anúncios.
    /// </summary>
    public string CampaignName { get; init; } = string.Empty;

    /// <summary>
    /// Plataforma de mídia (MetaAds, GoogleAds, TikTokAds, BingAds).
    /// </summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>
    /// Orçamento diário configurado na campanha, se aplicável.
    /// </summary>
    public decimal? DailyBudget { get; init; }

    /// <summary>
    /// Gasto acumulado na campanha durante o ciclo vigente.
    /// </summary>
    public decimal CurrentSpend { get; init; }

    /// <summary>
    /// Status operacional de consumo da campanha (OnTrack, Over, Under).
    /// </summary>
    public PacingStatus Status { get; init; }

    /// <summary>
    /// Razão entre o gasto realizado e o gasto esperado para a campanha.
    /// </summary>
    public decimal PacingRatio { get; init; }
}

/// <summary>
/// DTO com os dados consolidados de pacing, projeção e velocidade de consumo de um Workspace.
/// </summary>
public sealed record WorkspaceBudgetPacingDto
{
    /// <summary>
    /// Identificador único do Workspace (cliente).
    /// </summary>
    public Guid WorkspaceId { get; init; }

    /// <summary>
    /// Nome fantasia ou razão social do Workspace.
    /// </summary>
    public string WorkspaceName { get; init; } = string.Empty;

    /// <summary>
    /// Orçamento mensal contratado ou configurado para o Workspace.
    /// </summary>
    public decimal TargetBudget { get; init; }

    /// <summary>
    /// Gasto total realizado no período.
    /// </summary>
    public decimal CurrentSpend { get; init; }

    /// <summary>
    /// Saldo restante do orçamento no ciclo.
    /// </summary>
    public decimal RemainingBudget { get; init; }

    /// <summary>
    /// Quantidade total de dias no ciclo mensal.
    /// </summary>
    public int TotalDaysInCycle { get; init; }

    /// <summary>
    /// Quantidade de dias já decorridos no ciclo.
    /// </summary>
    public int ElapsedDays { get; init; }

    /// <summary>
    /// Quantidade de dias restantes até o fechamento do mês.
    /// </summary>
    public int RemainingDays { get; init; }

    /// <summary>
    /// Gasto ideal esperado na data de referência.
    /// </summary>
    public decimal ExpectedSpendToDate { get; init; }

    /// <summary>
    /// Razão matemática de pacing (Gasto Realizado / Gasto Esperado).
    /// </summary>
    public decimal PacingRatio { get; init; }

    /// <summary>
    /// Percentual de velocidade de consumo em relação ao ideal.
    /// </summary>
    public decimal PacingPercentage { get; init; }

    /// <summary>
    /// Ritmo médio diário de gasto realizado até o momento.
    /// </summary>
    public decimal ActualDailyRunRate { get; init; }

    /// <summary>
    /// Ritmo diário ideal planejado originalmente.
    /// </summary>
    public decimal IdealDailyRunRate { get; init; }

    /// <summary>
    /// Ritmo diário sugerido para os dias restantes atingirem a meta.
    /// </summary>
    public decimal RequiredDailyRunRate { get; init; }

    /// <summary>
    /// Estimativa de gasto total ao encerramento do ciclo no ritmo atual.
    /// </summary>
    public decimal ProjectedMonthEndSpend { get; init; }

    /// <summary>
    /// Diferença monetária projetada em relação à meta contratada.
    /// </summary>
    public decimal ProjectedVariance { get; init; }

    /// <summary>
    /// Percentual de desvio projetado.
    /// </summary>
    public decimal ProjectedVariancePercentage { get; init; }

    /// <summary>
    /// Classificação de status de pacing (OnTrack, Over, Under).
    /// </summary>
    public PacingStatus Status { get; init; }

    /// <summary>
    /// Recomendação tática orientada a dados.
    /// </summary>
    public string Recommendation { get; init; } = string.Empty;

    /// <summary>
    /// Código ISO da moeda (ex: BRL).
    /// </summary>
    public string Currency { get; init; } = "BRL";

    /// <summary>
    /// Data inicial do ciclo em UTC.
    /// </summary>
    public DateTime CycleStartDateUtc { get; init; }

    /// <summary>
    /// Data final do ciclo em UTC.
    /// </summary>
    public DateTime CycleEndDateUtc { get; init; }

    /// <summary>
    /// Data e hora de corte da consulta em UTC.
    /// </summary>
    public DateTime AsOfDateUtc { get; init; }

    /// <summary>
    /// Detalhamento do ritmo das campanhas ativas do Workspace.
    /// </summary>
    public IReadOnlyList<CampaignPacingDto> CampaignBreakdown { get; init; } = Array.Empty<CampaignPacingDto>();
}

/// <summary>
/// DTO consolidado para visão executiva de carteira de clientes (Portfolio Pacing).
/// </summary>
public sealed record PortfolioPacingSummaryDto
{
    /// <summary>
    /// Quantidade total de clientes/workspaces analisados.
    /// </summary>
    public int TotalWorkspaces { get; init; }

    /// <summary>
    /// Total de clientes com status No Ritmo.
    /// </summary>
    public int OnTrackCount { get; init; }

    /// <summary>
    /// Total de clientes com status Sobreaquecido.
    /// </summary>
    public int OverCount { get; init; }

    /// <summary>
    /// Total de clientes com status Subinvestido.
    /// </summary>
    public int UnderCount { get; init; }

    /// <summary>
    /// Soma dos orçamentos contratados de toda a carteira.
    /// </summary>
    public decimal TotalContractedBudget { get; init; }

    /// <summary>
    /// Soma dos investimentos já realizados na carteira no período.
    /// </summary>
    public decimal TotalCurrentSpend { get; init; }

    /// <summary>
    /// Soma da projeção de fechamento de mês de toda a carteira.
    /// </summary>
    public decimal TotalProjectedSpend { get; init; }

    /// <summary>
    /// Moeda de consolidação.
    /// </summary>
    public string Currency { get; init; } = "BRL";

    /// <summary>
    /// Lista dos workspaces com seus respectivos dados de pacing.
    /// </summary>
    public IReadOnlyList<WorkspaceBudgetPacingDto> Workspaces { get; init; } = Array.Empty<WorkspaceBudgetPacingDto>();
}

/// <summary>
/// Payload para simulação sob demanda de projeção e pacing.
/// </summary>
public sealed record SimulatePacingRequestDto
{
    /// <summary>
    /// Orçamento total planejado.
    /// </summary>
    public decimal TargetBudget { get; init; }

    /// <summary>
    /// Gasto acumulado até o momento.
    /// </summary>
    public decimal CurrentSpend { get; init; }

    /// <summary>
    /// Data de início do ciclo.
    /// </summary>
    public DateTime StartDateUtc { get; init; }

    /// <summary>
    /// Data de encerramento do ciclo.
    /// </summary>
    public DateTime EndDateUtc { get; init; }

    /// <summary>
    /// Data de referência para o cálculo.
    /// </summary>
    public DateTime AsOfDateUtc { get; init; }

    /// <summary>
    /// Margem de tolerância percentual opcional (padrão 0.10 para +/- 10%).
    /// </summary>
    public decimal? TolerancePercentage { get; init; }
}
