using BuildingBlocks.Application.Messaging;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;

/// <summary>
/// Inicia o fluxo OAuth orgânico para uma plataforma, retornando URL de autorização e state.
/// </summary>
/// <param name="WorkspaceId">Workspace que receberá a conexão.</param>
/// <param name="Platform">Rede orgânica alvo.</param>
/// <param name="RedirectUri">URI de retorno registrada no aplicativo da rede.</param>
public sealed record StartOrganicAuthorizationQuery(
    Guid WorkspaceId,
    OrganicPlatform Platform,
    string RedirectUri) : IQuery<StartOrganicAuthorizationResponse>;
