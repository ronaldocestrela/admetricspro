namespace SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;

/// <summary>
/// Resposta da consulta que inicia o fluxo OAuth orgânico (URL do provedor e state).
/// </summary>
/// <param name="AuthorizationUrl">URL HTTPS de consentimento da rede.</param>
/// <param name="State">State assinado anti-CSRF a ser validado no callback.</param>
/// <param name="ExpiresAtUtc">Expiração UTC do state (15 minutos após emissão).</param>
public sealed record StartOrganicAuthorizationResponse(
    string AuthorizationUrl,
    string State,
    DateTime ExpiresAtUtc);
