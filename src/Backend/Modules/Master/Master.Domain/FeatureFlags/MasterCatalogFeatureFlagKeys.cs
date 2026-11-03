namespace Master.Domain.FeatureFlags;

/// <summary>
/// Chaves canônicas de feature flags e kill switches semeadas no catálogo MasterDb.
/// </summary>
public static class MasterCatalogFeatureFlagKeys
{
    /// <summary>
    /// Kill switch global de automações cross-network.
    /// </summary>
    public const string AutomationGlobal = "killswitch.automation.global";

    /// <summary>
    /// Kill switch de automação Meta Ads.
    /// </summary>
    public const string AutomationMeta = "killswitch.automation.meta";

    /// <summary>
    /// Kill switch de automação Google Ads.
    /// </summary>
    public const string AutomationGoogle = "killswitch.automation.google";

    /// <summary>
    /// Kill switch de automação TikTok Ads.
    /// </summary>
    public const string AutomationTikTok = "killswitch.automation.tiktok";

    /// <summary>
    /// Kill switch de automação Bing Ads.
    /// </summary>
    public const string AutomationBing = "killswitch.automation.bing";

    /// <summary>
    /// Kill switch global de sincronização em background.
    /// </summary>
    public const string DataSyncGlobal = "killswitch.data-sync.global";

    /// <summary>
    /// Kill switch operacional do worker de disparo de publicação orgânica.
    /// </summary>
    public const string SocialPublishingDispatch = "killswitch.social-publishing.dispatch";

    /// <summary>
    /// Feature flag do motor MER v2.
    /// </summary>
    public const string AnalyticsMerV2 = "feature.analytics.mer-v2";

    /// <summary>
    /// Todas as chaves semeadas pelo catálogo (migrações e testes de conformidade).
    /// </summary>
    public static readonly IReadOnlyList<string> AllSeededKeys =
    [
        AutomationGlobal,
        AutomationMeta,
        AutomationGoogle,
        AutomationTikTok,
        AutomationBing,
        DataSyncGlobal,
        SocialPublishingDispatch,
        AnalyticsMerV2,
    ];
}
