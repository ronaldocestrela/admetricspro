namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Item de dead-letter para consulta operacional no Backoffice.
/// </summary>
/// <param name="EventId">Identificador do evento morto.</param>
/// <param name="MessageType">Tipo versionado da mensagem.</param>
/// <param name="TenantId">Tenant contextual quando aplicável.</param>
/// <param name="AttemptCount">Tentativas registradas.</param>
/// <param name="LastErrorCode">Último código de erro.</param>
/// <param name="CreatedAtUtc">Criação da linha.</param>
public sealed record IntegrationDeadLetterListItemDto(
    Guid EventId,
    string MessageType,
    Guid? TenantId,
    int AttemptCount,
    string? LastErrorCode,
    DateTime CreatedAtUtc);