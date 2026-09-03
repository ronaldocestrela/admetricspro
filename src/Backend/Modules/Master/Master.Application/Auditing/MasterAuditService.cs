using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using Master.Domain.Auditing;

namespace Master.Application.Auditing;

/// <summary>
/// Implementação concreta de <see cref="IMasterAuditService"/> para gravação de trilhas de auditoria globais imutáveis.
/// Intercepta o contexto de execução atual e garante a inclusão da tag 'performed_by_superadmin'
/// sempre que uma operação é executada sob Shadow Mode.
/// </summary>
public sealed class MasterAuditService : IMasterAuditService
{
    private readonly IMasterAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImpersonationContext _impersonationContext;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="MasterAuditService"/>.
    /// </summary>
    /// <param name="auditRepository">Repositório de auditoria master.</param>
    /// <param name="unitOfWork">Unidade de trabalho para confirmação transacional.</param>
    /// <param name="impersonationContext">Provedor de contexto de impersonação.</param>
    public MasterAuditService(
        IMasterAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IImpersonationContext impersonationContext)
    {
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _impersonationContext = impersonationContext;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> RecordAsync(
        string action,
        string resource,
        string? resourceId = null,
        string? details = null,
        Guid? tenantId = null,
        string? ipAddress = null,
        IEnumerable<string>? additionalTags = null,
        CancellationToken cancellationToken = default)
    {
        var isImpersonated = _impersonationContext.IsImpersonated;
        var superAdminId = isImpersonated ? _impersonationContext.OriginalSuperAdminId : null;
        var supportTicket = isImpersonated ? _impersonationContext.SupportTicketId : null;
        var sessionId = isImpersonated ? _impersonationContext.SessionId : null;
        var resolvedTenantId = tenantId ?? _impersonationContext.TargetTenantId;

        var entryResult = MasterAuditEntry.Record(
            tenantId: resolvedTenantId,
            action: action,
            resource: resource,
            resourceId: resourceId,
            details: details,
            isImpersonated: isImpersonated,
            superAdminId: superAdminId,
            supportTicketId: supportTicket,
            impersonationSessionId: sessionId,
            ipAddress: ipAddress,
            createdAtUtc: DateTime.UtcNow,
            additionalTags: additionalTags);

        if (entryResult.IsFailure)
        {
            return Result<Guid>.Failure(entryResult.Error);
        }

        var entry = entryResult.Value;
        await _auditRepository.AddAsync(entry, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<Guid>.Success(entry.Id);
    }
}
