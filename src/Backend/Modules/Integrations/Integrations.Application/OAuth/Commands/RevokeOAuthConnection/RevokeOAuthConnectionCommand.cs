using BuildingBlocks.Application.Messaging;

namespace Integrations.Application.OAuth.Commands.RevokeOAuthConnection;

/// <summary>
/// Comando para revogar a autorização e desconectar uma integração OAuth de um workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace.</param>
/// <param name="Platform">Nome da plataforma (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
public sealed record RevokeOAuthConnectionCommand(
    Guid WorkspaceId,
    string Platform) : ICommand;
