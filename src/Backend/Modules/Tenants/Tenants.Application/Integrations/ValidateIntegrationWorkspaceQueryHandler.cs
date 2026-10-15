using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Rbac.Models;
using Tenants.Application.Rbac.Services;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Application.Integrations;

/// <summary>Autoriza a operação usando usuário ativo, carteira e workspace do banco corrente.</summary>
/// <param name="currentUser">Usuário autenticado da requisição.</param>
/// <param name="permissions">Avaliador de permissões e carteira.</param>
/// <param name="workspaces">Repositório de workspaces do tenant.</param>
public sealed class ValidateIntegrationWorkspaceQueryHandler(
    ICurrentUserContext currentUser, IPermissionEvaluator permissions, IWorkspaceRepository workspaces)
    : IQueryHandler<ValidateIntegrationWorkspaceQuery, bool>
{
    /// <inheritdoc />
    public async Task<Result<bool>> Handle(ValidateIntegrationWorkspaceQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
            return Result<bool>.Failure(Error.Unauthorized("Auth.Unauthorized", "Autenticação necessária."));
        if (request.WorkspaceId == Guid.Empty)
            return await permissions.HasPermissionAsync(userId, TenantPermission.ManageSettings, cancellationToken: cancellationToken);
        var workspace = await workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace is null || (!workspace.IsActive && !request.IncludeInactive))
            return Result<bool>.Failure(Error.NotFound("Workspace.NotFound", "Workspace ativo não localizado."));
        return await permissions.HasPermissionAsync(userId,
            request.Permission ?? (request.EditCampaigns ? TenantPermission.EditCampaigns : request.Manage ? TenantPermission.ManageWorkspaces : TenantPermission.ViewCampaigns),
            new PermissionContext(WorkspaceId: request.WorkspaceId), cancellationToken);
    }
}
