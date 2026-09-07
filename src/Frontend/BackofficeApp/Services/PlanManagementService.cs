using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Plans.DTOs;
using BackofficeApp.Models;

namespace BackofficeApp.Services;

/// <summary>
/// Implementação do serviço de parametrização de planos para consumo dos componentes Blazor Server no Backoffice.
/// Consome as rotas versionadas da Web API via cliente HTTP fortemente tipado.
/// </summary>
public sealed class PlanManagementService : IPlanManagementService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PlanManagementService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado para acesso à Web API.</param>
    public PlanManagementService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PlanDto>>> GetPlansAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/plans?includeInactive={includeInactive}", cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<PlanDto>>>(JsonOptions, cancellationToken);
            return result ?? Result<IReadOnlyList<PlanDto>>.Failure(Error.Failure("Plans.FetchFailed", "Falha ao obter planos da API."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<PlanDto>>.Failure(Error.Failure("Plans.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PlanDto?>> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/plans/{id}", cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<PlanDto>>(JsonOptions, cancellationToken);

            if (result is null || result.IsFailure)
            {
                return Result<PlanDto?>.Failure(result?.Error ?? Error.NotFound("Plan.NotFound", "Plano não localizado na API."));
            }

            return Result<PlanDto?>.Success(result.Value);
        }
        catch (Exception ex)
        {
            return Result<PlanDto?>.Failure(Error.Failure("Plan.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreatePlanAsync(PlanFormViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            var payload = new
            {
                Name = model.Name,
                Description = model.Description,
                Tier = model.Tier,
                MonthlyPrice = model.MonthlyPrice,
                AnnualDiscountPercentage = model.AnnualDiscountPercentage,
                MaxSeats = model.MaxSeats,
                MaxWorkspaces = model.MaxWorkspaces,
                MonthlyAdSpendCap = model.MonthlyAdSpendCap,
                HasWhiteLabel = model.HasWhiteLabel,
                HasCustomCname = model.HasCustomCname,
                HasAiCopilot = model.HasAiCopilot,
                HasCrossNetworkAutomations = model.HasCrossNetworkAutomations
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/plans", payload, JsonOptions, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<Guid>>(JsonOptions, cancellationToken);

            return result ?? Result<Guid>.Failure(Error.Failure("Plan.CreateFailed", "Falha ao cadastrar plano na API."));
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(Error.Failure("Plan.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdatePlanAsync(PlanFormViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!model.PlanId.HasValue || model.PlanId.Value == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Plan.InvalidId", "O identificador do plano é obrigatório para atualização."));
        }

        try
        {
            var payload = new
            {
                Name = model.Name,
                Description = model.Description,
                MonthlyPrice = model.MonthlyPrice,
                AnnualDiscountPercentage = model.AnnualDiscountPercentage,
                MaxSeats = model.MaxSeats,
                MaxWorkspaces = model.MaxWorkspaces,
                MonthlyAdSpendCap = model.MonthlyAdSpendCap,
                HasWhiteLabel = model.HasWhiteLabel,
                HasCustomCname = model.HasCustomCname,
                HasAiCopilot = model.HasAiCopilot,
                HasCrossNetworkAutomations = model.HasCrossNetworkAutomations
            };

            var response = await _httpClient.PutAsJsonAsync($"/api/v1/plans/{model.PlanId.Value}", payload, JsonOptions, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(Error.Failure("Plan.UpdateFailed", "Falha ao atualizar plano na API."));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Plan.NetworkError", ex.Message));
        }
    }
}
