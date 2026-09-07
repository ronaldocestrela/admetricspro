using System.Text.RegularExpressions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;
using Master.Domain.Tenants.Events;

namespace Master.Domain.Tenants;

/// <summary>
/// Master catalog aggregate representing a SaaS tenant.
/// </summary>
public sealed partial class Tenant : AggregateRoot<TenantId>
{
    private Tenant(
        TenantId id,
        string companyName,
        string cnpj,
        string subdomain,
        SubscriptionTier tier,
        DateTime? subscriptionExpiresAtUtc,
        string? segment,
        string? monthlyAdSpendRange,
        string? billingCycle,
        string? customDomain,
        string? primaryColor,
        string? secondaryColor)
        : base(id)
    {
        CompanyName = companyName;
        Cnpj = cnpj;
        Subdomain = subdomain;
        Status = TenantStatus.Active;
        Tier = tier;
        SubscriptionExpiresAtUtc = subscriptionExpiresAtUtc;
        Segment = segment;
        MonthlyAdSpendRange = monthlyAdSpendRange;
        BillingCycle = billingCycle;
        CustomDomain = customDomain;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        DunningStage = DunningStage.None;
        PaymentDueDateUtc = null;
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
        DunningStage = DunningStage.None;
        PaymentDueDateUtc = null;
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
    /// Gets the current dunning stage of the tenant.
    /// </summary>
    public DunningStage DunningStage { get; private set; }

    /// <summary>
    /// Gets the UTC payment due timestamp if the tenant has an overdue invoice.
    /// </summary>
    public DateTime? PaymentDueDateUtc { get; private set; }

    /// <summary>
    /// Gets the UTC expiration date of the current subscription or trial period.
    /// </summary>
    public DateTime? SubscriptionExpiresAtUtc { get; private set; }

    /// <summary>
    /// Gets the custom CNAME domain mapped to the tenant for white-label routing.
    /// </summary>
    public string? CustomDomain { get; private set; }

    /// <summary>
    /// Gets the tenant's primary branding theme color in hexadecimal format.
    /// </summary>
    public string? PrimaryColor { get; private set; }

    /// <summary>
    /// Gets the tenant's secondary branding theme color in hexadecimal format.
    /// </summary>
    public string? SecondaryColor { get; private set; }

    /// <summary>
    /// Gets the tenant's market segment or business domain.
    /// </summary>
    public string? Segment { get; private set; }

    /// <summary>
    /// Gets the estimated monthly paid media spend range of the tenant.
    /// </summary>
    public string? MonthlyAdSpendRange { get; private set; }

    /// <summary>
    /// Gets the selected billing cycle frequency (Monthly or Annual).
    /// </summary>
    public string? BillingCycle { get; private set; }

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
    /// <param name="segment">Optional market segment or business domain.</param>
    /// <param name="monthlyAdSpendRange">Optional estimated monthly ad spend range.</param>
    /// <param name="billingCycle">Optional subscription billing cycle (Monthly or Annual).</param>
    /// <param name="customDomain">Optional custom CNAME domain for white-label routing.</param>
    /// <param name="primaryColor">Optional primary theme hex color code.</param>
    /// <param name="secondaryColor">Optional secondary theme hex color code.</param>
    /// <returns>A successful result with a new tenant or a validation failure.</returns>
    public static Result<Tenant> Create(
        string companyName,
        string cnpj,
        string subdomain,
        SubscriptionTier tier = SubscriptionTier.Trial,
        DateTime? subscriptionExpiresAtUtc = null,
        string? segment = null,
        string? monthlyAdSpendRange = null,
        string? billingCycle = null,
        string? customDomain = null,
        string? primaryColor = null,
        string? secondaryColor = null)
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

        var validationResult = ValidateBrandingAndProfile(primaryColor, secondaryColor, customDomain, billingCycle);
        if (validationResult.IsFailure)
        {
            return Result<Tenant>.Failure(validationResult.Error);
        }

        var (normalizedCustomDomain, normalizedBillingCycle) = validationResult.Value;
        var normalizedSubdomain = subdomain.Trim().ToLowerInvariant();
        var defaultExpiration = subscriptionExpiresAtUtc ?? (tier == SubscriptionTier.Trial ? DateTime.UtcNow.AddDays(14) : null);

        var tenant = new Tenant(
            TenantId.New(),
            companyName.Trim(),
            cnpj,
            normalizedSubdomain,
            tier,
            defaultExpiration,
            segment?.Trim(),
            monthlyAdSpendRange?.Trim(),
            normalizedBillingCycle,
            normalizedCustomDomain,
            primaryColor?.Trim(),
            secondaryColor?.Trim());

        return Result<Tenant>.Success(tenant);
    }

    /// <summary>
    /// Updates the tenant white-label branding identity attributes.
    /// </summary>
    /// <param name="primaryColor">Optional primary theme hex color code.</param>
    /// <param name="secondaryColor">Optional secondary theme hex color code.</param>
    /// <param name="customDomain">Optional custom CNAME domain for white-label routing.</param>
    /// <returns>Success result or domain validation failure.</returns>
    public Result UpdateBranding(string? primaryColor, string? secondaryColor, string? customDomain)
    {
        var validationResult = ValidateBrandingAndProfile(primaryColor, secondaryColor, customDomain, billingCycle: null);
        if (validationResult.IsFailure)
        {
            return Result.Failure(validationResult.Error);
        }

        PrimaryColor = primaryColor?.Trim();
        SecondaryColor = secondaryColor?.Trim();
        CustomDomain = validationResult.Value.NormalizedCustomDomain;

        return Result.Success();
    }

