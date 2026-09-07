namespace WebApi.Models;

/// <summary>
/// Carga de entrada para requisição de suspensão de um inquilino.
/// </summary>
/// <param name="Reason">Justificativa operacional ou financeira da suspensão.</param>
public sealed record SuspendTenantApiRequest(string Reason);
