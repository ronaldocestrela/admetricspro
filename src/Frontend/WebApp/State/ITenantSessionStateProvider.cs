using Tenants.Application.Auth.DTOs;

namespace WebApp.State;

/// <summary>
/// Provedor de estado da sessão autenticada do usuário do inquilino no circuito SignalR.
/// Mantém as credenciais ativas, informações do usuário logado e integra com o <see cref="ITenantStateProvider"/>,
/// assegurando persistência e restauração pós-recarregamento de tela (F5).
/// </summary>
public interface ITenantSessionStateProvider
{
    /// <summary>
    /// Indica se há um usuário autenticado com sessão válida ativa no circuito.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Dados consolidados do usuário e da organização autenticada.
    /// </summary>
    AuthenticatedTenantUserDto? CurrentSession { get; }

    /// <summary>
    /// Evento disparado quando o estado da sessão (login ou logout) é alterado.
    /// </summary>
    event Action? OnSessionChanged;

    /// <summary>
    /// Registra a nova sessão autenticada, persiste no armazenamento do navegador e sincroniza o branding.
    /// </summary>
    /// <param name="session">Dados completos do usuário, tenant e branding retornados pela autenticação.</param>
    void SetSession(AuthenticatedTenantUserDto session);

    /// <summary>
    /// Registra assincronamente a sessão e persiste de forma segura no armazenamento local do navegador.
    /// </summary>
    /// <param name="session">Dados completos do usuário autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task SetSessionAsync(AuthenticatedTenantUserDto session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restaura a sessão do usuário persistida no navegador após atualizações de página (F5) ou reconexões.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Verdadeiro se uma sessão válida foi restaurada com sucesso; caso contrário, falso.</returns>
    Task<bool> RestoreSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Encerra a sessão atual, remove os dados persistidos do navegador e restaura as configurações institucionais padrão.
    /// </summary>
    void ClearSession();
}
