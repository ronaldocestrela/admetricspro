using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Master.Application.Tenants.Commands.TerminateImpersonationSession;

/// <summary>
/// Comando para revogação e encerramento imediato de uma sessão ativa de Shadow Mode (impersonation).
/// </summary>
/// <param name="TenantId">Identificador do Tenant alvo da sessão.</param>
/// <param name="SessionId">Identificador da sessão ativa a ser revogada.</param>
/// <param name="Reason">Justificativa opcional do encerramento (ex.: "Atendimento concluído").</param>
public sealed record TerminateImpersonationSessionCommand(
    Guid TenantId,
    Guid SessionId,
    string? Reason = null) : IRequest<Result>;
