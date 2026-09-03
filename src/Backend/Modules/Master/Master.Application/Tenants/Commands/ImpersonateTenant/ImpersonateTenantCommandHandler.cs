using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Application.Services;

namespace Master.Application.Tenants.Commands.ImpersonateTenant;

/// <summary>
/// Handles <see cref="ImpersonateTenantCommand"/> to authenticate and issue contextual impersonation tokens.
/// </summary>
public sealed class ImpersonateTenantCommandHandler : ICommandHandler<ImpersonateTenantCommand, ImpersonateTenantResponse>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IImpersonationSessionRepository _sessionRepository;
    private readonly IImpersonationTokenService _tokenService;
    private readonly BuildingBlocks.Application.Persistence.IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImpersonateTenantCommandHandler"/> class.
    /// </summary>
    /// <param name="tenantRepository">Tenant repository.</param>
    /// <param name="sessionRepository">Impersonation session repository.</param>
    /// <param name="tokenService">Contextual impersonation token service.</param>
    /// <param name="unitOfWork">Unit of work for committing session transaction.</param>
    public ImpersonateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IImpersonationSessionRepository sessionRepository,
        IImpersonationTokenService tokenService,
        BuildingBlocks.Application.Persistence.IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _sessionRepository = sessionRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<ImpersonateTenantResponse>> Handle(
        ImpersonateTenantCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = await _tenantRepository.GetByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result<ImpersonateTenantResponse>.Failure(
                Error.NotFound("Tenant.NotFound", "Tenant not found for the specified identifier."));
        }

        if (tenant.Status != Domain.Tenants.TenantStatus.Active && tenant.Status != Domain.Tenants.TenantStatus.Trial)
        {
            return Result<ImpersonateTenantResponse>.Failure(Domain.Tenants.ImpersonationErrors.TenantInactive);
        }

        var sessionResult = Domain.Tenants.ImpersonationSession.Create(
            command.TenantId,
            command.SuperAdminId,
            command.SupportTicketId,
            command.Reason,
            command.DurationMinutes,
            DateTime.UtcNow);

        if (sessionResult.IsFailure)
        {
            return Result<ImpersonateTenantResponse>.Failure(sessionResult.Error);
        }

        var session = sessionResult.Value;

        var tokenResult = _tokenService.GenerateToken(session, tenant);
        if (tokenResult.IsFailure)
        {
            return Result<ImpersonateTenantResponse>.Failure(tokenResult.Error);
        }

        await _sessionRepository.AddAsync(session, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var response = new ImpersonateTenantResponse(
            tokenResult.Value,
            "Bearer",
            (int)(session.ExpiresAtUtc - session.CreatedAtUtc).TotalSeconds,
            session.Id,
            tenant.Id.Value,
            tenant.CompanyName,
            session.SuperAdminId,
            session.SupportTicketId,
            session.ExpiresAtUtc);

        return Result<ImpersonateTenantResponse>.Success(response);
    }
}
