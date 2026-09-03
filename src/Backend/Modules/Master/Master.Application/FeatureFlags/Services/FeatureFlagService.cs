using System.Collections.Concurrent;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Auditing;
using Master.Application.FeatureFlags.DTOs;
using Master.Application.FeatureFlags.Repositories;
using Master.Domain.FeatureFlags;
using Master.Domain.Integrations;
using Microsoft.Extensions.Caching.Memory;

namespace Master.Application.FeatureFlags.Services;

/// <summary>
/// Production implementation of <see cref="IFeatureFlagService"/> with in-memory caching,
/// instant cache invalidation, and immutable audit trails for emergency kill switch operations.
/// </summary>
public sealed class FeatureFlagService : IFeatureFlagService
{
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);
    private readonly IFeatureFlagRepository _repository;
    private readonly IMasterAuditService _auditService;
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, byte> _trackedCacheKeys = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagService"/> class.
    /// </summary>
    /// <param name="repository">Feature flag repository.</param>
    /// <param name="auditService">Master audit logging service.</param>
    /// <param name="memoryCache">Memory cache instance.</param>
    public FeatureFlagService(
        IFeatureFlagRepository repository,
        IMasterAuditService auditService,
        IMemoryCache memoryCache)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    /// <inheritdoc />
    public async Task<bool> IsFeatureEnabledAsync(string flagKey, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flagKey))
            return false;

        var normalizedKey = flagKey.Trim().ToLowerInvariant();
        var cacheKey = $"ff_eval:{normalizedKey}:{tenantId?.ToString() ?? "global"}";

        if (_memoryCache.TryGetValue<bool>(cacheKey, out var cachedValue))
            return cachedValue;

        var flag = await GetFlagFromCacheOrDbAsync(normalizedKey, cancellationToken);
        if (flag is null)
            return false;

        var result = flag.Evaluate(tenantId);
        _memoryCache.Set(cacheKey, result, DefaultCacheDuration);
        _trackedCacheKeys.TryAdd(cacheKey, 0);

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> IsAutomationFrozenAsync(AdPlatform? platform = null, CancellationToken cancellationToken = default)
    {
        // 1. Check Global Automation Kill Switch
        var globalFlag = await GetFlagFromCacheOrDbAsync("killswitch.automation.global", cancellationToken);
        if (globalFlag != null && globalFlag.IsKillSwitchActive)
            return true;

        // 2. Check Platform-Specific Automation Kill Switch
        if (platform.HasValue)
        {
            var platformKey = GetPlatformKillSwitchKey(platform.Value);
            var platformFlag = await GetFlagFromCacheOrDbAsync(platformKey, cancellationToken);
            if (platformFlag != null && platformFlag.IsKillSwitchActive)
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> EvaluateAsync(string flagKey, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flagKey))
            return Result<bool>.Failure(Error.Validation("FeatureFlag.EmptyKey", "A chave da feature flag é obrigatória."));

        var normalizedKey = flagKey.Trim().ToLowerInvariant();
        var flag = await GetFlagFromCacheOrDbAsync(normalizedKey, cancellationToken);
        if (flag is null)
            return Result<bool>.Failure(Error.NotFound("FeatureFlag.NotFound", $"Feature flag '{normalizedKey}' não encontrada."));

        return Result<bool>.Success(flag.Evaluate(tenantId));
    }

    /// <inheritdoc />
    public async Task<Result> ActivateKillSwitchAsync(
        string flagKey,
        string reason,
        string triggeredBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flagKey))
            return Result.Failure(Error.Validation("FeatureFlag.EmptyKey", "A chave do kill switch é obrigatória."));

        var normalizedKey = flagKey.Trim().ToLowerInvariant();
        var flag = await _repository.GetByKeyAsync(normalizedKey, cancellationToken);
        if (flag is null)
            return Result.Failure(Error.NotFound("FeatureFlag.NotFound", $"Kill switch '{normalizedKey}' não encontrado."));

        if (!flag.IsKillSwitch)
            return Result.Failure(Error.Validation("FeatureFlag.NotAKillSwitch", $"A flag '{normalizedKey}' não é um Kill Switch operacional."));

        var result = flag.ActivateKillSwitch(reason, triggeredBy, DateTime.UtcNow);
        if (result.IsFailure)
            return result;

        await _repository.UpdateAsync(flag, cancellationToken);
        InvalidateCache(normalizedKey);

        await _auditService.RecordAsync(
            action: "KillSwitch.Activated",
            resource: "FeatureFlag",
            resourceId: normalizedKey,
            details: reason,
            tenantId: null,
            ipAddress: null,
            additionalTags: new[] { "kill_switch", "operational_emergency", "circuit_breaker" },
            cancellationToken: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateKillSwitchAsync(
        string flagKey,
        string reason,
        string triggeredBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flagKey))
            return Result.Failure(Error.Validation("FeatureFlag.EmptyKey", "A chave do kill switch é obrigatória."));

        var normalizedKey = flagKey.Trim().ToLowerInvariant();
        var flag = await _repository.GetByKeyAsync(normalizedKey, cancellationToken);
        if (flag is null)
            return Result.Failure(Error.NotFound("FeatureFlag.NotFound", $"Kill switch '{normalizedKey}' não encontrado."));

        if (!flag.IsKillSwitch)
            return Result.Failure(Error.Validation("FeatureFlag.NotAKillSwitch", $"A flag '{normalizedKey}' não é um Kill Switch operacional."));

        var result = flag.DeactivateKillSwitch(reason, triggeredBy, DateTime.UtcNow);
        if (result.IsFailure)
            return result;

        await _repository.UpdateAsync(flag, cancellationToken);
        InvalidateCache(normalizedKey);

        await _auditService.RecordAsync(
            action: "KillSwitch.Deactivated",
            resource: "FeatureFlag",
            resourceId: normalizedKey,
            details: reason,
            tenantId: null,
            ipAddress: null,
            additionalTags: new[] { "kill_switch", "operational_restored" },
            cancellationToken: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FeatureFlagDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var flags = await _repository.GetAllAsync(cancellationToken);
        var dtos = flags.Select(MapToDto).ToList();
        return Result<IReadOnlyList<FeatureFlagDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<FeatureFlagDto>> GetByKeyAsync(string flagKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flagKey))
            return Result<FeatureFlagDto>.Failure(Error.Validation("FeatureFlag.EmptyKey", "A chave da feature flag é obrigatória."));

        var normalizedKey = flagKey.Trim().ToLowerInvariant();
        var flag = await GetFlagFromCacheOrDbAsync(normalizedKey, cancellationToken);
        if (flag is null)
            return Result<FeatureFlagDto>.Failure(Error.NotFound("FeatureFlag.NotFound", $"Feature flag '{normalizedKey}' não encontrada."));

        return Result<FeatureFlagDto>.Success(MapToDto(flag));
    }

    private async Task<FeatureFlag?> GetFlagFromCacheOrDbAsync(string key, CancellationToken cancellationToken)
    {
        var cacheKey = $"ff_entity:{key}";
        if (_memoryCache.TryGetValue<FeatureFlag>(cacheKey, out var cachedFlag))
            return cachedFlag;

        var flag = await _repository.GetByKeyAsync(key, cancellationToken);
        if (flag != null)
        {
            _memoryCache.Set(cacheKey, flag, DefaultCacheDuration);
            _trackedCacheKeys.TryAdd(cacheKey, 0);
        }

        return flag;
    }

    private void InvalidateCache(string flagKey)
    {
        var prefix = $"ff_eval:{flagKey}:";
        var entityKey = $"ff_entity:{flagKey}";

        _memoryCache.Remove(entityKey);

        foreach (var key in _trackedCacheKeys.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || key.Equals(entityKey, StringComparison.OrdinalIgnoreCase))
            {
                _memoryCache.Remove(key);
                _trackedCacheKeys.TryRemove(key, out _);
            }
        }
    }

    private static string GetPlatformKillSwitchKey(AdPlatform platform) => platform switch
    {
        AdPlatform.Meta => "killswitch.automation.meta",
        AdPlatform.Google => "killswitch.automation.google",
        AdPlatform.TikTok => "killswitch.automation.tiktok",
        AdPlatform.Bing => "killswitch.automation.bing",
        _ => "killswitch.automation.global"
    };

    private static FeatureFlagDto MapToDto(FeatureFlag flag) => new(
        flag.Id,
        flag.Key,
        flag.Name,
        flag.Description,
        flag.IsEnabled,
        flag.IsKillSwitch,
        flag.TargetingType,
        flag.RolloutPercentage,
        flag.TargetTenantIds,
        flag.KillSwitchActivatedAtUtc,
        flag.KillSwitchReason,
        flag.KillSwitchTriggeredBy,
        flag.CreatedBy,
        flag.CreatedAtUtc,
        flag.UpdatedAtUtc,
        flag.UpdatedBy);
}
