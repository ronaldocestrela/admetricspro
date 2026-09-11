using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Rbac.Attributes;

/// <summary>
/// Atributo decorador para comandos e consultas que requerem permissões operacionais granulares do inquilino.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RequireTenantPermissionAttribute : Attribute
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="RequireTenantPermissionAttribute"/>.
    /// </summary>
    /// <param name="permission">Permissão requerida para executar a requisição.</param>
    public RequireTenantPermissionAttribute(TenantPermission permission)
    {
        Permission = permission;
    }

    /// <summary>
    /// Obtém a permissão necessária.
    /// </summary>
    public TenantPermission Permission { get; }

    /// <summary>
    /// Nome da propriedade na requisição que fornece o identificador de Workspace para validação contextual de carteira.
    /// </summary>
    public string? WorkspaceIdProperty { get; init; }
}
