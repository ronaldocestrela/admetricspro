using BuildingBlocks.Domain.Primitives;
using Master.Application.Tenants.Commands.RegisterTenantOnboarding;
using Master.Application.Tenants.Queries.CheckSubdomainAvailability;
using MediatR;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Implementação de <see cref="ITenantOnboardingClientService"/> para o ambiente Blazor Server.
/// Despacha consultas e comandos diretamente via mediador in-memory com tratamento de resultado pelo padrão Result.
/// </summary>
public sealed class TenantOnboardingClientService : ITenantOnboardingClientService
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantOnboardingClientService"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory do MediatR.</param>
    public TenantOnboardingClientService(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <inheritdoc />
    public async Task<Result<SubdomainAvailabilityResponse>> CheckSubdomainAvailabilityAsync(
        string subdomain,
        CancellationToken cancellationToken = default)
    {
        var query = new CheckSubdomainAvailabilityQuery(subdomain);
        return await _sender.Send(query, cancellationToken);
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

        var command = new RegisterTenantOnboardingCommand(
            model.CompanyName,
            model.GetSanitizedCnpj(),
            model.GetSanitizedSubdomain(),
            model.Segment,
            model.MonthlyAdSpendRange,
            model.Tier,
            model.BillingCycle,
            model.AdminFullName,
            model.AdminEmail,
            model.AdminPhone,
            model.AdminPassword,
            model.CustomDomain,
            model.PrimaryColor,
            model.SecondaryColor);

        return await _sender.Send(command, cancellationToken);
    }
}
