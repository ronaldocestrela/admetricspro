using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Squads.DTOs;
using Tenants.Application.Users.DTOs;
using WebApp.Models;
using WebApp.State;

namespace WebApp.Services;

/// <summary>
/// Implementação concreta de <see cref="ITenantTeamClientService"/> consumindo a Web API.
/// </summary>
public sealed class TenantTeamClientService : ITenantTeamClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantTeamClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado apontando para a Web API.</param>
    /// <param name="tenantStateProvider">Provedor de estado do inquilino ativo.</param>
    public TenantTeamClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> InviteUserAsync(
        InviteTenantUserModel model,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            return Result<Guid>.Failure(
                Error.Validation("Request.Null", "Os dados do colaborador não podem ser nulos."));
        }

        try
        {
            var payload = new
            {
                FullName = model.FullName,
                Email = model.Email,
                Role = model.Role,
                PhoneNumber = model.PhoneNumber
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/tenants/users")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<Guid>>(JsonOptions, cancellationToken);

            return result ?? Result<Guid>.Failure(
                Error.Failure("TenantUser.InvalidResponse", "Resposta inválida ao cadastrar colaborador."));
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(
                Error.Failure("TenantUser.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
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
    public async Task<Result<IReadOnlyList<TenantUserDto>>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tenants/users");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<TenantUserDto>>>(JsonOptions, cancellationToken);

            return result ?? Result<IReadOnlyList<TenantUserDto>>.Failure(
                Error.Failure("TenantUser.InvalidResponse", "Resposta inválida ao listar colaboradores."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<TenantUserDto>>.Failure(
                Error.Failure("TenantUser.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SquadSummaryDto>>> GetSquadsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, "/api/v1/squads");
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

    private void AppendTenantHeader(HttpRequestMessage request)
    {
        var tenantId = _tenantStateProvider.CurrentTenant?.TenantId;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.Value.ToString());
        }
    }
}
