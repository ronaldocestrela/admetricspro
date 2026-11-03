using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Adaptador de OAuth orgânico por rede (autorização, troca e refresh na Fase 2).
/// </summary>
public interface IOrganicPlatformAdapter
{
    /// <summary>
    /// Plataforma orgânica atendida por este adaptador.
    /// </summary>
    OrganicPlatform Platform { get; }

    /// <summary>
    /// Monta a URL HTTPS de consentimento do provedor com o state anti-CSRF, sem chamada HTTP.
    /// </summary>
    /// <param name="state">State assinado emitido pelo módulo.</param>
    /// <param name="redirectUri">URI de retorno validada pelo handler.</param>
    /// <returns>URL completa de autorização ou falha de configuração.</returns>
    Result<string> BuildAuthorizationUrl(string state, string redirectUri);
}
