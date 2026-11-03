using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Publications;

namespace SocialPublishing.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core de <see cref="IPublicationJobRepository"/> com claim otimista.
/// </summary>
public sealed class PublicationJobRepository : IPublicationJobRepository
{
    private readonly SocialPublishingDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PublicationJobRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor scoped do contexto social.</param>
    public PublicationJobRepository(SocialPublishingDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<PublicationJob?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.PublicationJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PublicationJob?> GetActiveByPostIdAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.PublicationJobs
            .FirstOrDefaultAsync(
                j => j.PostId == postId &&
                     (j.Status == PublicationJobStatus.Pending ||
                      j.Status == PublicationJobStatus.RetryScheduled ||
                      j.Status == PublicationJobStatus.InProgress),
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> AddAsync(PublicationJob job, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result.Failure(contextResult.Error);
        }

        var context = contextResult.Value;
        var exists = await context.PublicationJobs
            .AnyAsync(j => j.IdempotencyKey == job.IdempotencyKey, cancellationToken);
        if (exists)
        {
            return Result.Failure(SocialErrors.PublicationJobAlreadyExists);
        }

        await context.PublicationJobs.AddAsync(job, cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PublicationJob job, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        contextResult.Value.PublicationJobs.Update(job);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<PublicationJob?> TryClaimAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        var context = contextResult.Value;
        var job = await context.PublicationJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job is null || !job.CanBeClaimed)
        {
            return null;
        }

        var claim = job.MarkInProgress();
        if (claim.IsFailure)
        {
            return null;
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return job;
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }
    }
}
