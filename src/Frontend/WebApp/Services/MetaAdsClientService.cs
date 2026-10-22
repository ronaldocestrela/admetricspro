using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.DTOs;
using Integrations.Application.Meta;
using Integrations.Domain.Meta;
using WebApp.State;

namespace WebApp.Services;

/// <summary>Consumo exclusivo da Web API com a sessão JWT do circuito Blazor.</summary>
/// <param name="client">Cliente HTTP configurado.</param>
/// <param name="session">Sessão autenticada do circuito Blazor.</param>
public sealed class MetaAdsClientService(HttpClient client, ITenantSessionStateProvider session) : IMetaAdsClientService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public Task<Result<MetaResourceDefinition>> GetResourceOptionsAsync(Guid workspaceId, Guid accountId, string resource, CancellationToken cancellationToken = default) =>
        SendAsync<MetaResourceDefinition>(HttpMethod.Get, $"/api/v1/integrations/meta/resource-options?workspaceId={workspaceId}&accountId={accountId}&resource={Uri.EscapeDataString(resource)}", null, cancellationToken);

    /// <inheritdoc />
    public Task<Result<MetaResourceConfiguration>> GetResourceConfigurationAsync(Guid workspaceId, Guid accountId, string resource,
        string externalId, IReadOnlyList<string>? fields = null, CancellationToken cancellationToken = default) =>
        SendAsync<MetaResourceConfiguration>(HttpMethod.Get, $"/api/v1/integrations/meta/resources/{Uri.EscapeDataString(resource)}/{Uri.EscapeDataString(externalId)}/configuration?workspaceId={workspaceId}&accountId={accountId}" +
            (fields is null ? "" : $"&fields={Uri.EscapeDataString(string.Join(',', fields))}"), null, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<JsonElement>>> SearchTargetingAsync(Guid workspaceId, Guid accountId, string type, string query, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<JsonElement>>(HttpMethod.Get, $"/api/v1/integrations/meta/targeting-search?workspaceId={workspaceId}&accountId={accountId}&type={Uri.EscapeDataString(type)}&query={Uri.EscapeDataString(query)}", null, cancellationToken);

    /// <inheritdoc />
    public Task<Result<MetaMediaResult>> GetVideoStatusAsync(Guid workspaceId, Guid accountId, Guid operationId, CancellationToken cancellationToken = default) =>
        SendAsync<MetaMediaResult>(HttpMethod.Get, $"/api/v1/integrations/meta/media/videos/{operationId}?workspaceId={workspaceId}&accountId={accountId}", null, cancellationToken);

    /// <inheritdoc />
    public async Task<Result<SyncCampaignHierarchySummaryDto>> SyncAsync(Guid workspaceId, Guid accountId, DateOnly since, DateOnly until, CancellationToken cancellationToken = default)
    {
        var hierarchy = await SendAsync<SyncCampaignHierarchySummaryDto>(HttpMethod.Post, "/api/v1/integrations/campaigns/sync",
            new { workspaceId, connectedAdAccountId = accountId, platform = "MetaAds" }, cancellationToken);
        if (hierarchy.IsFailure) return hierarchy;
        var metrics = await SendAsync<SyncCampaignMetricsSummaryDto>(HttpMethod.Post, "/api/v1/integrations/campaigns/metrics/sync",
            new { workspaceId, connectedAdAccountId = accountId, platform = "MetaAds", startDateUtc = since.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), endDateUtc = until.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), granularity = "Daily" }, cancellationToken);
        return metrics.IsFailure ? Result<SyncCampaignHierarchySummaryDto>.Failure(metrics.Error) : hierarchy;
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<MetaInsight>>> GetInsightsAsync(Guid workspaceId, Guid accountId, MetaInsightsFilter filter, CancellationToken cancellationToken = default)
    {
        var uri = $"/api/v1/integrations/meta/insights?workspaceId={workspaceId}&accountId={accountId}&since={filter.Since:yyyy-MM-dd}&until={filter.Until:yyyy-MM-dd}&level={Uri.EscapeDataString(filter.Level)}";
        if (!string.IsNullOrWhiteSpace(filter.ConversionAction)) uri += "&conversionAction=" + Uri.EscapeDataString(filter.ConversionAction);
        if (filter.Breakdown is not null) uri += "&breakdown=" + Uri.EscapeDataString(filter.Breakdown);
        if (filter.CampaignId is not null) uri += "&campaignId=" + Uri.EscapeDataString(filter.CampaignId);
        if (filter.AdSetId is not null) uri += "&adSetId=" + Uri.EscapeDataString(filter.AdSetId);
        if (filter.AdId is not null) uri += "&adId=" + Uri.EscapeDataString(filter.AdId);
        return SendAsync<IReadOnlyList<MetaInsight>>(HttpMethod.Get, uri, null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<MetaWriteResult>> WriteAsync(WriteMetaResourceCommand request, CancellationToken cancellationToken = default) =>
        SendAsync<MetaWriteResult>(HttpMethod.Post, "/api/v1/integrations/meta/resources", request, cancellationToken);

    /// <inheritdoc />
    public Task<Result<JsonElement>> GetResourceAsync(Guid workspaceId, Guid accountId, string resource, string externalId, CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Get, $"/api/v1/integrations/meta/resources/{Uri.EscapeDataString(resource)}/{Uri.EscapeDataString(externalId)}?workspaceId={workspaceId}&accountId={accountId}", null, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<JsonElement>>> GetAssetsAsync(Guid workspaceId, Guid accountId, string asset, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<JsonElement>>(HttpMethod.Get, $"/api/v1/integrations/meta/assets/{Uri.EscapeDataString(asset)}?workspaceId={workspaceId}&accountId={accountId}", null, cancellationToken);

    /// <inheritdoc />
    public async Task<Result<MetaMediaResult>> UploadAsync(Guid workspaceId, Guid accountId, Guid operationId, string name, string contentType, byte[] content, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(workspaceId.ToString()), "workspaceId");
        form.Add(new StringContent(accountId.ToString()), "accountId");
        form.Add(new StringContent(operationId.ToString()), "operationId");
        var file = new ByteArrayContent(content); file.Headers.ContentType = new(contentType);
        form.Add(file, "file", name);
        return await SendAsync<MetaMediaResult>(HttpMethod.Post, "/api/v1/integrations/meta/media", form, cancellationToken);
    }

    private async Task<Result<T>> SendAsync<T>(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        if (session.CurrentSession is not { } authenticated) return Result<T>.Failure(Error.Unauthorized("Auth.Unauthorized", "Entre novamente para gerenciar anúncios."));
        try
        {
            using var request = new HttpRequestMessage(method, uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authenticated.AccessToken);
            request.Headers.Add("X-Tenant-Id", authenticated.TenantId.ToString());
            if (body is HttpContent content) request.Content = content;
            else if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await client.SendAsync(request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<Result<T>>(JsonOptions, cancellationToken) ?? Result<T>.Failure(Error.Failure("Meta.InvalidResponse", "A API retornou uma resposta inválida."));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return Result<T>.Failure(Error.Failure("Meta.RequestTimeout", "A resposta demorou além do esperado. Consulte o estado da operação antes de repetir.")); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        { return Result<T>.Failure(Error.Failure("Meta.NetworkError", "Não foi possível consultar a API. Escritas podem exigir consulta do estado antes de repetir.")); }
    }
}
