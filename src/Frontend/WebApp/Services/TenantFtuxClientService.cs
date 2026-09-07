using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Ftux.DTOs;
using WebApp.Models;
using WebApp.State;

namespace WebApp.Services;

/// <summary>
/// Implementação concreta de <see cref="ITenantFtuxClientService"/> para o ambiente Blazor Server.
/// Despacha requisições HTTP para a Web API anexando o contexto do inquilino ativo.
/// </summary>
public sealed class TenantFtuxClientService : ITenantFtuxClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantFtuxClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado apontando para a Web API.</param>
    /// <param name="tenantStateProvider">Provedor de estado do inquilino ativo.</param>
    public TenantFtuxClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<TenantFtuxStatusDto>> GetFtuxStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tenants/ftux-status");
            AppendTenantHeader(request);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<TenantFtuxStatusDto>>(JsonOptions, cancellationToken);

            return result ?? Result<TenantFtuxStatusDto>.Failure(
                Error.Failure("Ftux.InvalidResponse", "Resposta inválida ao obter status do FTUX."));
        }
        catch (Exception ex)
        {
            return Result<TenantFtuxStatusDto>.Failure(
                Error.Failure("Ftux.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> ConnectDemoAccountAsync(
        ConnectDemoAccountModel model,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            return Result<Guid>.Failure(
                Error.Validation("Request.Null", "Os dados da requisição não podem ser nulos."));
        }

        try
        {
            var payload = new
            {
                WorkspaceId = model.WorkspaceId,
                Platform = model.Platform
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/integrations/demo-account")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<Guid>>(JsonOptions, cancellationToken);

            return result ?? Result<Guid>.Failure(
                Error.Failure("Ftux.InvalidResponse", "Resposta inválida ao vincular conta demo."));
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(
                Error.Failure("Ftux.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
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
