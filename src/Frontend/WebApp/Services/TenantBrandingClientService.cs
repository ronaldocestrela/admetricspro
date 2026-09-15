using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Branding.DTOs;
using WebApp.Models;
using WebApp.State;

namespace WebApp.Services;

/// <summary>
/// Implementação concreta de <see cref="ITenantBrandingClientService"/> consumindo a Web API.
/// </summary>
public sealed class TenantBrandingClientService : ITenantBrandingClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantBrandingClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado para a Web API.</param>
    /// <param name="tenantStateProvider">Provedor de estado do inquilino na sessão.</param>
    public TenantBrandingClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<TenantBrandingDetailsDto>> GetBrandingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tenants/branding");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<TenantBrandingDetailsDto>>(JsonOptions, cancellationToken);

            return result ?? Result<TenantBrandingDetailsDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API de branding retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<TenantBrandingDetailsDto>.Failure(
                Error.Failure("Http.Exception", $"Falha na comunicação com a API de branding: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<TenantBrandingDetailsDto>> UpdateBrandingAsync(
        UpdateTenantBrandingModel model,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            return Result<TenantBrandingDetailsDto>.Failure(
                Error.Validation("Request.Null", "Os dados de branding não podem ser nulos."));
        }

        try
        {
            var payload = new
            {
                PrimaryColor = model.PrimaryColor,
                SecondaryColor = model.SecondaryColor,
                LightLogoUrl = model.LightLogoUrl,
                DarkLogoUrl = model.DarkLogoUrl,
                FaviconUrl = model.FaviconUrl
            };

            using var message = new HttpRequestMessage(HttpMethod.Put, "/api/v1/tenants/branding")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<TenantBrandingDetailsDto>>(JsonOptions, cancellationToken);

            if (result is not null && result.IsSuccess)
            {
                // Sincroniza dinamicamente o estado do inquilino ativo no circuito Blazor
                var current = _tenantStateProvider.CurrentTenant;
                var updatedBranding = new TenantBranding(
                    PrimaryColor: result.Value.PrimaryColor,
                    SecondaryColor: result.Value.SecondaryColor,
                    AccentColor: current.Branding.AccentColor,
                    LogoUrl: result.Value.LightLogoUrl ?? current.Branding.LogoUrl,
                    DarkLogoUrl: result.Value.DarkLogoUrl ?? current.Branding.DarkLogoUrl,
                    FaviconUrl: result.Value.FaviconUrl ?? current.Branding.FaviconUrl,
                    CompanyName: current.Branding.CompanyName,
                    ShowPoweredBy: current.Branding.ShowPoweredBy);

                _tenantStateProvider.SetTenant(current with { Branding = updatedBranding });
            }

            return result ?? Result<TenantBrandingDetailsDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API de branding retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<TenantBrandingDetailsDto>.Failure(
                Error.Failure("Http.Exception", $"Falha na comunicação com a API de branding: {ex.Message}"));
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
