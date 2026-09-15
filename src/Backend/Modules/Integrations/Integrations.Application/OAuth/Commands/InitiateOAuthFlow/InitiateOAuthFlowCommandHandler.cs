using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.OAuth.DTOs;
using Integrations.Domain.OAuth;

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

    /// <summary>
    /// Inicializa uma nova instância de <see cref="InitiateOAuthFlowCommandHandler"/>.
    /// </summary>
    /// <param name="tenantContextAccessor">Acessor de contexto do inquilino corrente.</param>
    /// <param name="stateService">Serviço de geração de estado seguro.</param>
    /// <param name="authService">Orquestrador de autenticação OAuth das redes.</param>
    public InitiateOAuthFlowCommandHandler(
        ITenantContextAccessor tenantContextAccessor,
        IOAuthStateService stateService,
        IAdNetworkAuthService authService)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <inheritdoc />
    public Task<Result<OAuthAuthorizationUrlDto>> Handle(
        InitiateOAuthFlowCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Task.FromResult(Result<OAuthAuthorizationUrlDto>.Failure(
                Error.Validation("InitiateOAuth.EmptyWorkspaceId", "O identificador do workspace é obrigatório.")));
        }

        if (string.IsNullOrWhiteSpace(request.Platform) || !OAuthPlatform.IsSupported(request.Platform))
        {
            return Task.FromResult(Result<OAuthAuthorizationUrlDto>.Failure(
                Error.Validation("InitiateOAuth.UnsupportedPlatform", $"A plataforma '{request.Platform}' não é suportada.")));
        }

        if (string.IsNullOrWhiteSpace(request.RedirectUri))
        {
            return Task.FromResult(Result<OAuthAuthorizationUrlDto>.Failure(
                Error.Validation("InitiateOAuth.EmptyRedirectUri", "A URI de redirecionamento é obrigatória.")));
        }

        var tenantId = _tenantContextAccessor.TenantContext?.TenantId ?? Guid.Empty;
        var normalizedPlatform = OAuthPlatform.Normalize(request.Platform);

        var state = _stateService.GenerateState(tenantId, request.WorkspaceId, normalizedPlatform, request.RedirectUri);
        var urlResult = _authService.GetAuthorizationUrl(normalizedPlatform, state, request.RedirectUri);

        if (urlResult.IsFailure)
        {
            return Task.FromResult(Result<OAuthAuthorizationUrlDto>.Failure(urlResult.Error));
        }

        var dto = new OAuthAuthorizationUrlDto(urlResult.Value, state);
        return Task.FromResult(Result<OAuthAuthorizationUrlDto>.Success(dto));
    }
}
