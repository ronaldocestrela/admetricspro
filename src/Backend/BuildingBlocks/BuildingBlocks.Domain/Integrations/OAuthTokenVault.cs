using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Integrations;

/// <summary>
/// Entidade de domínio que representa o cofre de credenciais e tokens OAuth2 protegidos de um workspace da agência.
/// As credenciais brutas são sempre armazenadas de forma cifrada (AES-256) em repouso no banco de dados dedicado do inquilino.
/// </summary>
public sealed class OAuthTokenVault : Entity<Guid>
{
    private OAuthTokenVault(
        Guid id,
        Guid workspaceId,
        string platform,
        string externalAccountId,
        string externalAccountName,
        string encryptedAccessToken,
        string? encryptedRefreshToken,
        DateTime? accessTokenExpiresAtUtc,
        DateTime? refreshTokenExpiresAtUtc,
        string status,
        string scopes,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Platform = platform;
        ExternalAccountId = externalAccountId;
        ExternalAccountName = externalAccountName;
        EncryptedAccessToken = encryptedAccessToken;
        EncryptedRefreshToken = encryptedRefreshToken;
        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
        RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
        Status = status;
        Scopes = scopes;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    private OAuthTokenVault()
        : base(Guid.Empty)
    {
        WorkspaceId = Guid.Empty;
        Platform = string.Empty;
        ExternalAccountId = string.Empty;
        ExternalAccountName = string.Empty;
        EncryptedAccessToken = string.Empty;
        Status = OAuthConnectionStatus.Active;
        Scopes = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Identificador do workspace ao qual a credencial pertence.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Plataforma de mídia (MetaAds, GoogleAds, TikTokAds, BingAds).
    /// </summary>
    public string Platform { get; private set; }

    /// <summary>
    /// Identificador externo da conta na rede de anúncios (ex: act_123456789, customers/1234567890).
    /// </summary>
    public string ExternalAccountId { get; private set; }

    /// <summary>
    /// Nome amigável da conta obtido da plataforma.
    /// </summary>
    public string ExternalAccountName { get; private set; }

    /// <summary>
    /// Token de acesso criptografado com AES-256 em repouso.
    /// </summary>
    public string EncryptedAccessToken { get; private set; }

    /// <summary>
    /// Refresh token criptografado com AES-256 em repouso (opcional para redes que utilizam apenas long-lived tokens).
    /// </summary>
    public string? EncryptedRefreshToken { get; private set; }

    /// <summary>
    /// Data/hora UTC de expiração do access token.
    /// </summary>
    public DateTime? AccessTokenExpiresAtUtc { get; private set; }

    /// <summary>
    /// Data/hora UTC de expiração do refresh token (se aplicável).
    /// </summary>
    public DateTime? RefreshTokenExpiresAtUtc { get; private set; }

    /// <summary>
    /// Status operacional da conexão (<see cref="OAuthConnectionStatus"/>).
    /// </summary>
    public string Status { get; private set; }

    /// <summary>
    /// Escopos concedidos separados por vírgula.
    /// </summary>
    public string Scopes { get; private set; }

    /// <summary>
    /// Data/hora UTC de criação do registro no cofre.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Data/hora UTC da última atualização de credenciais ou status.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Cria uma nova instância de <see cref="OAuthTokenVault"/> validando as invariantes de negócio.
    /// </summary>
    /// <param name="id">Identificador único do registro.</param>
    /// <param name="workspaceId">Identificador do workspace associado.</param>
    /// <param name="platform">Plataforma OAuth (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
    /// <param name="externalAccountId">ID externo da conta de anúncios.</param>
    /// <param name="externalAccountName">Nome de exibição da conta.</param>
    /// <param name="encryptedAccessToken">Access token cifrado em AES-256.</param>
    /// <param name="encryptedRefreshToken">Refresh token cifrado em AES-256 opcional.</param>
    /// <param name="accessTokenExpiresAtUtc">Data/hora UTC de expiração do access token.</param>
    /// <param name="refreshTokenExpiresAtUtc">Data/hora UTC de expiração do refresh token.</param>
    /// <param name="scopes">Escopos concedidos.</param>
    /// <param name="createdAtUtc">Carimbo UTC de criação.</param>
    /// <returns>Resultado contendo a entidade criada ou falha de validação.</returns>
    public static Result<OAuthTokenVault> Create(
        Guid id,
        Guid workspaceId,
        string platform,
        string externalAccountId,
        string externalAccountName,
        string encryptedAccessToken,
        string? encryptedRefreshToken = null,
        DateTime? accessTokenExpiresAtUtc = null,
        DateTime? refreshTokenExpiresAtUtc = null,
        string scopes = "",
        DateTime? createdAtUtc = null)
    {
        if (id == Guid.Empty)
        {
            return Result<OAuthTokenVault>.Failure(
                Error.Validation("OAuthTokenVault.EmptyId", "O identificador do cofre não pode ser vazio."));
        }

        if (workspaceId == Guid.Empty)
        {
            return Result<OAuthTokenVault>.Failure(
                Error.Validation("OAuthTokenVault.EmptyWorkspaceId", "O identificador do workspace é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            return Result<OAuthTokenVault>.Failure(
                Error.Validation("OAuthTokenVault.InvalidPlatform", "A plataforma de anúncios é obrigatória."));
        }

        if (!OAuthPlatform.IsSupported(platform))
        {
            return Result<OAuthTokenVault>.Failure(
                Error.Validation("OAuthTokenVault.UnsupportedPlatform", $"A plataforma '{platform}' não é suportada."));
        }

        if (string.IsNullOrWhiteSpace(encryptedAccessToken))
        {
            return Result<OAuthTokenVault>.Failure(
                Error.Validation("OAuthTokenVault.EmptyAccessToken", "O access token criptografado é obrigatório."));
        }

        var normalizedPlatform = OAuthPlatform.Normalize(platform);
        var normalizedExternalId = string.IsNullOrWhiteSpace(externalAccountId) ? "default" : externalAccountId.Trim();
        var normalizedAccountName = string.IsNullOrWhiteSpace(externalAccountName) ? normalizedPlatform : externalAccountName.Trim();
        var creationTime = createdAtUtc ?? DateTime.UtcNow;

        var vault = new OAuthTokenVault(
            id,
            workspaceId,
            normalizedPlatform,
            normalizedExternalId,
            normalizedAccountName,
            encryptedAccessToken.Trim(),
            string.IsNullOrWhiteSpace(encryptedRefreshToken) ? null : encryptedRefreshToken.Trim(),
            accessTokenExpiresAtUtc,
            refreshTokenExpiresAtUtc,
            OAuthConnectionStatus.Active,
            scopes.Trim(),
            creationTime,
            null);

        return Result<OAuthTokenVault>.Success(vault);
    }

    /// <summary>
    /// Atualiza os tokens cifrados e datas de expiração após renovação bem-sucedida.
    /// </summary>
    /// <param name="encryptedAccessToken">Novo access token cifrado.</param>
    /// <param name="encryptedRefreshToken">Novo refresh token cifrado (ou mantém o anterior se nulo).</param>
    /// <param name="accessTokenExpiresAtUtc">Nova data/hora UTC de expiração do access token.</param>
    /// <param name="refreshTokenExpiresAtUtc">Nova data/hora UTC de expiração do refresh token.</param>
    /// <returns>Resultado da operação.</returns>
    public Result UpdateTokens(
        string encryptedAccessToken,
        string? encryptedRefreshToken = null,
        DateTime? accessTokenExpiresAtUtc = null,
        DateTime? refreshTokenExpiresAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(encryptedAccessToken))
        {
            return Result.Failure(
                Error.Validation("OAuthTokenVault.EmptyAccessToken", "O novo access token criptografado é obrigatório."));
        }

        EncryptedAccessToken = encryptedAccessToken.Trim();
        if (!string.IsNullOrWhiteSpace(encryptedRefreshToken))
        {
            EncryptedRefreshToken = encryptedRefreshToken.Trim();
        }

        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
        if (refreshTokenExpiresAtUtc.HasValue)
        {
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
        }

        Status = OAuthConnectionStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Verifica se o token de acesso irá expirar dentro do intervalo especificado.
    /// </summary>
    /// <param name="threshold">Janela de tempo para expiração iminente.</param>
    /// <returns>True se estiver a ponto de expirar; false caso contrário.</returns>
    public bool IsExpiringSoon(TimeSpan threshold)
    {
        if (!AccessTokenExpiresAtUtc.HasValue)
        {
            return false;
        }

        return AccessTokenExpiresAtUtc.Value <= DateTime.UtcNow.Add(threshold);
    }

    /// <summary>
    /// Marca a conexão como revogada e invalida o status operacional.
    /// </summary>
    public void Revoke()
    {
        Status = OAuthConnectionStatus.Revoked;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca o token como expirado.
    /// </summary>
    public void MarkExpired()
    {
        Status = OAuthConnectionStatus.Expired;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
