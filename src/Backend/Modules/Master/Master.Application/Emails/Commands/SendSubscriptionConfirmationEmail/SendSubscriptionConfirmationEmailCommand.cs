using BuildingBlocks.Application.Messaging;

namespace Master.Application.Emails.Commands.SendSubscriptionConfirmationEmail;

/// <summary>
/// Comando de integração para envio do e-mail de confirmação de assinatura.
/// </summary>
/// <param name="EventId">Identificador do evento de integração.</param>
/// <param name="TenantId">Tenant ativado.</param>
public sealed record SendSubscriptionConfirmationEmailCommand(Guid EventId, Guid TenantId) : ICommand;