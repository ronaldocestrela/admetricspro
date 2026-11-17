using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Resolve chaves de objeto de mídia orgânica em URLs HTTPS públicas consumíveis pelas redes.
/// </summary>
public interface IOrganicMediaPublicUrlResolver
{
    /// <summary>
    /// Converte uma chave de armazenamento em URL absoluta HTTPS.
    /// </summary>
    /// <param name="objectKey">Chave persistida no ativo de mídia.</param>
    /// <returns>URL pública ou falha de configuração.</returns>
    Result<string> ResolvePublicUrl(string objectKey);
}
