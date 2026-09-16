using Automations.Application.SafetyGuards.DTOs;
using Automations.Domain.SafetyGuards;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Application.SafetyGuards.Commands.CheckLandingPages;

/// <summary>
/// Comando CQRS para execução da trava de verificação de integridade de Landing Pages (Detector 404/500).
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace associado.</param>
public sealed record CheckLandingPagesCommand(Guid WorkspaceId) : ICommand<CheckLandingPagesResultDto>;

/// <summary>
/// Manipulador do comando <see cref="CheckLandingPagesCommand"/>.
/// </summary>
public sealed class CheckLandingPagesCommandHandler : ICommandHandler<CheckLandingPagesCommand, CheckLandingPagesResultDto>
{
    private readonly ILandingPageHealthChecker _checker;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CheckLandingPagesCommandHandler"/>.
    /// </summary>
    /// <param name="checker">Serviço de domínio do detector de páginas de destino.</param>
    public CheckLandingPagesCommandHandler(ILandingPageHealthChecker checker)
    {
        _checker = checker ?? throw new ArgumentNullException(nameof(checker));
    }

    /// <inheritdoc />
    public async Task<Result<CheckLandingPagesResultDto>> Handle(
        CheckLandingPagesCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var checkResult = await _checker.CheckAndMitigateLandingPagesAsync(
            command.WorkspaceId,
            cancellationToken);

        if (checkResult.IsFailure)
        {
            return Result<CheckLandingPagesResultDto>.Failure(checkResult.Error);
        }

        var value = checkResult.Value;
        var incidentsDto = value.Incidents.Select(SafetyIncidentDto.FromEntity).ToList();

        return Result<CheckLandingPagesResultDto>.Success(new CheckLandingPagesResultDto(
            value.WorkspaceId,
            value.EvaluatedAdsCount,
            value.HealthyAdsCount,
            value.BrokenAdsCount,
            value.PausedAdsCount,
            incidentsDto));
    }
}
