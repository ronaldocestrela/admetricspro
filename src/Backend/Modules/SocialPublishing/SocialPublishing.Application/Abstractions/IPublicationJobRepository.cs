using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Domain.Publications;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Persistência da outbox de publicação orgânica no banco do tenant.
/// </summary>
public interface IPublicationJobRepository
{
    /// <summary>
    /// Obtém um job pela chave de idempotência.
    /// </summary>
    /// <param name="idempotencyKey">Chave do slot.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Job existente ou null.</returns>
    Task<PublicationJob?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o job ativo do post (pendente, retentativa ou em progresso).
    /// </summary>
    /// <param name="postId">Identificador do post.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Job ativo ou null.</returns>
    Task<PublicationJob?> GetActiveByPostIdAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra um novo job na outbox, respeitando a chave única.
    /// </summary>
    /// <param name="job">Job a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sucesso ou conflito de idempotência.</returns>
    Task<Result> AddAsync(PublicationJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza um job rastreado no contexto atual.
    /// </summary>
    /// <param name="job">Job alterado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task UpdateAsync(PublicationJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenta reivindicar exclusivamente um job para processamento.
    /// </summary>
    /// <param name="jobId">Identificador do job.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Job em <see cref="PublicationJobStatus.InProgress"/> ou null se perdido ou indisponível.</returns>
    Task<PublicationJob?> TryClaimAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista jobs elegíveis para claim no instante UTC informado.
    /// </summary>
    /// <param name="utcNow">Relógio de referência em UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Jobs pendentes ou com retentativa devida.</returns>
    Task<IReadOnlyList<PublicationJob>> ListDueAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
