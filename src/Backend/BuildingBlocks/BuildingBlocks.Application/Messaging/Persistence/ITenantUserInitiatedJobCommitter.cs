using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging.Persistence;

/// <summary>
/// Persiste job de usuário e outbox no banco operacional do tenant.
/// </summary>
public interface ITenantUserInitiatedJobCommitter
{
    /// <summary>
    /// Adiciona job e outbox e consolida a transação.
    /// </summary>
    /// <param name="job">Job aceito.</param>
    /// <param name="outboxMessage">Linha de outbox pendente.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> CommitAcceptedJobAsync(
        UserInitiatedJob job,
        IntegrationOutboxMessage outboxMessage,
        CancellationToken cancellationToken = default);
}