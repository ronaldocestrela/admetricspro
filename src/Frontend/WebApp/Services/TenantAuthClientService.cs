using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Auth.DTOs;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Implementação concreta do cliente HTTP tipado de autenticação e identificação de inquilinos (<see cref="ITenantAuthClientService"/>).
/// Consome exclusivamente as rotas da Web API tratando envelopes <see cref="Result{T}"/>.
/// </summary>
public sealed class TenantAuthClientService : ITenantAuthClientService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantAuthClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado apontando para a base address da Web API.</param>
    public TenantAuthClientService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result<TenantPublicBrandingViewModel>> GetPublicBrandingAsync(
        string subdomain,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return Result<TenantPublicBrandingViewModel>.Failure(
                Error.Validation("Subdomain.Required", "O subdomínio da agência é obrigatório."));
        }

        try
        {
            var encodedSubdomain = Uri.EscapeDataString(subdomain.Trim().ToLowerInvariant());
            var response = await _httpClient.GetAsync(
                $"/api/v1/tenants/auth/branding?subdomain={encodedSubdomain}",
                cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<Result<TenantPublicBrandingDto>>(
                JsonOptions,
                cancellationToken);

            if (result is null || result.IsFailure)
            {
                return Result<TenantPublicBrandingViewModel>.Failure(
                    result?.Error ?? Error.NotFound("Tenant.NotFound", "Inquilino não localizado na API."));
            }

            var dto = result.Value;
            var viewModel = new TenantPublicBrandingViewModel(
                TenantId: dto.TenantId,
                CompanyName: dto.CompanyName,
                Subdomain: dto.Subdomain,
                CustomDomain: dto.CustomDomain,
                PrimaryColor: dto.PrimaryColor,
                SecondaryColor: dto.SecondaryColor,
                LogoUrl: dto.LogoUrl,
                IsActive: dto.IsActive);

            return Result<TenantPublicBrandingViewModel>.Success(viewModel);
        }
        catch (Exception ex)
        {
            return Result<TenantPublicBrandingViewModel>.Failure(
                Error.Failure("Tenant.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticatedTenantUserDto>> LoginAsync(
        TenantLoginModel model,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            return Result<AuthenticatedTenantUserDto>.Failure(
                Error.Validation("Model.Null", "Os dados de login não podem ser nulos."));
        }

        var validationResult = model.Validate();
        if (validationResult.IsFailure)
        {
            return Result<AuthenticatedTenantUserDto>.Failure(validationResult.Error);
        }

        try
        {
            var payload = new
            {
                Email = model.Email.Trim(),
                Password = model.Password,
                Subdomain = string.IsNullOrWhiteSpace(model.Subdomain) ? null : model.Subdomain.Trim().ToLowerInvariant()
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/tenants/auth/login",
                payload,
                JsonOptions,
                cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<Result<AuthenticatedTenantUserDto>>(
                JsonOptions,
                cancellationToken);

            if (result is null)
            {
                return Result<AuthenticatedTenantUserDto>.Failure(
                    Error.Failure("Auth.UnknownError", "Resposta vazia retornada pelo servidor."));
            }

            return result;
        }
        catch (Exception ex)
        {
            return Result<AuthenticatedTenantUserDto>.Failure(
                Error.Failure("Auth.NetworkError", $"Erro ao conectar ao servidor de autenticação: {ex.Message}"));
        }
    }
}
