using BuildingBlocks.Domain.Primitives;
using Master.Application.Tenants.Commands.RegisterTenantOnboarding;
using Master.Application.Tenants.Queries.CheckSubdomainAvailability;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Contrato de serviço do Frontend para o fluxo de onboarding e provisionamento de novo inquilino.
/// </summary>
public interface ITenantOnboardingClientService
{
    /// <summary>
    /// Verifica a disponibilidade de um subdomínio em tempo real.
    /// </summary>
    /// <param name="subdomain">Subdomínio a ser checado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com a resposta de disponibilidade.</returns>
    Task<Result<SubdomainAvailabilityResponse>> CheckSubdomainAvailabilityAsync(
        string subdomain,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica a validade e disponibilidade de um documento fiscal (CPF ou CNPJ) em tempo real.
    /// </summary>
    /// <param name="document">Documento a ser validado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com o diagnóstico de validação e disponibilidade.</returns>
    Task<Result<Master.Application.Tenants.Queries.CheckTaxDocumentAvailability.TaxDocumentAvailabilityResponse>> CheckTaxDocumentAvailabilityAsync(
        string document,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submete os dados de cadastro e aciona o provisionamento do banco dedicado do novo inquilino.
    /// </summary>
    /// <param name="model">Modelo de formulário validado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo as credenciais e URL de acesso do novo tenant.</returns>
    Task<Result<TenantOnboardingResult>> RegisterTenantAsync(
        TenantOnboardingFormModel model,
        CancellationToken cancellationToken = default);
}
