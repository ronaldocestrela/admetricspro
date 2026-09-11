using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Squads.DTOs;
using Tenants.Application.Workspaces.DTOs;
using WebApp.Models;
using WebApp.State;

namespace WebApp.Services;

/// <summary>
/// Implementação concreta do serviço de cliente HTTP para gestão de Squads e carteira de clientes.
/// </summary>
public sealed class SquadClientService : ISquadClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SquadClientService"/>.
    /// </summary>
    /// <param name="httpClient">Instância injetada do cliente HTTP apontando para a Web API.</param>
    /// <param name="tenantStateProvider">Provedor do estado contextual do inquilino.</param>
    public SquadClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateSquadAsync(
        CreateSquadModel model,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            return Result<Guid>.Failure(
                Error.Validation("Request.Null", "Os dados do squad não podem ser nulos."));
        }

        try
        {
            var payload = new
            {
                Name = model.Name,
                Description = model.Description
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/squads")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<Guid>>(JsonOptions, cancellationToken);

            return result ?? Result<Guid>.Failure(
                Error.Failure("Squad.InvalidResponse", "Resposta inválida ao cadastrar squad."));
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(
                Error.Failure("Squad.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SquadSummaryDto>>> GetSquadsAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = activeOnly.HasValue
                ? $"/api/v1/squads?activeOnly={activeOnly.Value.ToString().ToLowerInvariant()}"
                : "/api/v1/squads";

            using var message = new HttpRequestMessage(HttpMethod.Get, uri);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<SquadSummaryDto>>>(JsonOptions, cancellationToken);

            return result ?? Result<IReadOnlyList<SquadSummaryDto>>.Failure(
                Error.Failure("Squad.InvalidResponse", "Resposta inválida ao listar squads."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<SquadSummaryDto>>.Failure(
                Error.Failure("Squad.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<SquadDetailsDto>> GetSquadByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/squads/{id}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<SquadDetailsDto>>(JsonOptions, cancellationToken);

            return result ?? Result<SquadDetailsDto>.Failure(
                Error.Failure("Squad.InvalidResponse", "Resposta inválida ao obter detalhes do squad."));
        }
        catch (Exception ex)
        {
            return Result<SquadDetailsDto>.Failure(
                Error.Failure("Squad.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateSquadAsync(
        Guid id,
        UpdateSquadModel model,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            return Result.Failure(
                Error.Validation("Request.Null", "Os dados do squad não podem ser nulos."));
        }

        try
        {
            var payload = new
            {
                Name = model.Name,
                Description = model.Description
            };

            using var message = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/squads/{id}")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("Squad.InvalidResponse", "Resposta inválida ao atualizar squad."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Squad.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> ToggleSquadStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { IsActive = isActive };
            using var message = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/squads/{id}/status")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("Squad.InvalidResponse", "Resposta inválida ao alternar status do squad."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Squad.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> AddSquadMemberAsync(
        Guid squadId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { UserId = userId };
            using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/squads/{squadId}/members")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("Squad.InvalidResponse", "Resposta inválida ao vincular colaborador ao squad."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Squad.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> RemoveSquadMemberAsync(
        Guid squadId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/squads/{squadId}/members/{userId}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("Squad.InvalidResponse", "Resposta inválida ao desvincular colaborador do squad."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Squad.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> AssignSquadWorkspaceAsync(
        Guid squadId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { WorkspaceId = workspaceId };
            using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/squads/{squadId}/workspaces")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("Squad.InvalidResponse", "Resposta inválida ao alocar workspace ao squad."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Squad.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> UnassignSquadWorkspaceAsync(
        Guid squadId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/squads/{squadId}/workspaces/{workspaceId}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("Squad.InvalidResponse", "Resposta inválida ao desassociar workspace do squad."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Squad.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<WorkspaceDto>>> GetUserPortfolioAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/squads/users/{userId}/portfolio");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<WorkspaceDto>>>(JsonOptions, cancellationToken);

            return result ?? Result<IReadOnlyList<WorkspaceDto>>.Failure(
                Error.Failure("Squad.InvalidResponse", "Resposta inválida ao consultar carteira do colaborador."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<WorkspaceDto>>.Failure(
                Error.Failure("Squad.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
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
