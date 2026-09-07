using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Squads.DTOs;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Users.Repositories;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Application.Squads.Queries.GetSquadById;

/// <summary>
/// Manipulador responsável por carregar os dados detalhados de um squad, seus membros e workspaces da carteira.
/// </summary>
public sealed class GetSquadByIdQueryHandler : IQueryHandler<GetSquadByIdQuery, SquadDetailsDto>
{
    private readonly ISquadRepository _squadRepository;
    private readonly ITenantUserRepository _userRepository;
    private readonly IWorkspaceRepository _workspaceRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetSquadByIdQueryHandler"/>.
    /// </summary>
    /// <param name="squadRepository">Repositório de squads.</param>
    /// <param name="userRepository">Repositório de usuários do inquilino.</param>
    /// <param name="workspaceRepository">Repositório de workspaces do inquilino.</param>
    public GetSquadByIdQueryHandler(
        ISquadRepository squadRepository,
        ITenantUserRepository userRepository,
        IWorkspaceRepository workspaceRepository)
    {
        _squadRepository = squadRepository ?? throw new ArgumentNullException(nameof(squadRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
    }

    /// <inheritdoc />
    public async Task<Result<SquadDetailsDto>> Handle(GetSquadByIdQuery request, CancellationToken cancellationToken)
    {
        var squad = await _squadRepository.GetByIdAsync(request.SquadId, cancellationToken);
        if (squad is null)
        {
            return Result<SquadDetailsDto>.Failure(
                Error.NotFound("Squad.NotFound", $"Squad com identificador '{request.SquadId}' não encontrado."));
        }

        var memberDtos = new List<SquadMemberDto>();
        foreach (var member in squad.Members)
        {
            var user = await _userRepository.GetByIdAsync(member.UserId, cancellationToken);
            if (user is not null)
            {
                memberDtos.Add(new SquadMemberDto(
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Role.ToString(),
                    member.JoinedAtUtc));
            }
        }

        var workspaceDtos = new List<SquadWorkspaceDto>();
        foreach (var squadWorkspace in squad.Workspaces)
        {
            var ws = await _workspaceRepository.GetByIdAsync(squadWorkspace.WorkspaceId, cancellationToken);
            if (ws is not null)
            {
                workspaceDtos.Add(new SquadWorkspaceDto(
                    ws.Id,
                    ws.Name,
                    ws.CnpjOrCpf,
                    ws.MonthlyAdSpendBudget,
                    ws.Segment,
                    squadWorkspace.AssignedAtUtc));
            }
        }

        var dto = new SquadDetailsDto(
            squad.Id,
            squad.Name,
            squad.Description,
            squad.IsActive,
            memberDtos,
            workspaceDtos,
            squad.CreatedAtUtc,
            squad.UpdatedAtUtc);

        return Result<SquadDetailsDto>.Success(dto);
    }
}
