using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Messaging;

/// <summary>
/// Lease exclusivo para o worker de despacho de outbox no catálogo Master.
/// </summary>
public sealed class MessagingSchedulerLease : Entity<Guid>
{
    /// <summary>
    /// Chave estável do lease de despacho de outbox.
    /// </summary>
    public const string OutboxDispatchLeaseKey = "outbox-dispatch";

    private MessagingSchedulerLease(
        Guid id,
        string leaseKey,
        string holderInstanceId,
        DateTime acquiredAtUtc,
        DateTime expiresAtUtc)
        : base(id)
    {
        LeaseKey = leaseKey;
        HolderInstanceId = holderInstanceId;
        AcquiredAtUtc = acquiredAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    private MessagingSchedulerLease()
        : base(Guid.Empty)
    {
        LeaseKey = string.Empty;
        HolderInstanceId = string.Empty;
    }

    /// <summary>
    /// Obtém a chave única do lease.
    /// </summary>
    public string LeaseKey { get; private set; }

    /// <summary>
    /// Obtém o identificador da instância que detém o lease.
    /// </summary>
    public string HolderInstanceId { get; private set; }

    /// <summary>
    /// Obtém o instante UTC de aquisição.
    /// </summary>
    public DateTime AcquiredAtUtc { get; private set; }

    /// <summary>
    /// Obtém o instante UTC de expiração do lease.
    /// </summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Cria um novo lease.
    /// </summary>
    /// <param name="id">Identificador da linha.</param>
    /// <param name="leaseKey">Chave do lease.</param>
    /// <param name="holderInstanceId">Instância detentora.</param>
    /// <param name="acquiredAtUtc">Aquisição em UTC.</param>
    /// <param name="expiresAtUtc">Expiração em UTC.</param>
    /// <returns>Lease criado ou falha de validação.</returns>
    public static Result<MessagingSchedulerLease> Create(
        Guid id,
        string leaseKey,
        string holderInstanceId,
        DateTime acquiredAtUtc,
        DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(leaseKey))
        {
            return Result<MessagingSchedulerLease>.Failure(
                Error.Validation("Messaging.LeaseInvalid", "A chave do lease é obrigatória."));
        }

        if (string.IsNullOrWhiteSpace(holderInstanceId))
        {
            return Result<MessagingSchedulerLease>.Failure(
                Error.Validation("Messaging.LeaseInvalid", "O identificador da instância é obrigatório."));
        }

        if (acquiredAtUtc.Kind != DateTimeKind.Utc || expiresAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<MessagingSchedulerLease>.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        if (expiresAtUtc <= acquiredAtUtc)
        {
            return Result<MessagingSchedulerLease>.Failure(
                Error.Validation("Messaging.LeaseInvalid", "A expiração deve ser posterior à aquisição."));
        }

        return Result<MessagingSchedulerLease>.Success(
            new MessagingSchedulerLease(id, leaseKey.Trim(), holderInstanceId.Trim(), acquiredAtUtc, expiresAtUtc));
    }

    /// <summary>
    /// Renova o lease para a mesma instância.
    /// </summary>
    /// <param name="expiresAtUtc">Novo instante de expiração em UTC.</param>
    /// <returns>Resultado da operação.</returns>
    public Result Renew(DateTime expiresAtUtc)
    {
        if (expiresAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        if (expiresAtUtc <= AcquiredAtUtc)
        {
            return Result.Failure(Error.Validation("Messaging.LeaseInvalid", "A expiração deve ser posterior à aquisição."));
        }

        ExpiresAtUtc = expiresAtUtc;
        return Result.Success();
    }

    /// <summary>
    /// Troca o detentor quando o lease expirou ou está livre.
    /// </summary>
    /// <param name="newHolderInstanceId">Nova instância.</param>
    /// <param name="acquiredAtUtc">Novo instante de aquisição.</param>
    /// <param name="expiresAtUtc">Nova expiração.</param>
    /// <returns>Resultado da operação.</returns>
    public Result TakeOver(string newHolderInstanceId, DateTime acquiredAtUtc, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(newHolderInstanceId))
        {
            return Result.Failure(
                Error.Validation("Messaging.LeaseInvalid", "O identificador da instância é obrigatório."));
        }

        HolderInstanceId = newHolderInstanceId.Trim();
        AcquiredAtUtc = acquiredAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        return Result.Success();
    }
}