using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Ftux.DTOs;
using Tenants.Application.Integrations.Repositories;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Users.Repositories;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Application.Ftux.Queries.GetTenantFtuxStatus;

/// <summary>
/// Manipulador da consulta <see cref="GetTenantFtuxStatusQuery"/> que consolida os indicadores dos 4 passos essenciais de FTUX.
/// </summary>
public sealed class GetTenantFtuxStatusQueryHandler : IQueryHandler<GetTenantFtuxStatusQuery, TenantFtuxStatusDto>
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IConnectedAdAccountRepository _adAccountRepository;
    private readonly ITenantUserRepository _userRepository;
    private readonly ISquadRepository _squadRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantFtuxStatusQueryHandler"/>.
    /// </summary>
    /// <param name="workspaceRepository">Repositório de workspaces do inquilino.</param>
    /// <param name="adAccountRepository">Repositório de contas de anúncios conectadas.</param>
    /// <param name="userRepository">Repositório de colaboradores da agência.</param>
    /// <param name="squadRepository">Repositório de squads do inquilino.</param>
    public GetTenantFtuxStatusQueryHandler(
        IWorkspaceRepository workspaceRepository,
        IConnectedAdAccountRepository adAccountRepository,
        ITenantUserRepository userRepository,
        ISquadRepository squadRepository)
    {
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
        _adAccountRepository = adAccountRepository ?? throw new ArgumentNullException(nameof(adAccountRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _squadRepository = squadRepository ?? throw new ArgumentNullException(nameof(squadRepository));
    }

    /// <inheritdoc />
    public async Task<Result<TenantFtuxStatusDto>> Handle(
        GetTenantFtuxStatusQuery query,
        CancellationToken cancellationToken)
    {
        var workspaces = await _workspaceRepository.GetAllAsync(null, cancellationToken);
        var workspacesCount = workspaces.Count;

        var adAccountsCount = await _adAccountRepository.CountAsync(cancellationToken);

        var users = await _userRepository.GetAllAsync(null, cancellationToken);
        var usersCount = users.Count;

        var squads = await _squadRepository.GetAllAsync(null, cancellationToken);
        var squadsCount = squads.Count;

        // Regras de cálculo do progresso:
        // Passo 1: Provisionado (sempre true no contexto ativo do tenant) -> 25%
        // Passo 2: Cadastre seu 1º Cliente (Workspace) -> +25%
        // Passo 3: Conecte sua 1ª Conta de Anúncios (Meta ou Google) -> +25%
        // Passo 4: Convide um gestor ou crie seu 1º Squad -> +25%
        var step1Completed = true;
        var step2Completed = workspacesCount > 0;
        var step3Completed = adAccountsCount > 0;
        var step4Completed = usersCount > 1 || squadsCount > 0;

        var progress = 25;
        if (step2Completed) progress += 25;
        if (step3Completed) progress += 25;
        if (step4Completed) progress += 25;

        var isCompleted = progress >= 100;

        var statusDto = new TenantFtuxStatusDto(
            IsProvisioned: true,
            WorkspacesCount: workspacesCount,
            ConnectedAdAccountsCount: adAccountsCount,
            TeamMembersCount: usersCount,
            SquadsCount: squadsCount,
            Step1Completed: step1Completed,
            Step2Completed: step2Completed,
            Step3Completed: step3Completed,
            Step4Completed: step4Completed,
            ProgressPercentage: progress,
            IsCompleted: isCompleted);

        return Result<TenantFtuxStatusDto>.Success(statusDto);
    }
}
