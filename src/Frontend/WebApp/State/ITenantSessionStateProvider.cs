using Tenants.Application.Auth.DTOs;

namespace WebApp.State;

/// <summary>
/// Provedor de estado da sessão autenticada do usuário do inquilino no circuito SignalR.
/// Mantém as credenciais ativas, informações do usuário logado e integra com o <see cref="ITenantStateProvider"/>.
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
    /// Registra a nova sessão autenticada e sincroniza o branding do inquilino no estado geral.
    /// </summary>
    /// <param name="session">Dados completos do usuário, tenant e branding retornados pela autenticação.</param>
    void SetSession(AuthenticatedTenantUserDto session);

    /// <summary>
    /// Encerra a sessão atual e restaura as configurações institucionais padrão.
    /// </summary>
    void ClearSession();
}
