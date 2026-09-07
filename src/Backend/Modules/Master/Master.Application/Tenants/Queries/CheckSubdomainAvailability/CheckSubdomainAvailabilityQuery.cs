using BuildingBlocks.Application.Messaging;

namespace Master.Application.Tenants.Queries.CheckSubdomainAvailability;

/// <summary>
/// Consulta para verificar em tempo real se um subdomínio pretendido está disponível para novo tenant.
/// </summary>
/// <param name="Subdomain">Subdomínio desejado a ser verificado.</param>
public sealed record CheckSubdomainAvailabilityQuery(string Subdomain) : IQuery<SubdomainAvailabilityResponse>;
