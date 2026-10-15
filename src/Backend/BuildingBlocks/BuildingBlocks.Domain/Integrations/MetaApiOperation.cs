using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Integrations;

/// <summary>Diário de idempotência e auditoria sem tokens ou payload publicitário sensível.</summary>
public sealed class MetaApiOperation
{
    /// <summary>Chave de idempotência fornecida uma vez para cada intenção de escrita.</summary>
    public Guid Id { get; private set; }
    /// <summary>Workspace proprietário.</summary>
    public Guid WorkspaceId { get; private set; }
    /// <summary>Conta interna autorizada.</summary>
    public Guid AccountId { get; private set; }
    /// <summary>Usuário que solicitou a alteração.</summary>
    public Guid UserId { get; private set; }
    /// <summary>Tipo de recurso e operação.</summary>
    public string Resource { get; private set; } = string.Empty;
    /// <summary>Hash do pedido, incluindo conta, recurso e campos.</summary>
    public string RequestHash { get; private set; } = string.Empty;
    /// <summary>Pending, Succeeded, Rejected ou Unknown.</summary>
    public string State { get; private set; } = "Pending";
    /// <summary>Recurso externo criado ou alterado.</summary>
    public string? ExternalId { get; private set; }
    /// <summary>Código de falha sanitizado, sem corpo externo.</summary>
    public string? ErrorCode { get; private set; }
    /// <summary>Início UTC.</summary>
    public DateTime CreatedAtUtc { get; private set; }
    /// <summary>Conclusão UTC conhecida.</summary>
    public DateTime? CompletedAtUtc { get; private set; }

    private MetaApiOperation() { }

    /// <summary>Cria um registro pendente antes de qualquer POST externo.</summary>
    /// <param name="id">Chave idempotente.</param>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna.</param>
    /// <param name="userId">Autor autenticado.</param>
    /// <param name="resource">Recurso suportado.</param>
    /// <param name="requestHash">SHA-256 do pedido.</param>
    /// <returns>Registro ou falha de validação.</returns>
    public static Result<MetaApiOperation> Create(Guid id, Guid workspaceId, Guid accountId, Guid userId, string resource, string requestHash)
    {
        if (id == Guid.Empty || workspaceId == Guid.Empty || accountId == Guid.Empty || userId == Guid.Empty || requestHash.Length != 64 || resource.Length > 50)
            return Result<MetaApiOperation>.Failure(Error.Validation("Meta.InvalidOperation", "Operação Meta sem identificação ou contexto válido."));
        return Result<MetaApiOperation>.Success(new() { Id = id, WorkspaceId = workspaceId, AccountId = accountId, UserId = userId, Resource = resource, RequestHash = requestHash, CreatedAtUtc = DateTime.UtcNow });
    }

    /// <summary>Registra a resposta sem armazenar segredos ou repetir pedidos incertos.</summary>
    /// <param name="externalId">ID confirmado, quando houver.</param>
    /// <param name="errorCode">Código sanitizado, ou nulo em sucesso.</param>
    public void Complete(string? externalId, string? errorCode)
    {
        ExternalId = externalId;
        ErrorCode = errorCode;
        State = errorCode is null ? "Succeeded" : errorCode == "Meta.WriteOutcomeUnknown" ? "Unknown" : "Rejected";
        CompletedAtUtc = DateTime.UtcNow;
    }
}
