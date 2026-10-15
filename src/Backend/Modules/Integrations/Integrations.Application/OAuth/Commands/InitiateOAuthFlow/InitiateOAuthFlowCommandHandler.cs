using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.OAuth.DTOs;
using Integrations.Domain.OAuth;
using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Application.Security;
using Integrations.Application.Persistence;
using MediatR;

namespace Integrations.Application.OAuth.Commands.InitiateOAuthFlow;

/// <summary>
/// Manipulador do comando <see cref="InitiateOAuthFlowCommand"/>.
/// Gera o token de estado protegido contra CSRF e resolve a URL de consentimento da plataforma de anúncios.
/// </summary>
public sealed class InitiateOAuthFlowCommandHandler : ICommandHandler<InitiateOAuthFlowCommand, OAuthAuthorizationUrlDto>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IOAuthStateService _stateService;
    private readonly IAdNetworkAuthService _authService;
    private readonly ICurrentUserContext _user;
    private readonly ISender _sender;
    private readonly IOAuthTokenVaultRepository _vaultRepository;
    private readonly IIntegrationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="InitiateOAuthFlowCommandHandler"/>.
    /// </summary>
    /// <param name="tenantContextAccessor">Acessor de contexto do inquilino corrente.</param>
    /// <param name="stateService">Serviço de geração de estado seguro.</param>
    /// <param name="authService">Orquestrador de autenticação OAuth das redes.</param>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="sender">Mediador de autorização inter-módulos.</param>
    /// <param name="vaultRepository">Repositório do cofre e dos states.</param>
    /// <param name="unitOfWork">Unidade transacional.</param>
    public InitiateOAuthFlowCommandHandler(
        ITenantContextAccessor tenantContextAccessor,
        IOAuthStateService stateService,
        IAdNetworkAuthService authService,
        ICurrentUserContext user,
        ISender sender,
        IOAuthTokenVaultRepository vaultRepository,
        IIntegrationsUnitOfWork unitOfWork)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _user = user;
        _sender = sender;
        _vaultRepository = vaultRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<OAuthAuthorizationUrlDto>> Handle(
        InitiateOAuthFlowCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<OAuthAuthorizationUrlDto>.Failure(
                Error.Validation("InitiateOAuth.EmptyWorkspaceId", "O identificador do workspace é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.Platform) || !OAuthPlatform.IsSupported(request.Platform))
        {
            return Result<OAuthAuthorizationUrlDto>.Failure(
                Error.Validation("InitiateOAuth.UnsupportedPlatform", $"A plataforma '{request.Platform}' não é suportada."));
        }

        if (string.IsNullOrWhiteSpace(request.RedirectUri))
        {
            return Result<OAuthAuthorizationUrlDto>.Failure(
                Error.Validation("InitiateOAuth.EmptyRedirectUri", "A URI de redirecionamento é obrigatória."));
        }

        var tenantId = _tenantContextAccessor.TenantContext?.TenantId ?? Guid.Empty;
        var normalizedPlatform = OAuthPlatform.Normalize(request.Platform);

        if (!_user.IsAuthenticated || _user.UserId is not { } userId || tenantId == Guid.Empty)
            return Result<OAuthAuthorizationUrlDto>.Failure(Error.Unauthorized("OAuth.Unauthorized", "Usuário e tenant autenticados são obrigatórios."));
        var access = await _sender.Send(new ValidateIntegrationWorkspaceQuery(request.WorkspaceId), cancellationToken);
        if (access.IsFailure) return Result<OAuthAuthorizationUrlDto>.Failure(access.Error);
        if (!access.Value) return Result<OAuthAuthorizationUrlDto>.Failure(Error.Forbidden("OAuth.Forbidden", "Sem permissão para gerenciar integrações deste workspace."));

        var state = _stateService.GenerateState(tenantId, request.WorkspaceId, normalizedPlatform, request.RedirectUri, userId);
        var urlResult = _authService.GetAuthorizationUrl(normalizedPlatform, state, request.RedirectUri);

        if (urlResult.IsFailure)
        {
            return Result<OAuthAuthorizationUrlDto>.Failure(urlResult.Error);
        }

        var dto = new OAuthAuthorizationUrlDto(urlResult.Value, state);
        await _vaultRepository.RegisterStateAsync(state, DateTime.UtcNow.AddMinutes(15), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<OAuthAuthorizationUrlDto>.Success(dto);
    }
}
