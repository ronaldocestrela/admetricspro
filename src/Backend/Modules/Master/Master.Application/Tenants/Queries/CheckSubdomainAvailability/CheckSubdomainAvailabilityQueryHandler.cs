using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;

namespace Master.Application.Tenants.Queries.CheckSubdomainAvailability;

/// <summary>
/// Manipulador da consulta <see cref="CheckSubdomainAvailabilityQuery"/>.
/// Verifica a disponibilidade de um subdomínio no catálogo Master e protege nomes de sistema reservados.
/// </summary>
public sealed class CheckSubdomainAvailabilityQueryHandler : IQueryHandler<CheckSubdomainAvailabilityQuery, SubdomainAvailabilityResponse>
{
    private static readonly HashSet<string> ReservedSubdomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "api", "admin", "master", "app", "auth", "status", "backoffice", "portal",
        "system", "dashboard", "billing", "login", "register", "onboarding", "scalar", "swagger"
    };

    private readonly ITenantRepository _tenantRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CheckSubdomainAvailabilityQueryHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de persistência de tenants.</param>
    public CheckSubdomainAvailabilityQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    /// <inheritdoc />
    public async Task<Result<SubdomainAvailabilityResponse>> Handle(CheckSubdomainAvailabilityQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitized = (query.Subdomain ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return Result<SubdomainAvailabilityResponse>.Success(
                new SubdomainAvailabilityResponse(sanitized, false, "Subdomínio não pode ser vazio."));
        }

        // Verifica se é subdomínio reservado
        if (ReservedSubdomains.Contains(sanitized))
        {
            return Result<SubdomainAvailabilityResponse>.Success(
                new SubdomainAvailabilityResponse(
                    sanitized,
                    false,
                    $"O termo '{sanitized}' é um subdomínio reservado do sistema."));
        }

        // Verifica se já existe tenant com este subdomínio
        var existingTenant = await _tenantRepository.GetBySubdomainAsync(sanitized, cancellationToken);
        if (existingTenant is not null)
        {
            var randomSuffix = Random.Shared.Next(10, 99);
            var suggestedAlternative = $"{sanitized}-{randomSuffix}";

            return Result<SubdomainAvailabilityResponse>.Success(
                new SubdomainAvailabilityResponse(
                    sanitized,
                    false,
                    $"O subdomínio '{sanitized}' já está em uso por outro assinante.",
                    suggestedAlternative));
        }

        return Result<SubdomainAvailabilityResponse>.Success(
            new SubdomainAvailabilityResponse(sanitized, true));
    }
}
