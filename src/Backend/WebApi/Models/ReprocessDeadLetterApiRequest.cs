namespace WebApi.Models;

/// <summary>
/// Corpo opcional para reprocessar dead-letter operacional de tenant.
/// </summary>
/// <param name="TenantId">Identificador do tenant quando a linha não está no catálogo Master.</param>
public sealed record ReprocessDeadLetterApiRequest(Guid? TenantId);