using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Rbac.Models;
using Tenants.Application.Rbac.Services;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Squads.Services;
using Tenants.Application.Users.Repositories;

namespace Tenants.Infrastructure.Rbac;

/// <summary>
/// Implementação do avaliador central de controle de acesso baseado em papéis (RBAC) granular para inquilinos.
/// Aplica as regras de governança e isolamento de carteira para Owner, Admin, SquadLeader, MediaManager, Analyst e Guest.
/// </summary>
public sealed class TenantPermissionEvaluator : IPermissionEvaluator
{
    private readonly ITenantUserRepository _userRepository;
    private readonly IUserPortfolioService _portfolioService;
    private readonly ISquadRepository _squadRepository;
    private readonly decimal _maxBudgetLimitForMediaManager;

    private static readonly Dictionary<TenantRole, HashSet<TenantPermission>> RolePermissionsMap = new()
    {
        [TenantRole.Owner] =
        [
            TenantPermission.ManageBilling,
            TenantPermission.ManageSettings,
            TenantPermission.ViewAuditLogs,
            TenantPermission.ManageTeamMembers,
            TenantPermission.ManageSquads,
            TenantPermission.ManageWorkspaces,
            TenantPermission.ViewCampaigns,
            TenantPermission.EditCampaigns,
            TenantPermission.ManageAutomations,
            TenantPermission.ViewFinancialMargins,
            TenantPermission.ExportReports
        ],
        [TenantRole.Admin] =
        [
            TenantPermission.ManageSettings,
            TenantPermission.ViewAuditLogs,
            TenantPermission.ManageTeamMembers,
            TenantPermission.ManageSquads,
            TenantPermission.ManageWorkspaces,
            TenantPermission.ViewCampaigns,
            TenantPermission.EditCampaigns,
            TenantPermission.ManageAutomations,
            TenantPermission.ViewFinancialMargins,
            TenantPermission.ExportReports
        ],
        [TenantRole.SquadLeader] =
        [
            TenantPermission.ManageTeamMembers,
            TenantPermission.ManageSquads,
            TenantPermission.ManageWorkspaces,
            TenantPermission.ViewCampaigns,
            TenantPermission.EditCampaigns,
            TenantPermission.ManageAutomations,
            TenantPermission.ViewFinancialMargins,
            TenantPermission.ExportReports
        ],
        [TenantRole.MediaManager] =
        [
            TenantPermission.ViewCampaigns,
            TenantPermission.EditCampaigns,
            TenantPermission.ManageAutomations,
            TenantPermission.ExportReports
        ],
        [TenantRole.Analyst] =
        [
            TenantPermission.ViewCampaigns,
            TenantPermission.ExportReports
        ],
        [TenantRole.Guest] =
        [
            TenantPermission.ViewCampaigns,
            TenantPermission.ExportReports
        ]
    };

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantPermissionEvaluator"/>.
    /// </summary>
    /// <param name="userRepository">Repositório de usuários do inquilino.</param>
    /// <param name="portfolioService">Serviço de governança e isolamento de carteira.</param>
    /// <param name="squadRepository">Repositório de squads.</param>
    /// <param name="maxBudgetLimitForMediaManager">Teto máximo orçamentário permitido para edição de campanhas por Gestor de Mídia (default: R$ 50.000,00).</param>
    public TenantPermissionEvaluator(
        ITenantUserRepository userRepository,
        IUserPortfolioService portfolioService,
        ISquadRepository squadRepository,
        decimal maxBudgetLimitForMediaManager = 50000m)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _portfolioService = portfolioService ?? throw new ArgumentNullException(nameof(portfolioService));
        _squadRepository = squadRepository ?? throw new ArgumentNullException(nameof(squadRepository));
        _maxBudgetLimitForMediaManager = maxBudgetLimitForMediaManager;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> HasPermissionAsync(
        Guid userId,
        TenantPermission permission,
        PermissionContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Result<bool>.Failure(Error.Validation("TenantUser.InvalidId", "Identificador de usuário inválido."));
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<bool>.Failure(Error.NotFound("TenantUser.NotFound", "Usuário não localizado no inquilino."));
        }

        if (!user.IsActive)
        {
            return Result<bool>.Failure(Error.Forbidden("TenantUser.Inactive", "O usuário informado está inativo."));
        }

        // 1. Verificação na matriz canônica de papel
        var allowedPermissions = RolePermissionsMap.TryGetValue(user.Role, out var permissions)
            ? permissions
            : [];

        if (!allowedPermissions.Contains(permission))
        {
            return Result<bool>.Success(false);
        }

        // 2. Owner e Admin têm acesso irrestrito global (exceto regra já filtrada de Billing para Admin)
        if (user.Role is TenantRole.Owner or TenantRole.Admin)
        {
            return Result<bool>.Success(true);
        }

        // 3. Regra de blindagem estrita para Client Guest (Cliente Final)
        if (user.Role == TenantRole.Guest)
        {
            if (permission is TenantPermission.ViewFinancialMargins or TenantPermission.ManageAutomations or TenantPermission.ViewAuditLogs)
            {
                return Result<bool>.Success(false);
            }
        }

        // 4. Validação de teto orçamentário seguro para Gestor de Mídia
        if (user.Role == TenantRole.MediaManager && permission == TenantPermission.EditCampaigns)
        {
            if (context?.ProposedBudget.HasValue == true && context.ProposedBudget.Value > _maxBudgetLimitForMediaManager)
            {
                return Result<bool>.Success(false);
            }
        }

        // 5. Validação de contexto de Workspace e isolamento por carteira (SquadLeader, MediaManager, Analyst, Guest)
        if (context?.WorkspaceId.HasValue == true)
        {
            var workspaceAccessResult = await _portfolioService.HasAccessToWorkspaceAsync(
                userId,
                context.WorkspaceId.Value,
                cancellationToken);

            if (workspaceAccessResult.IsFailure || !workspaceAccessResult.Value)
            {
                return Result<bool>.Success(false);
            }
        }

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public IReadOnlySet<TenantPermission> GetPermissionsForRole(TenantRole role)
    {
        return RolePermissionsMap.TryGetValue(role, out var permissions)
            ? permissions
            : new HashSet<TenantPermission>();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlySet<TenantPermission>>> GetPermissionsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Result<IReadOnlySet<TenantPermission>>.Failure(
                Error.Validation("TenantUser.InvalidId", "Identificador de usuário inválido."));
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<IReadOnlySet<TenantPermission>>.Failure(
                Error.NotFound("TenantUser.NotFound", "Usuário não localizado no inquilino."));
        }

        if (!user.IsActive)
        {
            return Result<IReadOnlySet<TenantPermission>>.Failure(
                Error.Forbidden("TenantUser.Inactive", "O usuário informado está inativo."));
        }

        var permissions = GetPermissionsForRole(user.Role);
        return Result<IReadOnlySet<TenantPermission>>.Success(permissions);
    }
}
