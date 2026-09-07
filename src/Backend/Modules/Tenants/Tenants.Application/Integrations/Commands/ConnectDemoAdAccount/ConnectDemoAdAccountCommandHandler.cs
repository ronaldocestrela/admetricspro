using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Integrations.Repositories;
using Tenants.Application.Persistence;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Application.Integrations.Commands.ConnectDemoAdAccount;

/// <summary>
/// Manipulador do comando <see cref="ConnectDemoAdAccountCommand"/> que instancia e persiste uma conta de mídia demonstrativa.
/// </summary>
public sealed class ConnectDemoAdAccountCommandHandler : ICommandHandler<ConnectDemoAdAccountCommand, Guid>
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IConnectedAdAccountRepository _adAccountRepository;
    private readonly ITenantUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ConnectDemoAdAccountCommandHandler"/>.
    /// </summary>
    /// <param name="workspaceRepository">Repositório de workspaces do inquilino.</param>
    /// <param name="adAccountRepository">Repositório de contas de anúncios.</param>
    /// <param name="unitOfWork">Unidade de trabalho transacional do inquilino.</param>
    public ConnectDemoAdAccountCommandHandler(
        IWorkspaceRepository workspaceRepository,
        IConnectedAdAccountRepository adAccountRepository,
        ITenantUnitOfWork unitOfWork)
    {
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
        _adAccountRepository = adAccountRepository ?? throw new ArgumentNullException(nameof(adAccountRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(ConnectDemoAdAccountCommand command, CancellationToken cancellationToken)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(command.WorkspaceId, cancellationToken);
        if (workspace is null)
        {
            return Result<Guid>.Failure(
                Error.NotFound("Workspace.NotFound", $"Workspace com identificador '{command.WorkspaceId}' não foi localizado no inquilino."));
        }

        var demoAccountResult = ConnectedAdAccount.CreateDemo(command.WorkspaceId, command.Platform);
        if (demoAccountResult.IsFailure)
        {
            return Result<Guid>.Failure(demoAccountResult.Error);
        }

        var demoAccount = demoAccountResult.Value;
        await _adAccountRepository.AddAsync(demoAccount, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<Guid>.Success(demoAccount.Id);
    }
}
