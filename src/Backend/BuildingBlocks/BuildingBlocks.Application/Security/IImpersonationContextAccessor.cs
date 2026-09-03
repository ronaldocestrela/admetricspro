namespace BuildingBlocks.Application.Security;

/// <summary>
/// Provides access to the current <see cref="IImpersonationContext"/> in the executing scope.
/// </summary>
public interface IImpersonationContextAccessor
{
    /// <summary>
    /// Gets the current impersonation context.
    /// </summary>
    IImpersonationContext Current { get; }
}
