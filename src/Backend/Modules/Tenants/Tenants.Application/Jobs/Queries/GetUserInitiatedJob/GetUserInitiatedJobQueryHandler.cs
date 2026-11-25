using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Tenants.Application.Jobs.Queries.GetUserInitiatedJob;

/// <summary>
/// Manipulador da consulta <see cref="GetUserInitiatedJobQuery"/>.
/// </summary>
public sealed class GetUserInitiatedJobQueryHandler(IUserInitiatedJobStore jobStore)
    : IQueryHandler<GetUserInitiatedJobQuery, UserInitiatedJobDto>
{
    /// <inheritdoc />
    public async Task<Result<UserInitiatedJobDto>> Handle(
        GetUserInitiatedJobQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty || request.JobId == Guid.Empty)
        {
            return Result<UserInitiatedJobDto>.Failure(UserInitiatedJobErrors.InvalidId);
        }

        var job = await jobStore.GetByIdAsync(request.WorkspaceId, request.JobId, cancellationToken);
        if (job is null)
        {
            return Result<UserInitiatedJobDto>.Failure(UserInitiatedJobErrors.NotFound);
        }

        return Result<UserInitiatedJobDto>.Success(UserInitiatedJobDto.FromEntity(job));
    }
}