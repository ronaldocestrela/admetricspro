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
                SyncTenantBrandingState(result.Value);
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

    /// <inheritdoc />
    public async Task<Result<TenantBrandingDetailsDto>> UploadBrandAssetAsync(
        string slot,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var multipart = new MultipartFormDataContent();
            var streamContent = new StreamContent(content);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            multipart.Add(streamContent, "file", fileName);

            using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/tenants/branding/assets/{slot}")
            {
                Content = multipart
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<TenantBrandingDetailsDto>>(JsonOptions, cancellationToken);

            if (result is not null && result.IsSuccess)
            {
                SyncTenantBrandingState(result.Value);
            }

            return result ?? Result<TenantBrandingDetailsDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API de upload de branding retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<TenantBrandingDetailsDto>.Failure(
                Error.Failure("Http.Exception", $"Falha no upload de branding: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<TenantBrandingDetailsDto>> DeleteBrandAssetAsync(
        string slot,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/tenants/branding/assets/{slot}");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<TenantBrandingDetailsDto>>(JsonOptions, cancellationToken);

            if (result is not null && result.IsSuccess)
            {
                SyncTenantBrandingState(result.Value);
            }

            return result ?? Result<TenantBrandingDetailsDto>.Failure(
                Error.Failure("Http.EmptyResponse", "A resposta da API de exclusão de branding retornou vazia."));
        }
        catch (Exception ex)
        {
            return Result<TenantBrandingDetailsDto>.Failure(
                Error.Failure("Http.Exception", $"Falha ao remover ativo de branding: {ex.Message}"));
        }
    }

    private void SyncTenantBrandingState(TenantBrandingDetailsDto value)
    {
        var current = _tenantStateProvider.CurrentTenant;
        var updatedBranding = new TenantBranding(
            PrimaryColor: value.PrimaryColor,
            SecondaryColor: value.SecondaryColor,
            AccentColor: current.Branding.AccentColor,
            LogoUrl: value.LightLogoUrl ?? current.Branding.LogoUrl,
            DarkLogoUrl: value.DarkLogoUrl ?? current.Branding.DarkLogoUrl,
            FaviconUrl: value.FaviconUrl ?? current.Branding.FaviconUrl,
            CompanyName: current.Branding.CompanyName,
            ShowPoweredBy: current.Branding.ShowPoweredBy);

        _tenantStateProvider.SetTenant(current with { Branding = updatedBranding });
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
