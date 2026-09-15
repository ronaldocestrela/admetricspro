namespace BuildingBlocks.Domain.Integrations;

/// <summary>
/// Provedores de redes de anúncios suportados pelo Hub de Integrações OAuth2.
/// </summary>
public static class OAuthPlatform
{
    /// <summary>
    /// Meta Ads (Facebook e Instagram Ads).
    /// </summary>
    public const string MetaAds = "MetaAds";

    /// <summary>
    /// Google Ads (Search, Display, YouTube).
    /// </summary>
    public const string GoogleAds = "GoogleAds";

    /// <summary>
    /// Microsoft Advertising / Bing Ads.
    /// </summary>
    public const string BingAds = "BingAds";

    /// <summary>
    /// TikTok Marketing API.
    /// </summary>
    public const string TikTokAds = "TikTokAds";

    private static readonly HashSet<string> AllPlatforms = new(StringComparer.OrdinalIgnoreCase)
    {
        MetaAds,
        GoogleAds,
        BingAds,
        TikTokAds
    };

    /// <summary>
    /// Verifica se o nome da plataforma informada é suportada pelo SaaS.
    /// </summary>
    /// <param name="platform">Nome da plataforma.</param>
    /// <returns>True se suportada; caso contrário, false.</returns>
    public static bool IsSupported(string? platform)
    {
        return !string.IsNullOrWhiteSpace(platform) && AllPlatforms.Contains(platform.Trim());
    }

    /// <summary>
    /// Normaliza o nome da plataforma para a convenção canônica.
    /// </summary>
    /// <param name="platform">Nome da plataforma.</param>
    /// <returns>Nome canônico ou vazio.</returns>
    public static string Normalize(string platform)
    {
        if (string.Equals(platform, MetaAds, StringComparison.OrdinalIgnoreCase)) return MetaAds;
        if (string.Equals(platform, GoogleAds, StringComparison.OrdinalIgnoreCase)) return GoogleAds;
        if (string.Equals(platform, BingAds, StringComparison.OrdinalIgnoreCase)) return BingAds;
        if (string.Equals(platform, TikTokAds, StringComparison.OrdinalIgnoreCase)) return TikTokAds;
        return platform.Trim();
    }
}
