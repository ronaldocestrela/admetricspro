namespace WebApi.Models;

/// <summary>
/// Payload para requisição de emissão de token de impersonation seguro (Shadow Mode).
/// </summary>
/// <param name="SuperAdminId">Identificador único do operador/SuperAdmin solicitante.</param>
/// <param name="SupportTicketId">Identificador obrigatório do chamado de suporte técnico.</param>
/// <param name="Reason">Justificativa técnica para o acesso ao ambiente do cliente.</param>
/// <param name="DurationMinutes">Duração desejada para o acesso em minutos (entre 5 e 120, padrão 30).</param>
public sealed record ImpersonateTenantApiRequest(
    Guid SuperAdminId,
    string SupportTicketId,
    string Reason,
    int DurationMinutes = 30);
