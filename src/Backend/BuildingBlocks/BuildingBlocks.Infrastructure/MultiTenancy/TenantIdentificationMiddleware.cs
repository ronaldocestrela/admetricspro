using BuildingBlocks.Application.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.MultiTenancy;

/// <summary>
/// ASP.NET Core middleware that dynamically identifies the active tenant for each incoming HTTP request.
/// </summary>
public sealed class TenantIdentificationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IReadOnlyList<ITenantIdentificationStrategy> _orderedStrategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantIdentificationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The delegate representing the remaining middleware pipeline.</param>
    /// <param name="strategies">Collection of registered tenant identification strategies.</param>
    /// <param name="options">Tenant resolution configuration options.</param>
    public TenantIdentificationMiddleware(
        RequestDelegate next,
        IEnumerable<ITenantIdentificationStrategy> strategies,
        IOptions<TenantResolutionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(strategies);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        var optionsValue = options.Value;

        // Order strategies according to options.ResolutionOrder
        var strategyList = strategies.ToList();
        var ordered = new List<ITenantIdentificationStrategy>();

        foreach (var source in optionsValue.ResolutionOrder)
        {
            var match = strategyList.FirstOrDefault(s => s.Source == source);
            if (match is not null && !ordered.Contains(match))
            {
                ordered.Add(match);
            }
        }

        // Add any remaining strategies not explicitly listed in ResolutionOrder
        foreach (var remaining in strategyList)
        {
            if (!ordered.Contains(remaining))
            {
                ordered.Add(remaining);
            }
        }

        _orderedStrategies = ordered;
    }

    /// <summary>
    /// Executes tenant identification across configured strategies and updates the scoped tenant context.
    /// </summary>
    /// <param name="context">Active HTTP context.</param>
    /// <param name="contextAccessor">Scoped or ambient tenant context accessor.</param>
    /// <returns>A task representing middleware execution.</returns>
    public async Task InvokeAsync(HttpContext context, ITenantContextAccessor contextAccessor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contextAccessor);

        TenantIdentificationResult? identification = null;

        foreach (var strategy in _orderedStrategies)
        {
            identification = await strategy.IdentifyTenantAsync(context, context.RequestAborted);
            if (identification is not null)
            {
                break;
            }
        }

        if (identification is not null)
        {
            contextAccessor.TenantContext = TenantContext.Create(
                identification.TenantId,
                identification.Subdomain,
                identification.Source,
                identification.RawIdentifier);
        }
        else
        {
            contextAccessor.TenantContext = TenantContext.Empty;
        }

        await _next(context);
    }
}
