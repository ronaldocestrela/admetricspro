using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Persiste job de usuário e outbox no mesmo commit SQL do módulo social.
/// </summary>
public interface ISocialUserInitiatedJobCommitter
{
    /// <summary>
    /// Adiciona job e linha de outbox e consolida a transação.
    /// </summary>
    /// <param name="job">Job em <see cref="UserInitiatedJobStatus.Accepted"/>.</param>
    /// <param name="outboxMessage">Linha de outbox pendente.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> CommitAcceptedJobAsync(
        UserInitiatedJob job,
        IntegrationOutboxMessage outboxMessage,
        CancellationToken cancellationToken = default);
}