using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Campaigns.Commands;

/// <summary>
/// Tipo de ação em lote executável sobre campanhas publicitárias.
/// </summary>
public enum BulkCampaignActionType
{
    /// <summary>
    /// Ativa uma campanha pausada.
    /// </summary>
    Activate = 1,

    /// <summary>
    /// Pausa uma campanha ativa.
    /// </summary>
    Pause = 2,

    /// <summary>
    /// Reajusta o orçamento diário da campanha (fixo ou percentual).
    /// </summary>
    AdjustBudget = 3
}

/// <summary>
/// Item individual de operação dentro de um comando de lote.
/// </summary>
/// <param name="CampaignId">Identificador único da campanha no banco do inquilino.</param>
/// <param name="Action">Ação a ser executada sobre a campanha.</param>
/// <param name="DailyBudget">Novo valor absoluto do orçamento diário (quando fixo).</param>
/// <param name="PercentageChange">Variação percentual aplicada ao orçamento (ex: -20 para reduzir 20%, +15 para aumentar 15%).</param>
/// <param name="Reason">Justificativa ou contexto opcional da operação.</param>
public sealed record BulkCampaignOperationItem(
    Guid CampaignId,
    BulkCampaignActionType Action,
    decimal? DailyBudget = null,
    decimal? PercentageChange = null,
    string? Reason = null);

/// <summary>
/// Comando in-memory para orquestrar operações em lote com tolerância a falhas parciais em campanhas multiplataforma.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace associado.</param>
/// <param name="Operations">Coleção de operações a serem aplicadas no lote.</param>
public sealed record BulkCampaignOperationCommand(
    Guid WorkspaceId,
    IReadOnlyList<BulkCampaignOperationItem> Operations) : ICommand<BulkCampaignOperationResultDto>;

/// <summary>
/// Detalhes de um item do lote que foi processado com sucesso.
/// </summary>
/// <param name="CampaignId">Identificador da campanha alterada.</param>
/// <param name="CampaignName">Nome amigável da campanha.</param>
/// <param name="Platform">Rede de anúncios da campanha (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
/// <param name="Action">Ação aplicada com sucesso.</param>
/// <param name="PreviousStatus">Status anterior da campanha.</param>
/// <param name="NewStatus">Novo status da campanha.</param>
/// <param name="PreviousDailyBudget">Orçamento diário anterior.</param>
/// <param name="NewDailyBudget">Novo orçamento diário configurado.</param>
public sealed record BulkOperationSuccessItemDto(
    Guid CampaignId,
    string CampaignName,
    string Platform,
    BulkCampaignActionType Action,
    CampaignStatus PreviousStatus,
    CampaignStatus NewStatus,
    decimal? PreviousDailyBudget,
    decimal? NewDailyBudget);

/// <summary>
/// Detalhes de um item do lote que falhou durante o processamento.
/// </summary>
/// <param name="CampaignId">Identificador da campanha alvo da tentativa.</param>
/// <param name="Action">Ação solicitada que falhou.</param>
/// <param name="ErrorCode">Código semântico do erro de negócio.</param>
/// <param name="ErrorMessage">Mensagem descritiva do motivo da falha.</param>
public sealed record BulkOperationFailureItemDto(
    Guid CampaignId,
    BulkCampaignActionType Action,
    string ErrorCode,
    string ErrorMessage);

/// <summary>
/// Resultado consolidado da operação em lote com tolerância a falhas parciais.
/// </summary>
public sealed class BulkCampaignOperationResultDto
{
    /// <summary>
    /// Total de operações solicitadas no lote.
    /// </summary>
    public int TotalRequested { get; init; }

    /// <summary>
    /// Quantidade de operações processadas com êxito.
    /// </summary>
    public int TotalSucceeded { get; init; }

    /// <summary>
    /// Quantidade de operações que falharam individualmente.
    /// </summary>
    public int TotalFailed { get; init; }

    /// <summary>
    /// Lista de itens alterados com sucesso.
    /// </summary>
    public IReadOnlyList<BulkOperationSuccessItemDto> SucceededItems { get; init; } = Array.Empty<BulkOperationSuccessItemDto>();

    /// <summary>
    /// Lista de itens que falharam e seus respectivos erros.
    /// </summary>
    public IReadOnlyList<BulkOperationFailureItemDto> FailedItems { get; init; } = Array.Empty<BulkOperationFailureItemDto>();
}