    /// <summary>
    /// Updates the tenant business profile metadata.
    /// </summary>
    /// <param name="segment">Market segment or business domain.</param>
    /// <param name="monthlyAdSpendRange">Estimated monthly ad spend range.</param>
    /// <returns>Success result.</returns>
    public Result UpdateBusinessProfile(string? segment, string? monthlyAdSpendRange)
    {
        Segment = segment?.Trim();
        MonthlyAdSpendRange = monthlyAdSpendRange?.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Sets or updates the subscription billing cycle frequency.
    /// </summary>
    /// <param name="billingCycle">Billing cycle value (Monthly or Annual).</param>
    /// <returns>Success result or domain validation failure.</returns>
    public Result SetBillingCycle(string billingCycle)
    {
        if (string.IsNullOrWhiteSpace(billingCycle))
        {
            return Result.Failure(Error.Validation("Tenant.InvalidBillingCycle", "Billing cycle cannot be empty."));
        }

        var trimmed = billingCycle.Trim();
        if (string.Equals(trimmed, "Monthly", StringComparison.OrdinalIgnoreCase))
        {
            BillingCycle = "Monthly";
            return Result.Success();
        }

        if (string.Equals(trimmed, "Annual", StringComparison.OrdinalIgnoreCase))
        {
            BillingCycle = "Annual";
            return Result.Success();
        }

        return Result.Failure(Error.Validation("Tenant.InvalidBillingCycle", "Billing cycle must be Monthly or Annual."));
    }

    private static Result<(string? NormalizedCustomDomain, string? NormalizedBillingCycle)> ValidateBrandingAndProfile(
        string? primaryColor,
        string? secondaryColor,
        string? customDomain,
        string? billingCycle)
    {
        if (!string.IsNullOrWhiteSpace(primaryColor) && !HexColorRegex().IsMatch(primaryColor.Trim()))
        {
            return Result<(string?, string?)>.Failure(
                Error.Validation("Tenant.InvalidColorHex", "Primary color must be a valid hexadecimal color code (e.g. #4f46e5)."));
        }

        if (!string.IsNullOrWhiteSpace(secondaryColor) && !HexColorRegex().IsMatch(secondaryColor.Trim()))
        {
            return Result<(string?, string?)>.Failure(
                Error.Validation("Tenant.InvalidColorHex", "Secondary color must be a valid hexadecimal color code (e.g. #0f172a)."));
        }

        string? normalizedCustomDomain = null;
        if (!string.IsNullOrWhiteSpace(customDomain))
        {
            var trimmedDomain = customDomain.Trim().ToLowerInvariant();
            if (trimmedDomain.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                trimmedDomain.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                trimmedDomain.Any(char.IsWhiteSpace))
            {
                return Result<(string?, string?)>.Failure(
                    Error.Validation("Tenant.InvalidCustomDomain", "Custom domain cannot contain protocol (http/https) or spaces."));
            }

            normalizedCustomDomain = trimmedDomain;
        }

        string? normalizedBillingCycle = null;
        if (!string.IsNullOrWhiteSpace(billingCycle))
        {
            var trimmedCycle = billingCycle.Trim();
            if (string.Equals(trimmedCycle, "Monthly", StringComparison.OrdinalIgnoreCase))
            {
                normalizedBillingCycle = "Monthly";
            }
            else if (string.Equals(trimmedCycle, "Annual", StringComparison.OrdinalIgnoreCase))
            {
                normalizedBillingCycle = "Annual";
            }
            else
            {
                return Result<(string?, string?)>.Failure(
                    Error.Validation("Tenant.InvalidBillingCycle", "Billing cycle must be Monthly or Annual."));
            }
        }

        return Result<(string?, string?)>.Success((normalizedCustomDomain, normalizedBillingCycle));
    }

    [GeneratedRegex("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled)]
    private static partial Regex HexColorRegex();

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

    /// <summary>
    /// Marks the tenant as having an overdue invoice with a specific due date.
    /// </summary>
    /// <param name="dueDateUtc">The UTC timestamp when payment was due.</param>
    /// <returns>A successful result.</returns>
    public Result MarkPaymentOverdue(DateTime dueDateUtc)
    {
        PaymentDueDateUtc = dueDateUtc;
        return Result.Success();
    }

    /// <summary>
    /// Evaluates the tenant's dunning stage against a reference UTC timestamp, updating operational status and raising domain events when thresholds are exceeded.
    /// </summary>
    /// <param name="referenceUtc">The reference UTC timestamp.</param>
    /// <returns>A successful result.</returns>
    public Result EvaluateDunningStage(DateTime referenceUtc)
    {
        var previousStage = DunningStage;
        var newStage = DunningPolicy.EvaluateStage(PaymentDueDateUtc, referenceUtc);

        DunningStage = newStage;

        if (newStage == DunningStage.LoginBlocked && Status != TenantStatus.Suspended)
        {
            Status = TenantStatus.Suspended;
        }

        if (PaymentDueDateUtc.HasValue && newStage > DunningStage.None && (newStage != previousStage || previousStage == DunningStage.None))
        {
            var daysOverdue = DunningPolicy.CalculateDaysOverdue(PaymentDueDateUtc.Value, referenceUtc);
            RaiseDomainEvent(new TenantGracePeriodExceededEvent(
                Id,
                previousStage,
                newStage,
                daysOverdue,
                PaymentDueDateUtc.Value));
        }

        return Result.Success();
    }

    /// <summary>
    /// Regularizes tenant financial standing, clearing overdue dates and restoring active status if previously suspended.
    /// </summary>
    /// <returns>A successful result.</returns>
    public Result RegularizePayment()
    {
        DunningStage = DunningStage.None;
        PaymentDueDateUtc = null;

        if (Status == TenantStatus.Suspended)
        {
            Status = TenantStatus.Active;
        }

        return Result.Success();
    }
}