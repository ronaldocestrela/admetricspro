using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Domain.Plans;

namespace Master.Application.Plans.Commands.UpdatePlan;

/// <summary>
/// Handles updating an existing subscription plan's details, quotas, and features.
/// </summary>
public sealed class UpdatePlanCommandHandler : ICommandHandler<UpdatePlanCommand>
{
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatePlanCommandHandler"/> class.
    /// </summary>
    /// <param name="planRepository">Plan repository.</param>
    /// <param name="unitOfWork">Unit of work coordinator.</param>
    public UpdatePlanCommandHandler(IPlanRepository planRepository, IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdatePlanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var planId = new PlanId(command.PlanId);
        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
        {
            return Result.Failure(Error.NotFound("Plan.NotFound", $"Plano com ID '{command.PlanId}' não foi localizado."));
        }

        var nameExists = await _planRepository.ExistsByNameAsync(command.Name, plan.Id, cancellationToken);
        if (nameExists)
        {
            return Result.Failure(Error.Conflict(
                "Plan.NameAlreadyExists",
                $"Já existe outro plano cadastrado com o nome '{command.Name}'."));
        }

        var limitsResult = PlanLimits.Create(command.MaxSeats, command.MaxWorkspaces, command.MonthlyAdSpendCap);
        if (limitsResult.IsFailure)
        {
            return Result.Failure(limitsResult.Error);
        }

        var featuresResult = PlanFeatures.Create(
            command.HasWhiteLabel,
            command.HasCustomCname,
            command.HasAiCopilot,
            command.HasCrossNetworkAutomations);
        if (featuresResult.IsFailure)
        {
            return Result.Failure(featuresResult.Error);
        }

        var detailsResult = plan.UpdateDetails(
            command.Name,
            command.Description,
            command.MonthlyPrice,
            command.AnnualDiscountPercentage);
        if (detailsResult.IsFailure)
        {
            return detailsResult;
        }

        plan.UpdateLimits(limitsResult.Value);
        plan.UpdateFeatures(featuresResult.Value);

        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
