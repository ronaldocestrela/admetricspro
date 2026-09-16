using System.Net.Http.Json;
using System.Text.Json;
using Automations.Application.Pacing.DTOs;
using BuildingBlocks.Domain.Primitives;
using WebApp.State;

namespace WebApp.Services;

/// <summary>
/// Implementação concreta de <see cref="IBudgetPacingClientService"/> que consome a Web API AdMetricsPro.
/// </summary>
public sealed class BudgetPacingClientService : IBudgetPacingClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BudgetPacingClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado apontando para a Web API.</param>
    /// <param name="tenantStateProvider">Provedor de estado do inquilino atual na sessão.</param>
    public BudgetPacingClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<WorkspaceBudgetPacingDto>> GetWorkspacePacingAsync(
        Guid workspaceId,
        int? year = null,
        int? month = null,
        DateTime? asOfDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<WorkspaceBudgetPacingDto>.Failure(
                Error.Validation("Workspace.InvalidId", "O identificador do Workspace é obrigatório."));
        }

        try
        {
            var queryParams = new List<string>();
            if (year.HasValue) queryParams.Add($"year={year.Value}");
            if (month.HasValue) queryParams.Add($"month={month.Value}");
            if (asOfDateUtc.HasValue) queryParams.Add($"asOfDateUtc={Uri.EscapeDataString(asOfDateUtc.Value.ToString("O"))}");

            var queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
            var requestUri = $"/api/v1/automations/pacing/workspaces/{workspaceId}{queryString}";

            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<WorkspaceBudgetPacingDto>>(JsonOptions, cancellationToken);

            return result ?? Result<WorkspaceBudgetPacingDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<WorkspaceBudgetPacingDto>.Failure(
                Error.Failure("Http.Exception", $"Falha ao consultar pacing do workspace: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PortfolioPacingSummaryDto>> GetPortfolioPacingAsync(
        Guid? squadId = null,
        int? year = null,
        int? month = null,
        DateTime? asOfDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (squadId.HasValue && squadId.Value != Guid.Empty) queryParams.Add($"squadId={squadId.Value}");
            if (year.HasValue) queryParams.Add($"year={year.Value}");
            if (month.HasValue) queryParams.Add($"month={month.Value}");
            if (asOfDateUtc.HasValue) queryParams.Add($"asOfDateUtc={Uri.EscapeDataString(asOfDateUtc.Value.ToString("O"))}");

            var queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
            var requestUri = $"/api/v1/automations/pacing/portfolio{queryString}";

            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<PortfolioPacingSummaryDto>>(JsonOptions, cancellationToken);

            return result ?? Result<PortfolioPacingSummaryDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<PortfolioPacingSummaryDto>.Failure(
                Error.Failure("Http.Exception", $"Falha ao consultar pacing da carteira: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<WorkspaceBudgetPacingDto>> SimulatePacingAsync(
        SimulatePacingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<WorkspaceBudgetPacingDto>.Failure(
                Error.Validation("Request.Null", "Os dados da simulação são obrigatórios."));
        }

        try
        {
            const string requestUri = "/api/v1/automations/pacing/simulate";
            using var message = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<WorkspaceBudgetPacingDto>>(JsonOptions, cancellationToken);

            return result ?? Result<WorkspaceBudgetPacingDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da simulação retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<WorkspaceBudgetPacingDto>.Failure(
                Error.Failure("Http.Exception", $"Falha ao simular pacing: {ex.Message}"));
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
