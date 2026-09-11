using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Rbac.Models;

namespace Tenants.Application.Rbac.Services;

/// <summary>
/// Contrato do avaliador central de autorização e controle de acesso baseado em papéis (RBAC) granular do inquilino.
/// Avalia se um usuário possui privilégios para executar determinada ação dentro de seu contexto e carteira.
/// </summary>
public interface IPermissionEvaluator
{
    /// <summary>
    /// Avalia se o colaborador especificado possui autorização para executar a permissão requerida no contexto fornecido.
    /// </summary>
    /// <param name="userId">Identificador do colaborador/usuário do inquilino.</param>
    /// <param name="permission">Permissão canônica solicitada.</param>
    /// <param name="context">Contexto operacional contendo eventuais identificadores de workspace, squad ou orçamento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo booleano true se autorizado, ou falha de negócio/validação com código semântico.</returns>
    Task<Result<bool>> HasPermissionAsync(
        Guid userId,
        TenantPermission permission,
        PermissionContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o conjunto canônico de permissões associadas a um papel funcional do inquilino.
    /// </summary>
    /// <param name="role">Papel funcional do inquilino.</param>
    /// <returns>Conjunto de permissões padrão atribuídas ao papel.</returns>
    IReadOnlySet<TenantPermission> GetPermissionsForRole(TenantRole role);

    /// <summary>
    /// Obtém todas as permissões efetivas atribuídas ao colaborador no inquilino.
    /// </summary>
    /// <param name="userId">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Conjunto de permissões ativas do colaborador.</returns>
    Task<Result<IReadOnlySet<TenantPermission>>> GetPermissionsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
