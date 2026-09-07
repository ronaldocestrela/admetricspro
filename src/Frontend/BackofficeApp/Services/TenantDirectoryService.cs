using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Tenants.Queries.GetTenantDetails;
using BackofficeApp.Models;

namespace BackofficeApp.Services;

/// <summary>
/// Implementação do serviço de diretório 360º para consumo dos componentes do Blazor Server no Backoffice.
/// Consome as rotas versionadas da Web API via cliente HTTP fortemente tipado.
/// </summary>
public sealed class TenantDirectoryService : ITenantDirectoryService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantDirectoryService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado para acesso à Web API.</param>
    public TenantDirectoryService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TenantDirectoryItemViewModel>>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/tenants", cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<TenantDetailsResponse>>>(JsonOptions, cancellationToken);

            if (result is null || result.IsFailure)
            {
                return Result<IReadOnlyList<TenantDirectoryItemViewModel>>.Failure(
                    result?.Error ?? Error.Failure("Tenants.FetchFailed", "Falha ao obter catálogo de inquilinos da API."));
            }

            var viewModels = result.Value.Select(t => new TenantDirectoryItemViewModel(
                Id: t.Id,
                CompanyName: t.CompanyName,
                Cnpj: t.Cnpj,
                Subdomain: t.Subdomain,
                Status: t.Status,
                Tier: t.Tier,
                SubscriptionExpiresAtUtc: t.SubscriptionExpiresAtUtc,
                CreatedAtUtc: t.CreatedAtUtc,
                WorkspacesCount: 1,
                SunkAdSpend: 0m
            )).ToList();

            return Result<IReadOnlyList<TenantDirectoryItemViewModel>>.Success(viewModels);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<TenantDirectoryItemViewModel>>.Failure(Error.Failure("Tenants.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Tenant360DetailsViewModel>> GetTenant360DetailsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<Tenant360DetailsViewModel>.Failure(Error.Validation("Tenant.InvalidId", "O identificador do tenant não pode ser vazio."));
        }

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/tenants/{tenantId}", cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<TenantDetailsResponse>>(JsonOptions, cancellationToken);

            if (result is null || result.IsFailure)
            {
                return Result<Tenant360DetailsViewModel>.Failure(
                    result?.Error ?? Error.NotFound("Tenant.NotFound", "Inquilino não localizado na API."));
            }

            var details = result.Value;
            var viewModel = new Tenant360DetailsViewModel(
                Id: details.Id,
                CompanyName: details.CompanyName,
                Cnpj: details.Cnpj,
                Subdomain: details.Subdomain,
                CustomDomain: null,
                Status: details.Status,
                Tier: details.Tier,
                SubscriptionExpiresAtUtc: details.SubscriptionExpiresAtUtc,
                CreatedAtUtc: details.CreatedAtUtc,
                WorkspacesCount: 1,
                SunkAdSpend: 0m,
                ActiveIntegrationsCount: 0,
                TotalCampaignsCount: 0
            );

            return Result<Tenant360DetailsViewModel>.Success(viewModel);
        }
        catch (Exception ex)
        {
            return Result<Tenant360DetailsViewModel>.Failure(Error.Failure("Tenant.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> SuspendTenantAsync(Guid tenantId, string reason, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Tenant.InvalidId", "O identificador do tenant não pode ser vazio."));
        }

        try
        {
            var payload = new { Reason = reason };
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/tenants/{tenantId}/suspend", payload, JsonOptions, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(Error.Failure("Tenant.SuspendFailed", "Falha ao suspender inquilino na API."));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Tenant.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> ReactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Tenant.InvalidId", "O identificador do tenant não pode ser vazio."));
        }

        try
        {
            var response = await _httpClient.PostAsync($"/api/v1/tenants/{tenantId}/reactivate", null, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(Error.Failure("Tenant.ReactivateFailed", "Falha ao reativar inquilino na API."));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Tenant.NetworkError", ex.Message));
        }
    }
}
