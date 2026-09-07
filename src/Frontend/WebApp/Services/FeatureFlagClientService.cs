using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Master.Application.FeatureFlags.DTOs;
using Master.Domain.FeatureFlags;
using Master.Domain.Integrations;

namespace WebApp.Services;

/// <summary>
/// Implementação do serviço cliente de Feature Flags e Kill Switches para o Blazor Server.
/// Consome as rotas versionadas da Web API via cliente HTTP fortemente tipado.
/// </summary>
public sealed class FeatureFlagClientService : IFeatureFlagClientService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="FeatureFlagClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado para acesso à Web API.</param>
    public FeatureFlagClientService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FeatureFlagDto>>> GetAllFlagsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/admin/feature-flags", cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<FeatureFlagDto>>>(JsonOptions, cancellationToken);
            return result ?? Result<IReadOnlyList<FeatureFlagDto>>.Failure(Error.Failure("FeatureFlags.FetchFailed", "Falha ao obter feature flags da API."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<FeatureFlagDto>>.Failure(Error.Failure("FeatureFlags.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<FeatureFlagDto>> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/admin/feature-flags/{Uri.EscapeDataString(key)}", cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<FeatureFlagDto>>(JsonOptions, cancellationToken);
            return result ?? Result<FeatureFlagDto>.Failure(Error.NotFound("FeatureFlag.NotFound", "Feature flag não localizada na API."));
        }
        catch (Exception ex)
        {
            return Result<FeatureFlagDto>.Failure(Error.Failure("FeatureFlag.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAutomationFrozenAsync(AdPlatform? platform = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = platform.HasValue
                ? $"/api/v1/admin/feature-flags/automation-status?platform={platform.Value}"
                : "/api/v1/admin/feature-flags/automation-status";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<AutomationEngineStatusDto>>(JsonOptions, cancellationToken);

            return result?.IsSuccess == true && result.Value.IsFrozen;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Result> ActivateKillSwitchAsync(
        string key,
        string reason,
        string triggeredBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { Reason = reason, TriggeredBy = triggeredBy };
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/admin/feature-flags/{Uri.EscapeDataString(key)}/kill-switch/activate",
                payload,
                JsonOptions,
                cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);
            return result ?? Result.Failure(Error.Failure("KillSwitch.ActivateFailed", "Falha ao acionar Kill Switch na API."));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("KillSwitch.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateKillSwitchAsync(
        string key,
        string reason,
        string triggeredBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { Reason = reason, TriggeredBy = triggeredBy };
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/admin/feature-flags/{Uri.EscapeDataString(key)}/kill-switch/deactivate",
                payload,
                JsonOptions,
                cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);
            return result ?? Result.Failure(Error.Failure("KillSwitch.DeactivateFailed", "Falha ao desativar Kill Switch na API."));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("KillSwitch.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateFlagAsync(
        Guid id,
        bool isEnabled,
        FeatureFlagTargetingType targetingType,
        int rolloutPercentage,
        IReadOnlyCollection<Guid>? targetTenantIds,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                IsEnabled = isEnabled,
                TargetingType = targetingType,
                RolloutPercentage = rolloutPercentage,
                TargetTenantIds = targetTenantIds,
                UpdatedBy = updatedBy
            };

            var response = await _httpClient.PutAsJsonAsync(
                $"/api/v1/admin/feature-flags/{id}",
                payload,
                JsonOptions,
                cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);
            return result ?? Result.Failure(Error.Failure("FeatureFlag.UpdateFailed", "Falha ao atualizar feature flag na API."));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("FeatureFlag.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateFlagAsync(
        string key,
        string name,
        string description,
        bool isEnabled,
        bool isKillSwitch,
        FeatureFlagTargetingType targetingType,
        int rolloutPercentage,
        IReadOnlyCollection<Guid>? targetTenantIds,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                Key = key,
                Name = name,
                Description = description,
                IsEnabled = isEnabled,
                IsKillSwitch = isKillSwitch,
                TargetingType = targetingType,
                RolloutPercentage = rolloutPercentage,
                TargetTenantIds = targetTenantIds,
                CreatedBy = createdBy
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/admin/feature-flags",
                payload,
                JsonOptions,
                cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<Result<Guid>>(JsonOptions, cancellationToken);
            return result ?? Result<Guid>.Failure(Error.Failure("FeatureFlag.CreateFailed", "Falha ao criar feature flag na API."));
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(Error.Failure("FeatureFlag.NetworkError", ex.Message));
        }
    }
}
