using BuildingBlocks.Application.Messaging;

namespace Master.Application.Emails.Commands.SendTenantWelcomeEmail;

/// <summary>
/// Comando de integração para envio do e-mail de boas-vindas a partir do consumidor de catálogo.
/// </summary>
/// <param name="EventId">Identificador do evento de integração.</param>
/// <param name="TenantId">Tenant destinatário.</param>
public sealed record SendTenantWelcomeEmailCommand(Guid EventId, Guid TenantId) : ICommand;