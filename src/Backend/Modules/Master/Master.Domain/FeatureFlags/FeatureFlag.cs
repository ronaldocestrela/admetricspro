using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;
using Master.Domain.FeatureFlags.Events;

namespace Master.Domain.FeatureFlags;

/// <summary>
/// Aggregate root representing a feature flag or operational kill switch in the central Master catalog.
/// Supports global toggles, deterministic percentage rollouts, tenant allowlists, and emergency kill switches.
/// </summary>
public sealed class FeatureFlag : AggregateRoot<Guid>
{
    private List<Guid> _targetTenantIds = new();

    private FeatureFlag(
        Guid id,
        string key,
        string name,
        string description,
        bool isEnabled,
        bool isKillSwitch,
        FeatureFlagTargetingType targetingType,
        int rolloutPercentage,
        IEnumerable<Guid>? targetTenantIds,
        string createdBy,
        DateTime createdAtUtc)
        : base(id)
    {
        Key = key;
        Name = name;
        Description = description;
        IsEnabled = isEnabled;
        IsKillSwitch = isKillSwitch;
        TargetingType = targetingType;
        RolloutPercentage = rolloutPercentage;
        if (targetTenantIds != null)
        {
            _targetTenantIds.AddRange(targetTenantIds);
        }
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        UpdatedBy = createdBy;
    }

