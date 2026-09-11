using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Workspaces.DTOs;
using WebApp.Models;
using WebApp.State;

namespace WebApp.Services;

/// <summary>
/// Implementação concreta de <see cref="IWorkspaceClientService"/> consumindo a Web API.
/// </summary>
public sealed class WorkspaceClientService : IWorkspaceClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="WorkspaceClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP para consumo da Web API.</param>
    /// <param name="tenantStateProvider">Provedor de estado do inquilino ativo.</param>
    public WorkspaceClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateWorkspaceAsync(
        CreateWorkspaceModel model,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            return Result<Guid>.Failure(
                Error.Validation("Request.Null", "Os dados do workspace não podem ser nulos."));
        }

        try
        {
            var payload = new
            {
                Name = model.Name,
                CnpjOrCpf = model.CnpjOrCpf,
                MonthlyAdSpendBudget = model.MonthlyAdSpendBudget,
                Segment = model.Segment
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workspaces")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<Guid>>(JsonOptions, cancellationToken);

            return result ?? Result<Guid>.Failure(
                Error.Failure("Workspace.InvalidResponse", "Resposta inválida ao cadastrar workspace."));
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(
                Error.Failure("Workspace.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<WorkspaceDto>>> GetWorkspacesAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = activeOnly.HasValue
                ? $"/api/v1/workspaces?activeOnly={activeOnly.Value.ToString().ToLowerInvariant()}"
                : "/api/v1/workspaces";

            using var message = new HttpRequestMessage(HttpMethod.Get, uri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<WorkspaceDto>>>(JsonOptions, cancellationToken);

            return result ?? Result<IReadOnlyList<WorkspaceDto>>.Failure(
                Error.Failure("Workspace.InvalidResponse", "Resposta inválida ao listar workspaces."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<WorkspaceDto>>.Failure(
                Error.Failure("Workspace.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<WorkspaceDto>> GetWorkspaceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/workspaces/{id}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<WorkspaceDto>>(JsonOptions, cancellationToken);

            return result ?? Result<WorkspaceDto>.Failure(
                Error.Failure("Workspace.InvalidResponse", "Resposta inválida ao obter dados do workspace."));
        }
        catch (Exception ex)
        {
            return Result<WorkspaceDto>.Failure(
                Error.Failure("Workspace.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateWorkspaceAsync(
        Guid id,
        UpdateWorkspaceModel model,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            return Result.Failure(
                Error.Validation("Request.Null", "Os dados do workspace não podem ser nulos."));
        }

        try
        {
            var payload = new
            {
                Name = model.Name,
                CnpjOrCpf = model.CnpjOrCpf,
                MonthlyAdSpendBudget = model.MonthlyAdSpendBudget,
                Segment = model.Segment
            };

            using var message = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/workspaces/{id}")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("Workspace.InvalidResponse", "Resposta inválida ao atualizar workspace."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Workspace.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> ToggleWorkspaceStatusAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/workspaces/{id}/toggle-status");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("Workspace.InvalidResponse", "Resposta inválida ao alternar status do workspace."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Workspace.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    private void AppendTenantHeader(HttpRequestMessage request)
    {
        var tenantId = _tenantStateProvider.CurrentTenant?.TenantId;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.Value.ToString());
        }
    }
}
