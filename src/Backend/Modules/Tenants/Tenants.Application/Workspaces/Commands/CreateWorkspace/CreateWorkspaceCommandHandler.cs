using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Tenants.Queries.GetTenantPlanLimits;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using MediatR;
using Tenants.Application.Persistence;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Application.Workspaces.Commands.CreateWorkspace;

/// <summary>
/// Manipulador responsável pela validação de cotas e persistência na criação de um novo <see cref="Workspace"/>.
/// </summary>
public sealed class CreateWorkspaceCommandHandler : ICommandHandler<CreateWorkspaceCommand, Guid>
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CreateWorkspaceCommandHandler"/>.
    /// </summary>
    /// <param name="workspaceRepository">Repositório operacional de workspaces.</param>
    /// <param name="unitOfWork">Unidade de trabalho do banco dedicado do inquilino.</param>
    /// <param name="tenantContextAccessor">Acessor do contexto de inquilino ativo.</param>
    /// <param name="sender">Mediador in-memory para consulta de limites do catálogo Master.</param>
    public CreateWorkspaceCommandHandler(
        IWorkspaceRepository workspaceRepository,
        ITenantUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor,
        ISender sender)
    {
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        if (!tenantContext.IsResolved || !tenantContext.TenantId.HasValue || tenantContext.TenantId.Value == Guid.Empty)
        {
            return Result<Guid>.Failure(
                Error.Unauthorized("Tenant.Unresolved", "Acesso não autorizado: contexto do inquilino não identificado."));
        }

        var tenantId = tenantContext.TenantId.Value;

        // 1. Validação de Cotas por Plano via MediatR in-memory
        var planLimitsResult = await _sender.Send(new GetTenantPlanLimitsQuery(tenantId), cancellationToken);
        if (planLimitsResult.IsFailure)
        {
            return Result<Guid>.Failure(planLimitsResult.Error);
        }

        var currentActiveCount = await _workspaceRepository.CountActiveAsync(cancellationToken);
        if (currentActiveCount >= planLimitsResult.Value.MaxWorkspaces)
        {
            return Result<Guid>.Failure(
                Error.Validation("Workspace.QuotaExceeded",
                    $"Limite de clientes/workspaces atingido para o plano atual ({planLimitsResult.Value.MaxWorkspaces}). Faça upgrade do plano da agência para cadastrar mais clientes."));
        }

        // 2. Validação de unicidade do documento fiscal no inquilino
        var sanitizedDocument = TaxDocumentValidator.Sanitize(request.CnpjOrCpf);
        var documentExists = await _workspaceRepository.ExistsByCnpjOrCpfAsync(sanitizedDocument, null, cancellationToken);
        if (documentExists)
        {
            return Result<Guid>.Failure(
                Error.Conflict("Workspace.CnpjOrCpfAlreadyExists", "Já existe um cliente/workspace cadastrado com este CPF/CNPJ neste inquilino."));
        }

        // 3. Criação do agregado
        var workspaceResult = Workspace.Create(
            Guid.NewGuid(),
            request.Name,
            request.CnpjOrCpf,
            request.MonthlyAdSpendBudget,
            request.Segment);

        if (workspaceResult.IsFailure)
        {
            return Result<Guid>.Failure(workspaceResult.Error);
        }

        var workspace = workspaceResult.Value;

        // 4. Persistência
        await _workspaceRepository.AddAsync(workspace, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<Guid>.Success(workspace.Id);
    }
}
