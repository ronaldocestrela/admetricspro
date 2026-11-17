using BuildingBlocks.Application.Messaging;

namespace SocialPublishing.Contracts.Boost;

/// <summary>
/// Solicita a criação de um rascunho local pausado de anúncio pago a partir de um post orgânico com tração.
/// O handler concreto vive em Integrations.Application.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="SuggestionId">Identificador da sugestão de impulsionamento aceita.</param>
/// <param name="OrganicPostId">Post orgânico de origem.</param>
/// <param name="Network">Rede orgânica (Instagram, FacebookPage ou TikTok).</param>
/// <param name="Caption">Legenda do post orgânico como ponto de partida do criativo.</param>
/// <param name="MediaObjectKey">Chave do objeto de mídia primária; nula quando indisponível.</param>
public sealed record CreatePaidBoostDraftCommand(
    Guid WorkspaceId,
    Guid SuggestionId,
    Guid OrganicPostId,
    string Network,
    string Caption,
    string? MediaObjectKey) : ICommand<Guid>;
