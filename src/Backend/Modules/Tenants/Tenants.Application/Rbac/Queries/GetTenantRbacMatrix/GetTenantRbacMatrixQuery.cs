using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Rbac.DTOs;
using Tenants.Application.Rbac.Services;

namespace Tenants.Application.Rbac.Queries.GetTenantRbacMatrix;

/// <summary>
/// Consulta para obter a matriz completa de papéis e permissões canônicas para visualização na UI e governança.
/// </summary>
public sealed record GetTenantRbacMatrixQuery : IQuery<TenantRbacMatrixDto>;

/// <summary>
/// Manipulador da consulta <see cref="GetTenantRbacMatrixQuery"/>.
/// </summary>
public sealed class GetTenantRbacMatrixQueryHandler : IQueryHandler<GetTenantRbacMatrixQuery, TenantRbacMatrixDto>
{
    private readonly IPermissionEvaluator _permissionEvaluator;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantRbacMatrixQueryHandler"/>.
    /// </summary>
    /// <param name="permissionEvaluator">Avaliador central de permissões.</param>
    public GetTenantRbacMatrixQueryHandler(IPermissionEvaluator permissionEvaluator)
    {
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
    }

    /// <inheritdoc />
    public Task<Result<TenantRbacMatrixDto>> Handle(
        GetTenantRbacMatrixQuery query,
        CancellationToken cancellationToken)
    {
        var rolesDict = new Dictionary<string, IReadOnlyList<string>>();

        foreach (TenantRole role in Enum.GetValues<TenantRole>())
        {
            var permissions = _permissionEvaluator.GetPermissionsForRole(role);
            rolesDict[role.ToString()] = permissions.Select(p => p.ToString()).ToList();
        }

        var allPermissions = Enum.GetValues<TenantPermission>()
            .Select(p => p.ToString())
            .ToList();

        var dto = new TenantRbacMatrixDto(rolesDict, allPermissions);

        return Task.FromResult(Result<TenantRbacMatrixDto>.Success(dto));
    }
}
