using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Squads.Services;
using Tenants.Application.Users.Repositories;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Infrastructure.Squads;

/// <summary>
/// Implementação do serviço de governança e isolamento por carteira (<see cref="IUserPortfolioService"/>).
/// Aplica as regras de RBAC hierárquico e vinculação matricial de squads a clientes gerenciados.
/// </summary>
public sealed class UserPortfolioService : IUserPortfolioService
{
    private readonly ITenantUserRepository _userRepository;
    private readonly ISquadRepository _squadRepository;
    private readonly IWorkspaceRepository _workspaceRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="UserPortfolioService"/>.
    /// </summary>
    /// <param name="userRepository">Repositório de usuários do inquilino.</param>
    /// <param name="squadRepository">Repositório de squads do inquilino.</param>
    /// <param name="workspaceRepository">Repositório de workspaces do inquilino.</param>
    public UserPortfolioService(
        ITenantUserRepository userRepository,
        ISquadRepository squadRepository,
        IWorkspaceRepository workspaceRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _squadRepository = squadRepository ?? throw new ArgumentNullException(nameof(squadRepository));
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> HasAccessToWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Result<bool>.Failure(
                Error.Validation("User.InvalidId", "O identificador do usuário não pode ser vazio."));
        }

        if (workspaceId == Guid.Empty)
        {
            return Result<bool>.Failure(
                Error.Validation("Workspace.InvalidId", "O identificador do workspace não pode ser vazio."));
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<bool>.Failure(
                Error.NotFound("User.NotFound", $"Colaborador com identificador '{userId}' não encontrado no inquilino."));
        }

        if (!user.IsActive)
        {
            return Result<bool>.Failure(
                Error.Validation("User.Inactive", "Colaborador com conta inativa não possui acesso operacional."));
        }

        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace is null)
        {
            return Result<bool>.Failure(
                Error.NotFound("Workspace.NotFound", $"Workspace com identificador '{workspaceId}' não encontrado."));
        }

        // Papéis com governança executiva global (Owner e Admin) possuem acesso a todos os workspaces do tenant
        if (user.Role == TenantRole.Owner || user.Role == TenantRole.Admin)
        {
            return Result<bool>.Success(true);
        }

        // Papéis operacionais: acesso restrito aos clientes alocados em seus squads ativos
        var userSquads = await _squadRepository.GetSquadsByUserIdAsync(userId, cancellationToken);
        var hasAccess = userSquads
            .Where(s => s.IsActive)
            .SelectMany(s => s.Workspaces)
            .Any(w => w.WorkspaceId == workspaceId);

        return Result<bool>.Success(hasAccess);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Guid>>> GetAccessibleWorkspaceIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Result<IReadOnlyList<Guid>>.Failure(
                Error.Validation("User.InvalidId", "O identificador do usuário não pode ser vazio."));
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<IReadOnlyList<Guid>>.Failure(
                Error.NotFound("User.NotFound", $"Colaborador com identificador '{userId}' não encontrado no inquilino."));
        }

        if (!user.IsActive)
        {
            return Result<IReadOnlyList<Guid>>.Failure(
                Error.Validation("User.Inactive", "Colaborador com conta inativa não possui acesso operacional."));
        }

        // Papéis executivos têm visibilidade sobre todos os workspaces do inquilino
        if (user.Role == TenantRole.Owner || user.Role == TenantRole.Admin)
        {
            var allWorkspaces = await _workspaceRepository.GetAllAsync(activeOnly: null, cancellationToken);
            var allIds = allWorkspaces.Select(w => w.Id).ToList();
            return Result<IReadOnlyList<Guid>>.Success(allIds);
        }

        // Papéis operacionais consolidam as carteiras dos squads em que estão alocados
        var userSquads = await _squadRepository.GetSquadsByUserIdAsync(userId, cancellationToken);
        var accessibleIds = userSquads
            .Where(s => s.IsActive)
            .SelectMany(s => s.Workspaces)
            .Select(w => w.WorkspaceId)
            .Distinct()
            .ToList();

        return Result<IReadOnlyList<Guid>>.Success(accessibleIds);
    }
}
