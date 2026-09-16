using Analytics.Application.Copilot.DTOs;
using Analytics.Domain.Copilot;
using BuildingBlocks.Application.Messaging;

namespace Analytics.Application.Copilot.Commands.ExecuteCopilotRecommendation;

/// <summary>
/// Comando para execução em 1 clique de uma recomendação emitida pelo Copiloto de IA.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace.</param>
/// <param name="ActionId">Identificador único da ação.</param>
/// <param name="ActionType">Tipo da ação a executar.</param>
/// <param name="TargetEntityId">Identificador da entidade alvo.</param>
/// <param name="TargetEntityName">Nome descritivo da entidade alvo.</param>
/// <param name="Platform">Plataforma de mídia.</param>
/// <param name="Title">Título da ação.</param>
/// <param name="Description">Descrição técnica da ação.</param>
/// <param name="Parameters">Parâmetros adicionais da operação.</param>
public sealed record ExecuteCopilotRecommendationCommand(
    Guid WorkspaceId,
    Guid ActionId,
    CopilotActionType ActionType,
    Guid TargetEntityId,
    string TargetEntityName,
    string Platform,
    string Title,
    string Description,
    IReadOnlyDictionary<string, string>? Parameters = null) : ICommand<ExecuteCopilotActionResultDto>;
