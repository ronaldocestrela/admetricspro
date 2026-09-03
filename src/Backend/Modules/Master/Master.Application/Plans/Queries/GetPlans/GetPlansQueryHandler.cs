using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Plans.DTOs;
using Master.Application.Repositories;

namespace Master.Application.Plans.Queries.GetPlans;

/// <summary>
/// Handles retrieving all subscription plans from the catalog.
/// </summary>
public sealed class GetPlansQueryHandler : IQueryHandler<GetPlansQuery, IReadOnlyList<PlanDto>>
{
    private readonly IPlanReadOnlyRepository _readOnlyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPlansQueryHandler"/> class.
    /// </summary>
    /// <param name="readOnlyRepository">Read-only plan repository.</param>
    public GetPlansQueryHandler(IPlanReadOnlyRepository readOnlyRepository)
    {
        _readOnlyRepository = readOnlyRepository ?? throw new ArgumentNullException(nameof(readOnlyRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PlanDto>>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plans = await _readOnlyRepository.ListAllAsync(request.IncludeInactive, cancellationToken);

        return Result<IReadOnlyList<PlanDto>>.Success(plans);
    }
}
