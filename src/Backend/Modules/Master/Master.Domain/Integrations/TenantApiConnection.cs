using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace Master.Domain.Integrations;

/// <summary>
/// Domain entity representing the connection and health status of an ad platform integration for a tenant.
/// </summary>
public sealed class TenantApiConnection : Entity<Guid>
{
    private TenantApiConnection(
        Guid id,
        Guid tenantId,
        string tenantName,
        AdPlatform platform,
        string accountIdentifier,
        string accountName,
        DateTime? tokenExpiresAtUtc,
        DateTime createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        TenantName = tenantName;
        Platform = platform;
        AccountIdentifier = accountIdentifier;
        AccountName = accountName;
        Status = ApiConnectionStatus.Connected;
        TokenExpiresAtUtc = tokenExpiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    private TenantApiConnection()
        : base(Guid.NewGuid())
    {
        TenantName = string.Empty;
        AccountIdentifier = string.Empty;
        AccountName = string.Empty;
        Platform = AdPlatform.Meta;
        Status = ApiConnectionStatus.Connected;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the unique identifier of the tenant owner.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the tenant organization display name.
    /// </summary>
    public string TenantName { get; private set; }

    /// <summary>
    /// Gets the ad network platform for this connection.
    /// </summary>
    public AdPlatform Platform { get; private set; }

    /// <summary>
    /// Gets the platform account identifier (e.g. ad account ID or customer ID).
    /// </summary>
    public string AccountIdentifier { get; private set; }

    /// <summary>
    /// Gets the human-readable display name of the ad account.
    /// </summary>
    public string AccountName { get; private set; }

    /// <summary>
    /// Gets the operational health status of the connection token.
    /// </summary>
    public ApiConnectionStatus Status { get; private set; }

    /// <summary>
    /// Gets the UTC expiration date of the OAuth refresh or access token, if applicable.
    /// </summary>
    public DateTime? TokenExpiresAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp of the last successful data synchronization.
    /// </summary>
    public DateTime? LastSyncAtUtc { get; private set; }

    /// <summary>
    /// Gets any connection failure or revocation message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the last update timestamp in UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new <see cref="TenantApiConnection"/> instance.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="tenantName">Tenant display name.</param>
    /// <param name="platform">Ad platform.</param>
    /// <param name="accountIdentifier">External account identifier.</param>
    /// <param name="accountName">Friendly account name.</param>
    /// <param name="tokenExpiresAtUtc">Optional token expiration date.</param>
    /// <param name="createdAtUtc">Creation timestamp in UTC.</param>
    /// <returns>Result containing the connection entity or validation error.</returns>
    public static Result<TenantApiConnection> Create(
        Guid tenantId,
        string tenantName,
        AdPlatform platform,
        string accountIdentifier,
        string accountName,
        DateTime? tokenExpiresAtUtc,
        DateTime createdAtUtc)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<TenantApiConnection>.Failure(
                Error.Validation("ApiConnection.InvalidParameters", "O TenantId não pode ser vazio."));
        }

        if (string.IsNullOrWhiteSpace(tenantName) || string.IsNullOrWhiteSpace(accountIdentifier))
        {
            return Result<TenantApiConnection>.Failure(
                Error.Validation("ApiConnection.InvalidParameters", "Nome do inquilino e identificador da conta são obrigatórios."));
        }

        var connection = new TenantApiConnection(
            Guid.NewGuid(),
            tenantId,
            tenantName.Trim(),
            platform,
            accountIdentifier.Trim(),
            string.IsNullOrWhiteSpace(accountName) ? accountIdentifier.Trim() : accountName.Trim(),
            tokenExpiresAtUtc,
            createdAtUtc);

        return Result<TenantApiConnection>.Success(connection);
    }

    /// <summary>
    /// Evaluates whether the token is expiring soon or has already expired.
    /// </summary>
    /// <param name="nowUtc">Current UTC timestamp.</param>
    /// <param name="warningWindow">Warning window threshold (e.g. 7 days).</param>
    public void EvaluateExpiration(DateTime nowUtc, TimeSpan warningWindow)
    {
        if (Status == ApiConnectionStatus.Revoked || Status == ApiConnectionStatus.Disconnected)
        {
            return;
        }

        if (!TokenExpiresAtUtc.HasValue)
        {
            return;
        }

        if (TokenExpiresAtUtc.Value <= nowUtc)
        {
            Status = ApiConnectionStatus.Expired;
            ErrorMessage = "O token de autenticação expirou. Reconecte a conta.";
            UpdatedAtUtc = nowUtc;
        }
        else if (TokenExpiresAtUtc.Value - nowUtc <= warningWindow)
        {
            Status = ApiConnectionStatus.ExpiringSoon;
            ErrorMessage = $"O token de autenticação expira em breve ({(TokenExpiresAtUtc.Value - nowUtc).Days} dias).";
            UpdatedAtUtc = nowUtc;
        }
        else
        {
            Status = ApiConnectionStatus.Connected;
            ErrorMessage = null;
            UpdatedAtUtc = nowUtc;
        }
    }

    /// <summary>
    /// Marks the connection token as revoked.
    /// </summary>
    /// <param name="reason">Reason or error details.</param>
    /// <param name="nowUtc">Current UTC timestamp.</param>
    public void MarkRevoked(string reason, DateTime nowUtc)
    {
        Status = ApiConnectionStatus.Revoked;
        ErrorMessage = reason;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Re-establishes active connection state with a renewed token.
    /// </summary>
    /// <param name="newExpiresAtUtc">New expiration date.</param>
    /// <param name="nowUtc">Current UTC timestamp.</param>
    public void MarkConnected(DateTime? newExpiresAtUtc, DateTime nowUtc)
    {
        Status = ApiConnectionStatus.Connected;
        TokenExpiresAtUtc = newExpiresAtUtc;
        ErrorMessage = null;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Records a successful data synchronization.
    /// </summary>
    /// <param name="nowUtc">Sync timestamp.</param>
    public void RecordSync(DateTime nowUtc)
    {
        LastSyncAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }
}
