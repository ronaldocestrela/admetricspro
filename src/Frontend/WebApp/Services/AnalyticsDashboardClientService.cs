using System.Net.Http.Json;
using System.Text.Json;
using Analytics.Application.Dashboard.DTOs;
using BuildingBlocks.Domain.Primitives;
using WebApp.Models;
using WebApp.State;

namespace WebApp.Services;

/// <summary>
/// Implementação concreta de <see cref="IAnalyticsDashboardClientService"/> que consome a Web API.
/// </summary>
public sealed class AnalyticsDashboardClientService : IAnalyticsDashboardClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AnalyticsDashboardClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado para o WebApi.</param>
    /// <param name="tenantStateProvider">Provedor de estado de tenant do circuito Blazor.</param>
    public AnalyticsDashboardClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<ExecutiveDashboardDto>> GetExecutiveDashboardAsync(
        DashboardFiltersState filters,
        CancellationToken cancellationToken = default)
    {
        if (filters is null)
        {
            return Result<ExecutiveDashboardDto>.Failure(
                Error.Validation("Request.Null", "Os filtros do painel não podem ser nulos."));
        }

        try
        {
            var start = filters.GetEffectiveStartDate().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var end = filters.GetEffectiveEndDate().ToString("yyyy-MM-ddTHH:mm:ssZ");

            var queryParams = new List<string>
            {
                $"startDateUtc={Uri.EscapeDataString(start)}",
                $"endDateUtc={Uri.EscapeDataString(end)}",
                $"platform={Uri.EscapeDataString(filters.Platform ?? "All")}",
                $"device={Uri.EscapeDataString(filters.Device ?? "All")}",
                $"currency={Uri.EscapeDataString(filters.Currency ?? "BRL")}"
            };

            if (filters.WorkspaceId.HasValue && filters.WorkspaceId.Value != Guid.Empty)
            {
                queryParams.Add($"workspaceId={filters.WorkspaceId.Value}");
            }

            var requestUri = $"/api/v1/analytics/dashboard/executive?{string.Join("&", queryParams)}";
            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<ExecutiveDashboardDto>>(JsonOptions, cancellationToken);

            return result ?? Result<ExecutiveDashboardDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API de dashboard retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<ExecutiveDashboardDto>.Failure(
                Error.Failure("Http.Exception", $"Falha ao consultar dashboard executivo: {ex.Message}"));
        }
    }

    private void AppendTenantHeader(HttpRequestMessage message)
    {
        var tenantId = _tenantStateProvider.CurrentTenant?.TenantId ?? Guid.Empty;
        if (tenantId != Guid.Empty)
        {
            message.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.ToString());
        }
    }
}
