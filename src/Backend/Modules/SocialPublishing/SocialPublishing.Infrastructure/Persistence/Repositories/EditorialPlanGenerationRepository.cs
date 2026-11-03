using Microsoft.EntityFrameworkCore;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain.Planning;

namespace SocialPublishing.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core de <see cref="IEditorialPlanGenerationRepository"/>.
/// </summary>
public sealed class EditorialPlanGenerationRepository : IEditorialPlanGenerationRepository
{
    private readonly SocialPublishingDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="EditorialPlanGenerationRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor scoped do contexto social.</param>
    public EditorialPlanGenerationRepository(SocialPublishingDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<EditorialPlanGeneration?> GetByKeyAsync(
        Guid workspaceId,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        int seed,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.Set<EditorialPlanGeneration>()
            .FirstOrDefaultAsync(
                g => g.WorkspaceId == workspaceId &&
                     g.RangeStart == rangeStart &&
                     g.RangeEnd == rangeEnd &&
                     g.Seed == seed,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(EditorialPlanGeneration generation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        await contextResult.Value.Set<EditorialPlanGeneration>().AddAsync(generation, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(EditorialPlanGeneration generation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        contextResult.Value.Set<EditorialPlanGeneration>().Update(generation);
        await Task.CompletedTask;
    }
}
