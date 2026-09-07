using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Integrations.DTOs;
using Master.Domain.Integrations;

namespace WebApp.Services;

/// <summary>
/// Implementação do serviço cliente para monitoramento de saúde de APIs e cotas no Blazor Server.
/// Consome as rotas versionadas da Web API via cliente HTTP fortemente tipado.
/// </summary>
public sealed class ApiHealthClientService : IApiHealthClientService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ApiHealthClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado para acesso à Web API.</param>
    public ApiHealthClientService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result<ApiHealthOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/admin/api-health", cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<ApiHealthOverviewDto>>(JsonOptions, cancellationToken);
            return result ?? Result<ApiHealthOverviewDto>.Failure(Error.Failure("ApiHealth.FetchFailed", "Falha ao obter dados de saúde de APIs da API."));
        }
        catch (Exception ex)
        {
            return Result<ApiHealthOverviewDto>.Failure(Error.Failure("ApiHealth.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TenantApiConnectionDto>>> GetConnectionsAsync(
        AdPlatform? platform = null,
        ApiConnectionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string> { "pageNumber=1", "pageSize=100" };
            if (platform.HasValue) queryParams.Add($"platform={platform.Value}");
            if (status.HasValue) queryParams.Add($"status={status.Value}");

            var queryString = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"/api/v1/admin/api-health/connections?{queryString}", cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<TenantApiConnectionDto>>>(JsonOptions, cancellationToken);

            return result ?? Result<IReadOnlyList<TenantApiConnectionDto>>.Failure(
                Error.Failure("ApiHealth.ConnectionsFetchFailed", "Falha ao obter conexões de APIs da API."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<TenantApiConnectionDto>>.Failure(Error.Failure("ApiHealth.NetworkError", ex.Message));
        }
    }
}
