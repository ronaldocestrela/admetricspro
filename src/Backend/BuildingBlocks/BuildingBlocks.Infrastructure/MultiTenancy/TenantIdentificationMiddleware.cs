using BuildingBlocks.Application.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.MultiTenancy;

/// <summary>
/// ASP.NET Core middleware that dynamically identifies the active tenant for each incoming HTTP request.
/// </summary>
public sealed class TenantIdentificationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<TenantResolutionOptions> _options;
    private readonly IReadOnlyList<ITenantIdentificationStrategy>? _constructorStrategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantIdentificationMiddleware"/> class for host runtime execution.
    /// </summary>
    /// <param name="next">The delegate representing the remaining middleware pipeline.</param>
    /// <param name="options">Tenant resolution configuration options.</param>
    [ActivatorUtilitiesConstructor]
    public TenantIdentificationMiddleware(
        RequestDelegate next,
        IOptions<TenantResolutionOptions> options)
        : this(next, null, options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantIdentificationMiddleware"/> class with explicit strategies.
    /// </summary>
    /// <param name="next">The delegate representing the remaining middleware pipeline.</param>
    /// <param name="strategies">Collection of registered tenant identification strategies, or null to resolve per request.</param>
    /// <param name="options">Tenant resolution configuration options.</param>
    public TenantIdentificationMiddleware(
        RequestDelegate next,
        IEnumerable<ITenantIdentificationStrategy>? strategies,
        IOptions<TenantResolutionOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (strategies is not null)
        {
            _constructorStrategies = OrderStrategies(strategies, options.Value);
        }
    }

    /// <summary>
    /// Executes tenant identification across configured strategies and updates the scoped tenant context.
    /// </summary>
    /// <param name="context">Active HTTP context.</param>
    /// <param name="contextAccessor">Scoped or ambient tenant context accessor.</param>
    /// <param name="strategies">Optional scoped strategies resolved from the current request container.</param>
    /// <returns>A task representing middleware execution.</returns>
    public async Task InvokeAsync(
        HttpContext context,
        ITenantContextAccessor contextAccessor,
        IEnumerable<ITenantIdentificationStrategy>? strategies = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contextAccessor);

        var activeStrategies = _constructorStrategies
            ?? (strategies is not null ? OrderStrategies(strategies, _options.Value) : null)
            ?? OrderStrategies(context.RequestServices?.GetServices<ITenantIdentificationStrategy>() ?? [], _options.Value);

        TenantIdentificationResult? identification = null;

        foreach (var strategy in activeStrategies)
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

    private static IReadOnlyList<ITenantIdentificationStrategy> OrderStrategies(
        IEnumerable<ITenantIdentificationStrategy> strategies,
        TenantResolutionOptions optionsValue)
    {
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

        return ordered;
    }
}
