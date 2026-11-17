namespace SocialPublishing.Application.Options;

/// <summary>
/// Configurações OAuth2 do bounded context Social Publishing (orgânico), separadas do hub de anúncios.
/// </summary>
public sealed class OrganicOAuthOptions
{
    /// <summary>
    /// Nome da seção em <c>appsettings</c> / variáveis de ambiente.
    /// </summary>
    public const string SectionName = "SocialPublishing:OAuth";

    /// <summary>
    /// Chave secreta para assinatura HMAC-SHA256 do state anti-CSRF (mínimo 32 caracteres).
    /// </summary>
    public string StateSigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Opções compartilhadas da Meta (Instagram e Facebook Page).
    /// </summary>
    public MetaOrganicOAuthOptions Meta { get; set; } = new();

    /// <summary>
    /// Opções do TikTok Content Posting / Login Kit.
    /// </summary>
    public TikTokOrganicOAuthOptions TikTok { get; set; } = new();
}

/// <summary>
/// Credenciais e escopos orgânicos da Meta (Graph / Facebook Login).
/// </summary>
public sealed class MetaOrganicOAuthOptions
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
    /// Versão da Graph API usada no diálogo OAuth.
    /// </summary>
    public string GraphApiVersion { get; set; } = "v26.0";

    /// <summary>
    /// Escopos solicitados na conexão Instagram orgânica.
    /// </summary>
    public string InstagramScopes { get; set; } =
        "pages_show_list,pages_read_engagement,instagram_basic,instagram_content_publish";

    /// <summary>
    /// Escopos solicitados na conexão Facebook Page orgânica.
    /// </summary>
    public string FacebookPageScopes { get; set; } =
        "pages_show_list,pages_read_engagement,pages_manage_posts";
}

/// <summary>
/// Credenciais e escopos do TikTok Login Kit / Content Posting API.
/// </summary>
public sealed class TikTokOrganicOAuthOptions
{
    /// <summary>
    /// Client key do aplicativo TikTok for Developers.
    /// </summary>
    public string ClientKey { get; set; } = string.Empty;

    /// <summary>
    /// Client secret do aplicativo.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Escopos de publicação orgânica (Content Posting).
    /// </summary>
    public string Scopes { get; set; } = "user.info.basic,video.publish,video.upload";
}
