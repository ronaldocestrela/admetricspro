using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Domain.Connections.Events;

namespace SocialPublishing.Domain.Connections;

/// <summary>
/// Agregado que representa uma conexão OAuth orgânica de um workspace com uma rede social.
/// Tokens persistidos apenas como ciphertext; nunca expõe segredos em claro.
/// </summary>
public sealed class OrganicConnection : AggregateRoot<Guid>
{
    /// <summary>
    /// Marcador persistido no lugar do ciphertext após revogação local (não é segredo utilizável).
    /// </summary>
    public const string RevokedCredentialMarker = "revoked:local";

    /// <summary>Identifica o contexto criptográfico da credencial orgânica.</summary>
    /// <param name="platform">Plataforma orgânica.</param>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="connectionId">Identificador da conexão.</param>
    /// <returns>Finalidade autenticada do ciphertext (uso na Fase 2).</returns>
    public static string EncryptionPurpose(OrganicPlatform platform, Guid workspaceId, Guid connectionId) =>
        $"organic:{platform}:{workspaceId:D}:{connectionId:D}";

    private OrganicConnection(
        Guid id,
        Guid workspaceId,
        OrganicPlatform platform,
        string externalAccountId,
        string displayName,
        string encryptedAccessToken,
        string? encryptedRefreshToken,
        DateTime? accessTokenExpiresAtUtc,
        string scopes,
        OrganicConnectionStatus status,
        DateTime createdAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Platform = platform;
        ExternalAccountId = externalAccountId;
        DisplayName = displayName;
        EncryptedAccessToken = encryptedAccessToken;
        EncryptedRefreshToken = encryptedRefreshToken;
        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
        Scopes = scopes;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    private OrganicConnection()
        : base(Guid.Empty)
    {
        ExternalAccountId = string.Empty;
        DisplayName = string.Empty;
        EncryptedAccessToken = string.Empty;
        Scopes = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtém o identificador do workspace proprietário.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém a plataforma orgânica conectada.
    /// </summary>
    public OrganicPlatform Platform { get; private set; }

    /// <summary>
    /// Obtém o identificador externo da conta na rede.
    /// </summary>
    public string ExternalAccountId { get; private set; }

    /// <summary>
    /// Obtém o nome de exibição da conta.
    /// </summary>
    public string DisplayName { get; private set; }

    /// <summary>
    /// Obtém o access token cifrado em repouso.
    /// </summary>
    public string EncryptedAccessToken { get; private set; }

    /// <summary>
    /// Obtém o refresh token cifrado em repouso, se existir.
    /// </summary>
    public string? EncryptedRefreshToken { get; private set; }

    /// <summary>
    /// Obtém a expiração UTC do access token.
    /// </summary>
    public DateTime? AccessTokenExpiresAtUtc { get; private set; }

    /// <summary>
    /// Obtém os escopos concedidos (separados por vírgula ou espaço conforme persistência futura).
    /// </summary>
    public string Scopes { get; private set; }

    /// <summary>
    /// Obtém o status operacional da conexão.
    /// </summary>
    public OrganicConnectionStatus Status { get; private set; }

    /// <summary>
    /// Obtém o carimbo UTC de criação.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Cria uma nova conexão orgânica ativa com tokens já cifrados.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="platform">Plataforma orgânica.</param>
    /// <param name="externalAccountId">Identificador externo da conta.</param>
    /// <param name="displayName">Nome de exibição.</param>
    /// <param name="scopes">Escopos concedidos.</param>
    /// <param name="encryptedAccessToken">Access token cifrado.</param>
    /// <param name="encryptedRefreshToken">Refresh token cifrado opcional.</param>
    /// <param name="accessTokenExpiresAtUtc">Expiração UTC do access token.</param>
    /// <param name="id">Identificador opcional (usado para derivar o propósito de cifra antes da persistência).</param>
    /// <returns>Resultado com a conexão criada ou erro de validação.</returns>
    public static Result<OrganicConnection> Create(
        Guid workspaceId,
        OrganicPlatform platform,
        string externalAccountId,
        string displayName,
        string scopes,
        string encryptedAccessToken,
        string? encryptedRefreshToken = null,
        DateTime? accessTokenExpiresAtUtc = null,
        Guid? id = null)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<OrganicConnection>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (!Enum.IsDefined(platform))
        {
            return Result<OrganicConnection>.Failure(SocialErrors.PlatformUnsupported);
        }

        if (string.IsNullOrWhiteSpace(encryptedAccessToken))
        {
            return Result<OrganicConnection>.Failure(
                Error.Validation("Social.ConnectionTokenRequired", "O access token cifrado é obrigatório."));
        }

        var connection = new OrganicConnection(
            id ?? Guid.NewGuid(),
            workspaceId,
            platform,
            externalAccountId.Trim(),
            displayName.Trim(),
            encryptedAccessToken.Trim(),
            string.IsNullOrWhiteSpace(encryptedRefreshToken) ? null : encryptedRefreshToken.Trim(),
            accessTokenExpiresAtUtc,
            scopes.Trim(),
            OrganicConnectionStatus.Active,
            DateTime.UtcNow);

        return Result<OrganicConnection>.Success(connection);
    }

    /// <summary>
    /// Substitui os ciphertexts e a expiração após rotação de tokens na rede.
    /// </summary>
    /// <param name="encryptedAccessToken">Novo access token cifrado.</param>
    /// <param name="encryptedRefreshToken">Novo refresh token cifrado opcional.</param>
    /// <param name="accessTokenExpiresAtUtc">Nova expiração UTC.</param>
    /// <returns>Resultado da operação.</returns>
    public Result MarkTokensRotated(
        string encryptedAccessToken,
        string? encryptedRefreshToken,
        DateTime? accessTokenExpiresAtUtc)
    {
        if (Status == OrganicConnectionStatus.Revoked)
        {
            return Result.Failure(SocialErrors.TokenRevoked);
        }

        if (string.IsNullOrWhiteSpace(encryptedAccessToken))
        {
            return Result.Failure(
                Error.Validation("Social.ConnectionTokenRequired", "O access token cifrado é obrigatório."));
        }

        EncryptedAccessToken = encryptedAccessToken.Trim();
        EncryptedRefreshToken = string.IsNullOrWhiteSpace(encryptedRefreshToken) ? null : encryptedRefreshToken.Trim();
        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
        return Result.Success();
    }

    /// <summary>
    /// Reaplica tokens e metadados após novo consentimento OAuth (reconexão da mesma conta externa).
    /// </summary>
    /// <param name="encryptedAccessToken">Novo access token cifrado.</param>
    /// <param name="encryptedRefreshToken">Novo refresh token cifrado opcional.</param>
    /// <param name="accessTokenExpiresAtUtc">Nova expiração UTC.</param>
    /// <param name="displayName">Nome de exibição atualizado.</param>
    /// <param name="scopes">Escopos concedidos.</param>
    /// <returns>Resultado da operação.</returns>
    public Result Reauthorize(
        string encryptedAccessToken,
        string? encryptedRefreshToken,
        DateTime? accessTokenExpiresAtUtc,
        string displayName,
        string scopes)
    {
        if (string.IsNullOrWhiteSpace(encryptedAccessToken))
        {
            return Result.Failure(
                Error.Validation("Social.ConnectionTokenRequired", "O access token cifrado é obrigatório."));
        }

        EncryptedAccessToken = encryptedAccessToken.Trim();
        EncryptedRefreshToken = string.IsNullOrWhiteSpace(encryptedRefreshToken) ? null : encryptedRefreshToken.Trim();
        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
        DisplayName = displayName.Trim();
        Scopes = scopes.Trim();
        Status = OrganicConnectionStatus.Active;
        return Result.Success();
    }

    /// <summary>
    /// Revoga a conexão localmente. Idempotente: não emite evento duplicado.
    /// </summary>
    /// <returns>Resultado da operação.</returns>
    public Result Revoke()
    {
        if (Status == OrganicConnectionStatus.Revoked)
        {
            return Result.Success();
        }

        EncryptedAccessToken = RevokedCredentialMarker;
        EncryptedRefreshToken = null;
        Status = OrganicConnectionStatus.Revoked;
        RaiseDomainEvent(new OrganicConnectionRevoked(Id, WorkspaceId));
        return Result.Success();
    }
}
