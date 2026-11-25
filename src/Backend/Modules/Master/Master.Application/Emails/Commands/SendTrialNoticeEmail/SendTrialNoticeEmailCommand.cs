using BuildingBlocks.Application.Messaging;
using Master.Domain.Tenants;

namespace Master.Application.Emails.Commands.SendTrialNoticeEmail;

/// <summary>
/// Comando de integração para envio de um marco da régua de trial.
/// </summary>
/// <param name="EventId">Identificador do evento de integração.</param>
/// <param name="TenantId">Tenant em trial.</param>
/// <param name="NoticeType">Marco temporal (7, 3, 1 dia ou expirado).</param>
public sealed record SendTrialNoticeEmailCommand(
    Guid EventId,
    Guid TenantId,
    TrialNoticeType NoticeType) : ICommand;