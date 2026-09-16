using BuildingBlocks.Application.Messaging;

namespace BuildingBlocks.Application.Campaigns.Commands;

/// <summary>
/// Comando in-memory para pausar uma campanha publicitária via módulo Integrations.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace associado.</param>
/// <param name="CampaignId">Identificador da campanha a ser pausada.</param>
/// <param name="Reason">Motivo descritivo ou nome da regra que solicitou a pausa.</param>
public sealed record PauseCampaignCommand(
    Guid WorkspaceId,
    Guid CampaignId,
    string? Reason = null) : ICommand;

/// <summary>
/// Comando in-memory para pausar um anúncio / criativo específico via módulo Integrations.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace associado.</param>
/// <param name="AdId">Identificador do anúncio a ser pausado.</param>
/// <param name="Reason">Motivo descritivo da pausa.</param>
public sealed record PauseAdCommand(
    Guid WorkspaceId,
    Guid AdId,
    string? Reason = null) : ICommand;

/// <summary>
/// Comando in-memory para reajustar o orçamento diário de uma campanha (fixo ou percentual).
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace associado.</param>
/// <param name="CampaignId">Identificador da campanha com orçamento modificado.</param>
/// <param name="DailyBudget">Novo valor absoluto do orçamento diário (quando fixo).</param>
/// <param name="PercentageChange">Variação percentual aplicada ao orçamento (ex: -20 para reduzir 20%, +15 para aumentar 15%).</param>
public sealed record AdjustCampaignBudgetCommand(
    Guid WorkspaceId,
    Guid CampaignId,
    decimal? DailyBudget = null,
    decimal? PercentageChange = null) : ICommand;

/// <summary>
/// Comando in-memory para transferir/realocar saldo entre duas campanhas publicitárias.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace associado.</param>
/// <param name="SourceCampaignId">Campanha de origem que terá o orçamento reduzido.</param>
/// <param name="TargetCampaignId">Campanha de destino que receberá o saldo adicional.</param>
/// <param name="AmountOrPercentage">Valor numérico da transferência.</param>
/// <param name="IsPercentage">Verdadeiro se AmountOrPercentage for um percentual do orçamento da origem, falso se for valor monetário fixo.</param>
public sealed record ReallocateBudgetCommand(
    Guid WorkspaceId,
    Guid SourceCampaignId,
    Guid TargetCampaignId,
    decimal AmountOrPercentage,
    bool IsPercentage) : ICommand;
