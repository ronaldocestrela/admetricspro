using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Tenants.Commands.RegisterTenantOnboarding;
using Master.Application.Tenants.Queries.CheckSubdomainAvailability;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Implementação de <see cref="ITenantOnboardingClientService"/> para o ambiente Blazor Server.
/// Despacha consultas e comandos consumindo a Web API via cliente HTTP fortemente tipado.
/// </summary>
public sealed class TenantOnboardingClientService : ITenantOnboardingClientService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantOnboardingClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado para comunicação com a Web API.</param>
    public TenantOnboardingClientService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result<SubdomainAvailabilityResponse>> CheckSubdomainAvailabilityAsync(
        string subdomain,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/v1/tenants/check-subdomain?subdomain={Uri.EscapeDataString(subdomain ?? string.Empty)}",
                cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<Result<SubdomainAvailabilityResponse>>(JsonOptions, cancellationToken);
            return result ?? Result<SubdomainAvailabilityResponse>.Failure(Error.Failure("Onboarding.InvalidResponse", "Resposta inválida do servidor."));
        }
        catch (Exception ex)
        {
            return Result<SubdomainAvailabilityResponse>.Failure(Error.Failure("Onboarding.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<TenantOnboardingResult>> RegisterTenantAsync(
        TenantOnboardingFormModel model,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            return Result<TenantOnboardingResult>.Failure(
                Error.Validation("Onboarding.ModelNull", "Dados de onboarding não podem ser nulos."));
        }

        try
        {
            var payload = new
            {
                CompanyName = model.CompanyName,
                Cnpj = model.GetSanitizedCnpj(),
                Subdomain = model.GetSanitizedSubdomain(),
                Segment = model.Segment,
                MonthlyAdSpendRange = model.MonthlyAdSpendRange,
                Tier = model.Tier,
                BillingCycle = model.BillingCycle,
                AdminFullName = model.AdminFullName,
                AdminEmail = model.AdminEmail,
                AdminPhone = model.AdminPhone,
                AdminPassword = model.AdminPassword,
                CustomDomain = model.CustomDomain,
                PrimaryColor = model.PrimaryColor,
                SecondaryColor = model.SecondaryColor
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/tenants/onboarding",
                payload,
                JsonOptions,
                cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<Result<TenantOnboardingResult>>(JsonOptions, cancellationToken);
            return result ?? Result<TenantOnboardingResult>.Failure(Error.Failure("Onboarding.InvalidResponse", "Resposta inválida do servidor."));
        }
        catch (Exception ex)
        {
            return Result<TenantOnboardingResult>.Failure(Error.Failure("Onboarding.NetworkError", ex.Message));
        }
    }
}
