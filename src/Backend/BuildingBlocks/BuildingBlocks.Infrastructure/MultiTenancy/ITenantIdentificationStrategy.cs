using BuildingBlocks.Application.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Infrastructure.MultiTenancy;

/// <summary>
/// Strategy contract for identifying tenant identity from an incoming HTTP request.
/// </summary>
public interface ITenantIdentificationStrategy
{
    /// <summary>
    /// Gets the resolution source channel handled by this strategy.
    /// </summary>
    TenantResolutionSource Source { get; }

    /// <summary>
    /// Attempts to extract tenant identity from the given HTTP context.
    /// </summary>
    /// <param name="httpContext">The active HTTP context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing tenant identification details, or <c>null</c> if not identified.</returns>
    ValueTask<TenantIdentificationResult?> IdentifyTenantAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
