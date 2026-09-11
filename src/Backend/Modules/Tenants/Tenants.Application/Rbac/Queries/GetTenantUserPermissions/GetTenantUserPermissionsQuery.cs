using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Rbac.DTOs;
using Tenants.Application.Rbac.Services;
using Tenants.Application.Users.Repositories;

namespace Tenants.Application.Rbac.Queries.GetTenantUserPermissions;

/// <summary>
/// Consulta para obter a lista de permissões ativas de um colaborador específico.
/// </summary>
/// <param name="UserId">Identificador do colaborador.</param>
public sealed record GetTenantUserPermissionsQuery(Guid UserId) : IQuery<TenantUserPermissionsDto>;

/// <summary>
/// Manipulador da consulta <see cref="GetTenantUserPermissionsQuery"/>.
/// </summary>
public sealed class GetTenantUserPermissionsQueryHandler : IQueryHandler<GetTenantUserPermissionsQuery, TenantUserPermissionsDto>
{
    private readonly ITenantUserRepository _userRepository;
    private readonly IPermissionEvaluator _permissionEvaluator;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantUserPermissionsQueryHandler"/>.
    /// </summary>
    /// <param name="userRepository">Repositório de usuários do inquilino.</param>
    /// <param name="permissionEvaluator">Avaliador central de permissões.</param>
    public GetTenantUserPermissionsQueryHandler(
        ITenantUserRepository userRepository,
        IPermissionEvaluator permissionEvaluator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
    }

    /// <inheritdoc />
    public async Task<Result<TenantUserPermissionsDto>> Handle(
        GetTenantUserPermissionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return Result<TenantUserPermissionsDto>.Failure(
                Error.NotFound("TenantUser.NotFound", "Colaborador não localizado no inquilino."));
        }

        if (!user.IsActive)
        {
            return Result<TenantUserPermissionsDto>.Failure(
                Error.Forbidden("TenantUser.Inactive", "Colaborador inativo."));
        }

        var permissions = _permissionEvaluator.GetPermissionsForRole(user.Role);
        var permissionsList = permissions.Select(p => p.ToString()).ToList();

        var dto = new TenantUserPermissionsDto(user.Id, user.Role.ToString(), permissionsList);

        return Result<TenantUserPermissionsDto>.Success(dto);
    }
}
