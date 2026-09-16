using System.Net.Http.Json;
using System.Text.Json;
using Analytics.Application.Creatives.DTOs;
using BuildingBlocks.Domain.Primitives;
using WebApp.State;

namespace WebApp.Services.Creatives;

/// <summary>
/// Implementação concreta de <see cref="ICreativeHubClientService"/> que consome a Web API AdMetricsPro.
/// </summary>
public sealed class CreativeHubClientService : ICreativeHubClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CreativeHubClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado apontando para a Web API.</param>
    /// <param name="tenantStateProvider">Provedor de estado do inquilino atual na sessão.</param>
    public CreativeHubClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<CreativeHubOverviewDto>> GetOverviewAsync(
        Guid workspaceId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<CreativeHubOverviewDto>.Failure(
                Error.Validation("Workspace.InvalidId", "O identificador do Workspace é obrigatório."));
        }

        try
        {
            var queryParams = new List<string> { $"workspaceId={workspaceId}" };
            if (startDate.HasValue) queryParams.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("O"))}");
            if (endDate.HasValue) queryParams.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("O"))}");

            var requestUri = $"/api/v1/analytics/creatives/overview?{string.Join("&", queryParams)}";
            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<CreativeHubOverviewDto>>(JsonOptions, cancellationToken);

            return result ?? Result<CreativeHubOverviewDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<CreativeHubOverviewDto>.Failure(
                Error.Failure("Http.ClientError", $"Falha ao se comunicar com a Web API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<CreativeFatigueDto>> GetFatigueAnalysisAsync(
        Guid workspaceId,
        Guid adId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<CreativeFatigueDto>.Failure(
                Error.Validation("Workspace.InvalidId", "O identificador do Workspace é obrigatório."));
        }

        if (adId == Guid.Empty)
        {
            return Result<CreativeFatigueDto>.Failure(
                Error.Validation("Ad.InvalidId", "O identificador do Anúncio é obrigatório."));
        }

        try
        {
            var queryParams = new List<string> { $"workspaceId={workspaceId}", $"adId={adId}" };
            if (startDate.HasValue) queryParams.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("O"))}");
            if (endDate.HasValue) queryParams.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("O"))}");

            var requestUri = $"/api/v1/analytics/creatives/fatigue?{string.Join("&", queryParams)}";
            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<CreativeFatigueDto>>(JsonOptions, cancellationToken);

            return result ?? Result<CreativeFatigueDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<CreativeFatigueDto>.Failure(
                Error.Failure("Http.ClientError", $"Falha ao se comunicar com a Web API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<CrossPlatformComparisonDto>> GetCrossPlatformComparisonAsync(
        Guid workspaceId,
        string assetFingerprint,
        string? assetName = null,
        string? previewUrl = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<CrossPlatformComparisonDto>.Failure(
                Error.Validation("Workspace.InvalidId", "O identificador do Workspace é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(assetFingerprint))
        {
            return Result<CrossPlatformComparisonDto>.Failure(
                Error.Validation("CreativeComparison.InvalidFingerprint", "O hash do ativo de mídia é obrigatório."));
        }

        try
        {
            var queryParams = new List<string>
            {
                $"workspaceId={workspaceId}",
                $"assetFingerprint={Uri.EscapeDataString(assetFingerprint)}"
            };
            if (!string.IsNullOrWhiteSpace(assetName)) queryParams.Add($"assetName={Uri.EscapeDataString(assetName)}");
            if (!string.IsNullOrWhiteSpace(previewUrl)) queryParams.Add($"previewUrl={Uri.EscapeDataString(previewUrl)}");
            if (startDate.HasValue) queryParams.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("O"))}");
            if (endDate.HasValue) queryParams.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("O"))}");

            var requestUri = $"/api/v1/analytics/creatives/comparison?{string.Join("&", queryParams)}";
            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<CrossPlatformComparisonDto>>(JsonOptions, cancellationToken);

            return result ?? Result<CrossPlatformComparisonDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<CrossPlatformComparisonDto>.Failure(
                Error.Failure("Http.ClientError", $"Falha ao se comunicar com a Web API: {ex.Message}"));
        }
    }

    private void AppendTenantHeader(HttpRequestMessage request)
    {
        var tenantId = _tenantStateProvider.CurrentTenant?.TenantId ?? Guid.Empty;
        if (tenantId != Guid.Empty)
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.ToString());
        }
    }
}
