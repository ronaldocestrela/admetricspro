using BuildingBlocks.Domain.Primitives;
using Master.Application.Plans.Commands.CreatePlan;
using Master.Application.Plans.Commands.UpdatePlan;
using Master.Application.Plans.DTOs;
using Master.Application.Plans.Queries.GetPlanById;
using Master.Application.Plans.Queries.GetPlans;
using MediatR;
using BackofficeApp.Models;

namespace BackofficeApp.Services;

/// <summary>
/// Implementação do serviço de parametrização de planos para consumo dos componentes Blazor Server.
/// </summary>
public sealed class PlanManagementService : IPlanManagementService
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PlanManagementService"/>.
    /// </summary>
    /// <param name="sender">Mediador de comandos e consultas in-memory.</param>
    public PlanManagementService(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<PlanDto>>> GetPlansAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        return _sender.Send(new GetPlansQuery(includeInactive), cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<PlanDto?>> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _sender.Send(new GetPlanByIdQuery(id), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreatePlanAsync(PlanFormViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        var command = new CreatePlanCommand(
            model.Name,
            model.Description,
            model.Tier,
            model.MonthlyPrice,
            model.AnnualDiscountPercentage,
            model.MaxSeats,
            model.MaxWorkspaces,
            model.MonthlyAdSpendCap,
            model.HasWhiteLabel,
            model.HasCustomCname,
            model.HasAiCopilot,
            model.HasCrossNetworkAutomations);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return Result<Guid>.Failure(result.Error);
        }

        return Result<Guid>.Success(result.Value.Value);
    }

    /// <inheritdoc />
    public async Task<Result> UpdatePlanAsync(PlanFormViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!model.PlanId.HasValue || model.PlanId.Value == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Plan.InvalidId", "O identificador do plano é obrigatório para atualização."));
        }

        var command = new UpdatePlanCommand(
            model.PlanId.Value,
            model.Name,
            model.Description,
            model.MonthlyPrice,
            model.AnnualDiscountPercentage,
            model.MaxSeats,
            model.MaxWorkspaces,
            model.MonthlyAdSpendCap,
            model.HasWhiteLabel,
            model.HasCustomCname,
            model.HasAiCopilot,
            model.HasCrossNetworkAutomations);

        return await _sender.Send(command, cancellationToken);
    }
}
