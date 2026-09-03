using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Auditing;
using Master.Application.Repositories;
using Master.Domain.Auditing;
using Master.Domain.Tenants;
using MediatR;

namespace Master.Application.Tenants.Commands.TerminateImpersonationSession;

/// <summary>
/// Manipulador responsável por revogar e registrar o encerramento de sessões ativas de Shadow Mode.
/// </summary>
public sealed class TerminateImpersonationSessionCommandHandler : IRequestHandler<TerminateImpersonationSessionCommand, Result>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IImpersonationSessionRepository _sessionRepository;
    private readonly IMasterAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TerminateImpersonationSessionCommandHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de tenants.</param>
    /// <param name="sessionRepository">Repositório de sessões de impersonation.</param>
    /// <param name="auditService">Serviço de auditoria master imutável.</param>
    /// <param name="unitOfWork">Unidade de trabalho transacional.</param>
    public TerminateImpersonationSessionCommandHandler(
        ITenantRepository tenantRepository,
        IImpersonationSessionRepository sessionRepository,
        IMasterAuditService auditService,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _sessionRepository = sessionRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(TerminateImpersonationSessionCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(new TenantId(request.TenantId), cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant not found for the specified identifier."));
        }

        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure(ImpersonationErrors.SessionNotFound);
        }

        var revokeResult = session.Revoke(
            string.IsNullOrWhiteSpace(request.Reason) ? "Manual session termination via banner" : request.Reason,
            DateTime.UtcNow);

        if (revokeResult.IsFailure)
        {
            return revokeResult;
        }

        _sessionRepository.Update(session);
        await _unitOfWork.CommitAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Impersonation.Terminated",
            resource: "ImpersonationSession",
            resourceId: session.Id.ToString(),
            details: $"Sessão {session.Id} encerrada. Motivo: {session.RevokeReason}. Ticket: {session.SupportTicketId}",
            tenantId: tenant.Id.Value,
            additionalTags: [MasterAuditTags.PerformedBySuperadmin, MasterAuditTags.SupportIntervention],
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
