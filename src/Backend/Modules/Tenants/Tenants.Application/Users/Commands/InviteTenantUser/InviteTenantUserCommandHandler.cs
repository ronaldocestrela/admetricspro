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

namespace Tenants.Application.Users.Commands.InviteTenantUser;

/// <summary>
/// Manipulador do comando <see cref="InviteTenantUserCommand"/> que valida invariantes, persiste o colaborador pendente e envia o convite.
/// </summary>
public sealed class InviteTenantUserCommandHandler : ICommandHandler<InviteTenantUserCommand, InviteTenantUserResultDto>
{
    private readonly ITenantUserRepository _userRepository;
    private readonly ITenantUserInvitationRepository _invitationRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;
    private readonly ITenantInvitationEmailComposer _emailComposer;
    private readonly ITenantInvitationTokenService _tokenService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ITenantRepository _tenantRepository;
    private readonly TenantInvitationOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="InviteTenantUserCommandHandler"/>.
    /// </summary>
    public InviteTenantUserCommandHandler(
        ITenantUserRepository userRepository,
        ITenantUserInvitationRepository invitationRepository,
        IPasswordHasher passwordHasher,
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
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
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
    public async Task<Result<InviteTenantUserResultDto>> Handle(InviteTenantUserCommand command, CancellationToken cancellationToken)
    {
        if (_currentUserContext.Role is not (TenantRole.Owner or TenantRole.Admin))
        {
            return Result<InviteTenantUserResultDto>.Failure(
                Error.Forbidden("TenantUser.InviteForbidden", "Somente proprietários e administradores podem convidar colaboradores."));
        }

        if (string.IsNullOrWhiteSpace(command.Role) || !Enum.TryParse<TenantRole>(command.Role.Trim(), true, out var role))
        {
            return Result<InviteTenantUserResultDto>.Failure(
                Error.Validation("TenantUser.InvalidRole", $"O papel '{command.Role}' não é um nível de governança válido no inquilino."));
        }

        if (role == TenantRole.Owner)
        {
            return Result<InviteTenantUserResultDto>.Failure(
                Error.Validation("TenantUser.OwnerNotAssignable", "O papel Owner só é provisionado no onboarding da agência."));
        }

        var subdomainResult = await ResolveSubdomainAsync(cancellationToken);
        if (subdomainResult.IsFailure)
        {
            return Result<InviteTenantUserResultDto>.Failure(subdomainResult.Error);
        }

        var subdomain = subdomainResult.Value;

        var normalizedEmail = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            return Result<InviteTenantUserResultDto>.Failure(
                Error.Conflict("TenantUser.EmailAlreadyInUse", $"Já existe um colaborador cadastrado com o e-mail '{command.Email}'."));
        }

        var placeholderPassword = $"Pending@{Guid.NewGuid():N}";
        var passwordHash = _passwordHasher.HashPassword(placeholderPassword);

        var userResult = TenantUser.CreatePending(
            Guid.NewGuid(),
            command.FullName,
            normalizedEmail,
            command.PhoneNumber,
            passwordHash,
            role);

        if (userResult.IsFailure)
        {
            return Result<InviteTenantUserResultDto>.Failure(userResult.Error);
        }

        var user = userResult.Value;
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

        await _userRepository.AddAsync(user, cancellationToken);
        await _invitationRepository.AddAsync(invitationResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var agencyName = _tenantContextAccessor.TenantContext?.Subdomain ?? subdomain;
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
                Error.Validation("Tenant.SubdomainRequired", "Subdomínio do inquilino é necessário para emitir convites."));
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