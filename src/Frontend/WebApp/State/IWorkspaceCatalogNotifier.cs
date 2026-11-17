namespace WebApp.State;

/// <summary>
/// Notificador in-memory do catálogo de clientes/workspaces no circuito Blazor.
/// Permite que componentes persistentes (ex.: menu lateral) recarreguem a lista após mutações na API.
/// </summary>
public interface IWorkspaceCatalogNotifier
{
    /// <summary>
    /// Evento disparado quando o catálogo de workspaces foi alterado com sucesso (criação, edição ou alteração de status).
    /// </summary>
    event Action? Changed;

    /// <summary>
    /// Notifica assinantes de que o catálogo de workspaces deve ser recarregado.
    /// </summary>
    void NotifyChanged();
}