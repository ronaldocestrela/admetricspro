using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.DTOs;
using WebApp.State;

namespace WebApp.Services;

/// <summary>
/// Implementação concreta do cliente HTTP <see cref="IBulkCampaignClientService"/> que consome a Web API AdMetricsPro.
/// </summary>
public sealed class BulkCampaignClientService : IBulkCampaignClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private readonly ITenantSessionStateProvider? _session;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BulkCampaignClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado.</param>
    /// <param name="session">Sessão autenticada para chamadas de integração.</param>
    /// <param name="tenantStateProvider">Provedor de estado do inquilino na sessão.</param>
    public BulkCampaignClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider, ITenantSessionStateProvider? session = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CampaignHierarchyDto>>> GetCampaignsForBulkAsync(
        Guid workspaceId,
        string? platform = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<IReadOnlyList<CampaignHierarchyDto>>.Failure(
                Error.Validation("Workspace.InvalidId", "O identificador do Workspace é obrigatório."));
        }

        try
        {
            var queryParams = new List<string> { $"workspaceId={workspaceId}" };
            if (!string.IsNullOrWhiteSpace(platform)) queryParams.Add($"platform={Uri.EscapeDataString(platform)}");
            if (!string.IsNullOrWhiteSpace(status)) queryParams.Add($"status={Uri.EscapeDataString(status)}");

            var requestUri = $"/api/v1/integrations/campaigns?{string.Join("&", queryParams)}";

            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<CampaignHierarchyDto>>>(JsonOptions, cancellationToken);

            return result ?? Result<IReadOnlyList<CampaignHierarchyDto>>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CampaignHierarchyDto>>.Failure(
                Error.Failure("Http.Exception", $"Falha ao consultar campanhas: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<BulkCampaignOperationResultDto>> ExecuteBulkOperationsAsync(
        Guid workspaceId,
        IReadOnlyList<BulkCampaignOperationItem> operations,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<BulkCampaignOperationResultDto>.Failure(
                Error.Validation("Workspace.InvalidId", "O identificador do Workspace é obrigatório."));
        }

        if (operations is null || operations.Count == 0)
        {
            return Result<BulkCampaignOperationResultDto>.Failure(
                Error.Validation("BulkCampaign.EmptyOperations", "A lista de operações não pode ser vazia."));
        }

        try
        {
            var requestUri = "/api/v1/integrations/campaigns/bulk";
            var payload = new
            {
                WorkspaceId = workspaceId,
                Operations = operations.Select(o => new
                {
                    CampaignId = o.CampaignId,
                    Action = o.Action,
                    DailyBudget = o.DailyBudget,
                    PercentageChange = o.PercentageChange,
                    Reason = o.Reason
                })
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<BulkCampaignOperationResultDto>>(JsonOptions, cancellationToken);

            return result ?? Result<BulkCampaignOperationResultDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<BulkCampaignOperationResultDto>.Failure(
                Error.Failure("Http.Exception", $"Falha ao executar operações em lote: {ex.Message}"));
        }
    }

    private void AppendTenantHeader(HttpRequestMessage message)
    {
        var accessToken = _session?.CurrentSession?.AccessToken;
        if (!string.IsNullOrWhiteSpace(accessToken))
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var tenantId = _tenantStateProvider.CurrentTenant?.TenantId ?? Guid.Empty;
        if (tenantId != Guid.Empty)
        {
            message.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<Result<SyncCampaignHierarchySummaryDto>> SynchronizeAccountAsync(Guid workspaceId, Guid accountId,
        string platform, DateOnly since, DateOnly until, CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || accountId == Guid.Empty || since > until ||
            platform is not ("MetaAds" or "GoogleAds" or "TikTokAds" or "BingAds"))
            return Result<SyncCampaignHierarchySummaryDto>.Failure(Error.Validation("Sync.InvalidRequest", "Selecione conta, plataforma e período válidos."));
        var hierarchy = await SendSyncAsync<SyncCampaignHierarchySummaryDto>("/api/v1/integrations/campaigns/sync",
            new { workspaceId, connectedAdAccountId = accountId, platform }, cancellationToken);
        if (hierarchy.IsFailure) return hierarchy;
        var metrics = await SendSyncAsync<SyncCampaignMetricsSummaryDto>("/api/v1/integrations/campaigns/metrics/sync",
            new { workspaceId, connectedAdAccountId = accountId, platform,
                startDateUtc = since.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                endDateUtc = until.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), granularity = "Daily" }, cancellationToken);
        return metrics.IsFailure ? Result<SyncCampaignHierarchySummaryDto>.Failure(metrics.Error) : hierarchy;
    }

    private async Task<Result<T>> SendSyncAsync<T>(string uri, object payload, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = JsonContent.Create(payload) };
            AppendTenantHeader(request);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<Result<T>>(JsonOptions, cancellationToken)
                ?? Result<T>.Failure(Error.Failure("Http.EmptyResponse", "Resposta de sincronização vazia."));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            return Result<T>.Failure(Error.Failure("Sync.NetworkError", "Não foi possível comunicar com a API para sincronizar a conta."));
        }
    }
}
