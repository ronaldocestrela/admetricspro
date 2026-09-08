using BuildingBlocks.Domain.Abstractions;

namespace Master.Domain.Tenants.Events;

/// <summary>
/// Evento de domínio emitido quando um lembrete da régua de trial é despachado com sucesso.
/// </summary>
/// <param name="TenantId">Identificador do tenant notificado.</param>
/// <param name="NoticeType">Tipo do lembrete de trial disparado.</param>
/// <param name="DaysRemaining">Total de dias restantes calculados até a expiração.</param>
/// <param name="RecipientEmail">Endereço de e-mail de destino.</param>
/// <param name="SentAtUtc">Timestamp de envio em UTC.</param>
public sealed record TenantTrialNoticeSentEvent(
    TenantId TenantId,
    TrialNoticeType NoticeType,
    double DaysRemaining,
    string RecipientEmail,
    DateTime SentAtUtc) : IDomainEvent;
