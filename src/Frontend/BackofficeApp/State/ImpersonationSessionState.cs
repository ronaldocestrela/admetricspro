namespace BackofficeApp.State;

/// <summary>
/// Representa o estado contextual de uma sessão de Shadow Mode (impersonation) ativa no frontend Blazor.
/// </summary>
/// <param name="IsActive">Indica se o operador está personificando a sessão de um tenant.</param>
/// <param name="SessionId">Identificador único da sessão de impersonation persistida.</param>
/// <param name="TenantId">Identificador do tenant atendido.</param>
/// <param name="TenantName">Nome do tenant para exibição na interface.</param>
/// <param name="SuperAdminId">Identificador do SuperAdmin responsável pelo chamado.</param>
/// <param name="SupportTicketId">Número do ticket de chamado de suporte (ex.: INC-84920).</param>
/// <param name="Reason">Justificativa técnica documentada para a intervenção.</param>
/// <param name="ExpiresAtUtc">Timestamp UTC no qual a sessão expira automaticamente.</param>
public sealed record ImpersonationSessionState(
    bool IsActive,
    Guid? SessionId,
    Guid? TenantId,
    string? TenantName,
    Guid? SuperAdminId,
    string? SupportTicketId,
    string? Reason,
    DateTime? ExpiresAtUtc)
{
    /// <summary>
    /// Estado estático para representação de nenhuma personificação ativa.
    /// </summary>
    public static readonly ImpersonationSessionState Inactive = new(
        IsActive: false,
        SessionId: null,
        TenantId: null,
        TenantName: null,
        SuperAdminId: null,
        SupportTicketId: null,
        Reason: null,
        ExpiresAtUtc: null);
}
