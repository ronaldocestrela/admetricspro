using SocialPublishing.Domain.Planning;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Repositório de snapshots idempotentes de materialização de planos editoriais.
/// </summary>
public interface IEditorialPlanGenerationRepository
{
    /// <summary>
    /// Obtém um registro pela chave de idempotência do plano.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="rangeStart">Data inicial inclusiva.</param>
    /// <param name="rangeEnd">Data final inclusiva.</param>
    /// <param name="seed">Semente do planner.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O registro ou <c>null</c> se não existir.</returns>
    Task<EditorialPlanGeneration?> GetByKeyAsync(
        Guid workspaceId,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        int seed,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo registro de materialização.
    /// </summary>
    /// <param name="generation">Agregado a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de conclusão.</returns>
    Task AddAsync(EditorialPlanGeneration generation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca um registro existente como modificado no contexto.
    /// </summary>
    /// <param name="generation">Agregado atualizado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de conclusão.</returns>
    Task UpdateAsync(EditorialPlanGeneration generation, CancellationToken cancellationToken = default);
}
