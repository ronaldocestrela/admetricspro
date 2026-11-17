using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Microsoft.Extensions.Options;
using Tenants.Application.Persistence;
using Tenants.Application.Users.DTOs;
using Tenants.Application.Users.Options;
using Tenants.Application.Users.Repositories;
using Tenants.Application.Users.Services;

namespace Tenants.Application.Users.Commands.ResendTenantUserInvitation;

/// <summary>
/// Manipulador de <see cref="ResendTenantUserInvitationCommand"/>.
/// </summary>
public sealed class ResendTenantUserInvitationCommandHandler : ICommandHandler<ResendTenantUserInvitationCommand, InviteTenantUserResultDto>
{
    private readonly ITenantUserRepository _userRepository;
    private readonly ITenantUserInvitationRepository _invitationRepository;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;
    private readonly ITenantInvitationEmailComposer _emailComposer;
    private readonly ITenantInvitationTokenService _tokenService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ITenantRepository _tenantRepository;
    private readonly TenantInvitationOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ResendTenantUserInvitationCommandHandler"/>.
    /// </summary>
    public ResendTenantUserInvitationCommandHandler(
        ITenantUserRepository userRepository,
        ITenantUserInvitationRepository invitationRepository,
        ITenantUnitOfWork unitOfWork,
        IEmailSender emailSender,
        ITenantInvitationEmailComposer emailComposer,
        ITenantInvitationTokenService tokenService,
        ICurrentUserContext currentUserContext,
        ITenantContextAccessor tenantContextAccessor,
        ITenantRepository tenantRepository,
        IOptions<TenantInvitationOptions> options)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _invitationRepository = invitationRepository ?? throw new ArgumentNullException(nameof(invitationRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _emailComposer = emailComposer ?? throw new ArgumentNullException(nameof(emailComposer));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<Result<InviteTenantUserResultDto>> Handle(
        ResendTenantUserInvitationCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUserContext.Role is not (TenantRole.Owner or TenantRole.Admin))
        {
            return Result<InviteTenantUserResultDto>.Failure(
                Error.Forbidden("TenantUser.InviteForbidden", "Somente proprietários e administradores podem reenviar convites."));
        }

        var subdomainResult = await ResolveSubdomainAsync(cancellationToken);
        if (subdomainResult.IsFailure)
        {
            return Result<InviteTenantUserResultDto>.Failure(subdomainResult.Error);
        }

        var subdomain = subdomainResult.Value;

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result<InviteTenantUserResultDto>.Failure(
                Error.NotFound("TenantUser.NotFound", "Colaborador não localizado no inquilino."));
        }

        if (user.IsActive)
        {
            return Result<InviteTenantUserResultDto>.Failure(
                Error.Validation("TenantUser.AlreadyActive", "Este colaborador já ativou a conta; não há convite pendente."));
        }

        var openInvitation = await _invitationRepository.GetOpenByUserIdAsync(user.Id, cancellationToken);
        if (openInvitation is not null)
        {
            var revoke = openInvitation.Revoke();
            if (revoke.IsFailure)
            {
                return Result<InviteTenantUserResultDto>.Failure(revoke.Error);
            }

            _invitationRepository.Update(openInvitation);
        }

        var (rawToken, tokenHash) = _tokenService.Generate();
        var expiresAtUtc = DateTime.UtcNow.AddHours(_options.TokenValidityHours <= 0 ? 72 : _options.TokenValidityHours);
        var invitationResult = TenantUserInvitation.Create(
            Guid.NewGuid(),
            user.Id,
            tokenHash,
            expiresAtUtc,
            _currentUserContext.UserId);

        if (invitationResult.IsFailure)
        {
            return Result<InviteTenantUserResultDto>.Failure(invitationResult.Error);
        }

        await _invitationRepository.AddAsync(invitationResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var agencyName = subdomain;
        var emailMessageResult = _emailComposer.ComposeInvitationEmail(
            user.Email,
            user.FullName,
            agencyName,
            subdomain,
            rawToken,
            expiresAtUtc);

        var emailDispatched = false;
        if (emailMessageResult.IsSuccess)
        {
            var sendResult = await _emailSender.SendEmailAsync(emailMessageResult.Value, cancellationToken);
            emailDispatched = sendResult.IsSuccess;
        }

        return Result<InviteTenantUserResultDto>.Success(new InviteTenantUserResultDto(user.Id, emailDispatched));
    }

    private async Task<Result<string>> ResolveSubdomainAsync(CancellationToken cancellationToken)
    {
        var subdomain = _tenantContextAccessor.TenantContext?.Subdomain;
        if (!string.IsNullOrWhiteSpace(subdomain))
        {
            return Result<string>.Success(subdomain.Trim().ToLowerInvariant());
        }

        var tenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
        {
            return Result<string>.Failure(
                Error.Validation("Tenant.SubdomainRequired", "Subdomínio do inquilino é necessário para reenviar convites."));
        }

        var tenant = await _tenantRepository.GetByIdAsync(new TenantId(tenantId.Value), cancellationToken);
        if (tenant is null || string.IsNullOrWhiteSpace(tenant.Subdomain))
        {
            return Result<string>.Failure(
                Error.NotFound("Tenant.NotFound", "Inquilino não encontrado para compor o link do convite."));
        }

        return Result<string>.Success(tenant.Subdomain.Trim().ToLowerInvariant());
    }
}