    private FeatureFlag()
        : base(Guid.NewGuid())
    {
        Key = string.Empty;
        Name = string.Empty;
        Description = string.Empty;
        CreatedBy = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the unique string key identifying the flag (e.g., "killswitch.automation.global", "feature.analytics.mer-v2").
    /// </summary>
    public string Key { get; private set; }

    /// <summary>
    /// Gets the friendly human-readable display name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the operational or functional description.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the flag is enabled.
    /// For Kill Switches, true signifies that the emergency break is ENGAGED/ACTIVE (subsystem halted).
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this record acts as an emergency operational Kill Switch.
    /// </summary>
    public bool IsKillSwitch { get; private set; }

    /// <summary>
    /// Gets the targeting strategy applied during evaluation.
    /// </summary>
    public FeatureFlagTargetingType TargetingType { get; private set; }

    /// <summary>
    /// Gets the rollout percentage (0 to 100) when targeting is <see cref="FeatureFlagTargetingType.PercentageRollout"/>.
    /// </summary>
    public int RolloutPercentage { get; private set; }

    /// <summary>
    /// Gets the collection of tenant IDs explicitly targeted when targeting is <see cref="FeatureFlagTargetingType.TenantList"/>.
    /// </summary>
    public IReadOnlyCollection<Guid> TargetTenantIds => _targetTenantIds.AsReadOnly();

    /// <summary>
    /// Gets the UTC timestamp when a kill switch was triggered, if currently or previously active.
    /// </summary>
    public DateTime? KillSwitchActivatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the operational justification/reason for triggering or restoring the kill switch.
    /// </summary>
    public string? KillSwitchReason { get; private set; }

    /// <summary>
    /// Gets the operator/admin who triggered or updated the kill switch.
    /// </summary>
    public string? KillSwitchTriggeredBy { get; private set; }

    /// <summary>
    /// Gets the user who originally created the record.
    /// </summary>
    public string CreatedBy { get; private set; }

    /// <summary>
    /// Gets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the last modification timestamp in UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the user who last modified the flag.
    /// </summary>
    public string? UpdatedBy { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the kill switch is currently armed/engaged.
    /// </summary>
    public bool IsKillSwitchActive => IsKillSwitch && IsEnabled;

    /// <summary>
    /// Creates a new functional feature flag.
    /// </summary>
    /// <param name="key">Unique key.</param>
    /// <param name="name">Display name.</param>
    /// <param name="description">Description.</param>
    /// <param name="isEnabled">Initial state.</param>
    /// <param name="isKillSwitch">Whether this is a kill switch.</param>
    /// <param name="targetingType">Targeting model.</param>
    /// <param name="rolloutPercentage">Rollout percentage (0-100).</param>
    /// <param name="targetTenantIds">Optional allowlist of tenants.</param>
    /// <param name="createdBy">Creator identifier.</param>
    /// <returns>Result containing the created feature flag or validation failure.</returns>
    public static Result<FeatureFlag> Create(
        string key,
        string name,
        string description,
        bool isEnabled,
        bool isKillSwitch,
        FeatureFlagTargetingType targetingType,
        int rolloutPercentage,
        IEnumerable<Guid>? targetTenantIds,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Result<FeatureFlag>.Failure(Error.Validation("FeatureFlag.EmptyKey", "A chave identificadora da feature flag é obrigatória."));

        if (rolloutPercentage < 0 || rolloutPercentage > 100)
            return Result<FeatureFlag>.Failure(Error.Validation("FeatureFlag.InvalidRolloutPercentage", "O percentual de rollout deve estar entre 0 e 100."));

        var flag = new FeatureFlag(
            Guid.NewGuid(),
            key.Trim().ToLowerInvariant(),
            name.Trim(),
            description.Trim(),
            isEnabled,
            isKillSwitch,
            targetingType,
            rolloutPercentage,
            targetTenantIds,
            createdBy,
            DateTime.UtcNow);

        return Result<FeatureFlag>.Success(flag);
    }

    /// <summary>
    /// Factory helper to create an operational emergency Kill Switch.
    /// Kill switches start disengaged (IsEnabled = false) by default.
    /// </summary>
    /// <param name="key">Unique kill switch key (e.g. "killswitch.automation.global").</param>
    /// <param name="name">Human readable name.</param>
    /// <param name="description">Subsystem description.</param>
    /// <param name="createdBy">Creator identifier.</param>
    /// <returns>Created kill switch feature flag.</returns>
    public static Result<FeatureFlag> CreateKillSwitch(
        string key,
        string name,
        string description,
        string createdBy)
    {
        return Create(
            key,
            name,
            description,
            isEnabled: false,
            isKillSwitch: true,
            targetingType: FeatureFlagTargetingType.Global,
            rolloutPercentage: 100,
            targetTenantIds: null,
            createdBy: createdBy);
    }

    /// <summary>
    /// Evaluates whether this feature is active for the given tenant context.
    /// </summary>
    /// <param name="tenantId">Optional tenant ID to evaluate against.</param>
    /// <returns>True if active/enabled for the context; otherwise, false.</returns>
    public bool Evaluate(Guid? tenantId = null)
    {
        if (!IsEnabled)
            return false;

        if (IsKillSwitch)
            return true;

        switch (TargetingType)
        {
            case FeatureFlagTargetingType.Global:
                return IsEnabled;

            case FeatureFlagTargetingType.PercentageRollout:
                if (RolloutPercentage >= 100)
                    return true;
                if (RolloutPercentage <= 0 || !tenantId.HasValue)
                    return false;
                return ComputeDeterministicBucket(tenantId.Value, Key) < RolloutPercentage;

            case FeatureFlagTargetingType.TenantList:
                if (!tenantId.HasValue)
                    return false;
                return _targetTenantIds.Contains(tenantId.Value);

            default:
                return false;
        }
    }

    /// <summary>
    /// Activates/engages the emergency Kill Switch, halting the protected subsystem.
    /// </summary>
    /// <param name="reason">Mandatory operational justification.</param>
    /// <param name="triggeredBy">Operator who triggered the switch.</param>
    /// <param name="utcNow">Timestamp of activation.</param>
    /// <returns>Result indicating success or validation failure.</returns>
    public Result ActivateKillSwitch(string reason, string triggeredBy, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("KillSwitch.ReasonRequired", "É obrigatório fornecer a justificativa operacional para acionar o Kill Switch."));

        IsEnabled = true;
        KillSwitchReason = reason.Trim();
        KillSwitchTriggeredBy = triggeredBy;
        KillSwitchActivatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        UpdatedBy = triggeredBy;

        RaiseDomainEvent(new KillSwitchActivatedDomainEvent(Key, Name, KillSwitchReason, KillSwitchTriggeredBy, utcNow));
        return Result.Success();
    }

    /// <summary>
    /// Deactivates/disengages the emergency Kill Switch, resuming operations.
    /// </summary>
    /// <param name="reason">Mandatory operational justification for restoration.</param>
    /// <param name="triggeredBy">Operator who restored the service.</param>
    /// <param name="utcNow">Timestamp of restoration.</param>
    /// <returns>Result indicating success or validation failure.</returns>
    public Result DeactivateKillSwitch(string reason, string triggeredBy, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("KillSwitch.ReasonRequired", "É obrigatório fornecer a justificativa para desativar o Kill Switch."));

        IsEnabled = false;
        KillSwitchReason = reason.Trim();
        KillSwitchTriggeredBy = triggeredBy;
        UpdatedAtUtc = utcNow;
        UpdatedBy = triggeredBy;

        RaiseDomainEvent(new KillSwitchDeactivatedDomainEvent(Key, Name, KillSwitchReason, KillSwitchTriggeredBy, utcNow));
        return Result.Success();
    }

