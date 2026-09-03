using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Domain.Plans;

namespace Master.Application.Plans.Commands.CreatePlan;

/// <summary>
/// Handles the creation of a new subscription plan.
/// </summary>
public sealed class CreatePlanCommandHandler : ICommandHandler<CreatePlanCommand, PlanId>
{
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePlanCommandHandler"/> class.
    /// </summary>
    /// <param name="planRepository">Plan repository.</param>
    /// <param name="unitOfWork">Unit of work coordinator.</param>
    public CreatePlanCommandHandler(IPlanRepository planRepository, IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result<PlanId>> Handle(CreatePlanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var nameExists = await _planRepository.ExistsByNameAsync(command.Name, null, cancellationToken);
        if (nameExists)
        {
            return Result<PlanId>.Failure(Error.Conflict(
                "Plan.NameAlreadyExists",
                $"Já existe um plano cadastrado com o nome '{command.Name}'."));
        }

        var limitsResult = PlanLimits.Create(command.MaxSeats, command.MaxWorkspaces, command.MonthlyAdSpendCap);
        if (limitsResult.IsFailure)
        {
            return Result<PlanId>.Failure(limitsResult.Error);
        }

        var featuresResult = PlanFeatures.Create(
            command.HasWhiteLabel,
            command.HasCustomCname,
            command.HasAiCopilot,
            command.HasCrossNetworkAutomations);
        if (featuresResult.IsFailure)
        {
            return Result<PlanId>.Failure(featuresResult.Error);
        }

        var planResult = SubscriptionPlan.Create(
            command.Name,
            command.Description,
            command.Tier,
            command.MonthlyPrice,
            command.AnnualDiscountPercentage,
            limitsResult.Value,
            featuresResult.Value);

        if (planResult.IsFailure)
        {
            return Result<PlanId>.Failure(planResult.Error);
        }

        var plan = planResult.Value;
        await _planRepository.AddAsync(plan, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<PlanId>.Success(plan.Id);
    }
}
