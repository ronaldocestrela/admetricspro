using BuildingBlocks.Application.MultiTenancy;

namespace BuildingBlocks.Infrastructure.MultiTenancy;

/// <summary>
/// Default implementation of <see cref="ITenantContextAccessor"/> combining scoped instance storage
/// with <see cref="AsyncLocal{T}"/> to guarantee both request scope availability and async execution isolation.
/// </summary>
public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private sealed class ContextHolder
    {
        public ITenantContext Context { get; set; } = MultiTenancy.TenantContext.Empty;
    }

    private static readonly AsyncLocal<ContextHolder?> AsyncLocalContext = new();
    private ITenantContext? _scopedContext;

    /// <inheritdoc />
    public ITenantContext TenantContext
    {
        get
        {
            return AsyncLocalContext.Value?.Context
                ?? _scopedContext
                ?? MultiTenancy.TenantContext.Empty;
        }
        set
        {
            var newContext = value ?? MultiTenancy.TenantContext.Empty;
            _scopedContext = newContext;

            var existingHolder = AsyncLocalContext.Value;
            if (existingHolder is not null)
            {
                existingHolder.Context = MultiTenancy.TenantContext.Empty;
            }

            AsyncLocalContext.Value = new ContextHolder { Context = newContext };
        }
    }
}
