namespace WebApi.Models;

/// <summary>
/// Modelo de requisição HTTP para solicitação de revogação de sessão de impersonation.
/// </summary>
/// <param name="Reason">Justificativa opcional do encerramento precoce da sessão.</param>
public sealed record TerminateImpersonationApiRequest(string? Reason = null);
