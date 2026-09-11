namespace Tenants.Application.Rbac.DTOs;

/// <summary>
/// DTO representando o conjunto de permissões efetivas do usuário autenticado no inquilino.
/// </summary>
/// <param name="UserId">Identificador do usuário.</param>
/// <param name="Role">Papel funcional do usuário no inquilino.</param>
/// <param name="Permissions">Lista de permissões ativas atribuídas ao usuário.</param>
public sealed record TenantUserPermissionsDto(
    Guid UserId,
    string Role,
    IReadOnlyList<string> Permissions);
