using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Master.Application.Billing.Trial;

/// <summary>
/// Manipulador do comando <see cref="ExecuteTrialNoticeCycleCommand"/>.
/// </summary>
public sealed class ExecuteTrialNoticeCycleCommandHandler : ICommandHandler<ExecuteTrialNoticeCycleCommand, TrialNoticeExecutionSummary>
{
    private readonly ITrialNotificationEngineService _engineService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ExecuteTrialNoticeCycleCommandHandler"/>.
    /// </summary>
    /// <param name="engineService">Serviço do motor de notificações de trial.</param>
    public ExecuteTrialNoticeCycleCommandHandler(ITrialNotificationEngineService engineService)
    {
        _engineService = engineService ?? throw new ArgumentNullException(nameof(engineService));
    }

    /// <inheritdoc />
    public Task<Result<TrialNoticeExecutionSummary>> Handle(ExecuteTrialNoticeCycleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return _engineService.ProcessTrialNoticesCycleAsync(command.ReferenceDateUtc, cancellationToken);
    }
}
