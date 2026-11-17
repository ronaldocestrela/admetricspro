using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Logging;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;

/// <summary>
/// Valida o state, troca o código OAuth, cifra tokens com propósito e persiste <see cref="OrganicConnection"/>.
/// </summary>
public sealed class CompleteOrganicAuthorizationCommandHandler
    : ICommandHandler<CompleteOrganicAuthorizationCommand, CompleteOrganicAuthorizationResponse>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IOrganicOAuthStateService _stateService;
    private readonly IReadOnlyDictionary<OrganicPlatform, IOrganicPlatformAdapter> _adaptersByPlatform;
    private readonly IOrganicTokenProtector _tokenProtector;
    private readonly IOrganicConnectionRepository _connectionRepository;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ILogger<CompleteOrganicAuthorizationCommandHandler> _logger;

    /// <summary>
    /// Inicializa o manipulador do callback OAuth orgânico.
    /// </summary>
    /// <param name="tenantContextAccessor">Contexto de tenant da requisição.</param>
    /// <param name="stateService">Validação do state HMAC.</param>
    /// <param name="adapters">Adaptadores OAuth por plataforma.</param>
    /// <param name="tokenProtector">Cofre orgânico com envelope de propósito.</param>
    /// <param name="connectionRepository">Repositório de conexões.</param>
    /// <param name="unitOfWork">Commit transacional do módulo.</param>
    /// <param name="logger">Logger operacional (sem segredos OAuth).</param>
    public CompleteOrganicAuthorizationCommandHandler(
        ITenantContextAccessor tenantContextAccessor,
        IOrganicOAuthStateService stateService,
        IEnumerable<IOrganicPlatformAdapter> adapters,
        IOrganicTokenProtector tokenProtector,
        IOrganicConnectionRepository connectionRepository,
        ISocialPublishingUnitOfWork unitOfWork,
        ILogger<CompleteOrganicAuthorizationCommandHandler> logger)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _tokenProtector = tokenProtector ?? throw new ArgumentNullException(nameof(tokenProtector));
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adaptersByPlatform = (adapters ?? throw new ArgumentNullException(nameof(adapters)))
            .ToDictionary(a => a.Platform);
    }

    /// <inheritdoc />
    public async Task<Result<CompleteOrganicAuthorizationResponse>> Handle(
        CompleteOrganicAuthorizationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<CompleteOrganicAuthorizationResponse>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (!string.IsNullOrWhiteSpace(request.ProviderError))
        {
            return Result<CompleteOrganicAuthorizationResponse>.Failure(SocialErrors.OAuthExchangeFailed);
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Result<CompleteOrganicAuthorizationResponse>.Failure(
                Error.Validation("Social.OAuthCodeRequired", "O código de autorização é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.State))
        {
            return Result<CompleteOrganicAuthorizationResponse>.Failure(SocialErrors.OAuthStateInvalid);
        }

        var tenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return Result<CompleteOrganicAuthorizationResponse>.Failure(SocialErrors.TenantRequired);
        }

        var stateResult = _stateService.Validate(
            request.State,
            tenantId.Value,
            request.WorkspaceId,
            DateTime.UtcNow);

        if (stateResult.IsFailure)
        {
            return Result<CompleteOrganicAuthorizationResponse>.Failure(stateResult.Error);
        }

        var payload = stateResult.Value;
        if (!string.Equals(payload.RedirectUri, request.RedirectUri, StringComparison.Ordinal))
        {
            return Result<CompleteOrganicAuthorizationResponse>.Failure(SocialErrors.OAuthStateInvalid);
        }

        if (!_adaptersByPlatform.TryGetValue(payload.Platform, out var adapter))
        {
            return Result<CompleteOrganicAuthorizationResponse>.Failure(SocialErrors.PlatformUnsupported);
        }

        var exchangeResult = await adapter.ExchangeCodeAsync(request.Code, request.RedirectUri, cancellationToken);
        if (exchangeResult.IsFailure)
        {
            return Result<CompleteOrganicAuthorizationResponse>.Failure(exchangeResult.Error);
        }

        var grant = exchangeResult.Value;
        var existing = await _connectionRepository.GetByWorkspacePlatformAndExternalAccountAsync(
            request.WorkspaceId,
            payload.Platform,
            grant.ExternalAccountId,
            cancellationToken);

        var connectionId = existing?.Id ?? Guid.NewGuid();
        var purpose = OrganicConnection.EncryptionPurpose(payload.Platform, request.WorkspaceId, connectionId);
        var encryptedAccess = _tokenProtector.Protect(grant.AccessToken, purpose);
        string? encryptedRefresh = string.IsNullOrWhiteSpace(grant.RefreshToken)
            ? null
            : _tokenProtector.Protect(grant.RefreshToken, purpose);

        OrganicConnection connection;
        if (existing is null)
        {
            var createResult = OrganicConnection.Create(
                request.WorkspaceId,
                payload.Platform,
                grant.ExternalAccountId,
                grant.DisplayName,
                grant.Scopes,
                encryptedAccess,
                encryptedRefresh,
                grant.ExpiresAtUtc,
                connectionId);

            if (createResult.IsFailure)
            {
                return Result<CompleteOrganicAuthorizationResponse>.Failure(createResult.Error);
            }

            connection = createResult.Value;
            await _connectionRepository.AddAsync(connection, cancellationToken);
        }
        else
        {
            var reauthorize = existing.Reauthorize(
                encryptedAccess,
                encryptedRefresh,
                grant.ExpiresAtUtc,
                grant.DisplayName,
                grant.Scopes);

            if (reauthorize.IsFailure)
            {
                return Result<CompleteOrganicAuthorizationResponse>.Failure(reauthorize.Error);
            }

            connection = existing;
            await _connectionRepository.UpdateAsync(connection, cancellationToken);
        }

        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Conexão orgânica {ConnectionId} persistida para workspace {WorkspaceId} na plataforma {Platform}",
            connection.Id,
            request.WorkspaceId,
            connection.Platform);

        var response = new CompleteOrganicAuthorizationResponse(
            connection.Id,
            connection.Platform,
            connection.DisplayName,
            connection.AccessTokenExpiresAtUtc);

        return Result<CompleteOrganicAuthorizationResponse>.Success(response);
    }
}
