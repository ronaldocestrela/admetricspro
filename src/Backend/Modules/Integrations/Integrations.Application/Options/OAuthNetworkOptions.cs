namespace Integrations.Application.Options;

/// <summary>
/// Configurações gerais dos provedores OAuth2 suportados pelo Hub de Integrações.
/// </summary>
public sealed class OAuthNetworkOptions
{
    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public const string SectionName = "Integrations:OAuth";

    /// <summary>
    /// Chave secreta para assinatura HMAC do state anti-CSRF.
    /// </summary>
    public string StateSigningKey { get; set; } = string.Empty;

    /// <summary>Chave AES-256 em Base64, externa ao código, para proteger tokens com AES-GCM.</summary>
    public string TokenEncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Opções do Meta Ads.
    /// </summary>
    public MetaAdsOAuthOptions Meta { get; set; } = new();

    /// <summary>
    /// Opções do Google Ads.
    /// </summary>
    public GoogleAdsOAuthOptions Google { get; set; } = new();

    /// <summary>
    /// Opções do Bing Ads / Microsoft Advertising.
    /// </summary>
    public BingAdsOAuthOptions Bing { get; set; } = new();

    /// <summary>
    /// Opções do TikTok Ads.
    /// </summary>
    public TikTokAdsOAuthOptions TikTok { get; set; } = new();
}

/// <summary>
/// Opções do Meta Ads (Graph API).
/// </summary>
public sealed class MetaAdsOAuthOptions
{
    /// <summary>
    /// Identificador do aplicativo Meta.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Chave secreta do aplicativo Meta.
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// Escopos padrão solicitados na autorização.
    /// </summary>
    public string DefaultScopes { get; set; } = "ads_read,ads_management";

    /// <summary>
    /// Versão da Meta Graph API.
    /// </summary>
    public string GraphApiVersion { get; set; } = "v26.0";

    /// <summary>Callback HTTPS previamente registrado no aplicativo Meta.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Callbacks adicionais de white-label, cadastrados explicitamente também na Meta.</summary>
    public string[] AdditionalRedirectUris { get; set; } = [];

    /// <summary>Timeout das chamadas Graph em segundos.</summary>
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Opções do Google Ads API.
/// </summary>
public sealed class GoogleAdsOAuthOptions
{
    /// <summary>
    /// Client ID do projeto no Google Cloud Console.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client Secret do aplicativo.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Token de desenvolvedor para acesso à Google Ads API.
    /// </summary>
    public string DeveloperToken { get; set; } = string.Empty;

    /// <summary>
    /// Escopos padrão solicitados na autorização.
    /// </summary>
    public string DefaultScopes { get; set; } = "https://www.googleapis.com/auth/adwords";
}

/// <summary>
/// Opções do Bing Ads / Microsoft Advertising Platform.
/// </summary>
public sealed class BingAdsOAuthOptions
{
    /// <summary>
    /// Application (client) ID registrado no Microsoft Entra / Azure AD.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret do registro de aplicativo.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Escopos padrão solicitados.
    /// </summary>
    public string DefaultScopes { get; set; } = "https://ads.microsoft.com/msads.manage offline_access";
}

/// <summary>
/// Opções do TikTok Marketing API.
/// </summary>
public sealed class TikTokAdsOAuthOptions
{
    /// <summary>
    /// App ID registrado no TikTok for Business Developers.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Secret associado ao aplicativo TikTok.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Escopos padrão solicitados.
    /// </summary>
    public string DefaultScopes { get; set; } = "user.info.basic,video.list,advertiser.insights";
}
