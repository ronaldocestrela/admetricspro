using System.Security.Claims;
using BuildingBlocks.Domain.Primitives;
using Master.Domain.Tenants;

namespace Master.Application.Services;

/// <summary>
/// Service contract for issuing and verifying contextual JSON Web Tokens for tenant impersonation (Shadow Mode).
/// </summary>
public interface IImpersonationTokenService
{
    /// <summary>
    /// Issues a signed contextual JWT containing the required audit claims for tenant impersonation.
    /// </summary>
    /// <param name="session">Active impersonation session details.</param>
    /// <param name="tenant">Target tenant being impersonated.</param>
    /// <returns>Result containing the serialized JWT string or failure.</returns>
    Result<string> GenerateToken(ImpersonationSession session, Tenant tenant);

    /// <summary>
    /// Validates an impersonation JWT signature, lifespan, and contextual claims.
    /// </summary>
    /// <param name="token">Serialized JWT string.</param>
    /// <returns>Result containing ClaimsPrincipal if valid, or failure error.</returns>
    Result<ClaimsPrincipal> ValidateToken(string token);
}
