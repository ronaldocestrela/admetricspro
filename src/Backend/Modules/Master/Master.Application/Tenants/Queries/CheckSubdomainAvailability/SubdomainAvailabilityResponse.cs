namespace Master.Application.Tenants.Queries.CheckSubdomainAvailability;

/// <summary>
/// Resposta de verificação de disponibilidade de subdomínio para instâncias de tenant.
/// </summary>
/// <param name="Subdomain">Subdomínio higienizado analisado.</param>
/// <param name="IsAvailable">Indica se o subdomínio está livre para registro.</param>
/// <param name="Reason">Motivo caso o subdomínio não esteja disponível.</param>
/// <param name="SuggestedAlternative">Sugestão de subdomínio alternativo quando houver conflito.</param>
public sealed record SubdomainAvailabilityResponse(
    string Subdomain,
    bool IsAvailable,
    string? Reason = null,
    string? SuggestedAlternative = null);
