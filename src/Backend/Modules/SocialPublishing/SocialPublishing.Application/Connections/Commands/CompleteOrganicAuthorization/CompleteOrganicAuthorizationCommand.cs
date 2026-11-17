using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;

/// <summary>
/// Conclui o fluxo OAuth orgânico trocando o código, cifrando tokens e persistindo a conexão.
/// </summary>
/// <param name="WorkspaceId">Workspace que receberá ou atualizará a conexão.</param>
/// <param name="Code">Código de autorização retornado pela rede.</param>
/// <param name="State">State anti-CSRF emitido na etapa de authorize.</param>
/// <param name="RedirectUri">URI de retorno utilizada no fluxo.</param>
/// <param name="ProviderError">Erro retornado pelo provedor OAuth, se houver.</param>
[RequireTenantPermission(TenantPermission.SocialConnectionsManage, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record CompleteOrganicAuthorizationCommand(
    Guid WorkspaceId,
    string Code,
    string State,
    string RedirectUri,
    string? ProviderError = null) : ICommand<CompleteOrganicAuthorizationResponse>;
