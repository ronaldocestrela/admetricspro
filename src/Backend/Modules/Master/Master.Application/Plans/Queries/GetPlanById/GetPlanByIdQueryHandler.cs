using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Plans.DTOs;
using Master.Application.Repositories;
using Master.Domain.Plans;

namespace Master.Application.Plans.Queries.GetPlanById;

/// <summary>
/// Handles retrieving a single subscription plan by its identifier.
/// </summary>
public sealed class GetPlanByIdQueryHandler : IQueryHandler<GetPlanByIdQuery, PlanDto?>
{
    private readonly IPlanReadOnlyRepository _readOnlyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPlanByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="readOnlyRepository">Read-only plan repository.</param>
    public GetPlanByIdQueryHandler(IPlanReadOnlyRepository readOnlyRepository)
    {
        _readOnlyRepository = readOnlyRepository ?? throw new ArgumentNullException(nameof(readOnlyRepository));
    }

    /// <inheritdoc />
    public async Task<Result<PlanDto?>> Handle(GetPlanByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = await _readOnlyRepository.GetByIdAsync(new PlanId(request.PlanId), cancellationToken);

        return Result<PlanDto?>.Success(plan);
    }
}
