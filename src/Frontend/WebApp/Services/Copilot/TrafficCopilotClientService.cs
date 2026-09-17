using System.Net.Http.Json;
using System.Text.Json;
using Analytics.Application.Copilot.DTOs;
using BuildingBlocks.Domain.Primitives;
using WebApp.State;

namespace WebApp.Services.Copilot;

/// <summary>
/// Implementação concreta de <see cref="ITrafficCopilotClientService"/> consumindo a Web API AdMetricsPro.
/// </summary>
public sealed class TrafficCopilotClientService : ITrafficCopilotClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TrafficCopilotClientService"/>.
    /// </summary>
    public TrafficCopilotClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<DailyDiagnosticReportDto>> GetDailyDiagnosticAsync(
        Guid workspaceId,
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<DailyDiagnosticReportDto>.Failure(
                Error.Validation("Workspace.InvalidId", "O identificador do Workspace é obrigatório."));
        }

        try
        {
            var queryParams = new List<string> { $"workspaceId={workspaceId}" };
            if (date.HasValue)
            {
                queryParams.Add($"date={Uri.EscapeDataString(date.Value.ToString("O"))}");
            }

            var requestUri = $"/api/v1/analytics/copilot/diagnostic?{string.Join("&", queryParams)}";
            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var contentString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(contentString))
            {
                return Result<DailyDiagnosticReportDto>.Failure(
                    Error.Failure("Http.EmptyResponse", $"A API retornou status {(int)response.StatusCode} ({response.ReasonPhrase}) sem conteúdo."));
            }

            try
            {
                var result = JsonSerializer.Deserialize<Result<DailyDiagnosticReportDto>>(contentString, JsonOptions);
                return result ?? Result<DailyDiagnosticReportDto>.Failure(
                    Error.Failure("Http.EmptyResponse", "A resposta da API retornou vazia."));
            }
            catch (JsonException ex)
            {
                return Result<DailyDiagnosticReportDto>.Failure(
                    Error.Failure("Http.InvalidJson", $"Falha ao interpretar resposta da API (status {(int)response.StatusCode}): {ex.Message}"));
            }
        }
        catch (Exception ex)
        {
            return Result<DailyDiagnosticReportDto>.Failure(
                Error.Failure("Http.RequestFailed", $"Falha ao consultar diagnóstico do Copiloto: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<ExecuteCopilotActionResultDto>> ExecuteActionAsync(
        Guid workspaceId,
        CopilotRecommendationActionDto action,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<ExecuteCopilotActionResultDto>.Failure(
                Error.Validation("Workspace.InvalidId", "O identificador do Workspace é obrigatório."));
        }

        if (action is null)
        {
            return Result<ExecuteCopilotActionResultDto>.Failure(
                Error.Validation("Copilot.NullAction", "A ação de recomendação não pode ser nula."));
        }

        try
        {
            var requestBody = new
            {
                WorkspaceId = workspaceId,
                ActionId = action.ActionId,
                ActionType = action.ActionType,
                TargetEntityId = action.TargetEntityId,
                TargetEntityName = action.TargetEntityName,
                Platform = action.Platform,
                Title = action.Title,
                Description = action.Description,
                Parameters = action.Parameters
            };

            var requestUri = "/api/v1/analytics/copilot/actions/execute";
            using var message = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(requestBody, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var contentString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(contentString))
            {
                return Result<ExecuteCopilotActionResultDto>.Failure(
                    Error.Failure("Http.EmptyResponse", $"A API retornou status {(int)response.StatusCode} ({response.ReasonPhrase}) sem conteúdo."));
            }

            try
            {
                var result = JsonSerializer.Deserialize<Result<ExecuteCopilotActionResultDto>>(contentString, JsonOptions);
                return result ?? Result<ExecuteCopilotActionResultDto>.Failure(
                    Error.Failure("Http.EmptyResponse", "A resposta da API retornou vazia."));
            }
            catch (JsonException ex)
            {
                return Result<ExecuteCopilotActionResultDto>.Failure(
                    Error.Failure("Http.InvalidJson", $"Falha ao interpretar resposta da API (status {(int)response.StatusCode}): {ex.Message}"));
            }
        }
        catch (Exception ex)
        {
            return Result<ExecuteCopilotActionResultDto>.Failure(
                Error.Failure("Http.RequestFailed", $"Falha ao despachar ação do Copiloto: {ex.Message}"));
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
