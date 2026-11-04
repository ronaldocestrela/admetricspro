using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using System.Globalization;
using SocialPublishing.Application.Approvals.DTOs;
using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;
using SocialPublishing.Application.Planning.DTOs;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Application.Connections.DTOs;
using SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Application.Traction.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using WebApp.State;

namespace WebApp.Services.Social;

/// <summary>
/// Implementação concreta de <see cref="ISocialPublishingClient"/> consumindo a Web API versionada.
/// </summary>
public sealed class SocialPublishingClient : ISocialPublishingClient
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private readonly ITenantSessionStateProvider _session;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SocialPublishingClient"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado.</param>
    /// <param name="tenantStateProvider">Provedor de estado do inquilino.</param>
    /// <param name="session">Sessão autenticada do circuito Blazor.</param>
    public SocialPublishingClient(
        HttpClient httpClient,
        ITenantStateProvider tenantStateProvider,
        ITenantSessionStateProvider session)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OrganicConnectionSummaryDto>>> ListConnectionsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Failure(SocialPublishing.Domain.SocialErrors.WorkspaceRequired);
        }

        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/social/connections?workspaceId={workspaceId}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<OrganicConnectionSummaryDto>>>(JsonOptions, cancellationToken);

            return result ?? Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao listar conexões orgânicas."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao listar conexões orgânicas: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<StartOrganicAuthorizationResponse>> StartAuthorizationAsync(
        Guid workspaceId,
        OrganicPlatform platform,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<StartOrganicAuthorizationResponse>.Failure(SocialPublishing.Domain.SocialErrors.WorkspaceRequired);
        }

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/social/connections/authorize")
            {
                Content = JsonContent.Create(new { workspaceId, platform, redirectUri }, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<StartOrganicAuthorizationResponse>>(JsonOptions, cancellationToken);

            return result ?? Result<StartOrganicAuthorizationResponse>.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao iniciar autorização orgânica."));
        }
        catch (Exception ex)
        {
            return Result<StartOrganicAuthorizationResponse>.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao iniciar autorização orgânica: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<CompleteOrganicAuthorizationResponse>> CompleteAuthorizationAsync(
        Guid workspaceId,
        string code,
        string state,
        string redirectUri,
        string? providerError = null,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<CompleteOrganicAuthorizationResponse>.Failure(SocialPublishing.Domain.SocialErrors.WorkspaceRequired);
        }

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/social/connections/callback")
            {
                Content = JsonContent.Create(
                    new { workspaceId, code, state, redirectUri, providerError },
                    options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<CompleteOrganicAuthorizationResponse>>(JsonOptions, cancellationToken);

            return result ?? Result<CompleteOrganicAuthorizationResponse>.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao concluir autorização orgânica."));
        }
        catch (Exception ex)
        {
            return Result<CompleteOrganicAuthorizationResponse>.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao concluir autorização orgânica: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> RevokeConnectionAsync(
        Guid workspaceId,
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || connectionId == Guid.Empty)
        {
            return Result.Failure(SocialPublishing.Domain.SocialErrors.WorkspaceRequired);
        }

        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/v1/social/connections/{connectionId}?workspaceId={workspaceId}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao revogar conexão orgânica."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao revogar conexão orgânica: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OrganicPostDto>>> ListPostsAsync(
        Guid workspaceId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<IReadOnlyList<OrganicPostDto>>.Failure(SocialPublishing.Domain.SocialErrors.WorkspaceRequired);
        }

        var from = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc);
        var fromQuery = Uri.EscapeDataString(from.ToString("O", CultureInfo.InvariantCulture));
        var toQuery = Uri.EscapeDataString(to.ToString("O", CultureInfo.InvariantCulture));

        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/social/posts?workspaceId={workspaceId}&from={fromQuery}&to={toQuery}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<OrganicPostDto>>>(JsonOptions, cancellationToken);

            return result ?? Result<IReadOnlyList<OrganicPostDto>>.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao listar posts orgânicos."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<OrganicPostDto>>.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao listar posts orgânicos: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<OrganicPostDto>> RescheduleAsync(
        Guid workspaceId,
        Guid postId,
        DateTime scheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<OrganicPostDto>.Failure(SocialPublishing.Domain.SocialErrors.WorkspaceRequired);
        }

        if (postId == Guid.Empty)
        {
            return Result<OrganicPostDto>.Failure(
                Error.Validation("Social.PostNotFound", "Post orgânico não encontrado."));
        }

        var utc = DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc);

        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/social/posts/{postId}:reschedule?workspaceId={workspaceId}")
            {
                Content = JsonContent.Create(new { scheduledAtUtc = utc }, options: JsonOptions),
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<OrganicPostDto>>(JsonOptions, cancellationToken);

            return result ?? Result<OrganicPostDto>.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao reagendar post orgânico."));
        }
        catch (Exception ex)
        {
            return Result<OrganicPostDto>.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao reagendar post orgânico: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<BrandVoiceDto>> GetBrandVoiceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<BrandVoiceDto>.Failure(SocialPublishing.Domain.SocialErrors.WorkspaceRequired);
        }

        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/social/brand-voice/{workspaceId}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<BrandVoiceDto>>(JsonOptions, cancellationToken);

            return result ?? Result<BrandVoiceDto>.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao obter voz de marca."));
        }
        catch (Exception ex)
        {
            return Result<BrandVoiceDto>.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao obter voz de marca: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<BrandVoiceDto>> UpsertBrandVoiceAsync(
        Guid workspaceId,
        string tone,
        string persona,
        IReadOnlyList<string> mandatoryLinks,
        IReadOnlyList<string> forbiddenTerms,
        EmojiPolicy emojiPolicy,
        int hashtagLimit,
        int authorityWeight,
        int saleWeight,
        int connectionWeight,
        int socialProofWeight,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<BrandVoiceDto>.Failure(SocialPublishing.Domain.SocialErrors.WorkspaceRequired);
        }

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/social/brand-voice/{workspaceId}")
            {
                Content = JsonContent.Create(
                    new
                    {
                        tone,
                        persona,
                        mandatoryLinks,
                        forbiddenTerms,
                        emojiPolicy,
                        hashtagLimit,
                        authorityWeight,
                        saleWeight,
                        connectionWeight,
                        socialProofWeight,
                        languageCode,
                    },
                    options: JsonOptions),
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<BrandVoiceDto>>(JsonOptions, cancellationToken);

            return result ?? Result<BrandVoiceDto>.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao salvar voz de marca."));
        }
        catch (Exception ex)
        {
            return Result<BrandVoiceDto>.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao salvar voz de marca: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<GeneratedPlanDto>> GeneratePlanAsync(
        Guid workspaceId,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        string timeZoneId,
        int postsPerWeek,
        int seed,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<GeneratedPlanDto>.Failure(SocialPublishing.Domain.SocialErrors.WorkspaceRequired);
        }

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/social/plans:generate")
            {
                Content = JsonContent.Create(
                    new
                    {
                        workspaceId,
                        rangeStart,
                        rangeEnd,
                        timeZoneId,
                        postsPerWeek,
                        seed,
                    },
                    options: JsonOptions),
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<GeneratedPlanDto>>(JsonOptions, cancellationToken);

            return result ?? Result<GeneratedPlanDto>.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao gerar cronograma."));
        }
        catch (Exception ex)
        {
            return Result<GeneratedPlanDto>.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao gerar cronograma: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<ApprovalLinkCreatedDto>> CreateApprovalLinkAsync(
        Guid workspaceId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/social/approval-links")
            {
                Content = JsonContent.Create(new { workspaceId, expiresAtUtc }, options: JsonOptions),
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<ApprovalLinkCreatedDto>>(JsonOptions, cancellationToken);
            return result ?? Result<ApprovalLinkCreatedDto>.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao emitir link de aprovação."));
        }
        catch (Exception ex)
        {
            return Result<ApprovalLinkCreatedDto>.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao emitir link: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PaidBoostSuggestionDto>>> ListBoostSuggestionsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<IReadOnlyList<PaidBoostSuggestionDto>>.Failure(SocialErrors.WorkspaceRequired);
        }

        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/social/boost-suggestions?workspaceId={workspaceId}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<PaidBoostSuggestionDto>>>(JsonOptions, cancellationToken);

            return result ?? Result<IReadOnlyList<PaidBoostSuggestionDto>>.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao listar sugestões de impulsionamento."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<PaidBoostSuggestionDto>>.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao listar sugestões: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PaidBoostSuggestionDto>> AcceptBoostSuggestionAsync(
        Guid workspaceId,
        Guid suggestionId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<PaidBoostSuggestionDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/social/boost-suggestions/{suggestionId}:accept?workspaceId={workspaceId}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<PaidBoostSuggestionDto>>(JsonOptions, cancellationToken);

            return result ?? Result<PaidBoostSuggestionDto>.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao aceitar sugestão de impulsionamento."));
        }
        catch (Exception ex)
        {
            return Result<PaidBoostSuggestionDto>.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao aceitar sugestão: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PaidBoostSuggestionDto>> DismissBoostSuggestionAsync(
        Guid workspaceId,
        Guid suggestionId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<PaidBoostSuggestionDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/social/boost-suggestions/{suggestionId}:dismiss?workspaceId={workspaceId}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<PaidBoostSuggestionDto>>(JsonOptions, cancellationToken);

            return result ?? Result<PaidBoostSuggestionDto>.Failure(
                Error.Failure("Social.InvalidResponse", "Resposta inválida ao dispensar sugestão de impulsionamento."));
        }
        catch (Exception ex)
        {
            return Result<PaidBoostSuggestionDto>.Failure(
                Error.Failure("Social.NetworkError", $"Erro de comunicação ao dispensar sugestão: {ex.Message}"));
        }
    }

    private void AppendTenantHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_session.CurrentSession?.AccessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                _session.CurrentSession.AccessToken);
        }

        var tenantId = _tenantStateProvider.CurrentTenant?.TenantId;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.Value.ToString());
        }
    }
}