    /// <summary>
    /// Updates the progressive rollout percentage.
    /// </summary>
    /// <param name="percentage">New percentage (0 to 100).</param>
    /// <param name="updatedBy">Operator who made the change.</param>
    /// <param name="utcNow">Timestamp of update.</param>
    /// <returns>Success or validation error.</returns>
    public Result SetRolloutPercentage(int percentage, string updatedBy, DateTime utcNow)
    {
        if (percentage < 0 || percentage > 100)
            return Result.Failure(Error.Validation("FeatureFlag.InvalidRolloutPercentage", "O percentual de rollout deve estar entre 0 e 100."));

        RolloutPercentage = percentage;
        TargetingType = FeatureFlagTargetingType.PercentageRollout;
        UpdatedAtUtc = utcNow;
        UpdatedBy = updatedBy;

        RaiseDomainEvent(new FeatureFlagUpdatedDomainEvent(Key, IsEnabled, RolloutPercentage, UpdatedBy, utcNow));
        return Result.Success();
    }

    /// <summary>
    /// Updates the target allowlist of tenants.
    /// </summary>
    /// <param name="tenantIds">Allowed tenant IDs.</param>
    /// <param name="updatedBy">Operator who made the change.</param>
    /// <param name="utcNow">Timestamp of update.</param>
    /// <returns>Success result.</returns>
    public Result SetTenantTargeting(IEnumerable<Guid> tenantIds, string updatedBy, DateTime utcNow)
    {
        _targetTenantIds.Clear();
        if (tenantIds != null)
        {
            _targetTenantIds.AddRange(tenantIds);
        }

        TargetingType = FeatureFlagTargetingType.TenantList;
        UpdatedAtUtc = utcNow;
        UpdatedBy = updatedBy;

        RaiseDomainEvent(new FeatureFlagUpdatedDomainEvent(Key, IsEnabled, RolloutPercentage, UpdatedBy, utcNow));
        return Result.Success();
    }

    /// <summary>
    /// Enables the feature flag.
    /// </summary>
    /// <param name="updatedBy">Operator who made the change.</param>
    /// <param name="utcNow">Timestamp of update.</param>
    public void Enable(string updatedBy, DateTime utcNow)
    {
        IsEnabled = true;
        UpdatedAtUtc = utcNow;
        UpdatedBy = updatedBy;
        RaiseDomainEvent(new FeatureFlagUpdatedDomainEvent(Key, IsEnabled, RolloutPercentage, UpdatedBy, utcNow));
    }

    /// <summary>
    /// Disables the feature flag.
    /// </summary>
    /// <param name="updatedBy">Operator who made the change.</param>
    /// <param name="utcNow">Timestamp of update.</param>
    public void Disable(string updatedBy, DateTime utcNow)
    {
        IsEnabled = false;
        UpdatedAtUtc = utcNow;
        UpdatedBy = updatedBy;
        RaiseDomainEvent(new FeatureFlagUpdatedDomainEvent(Key, IsEnabled, RolloutPercentage, UpdatedBy, utcNow));
    }

    /// <summary>
    /// Computes a deterministic integer bucket (0 to 99) for the tenant and flag key using SHA-256.
    /// </summary>
    private static int ComputeDeterministicBucket(Guid tenantId, string flagKey)
    {
        var input = $"{flagKey.ToLowerInvariant()}:{tenantId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var value = BitConverter.ToUInt32(hash, 0);
        return (int)(value % 100);
    }
}
