using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Application.Integrations;

/// <summary>Exporta IDs de workspaces autorizados para leitura no tenant atual.</summary>
/// <param name="user">Usuário da requisição.</param>
/// <param name="workspaces">Repositório do módulo Tenants.</param>
/// <param name="sender">Mediador de autorização.</param>
public sealed class GetAnalyticsWorkspaceIdsQueryHandler(ICurrentUserContext user, IWorkspaceRepository workspaces, ISender sender)
    : IQueryHandler<GetAnalyticsWorkspaceIdsQuery, IReadOnlyList<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Guid>>> Handle(GetAnalyticsWorkspaceIdsQuery request, CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated || user.UserId is null)
            return Result<IReadOnlyList<Guid>>.Failure(Error.Unauthorized("Auth.Unauthorized", "Autenticação válida necessária."));
        if (request.WorkspaceId.HasValue)
        {
            if (request.WorkspaceId == Guid.Empty)
                return Result<IReadOnlyList<Guid>>.Failure(Error.Validation("Dashboard.InvalidWorkspace", "Workspace inválido."));
            var access = await sender.Send(new ValidateIntegrationWorkspaceQuery(request.WorkspaceId.Value, Manage: false), cancellationToken);
            if (access.IsFailure) return Result<IReadOnlyList<Guid>>.Failure(access.Error);
            return access.Value ? Result<IReadOnlyList<Guid>>.Success([request.WorkspaceId.Value])
                : Result<IReadOnlyList<Guid>>.Failure(Error.Forbidden("Dashboard.Forbidden", "Sem permissão para visualizar este workspace."));
        }
        var ids = new List<Guid>();
        foreach (var workspace in await workspaces.GetAllAsync(true, cancellationToken))
        {
            var access = await sender.Send(new ValidateIntegrationWorkspaceQuery(workspace.Id, Manage: false), cancellationToken);
            if (access.IsFailure) return Result<IReadOnlyList<Guid>>.Failure(access.Error);
            if (access.Value) ids.Add(workspace.Id);
        }
        return Result<IReadOnlyList<Guid>>.Success(ids);
    }
}
