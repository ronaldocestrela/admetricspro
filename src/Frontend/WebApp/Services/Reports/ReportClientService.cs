using System.Net.Http.Json;
using System.Text.Json;
using Analytics.Application.Reports.DTOs;
using BuildingBlocks.Domain.Primitives;
using WebApp.State;

namespace WebApp.Services.Reports;

/// <summary>
/// Implementação concreta de <see cref="IReportClientService"/> consumindo a Web API autenticada.
/// </summary>
public sealed class ReportClientService : IReportClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ReportClientService"/>.
    /// </summary>
    public ReportClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ReportScheduleDto>>> GetSchedulesAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri = $"/api/v1/workspaces/{workspaceId}/reports/schedules";
            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<ReportScheduleDto>>>(JsonOptions, cancellationToken);
            return result ?? Result<IReadOnlyList<ReportScheduleDto>>.Failure(Error.Failure("Http.EmptyResponse", "Resposta vazia da API."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ReportScheduleDto>>.Failure(Error.Failure("Http.RequestFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateScheduleAsync(Guid workspaceId, CreateReportScheduleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri = $"/api/v1/workspaces/{workspaceId}/reports/schedules";
            using var message = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<Guid>>(JsonOptions, cancellationToken);
            return result ?? Result<Guid>.Failure(Error.Failure("Http.EmptyResponse", "Resposta vazia da API."));
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(Error.Failure("Http.RequestFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateScheduleAsync(Guid workspaceId, Guid scheduleId, CreateReportScheduleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri = $"/api/v1/workspaces/{workspaceId}/reports/schedules/{scheduleId}";
            using var message = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);
            return result ?? Result.Failure(Error.Failure("Http.EmptyResponse", "Resposta vazia da API."));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Http.RequestFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteScheduleAsync(Guid workspaceId, Guid scheduleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri = $"/api/v1/workspaces/{workspaceId}/reports/schedules/{scheduleId}";
            using var message = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);
            return result ?? Result.Failure(Error.Failure("Http.EmptyResponse", "Resposta vazia da API."));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Http.RequestFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> GenerateReportAsync(Guid workspaceId, GenerateReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri = $"/api/v1/workspaces/{workspaceId}/reports/generate";
            using var message = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<Guid>>(JsonOptions, cancellationToken);
            return result ?? Result<Guid>.Failure(Error.Failure("Http.EmptyResponse", "Resposta vazia da API."));
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(Error.Failure("Http.RequestFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> DispatchScheduleAsync(Guid workspaceId, Guid scheduleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri = $"/api/v1/workspaces/{workspaceId}/reports/schedules/{scheduleId}/dispatch";
            using var message = new HttpRequestMessage(HttpMethod.Post, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<Guid>>(JsonOptions, cancellationToken);
            return result ?? Result<Guid>.Failure(Error.Failure("Http.EmptyResponse", "Resposta vazia da API."));
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(Error.Failure("Http.RequestFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GeneratedReportSummaryDto>>> GetHistoryAsync(Guid workspaceId, int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri = $"/api/v1/workspaces/{workspaceId}/reports/history?limit={limit}";
            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<GeneratedReportSummaryDto>>>(JsonOptions, cancellationToken);
            return result ?? Result<IReadOnlyList<GeneratedReportSummaryDto>>.Failure(Error.Failure("Http.EmptyResponse", "Resposta vazia da API."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<GeneratedReportSummaryDto>>.Failure(Error.Failure("Http.RequestFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> DownloadPdfAsync(Guid workspaceId, Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri = $"/api/v1/workspaces/{workspaceId}/reports/{reportId}/pdf";
            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result<byte[]>.Failure(Error.Failure("Http.NotFound", "Não foi possível baixar o arquivo PDF."));
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return Result<byte[]>.Success(bytes);
        }
        catch (Exception ex)
        {
            return Result<byte[]>.Failure(Error.Failure("Http.RequestFailed", ex.Message));
        }
    }

    private void AppendTenantHeader(HttpRequestMessage message)
    {
        var tenantId = _tenantStateProvider.CurrentTenant?.TenantId;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            message.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.Value.ToString());
        }
    }
}
