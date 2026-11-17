namespace SocialPublishing.Application.Publishing;

/// <summary>
/// Referência HTTPS de mídia já resolvida para publicação na rede (sem binário).
/// </summary>
/// <param name="SourceUrl">URL pública HTTPS do objeto de mídia.</param>
/// <param name="SortOrder">Ordem no carrossel ou zero para mídia única.</param>
public sealed record OrganicPublishMediaItem(string SourceUrl, int SortOrder);
