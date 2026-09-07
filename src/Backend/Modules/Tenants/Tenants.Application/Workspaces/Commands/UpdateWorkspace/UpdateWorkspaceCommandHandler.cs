using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Persistence;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Application.Workspaces.Commands.UpdateWorkspace;

/// <summary>
/// Manipulador responsável pela validação e atualização cadastral de um <see cref="Workspace"/>.
/// </summary>
public sealed class UpdateWorkspaceCommandHandler : ICommandHandler<UpdateWorkspaceCommand>
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly ITenantUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="UpdateWorkspaceCommandHandler"/>.
    /// </summary>
    /// <param name="workspaceRepository">Repositório de workspaces.</param>
    /// <param name="unitOfWork">Unidade de trabalho do banco de dados do inquilino.</param>
    public UpdateWorkspaceCommandHandler(
        IWorkspaceRepository workspaceRepository,
        ITenantUnitOfWork unitOfWork)
    {
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(request.Id, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure(
                Error.NotFound("Workspace.NotFound", $"Workspace com identificador '{request.Id}' não encontrado."));
        }

        var sanitizedDocument = TaxDocumentValidator.Sanitize(request.CnpjOrCpf);
        var documentInUse = await _workspaceRepository.ExistsByCnpjOrCpfAsync(sanitizedDocument, request.Id, cancellationToken);
        if (documentInUse)
        {
            return Result.Failure(
                Error.Conflict("Workspace.CnpjOrCpfAlreadyExists", "Já existe outro cliente/workspace cadastrado com este CPF/CNPJ neste inquilino."));
        }

        var updateResult = workspace.UpdateDetails(
            request.Name,
            request.CnpjOrCpf,
            request.MonthlyAdSpendBudget,
            request.Segment);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        _workspaceRepository.Update(workspace);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
