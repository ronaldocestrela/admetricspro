using BuildingBlocks.Application.Messaging;
using Master.Domain.Tenants;

namespace Master.Application.Emails.Commands.SendDunningStageEmail;

/// <summary>
/// Comando de integração para envio de e-mail de transição de estágio de inadimplência.
/// </summary>
/// <param name="EventId">Identificador do evento de integração.</param>
/// <param name="TenantId">Tenant inadimplente.</param>
/// <param name="DunningStage">Estágio atingido.</param>
/// <param name="PaymentDueDateUtc">Vencimento da fatura em aberto.</param>
public sealed record SendDunningStageEmailCommand(
    Guid EventId,
    Guid TenantId,
    DunningStage DunningStage,
    DateTime PaymentDueDateUtc) : ICommand;