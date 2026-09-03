using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace Master.Domain.Tenants;

/// <summary>
/// Master catalog aggregate representing a SaaS tenant.
/// </summary>
public sealed class Tenant : AggregateRoot<TenantId>
{
    private Tenant(TenantId id, string companyName, string cnpj, string subdomain, SubscriptionTier tier, DateTime? subscriptionExpiresAtUtc)
        : base(id)
    {
        CompanyName = companyName;
        Cnpj = cnpj;
        Subdomain = subdomain;
        Status = TenantStatus.Active;
        Tier = tier;
        SubscriptionExpiresAtUtc = subscriptionExpiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private Tenant()
        : base(new TenantId(Guid.NewGuid()))
    {
        CompanyName = string.Empty;
        Cnpj = string.Empty;
        Subdomain = string.Empty;
        EncryptedConnectionString = string.Empty;
        Status = TenantStatus.Trial;
        Tier = SubscriptionTier.Trial;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the legal name of the tenant company.
    /// </summary>
    public string CompanyName { get; private set; }

    /// <summary>
    /// Gets the tenant CNPJ (numeric only).
    /// </summary>
    public string Cnpj { get; private set; }

    /// <summary>
    /// Gets the unique subdomain assigned to the tenant.
    /// </summary>
    public string Subdomain { get; private set; }

    /// <summary>
    /// Gets the encrypted tenant database connection string.
    /// </summary>
    public string EncryptedConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the tenant lifecycle status.
    /// </summary>
    public TenantStatus Status { get; private set; }

    /// <summary>
    /// Gets the tenant subscription tier level.
    /// </summary>
    public SubscriptionTier Tier { get; private set; }

    /// <summary>
    /// Gets the UTC expiration date of the current subscription or trial period.
    /// </summary>
    public DateTime? SubscriptionExpiresAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new tenant aggregate after validating business inputs.
    /// </summary>
    /// <param name="companyName">Company legal name.</param>
    /// <param name="cnpj">CNPJ digits-only string.</param>
    /// <param name="subdomain">Tenant subdomain.</param>
    /// <param name="tier">Optional initial subscription tier. Defaults to Trial.</param>
    /// <param name="subscriptionExpiresAtUtc">Optional subscription expiration date. Defaults to 14 days from now if Trial.</param>
    /// <returns>A successful result with a new tenant or a validation failure.</returns>
    public static Result<Tenant> Create(
        string companyName,
        string cnpj,
        string subdomain,
        SubscriptionTier tier = SubscriptionTier.Trial,
        DateTime? subscriptionExpiresAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            return Result<Tenant>.Failure(Error.Validation("Tenant.CompanyNameRequired", "Company name is required."));
        }

        if (string.IsNullOrWhiteSpace(cnpj) || cnpj.Length != 14 || !cnpj.All(char.IsDigit))
        {
            return Result<Tenant>.Failure(Error.Validation("Tenant.InvalidCnpj", "CNPJ must contain exactly 14 digits."));
        }

        if (string.IsNullOrWhiteSpace(subdomain) || subdomain.Any(char.IsWhiteSpace))
        {
            return Result<Tenant>.Failure(Error.Validation("Tenant.InvalidSubdomain", "Subdomain is invalid."));
        }

        var normalizedSubdomain = subdomain.Trim().ToLowerInvariant();
        var defaultExpiration = subscriptionExpiresAtUtc ?? (tier == SubscriptionTier.Trial ? DateTime.UtcNow.AddDays(14) : null);
        var tenant = new Tenant(TenantId.New(), companyName.Trim(), cnpj, normalizedSubdomain, tier, defaultExpiration);
        return Result<Tenant>.Success(tenant);
    }

    /// <summary>
    /// Sets the encrypted connection string generated for the tenant database.
    /// </summary>
    /// <param name="encryptedConnectionString">Encrypted connection string payload.</param>
    /// <returns>Success when value is valid; otherwise validation failure.</returns>
    public Result SetEncryptedConnectionString(string encryptedConnectionString)
    {
        if (string.IsNullOrWhiteSpace(encryptedConnectionString))
        {
            return Result.Failure(Error.Validation("Tenant.EncryptedConnectionStringRequired", "Encrypted connection string is required."));
        }

        EncryptedConnectionString = encryptedConnectionString;
        return Result.Success();
    }

    /// <summary>
    /// Upgrades or modifies the tenant's subscription tier.
    /// </summary>
    /// <param name="newTier">The target subscription tier.</param>
    /// <param name="expiresAtUtc">Optional new expiration timestamp in UTC.</param>
    /// <returns>A success result or validation error.</returns>
    public Result UpgradeSubscription(SubscriptionTier newTier, DateTime? expiresAtUtc = null)
    {
        Tier = newTier;
        SubscriptionExpiresAtUtc = expiresAtUtc;
        return Result.Success();
    }

    /// <summary>
    /// Extends the active trial period for the tenant.
    /// </summary>
    /// <param name="newExpirationUtc">New expiration timestamp which must be in the future.</param>
    /// <returns>A success result or validation error.</returns>
    public Result ExtendTrial(DateTime newExpirationUtc)
    {
        if (newExpirationUtc <= DateTime.UtcNow)
        {
            return Result.Failure(Error.Validation("Tenant.InvalidExpirationDate", "Trial expiration date must be in the future."));
        }

        SubscriptionExpiresAtUtc = newExpirationUtc;
        return Result.Success();
    }

    /// <summary>
    /// Suspends tenant operations.
    /// </summary>
    /// <param name="reason">Suspension reason description.</param>
    /// <returns>A success result or validation error.</returns>
    public Result Suspend(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(Error.Validation("Tenant.SuspensionReasonRequired", "Suspension reason is required."));
        }

        Status = TenantStatus.Suspended;
        return Result.Success();
    }

    /// <summary>
    /// Reactivates a suspended tenant.
    /// </summary>
    /// <returns>A success result.</returns>
    public Result Reactivate()
    {
        Status = TenantStatus.Active;
        return Result.Success();
    }
}