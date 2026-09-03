using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;

namespace Master.Application.Tenants.Queries.GetTenantDetails;

/// <summary>
/// Handles <see cref="GetTenantDetailsQuery"/> to fetch tenant directory information.
/// </summary>
public sealed class GetTenantDetailsQueryHandler : IQueryHandler<GetTenantDetailsQuery, TenantDetailsResponse>
{
    private readonly ITenantReadOnlyRepository _readOnlyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetTenantDetailsQueryHandler"/> class.
    /// </summary>
    /// <param name="readOnlyRepository">Read-only tenant repository.</param>
    public GetTenantDetailsQueryHandler(ITenantReadOnlyRepository readOnlyRepository)
    {
        _readOnlyRepository = readOnlyRepository;
    }

    /// <inheritdoc />
    public async Task<Result<TenantDetailsResponse>> Handle(GetTenantDetailsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var details = await _readOnlyRepository.GetDetailsByIdAsync(query.TenantId, cancellationToken);
        if (details is null)
        {
            return Result<TenantDetailsResponse>.Failure(
                Error.NotFound("Tenant.NotFound", "Tenant not found for the specified identifier."));
        }

        return Result<TenantDetailsResponse>.Success(details);
    }
}
