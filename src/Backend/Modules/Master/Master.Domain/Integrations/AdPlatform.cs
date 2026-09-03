namespace Master.Domain.Integrations;

/// <summary>
/// Identifies the paid media ad platforms integrated into the system.
/// </summary>
public enum AdPlatform
{
    /// <summary>
    /// Meta Graph &amp; Marketing API (Facebook &amp; Instagram Ads).
    /// </summary>
    Meta = 1,

    /// <summary>
    /// Google Ads API.
    /// </summary>
    Google = 2,

    /// <summary>
    /// TikTok Marketing API.
    /// </summary>
    TikTok = 3,

    /// <summary>
    /// Microsoft Advertising / Bing Ads API.
    /// </summary>
    Bing = 4
}
