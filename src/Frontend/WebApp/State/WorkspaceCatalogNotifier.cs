namespace WebApp.State;

/// <summary>
/// Implementação scoped de <see cref="IWorkspaceCatalogNotifier"/> compartilhada pelo circuito Blazor Server.
/// </summary>
public sealed class WorkspaceCatalogNotifier : IWorkspaceCatalogNotifier
{
    /// <inheritdoc />
    public event Action? Changed;

    /// <inheritdoc />
    public void NotifyChanged() => Changed?.Invoke();
}