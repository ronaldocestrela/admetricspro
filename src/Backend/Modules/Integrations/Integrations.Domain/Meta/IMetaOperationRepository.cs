using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;

namespace Integrations.Domain.Meta;

/// <summary>Reserva e auditoria durável das operações de escrita da Meta.</summary>
public interface IMetaOperationRepository
{
    /// <summary>Reserva a chave antes do envio, consolidando a reserva pelo UnitOfWork.</summary>
    /// <param name="operation">Registro pendente.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>True quando adquirido; false se outra execução já reservou a chave.</returns>
    Task<Result<bool>> TryBeginAsync(MetaApiOperation operation, CancellationToken cancellationToken);
    /// <summary>Consulta uma operação pelo contexto completo.</summary>
    /// <param name="id">Chave idempotente.</param>
    /// <param name="workspaceId">Workspace autorizado.</param>
    /// <param name="accountId">Conta autorizada.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Registro do contexto solicitado, ou nulo.</returns>
    Task<MetaApiOperation?> GetAsync(Guid id, Guid workspaceId, Guid accountId, CancellationToken cancellationToken);
    /// <summary>Atualiza a conclusão para commit pela unidade de trabalho.</summary>
    /// <param name="operation">Operação pertencente ao contexto atual.</param>
    void Update(MetaApiOperation operation);
}
