using BuildingBlocks.Application.Messaging;

namespace Master.Application.Tenants.Commands.ResendWelcomeEmail;

/// <summary>
/// Comando para reenvio manual do e-mail de boas-vindas para o gestor principal do inquilino.
/// </summary>
/// <param name="TenantId">Identificador único do tenant no catálogo Master.</param>
public sealed record ResendWelcomeEmailCommand(Guid TenantId) : ICommand<bool>;
