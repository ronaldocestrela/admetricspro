using Automations.Application.SafetyGuards.DTOs;
using Automations.Domain.SafetyGuards;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Application.SafetyGuards.Commands.CheckOverspending;

/// <summary>
/// Comando CQRS para execução da trava de segurança de Overspending no workspace especificado.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace.</param>
/// <param name="ThresholdMultiplier">Multiplicador de estouro (padrão: 1.20 para 120%).</param>
public sealed record CheckOverspendingCommand(
    Guid WorkspaceId,
    decimal ThresholdMultiplier = IOverspendingGuard.DefaultOverspendingThreshold) : ICommand<CheckOverspendingResultDto>;

/// <summary>
/// Manipulador do comando <see cref="CheckOverspendingCommand"/>.
/// </summary>
public sealed class CheckOverspendingCommandHandler : ICommandHandler<CheckOverspendingCommand, CheckOverspendingResultDto>
{
    private readonly IOverspendingGuard _guard;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CheckOverspendingCommandHandler"/>.
    /// </summary>
    /// <param name="guard">Serviço de domínio da trava de Overspending.</param>
    public CheckOverspendingCommandHandler(IOverspendingGuard guard)
    {
        _guard = guard ?? throw new ArgumentNullException(nameof(guard));
    }

    /// <inheritdoc />
    public async Task<Result<CheckOverspendingResultDto>> Handle(
        CheckOverspendingCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var checkResult = await _guard.CheckAndMitigateOverspendingAsync(
            command.WorkspaceId,
            command.ThresholdMultiplier,
            cancellationToken);

        if (checkResult.IsFailure)
        {
            return Result<CheckOverspendingResultDto>.Failure(checkResult.Error);
        }

        var value = checkResult.Value;
        var incidentsDto = value.Incidents.Select(SafetyIncidentDto.FromEntity).ToList();

        return Result<CheckOverspendingResultDto>.Success(new CheckOverspendingResultDto(
            value.WorkspaceId,
            value.EvaluatedCampaignsCount,
            value.ViolatedCampaignsCount,
            value.PausedCampaignsCount,
            incidentsDto));
    }
}
