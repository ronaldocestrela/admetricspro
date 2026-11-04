using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Approvals;
using SocialPublishing.Application.Approvals.DTOs;
using SocialPublishing.Domain;
namespace WebApp.Services.Social;

/// <summary>
/// Implementação de <see cref="IPublicSocialApprovalClient"/> contra rotas públicas da Web API.
/// </summary>
public sealed class PublicSocialApprovalClient : IPublicSocialApprovalClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PublicSocialApprovalClient"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com BaseAddress da API.</param>
    public PublicSocialApprovalClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result<PublicApprovalBoardDto>> GetBoardAsync(
        string token,
        string? tenantSlug,
        string? browserHost,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result<PublicApprovalBoardDto>.Failure(SocialErrors.ApprovalTokenInvalid);
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/public/social/approvals/{Uri.EscapeDataString(token)}");
        ApplyTenantHeaders(request, tenantSlug, browserHost);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<Result<PublicApprovalBoardDto>>(JsonOptions, cancellationToken);
        if (envelope is null)
        {
            return Result<PublicApprovalBoardDto>.Failure(SocialErrors.ApprovalTokenInvalid);
        }

        if (!response.IsSuccessStatusCode || envelope.IsFailure)
        {
            return Result<PublicApprovalBoardDto>.Failure(SocialErrors.ApprovalTokenInvalid);
        }

        return envelope;
    }

    /// <inheritdoc />
    public async Task<Result> SubmitDecisionAsync(
        string token,
        IReadOnlyList<Guid> postIds,
        PublicApprovalDecision decision,
        string? comment,
        string? tenantSlug,
        string? browserHost,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure(SocialErrors.ApprovalTokenInvalid);
        }

        var payload = new PublicApprovalDecisionRequest(postIds, decision, comment);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/public/social/approvals/{Uri.EscapeDataString(token)}/decisions")
        {
            Content = JsonContent.Create(payload, options: JsonOptions),
        };
        ApplyTenantHeaders(request, tenantSlug, browserHost);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);
        return envelope ?? Result.Failure(SocialErrors.ApprovalTokenInvalid);
    }

    private static void ApplyTenantHeaders(HttpRequestMessage request, string? tenantSlug, string? browserHost)
    {
        if (!string.IsNullOrWhiteSpace(tenantSlug))
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantSlug.Trim());
        }

        if (!string.IsNullOrWhiteSpace(browserHost))
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Host", browserHost.Trim());
        }
    }
}
