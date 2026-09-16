using BuildingBlocks.Domain.Automations.SafetyGuards;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Domain.SafetyGuards;

/// <summary>
/// Contrato do despachador de alertas de segurança multi-canal (Slack, WhatsApp, E-mail e Webhook).
/// </summary>
public interface ISecurityAlertNotifier
{
    /// <summary>
    /// Despacha a notificação de alerta de emergência/crítica para todos os canais configurados de forma resiliente.
    /// </summary>
    /// <param name="payload">Dados estruturados do alerta contendo contexto técnico e métricas.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo relatório de entrega por canal.</returns>
    Task<Result<IReadOnlyDictionary<SafetyAlertChannel, bool>>> DispatchAlertAsync(
        SafetyAlertPayload payload,
        CancellationToken cancellationToken = default);
}
