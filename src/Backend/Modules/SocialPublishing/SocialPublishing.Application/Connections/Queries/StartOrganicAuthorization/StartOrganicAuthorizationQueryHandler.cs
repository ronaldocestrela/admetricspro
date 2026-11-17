using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;

/// <summary>
/// Manipulador de <see cref="StartOrganicAuthorizationQuery"/> que emite state HMAC e monta a URL do provedor.
/// </summary>
public sealed class StartOrganicAuthorizationQueryHandler
    : IQueryHandler<StartOrganicAuthorizationQuery, StartOrganicAuthorizationResponse>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IOrganicOAuthStateService _stateService;
    private readonly IReadOnlyDictionary<OrganicPlatform, IOrganicPlatformAdapter> _adaptersByPlatform;

    /// <summary>
    /// Inicializa o manipulador com contexto de tenant, serviço de state e adaptadores por plataforma.
    /// </summary>
    /// <param name="tenantContextAccessor">Acessor do tenant corrente.</param>
    /// <param name="stateService">Serviço de state anti-CSRF.</param>
    /// <param name="adapters">Adaptadores OAuth orgânicos registrados na composição.</param>
    public StartOrganicAuthorizationQueryHandler(
        ITenantContextAccessor tenantContextAccessor,
        IOrganicOAuthStateService stateService,
        IEnumerable<IOrganicPlatformAdapter> adapters)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _adaptersByPlatform = (adapters ?? throw new ArgumentNullException(nameof(adapters)))
            .ToDictionary(a => a.Platform);
    }

    /// <inheritdoc />
    public Task<Result<StartOrganicAuthorizationResponse>> Handle(
        StartOrganicAuthorizationQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Task.FromResult(Result<StartOrganicAuthorizationResponse>.Failure(SocialErrors.WorkspaceRequired));
        }

        if (!Enum.IsDefined(request.Platform))
        {
            return Task.FromResult(Result<StartOrganicAuthorizationResponse>.Failure(SocialErrors.PlatformUnsupported));
        }

        var redirectValidation = ValidateRedirectUri(request.RedirectUri);
        if (redirectValidation.IsFailure)
        {
            return Task.FromResult(Result<StartOrganicAuthorizationResponse>.Failure(redirectValidation.Error));
        }

        var tenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return Task.FromResult(Result<StartOrganicAuthorizationResponse>.Failure(SocialErrors.TenantRequired));
        }

        if (!_adaptersByPlatform.TryGetValue(request.Platform, out var adapter))
        {
            return Task.FromResult(Result<StartOrganicAuthorizationResponse>.Failure(SocialErrors.PlatformUnsupported));
        }

        var issuedAt = DateTime.UtcNow;
        var issue = _stateService.Issue(
            tenantId.Value,
            request.WorkspaceId,
            request.Platform,
            request.RedirectUri,
            issuedAt);

        var urlResult = adapter.BuildAuthorizationUrl(issue.State, request.RedirectUri);
        if (urlResult.IsFailure)
        {
            return Task.FromResult(Result<StartOrganicAuthorizationResponse>.Failure(urlResult.Error));
        }

        var response = new StartOrganicAuthorizationResponse(urlResult.Value, issue.State, issue.ExpiresAtUtc);
        return Task.FromResult(Result<StartOrganicAuthorizationResponse>.Success(response));
    }

    private static Result ValidateRedirectUri(string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri) ||
            !Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
        {
            return Result.Failure(SocialErrors.RedirectUriInvalid);
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return Result.Success();
        }

        if (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)
        {
            return Result.Success();
        }

        return Result.Failure(SocialErrors.RedirectUriInvalid);
    }
}
