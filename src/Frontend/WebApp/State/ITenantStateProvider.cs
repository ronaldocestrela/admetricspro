namespace WebApp.State;

/// <summary>
/// Provedor de estado do Tenant ativo no circuito da sessão Blazor Server.
/// </summary>
public interface ITenantStateProvider
{
    /// <summary>
    /// Obtém o estado atual do Tenant na sessão ativa.
    /// </summary>
    TenantState CurrentTenant { get; }

    /// <summary>
    /// Indica se o estado do Tenant já foi carregado e inicializado.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Evento disparado quando o estado ou branding do Tenant é modificado na sessão.
    /// </summary>
    event Action? OnTenantChanged;

    /// <summary>
    /// Atualiza o Tenant ativo na sessão e dispara as notificações de re-renderização.
    /// </summary>
    /// <param name="state">Novo estado completo do Tenant.</param>
    void SetTenant(TenantState state);

    /// <summary>
    /// Inicializa assincronamente o estado do Tenant na inicialização do circuito.
    /// </summary>
    /// <param name="initialState">Estado inicial opcional a ser definido.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa assíncrona representando a conclusão da inicialização.</returns>
    Task InitializeAsync(TenantState? initialState = null, CancellationToken cancellationToken = default);
}
