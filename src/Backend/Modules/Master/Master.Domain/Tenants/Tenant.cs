using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace Master.Domain.Tenants;

/// <summary>
/// Master catalog aggregate representing a SaaS tenant.
/// </summary>
public sealed class Tenant : AggregateRoot<TenantId>
{
    private Tenant(TenantId id, string companyName, string cnpj, string subdomain)
        : base(id)
    {
        CompanyName = companyName;
        Cnpj = cnpj;
        Subdomain = subdomain;
        Status = TenantStatus.Active;
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
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new tenant aggregate after validating business inputs.
    /// </summary>
    /// <param name="companyName">Company legal name.</param>
    /// <param name="cnpj">CNPJ digits-only string.</param>
    /// <param name="subdomain">Tenant subdomain.</param>
    /// <returns>A successful result with a new tenant or a validation failure.</returns>
    public static Result<Tenant> Create(string companyName, string cnpj, string subdomain)
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
        var tenant = new Tenant(TenantId.New(), companyName.Trim(), cnpj, normalizedSubdomain);
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
}