using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Tenants.Queries.GetTenantCustomDomain;
using WebApp.State;

namespace WebApp.Services;

/// <summary>
/// Implementação concreta de <see cref="ITenantCnameClientService"/> consumindo os endpoints da Web API.
/// </summary>
public sealed class TenantCnameClientService : ITenantCnameClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantCnameClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado para a Web API.</param>
    /// <param name="tenantStateProvider">Provedor de estado do inquilino na sessão.</param>
    public TenantCnameClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<TenantCustomDomainDto>> GetCnameDetailsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tenants/cname");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<TenantCustomDomainDto>>(JsonOptions, cancellationToken);

            return result ?? Result<TenantCustomDomainDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API de CNAME retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<TenantCustomDomainDto>.Failure(
                Error.Failure("Http.Exception", $"Falha na comunicação com a API de CNAME: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> ConfigureCnameAsync(string customDomain, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { CustomDomain = customDomain };
            using var message = new HttpRequestMessage(HttpMethod.Put, "/api/v1/tenants/cname")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API de CNAME retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Http.Exception", $"Falha na comunicação com a API de CNAME: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> RemoveCnameAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/tenants/cname");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API de CNAME retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Http.Exception", $"Falha na comunicação com a API de CNAME: {ex.Message}"));
        }
    }

    private void AppendTenantHeader(HttpRequestMessage message)
    {
        var tenantId = _tenantStateProvider.CurrentTenant.TenantId;
        if (tenantId != Guid.Empty)
        {
            message.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.ToString());
        }
    }
}
