using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Entidade de domínio que representa uma conta de anúncios conectada (Meta, Google, TikTok, Bing) a um workspace da agência.
/// Suporta contas reais autenticadas via OAuth2 e contas em modo demonstração para aceleração do FTUX.
/// </summary>
public sealed class ConnectedAdAccount : Entity<Guid>
{
    private static readonly HashSet<string> SupportedPlatforms = new(StringComparer.OrdinalIgnoreCase)
    {
        "MetaAds",
        "GoogleAds",
        "TikTokAds",
        "BingAds"
    };

    private ConnectedAdAccount(
        Guid id,
        Guid workspaceId,
        string platform,
        string externalAccountId,
        string accountName,
        string currency,
        string status,
        bool isDemo,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Platform = platform;
        ExternalAccountId = externalAccountId;
        AccountName = accountName;
        Currency = currency;
        Status = status;
        IsDemo = isDemo;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    private ConnectedAdAccount()
        : base(Guid.Empty)
    {
        WorkspaceId = Guid.Empty;
        Platform = string.Empty;
        ExternalAccountId = string.Empty;
        AccountName = string.Empty;
        Currency = "BRL";
        Status = "Disconnected";
        IsDemo = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtém o identificador único do workspace (cliente) ao qual a conta pertence.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém o nome da plataforma de anúncios (ex: MetaAds, GoogleAds, TikTokAds, BingAds).
    /// </summary>
    public string Platform { get; private set; }

    /// <summary>
    /// Obtém o identificador externo da conta na rede de anúncios (ex: act_123456789).
    /// </summary>
    public string ExternalAccountId { get; private set; }

    /// <summary>
    /// Obtém o nome de exibição amigável da conta de anúncios.
    /// </summary>
    public string AccountName { get; private set; }

    /// <summary>
    /// Obtém o código ISO da moeda utilizada na conta (padrão: BRL).
    /// </summary>
    public string Currency { get; private set; }

    /// <summary>
    /// Obtém o status operacional da conexão (ex: Connected, Disconnected, Expired).
    /// </summary>
    public string Status { get; private set; }

    /// <summary>
    /// Indica se a conta opera em modo demonstração com métricas sintetizadas para o FTUX.
    /// </summary>
    public bool IsDemo { get; private set; }

    /// <summary>
    /// Obtém o carimbo de data/hora UTC em que a conta foi vinculada.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o carimbo de data/hora UTC da última atualização cadastral ou sincronização.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Cria uma nova instância de <see cref="ConnectedAdAccount"/> validando as invariantes de negócio.
    /// </summary>
    /// <param name="id">Identificador único da conta conectada.</param>
    /// <param name="workspaceId">Identificador do workspace associado.</param>
    /// <param name="platform">Plataforma de mídia (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
    /// <param name="externalAccountId">ID externo da conta de anúncios.</param>
    /// <param name="accountName">Nome descritivo da conta.</param>
    /// <param name="currency">Moeda da conta.</param>
    /// <param name="isDemo">Flag indicando se a conta é demonstrativa.</param>
    /// <param name="createdAtUtc">Carimbo UTC opcional.</param>
    /// <returns>Resultado contendo a entidade criada ou erro de validação.</returns>
    public static Result<ConnectedAdAccount> Create(
        Guid id,
        Guid workspaceId,
        string platform,
        string externalAccountId,
        string accountName,
        string currency = "BRL",
        bool isDemo = false,
        DateTime? createdAtUtc = null)
    {
        if (id == Guid.Empty)
        {
            return Result<ConnectedAdAccount>.Failure(
                Error.Validation("ConnectedAdAccount.EmptyId", "O identificador da conta de anúncios não pode ser vazio."));
        }

        if (workspaceId == Guid.Empty)
        {
            return Result<ConnectedAdAccount>.Failure(
                Error.Validation("ConnectedAdAccount.EmptyWorkspaceId", "O workspace associado é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(platform) || !SupportedPlatforms.Contains(platform.Trim()))
        {
            return Result<ConnectedAdAccount>.Failure(
                Error.Validation("ConnectedAdAccount.InvalidPlatform", "A plataforma de anúncios informada não é suportada."));
        }

        if (string.IsNullOrWhiteSpace(externalAccountId))
        {
            return Result<ConnectedAdAccount>.Failure(
                Error.Validation("ConnectedAdAccount.EmptyExternalAccountId", "O identificador externo da conta é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(accountName))
        {
            return Result<ConnectedAdAccount>.Failure(
                Error.Validation("ConnectedAdAccount.EmptyAccountName", "O nome da conta de anúncios é obrigatório."));
        }

        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? "BRL" : currency.Trim().ToUpperInvariant();
        var creationTime = createdAtUtc ?? DateTime.UtcNow;

        var account = new ConnectedAdAccount(
            id,
            workspaceId,
            platform.Trim(),
            externalAccountId.Trim(),
            accountName.Trim(),
            normalizedCurrency,
            "Connected",
            isDemo,
            creationTime,
            null);

        return Result<ConnectedAdAccount>.Success(account);
    }

    /// <summary>
    /// Cria uma conta de anúncios em modo demonstração para aceleração do primeiro acesso (FTUX).
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace associado.</param>
    /// <param name="platform">Plataforma desejada (padrão: MetaAds).</param>
    /// <returns>Resultado contendo a entidade demonstrativa pronta para uso.</returns>
    public static Result<ConnectedAdAccount> CreateDemo(Guid workspaceId, string platform = "MetaAds")
    {
        var platformName = string.IsNullOrWhiteSpace(platform) ? "MetaAds" : platform.Trim();
        var displayName = platformName switch
        {
            "GoogleAds" => "Google Ads (Demonstração Leads)",
            "TikTokAds" => "TikTok Ads (Demonstração Viral)",
            "BingAds" => "Bing Ads (Demonstração B2B)",
            _ => "Meta Ads (Demonstração E-commerce)"
        };

        var externalId = $"demo_{Guid.NewGuid():N}";
        return Create(
            Guid.NewGuid(),
            workspaceId,
            platformName,
            externalId,
            displayName,
            "BRL",
            isDemo: true);
    }
}
