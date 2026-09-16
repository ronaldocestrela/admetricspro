using Analytics.Application.Copilot.DTOs;
using Analytics.Domain.Copilot;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Application.Copilot.Commands.ExecuteCopilotRecommendation;

/// <summary>
/// Manipulador do comando <see cref="ExecuteCopilotRecommendationCommand"/>.
/// Executa a ação de 1 clique, persiste as alterações operacionais e registra log imutável de auditoria no Tenant.
/// </summary>
public sealed class ExecuteCopilotRecommendationCommandHandler : ICommandHandler<ExecuteCopilotRecommendationCommand, ExecuteCopilotActionResultDto>
{
    private readonly ICopilotDataProvider _dataProvider;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ExecuteCopilotRecommendationCommandHandler"/>.
    /// </summary>
    /// <param name="dataProvider">Provedor de dados e operações do Copiloto.</param>
    public ExecuteCopilotRecommendationCommandHandler(ICopilotDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    /// <inheritdoc />
    public async Task<Result<ExecuteCopilotActionResultDto>> Handle(
        ExecuteCopilotRecommendationCommand command,
        CancellationToken cancellationToken)
    {
        if (command.WorkspaceId == Guid.Empty)
        {
            return Result<ExecuteCopilotActionResultDto>.Failure(
                Error.Validation("Copilot.InvalidWorkspaceId", "O identificador do workspace é obrigatório."));
        }

        if (command.TargetEntityId == Guid.Empty)
        {
            return Result<ExecuteCopilotActionResultDto>.Failure(
                Error.Validation("Copilot.InvalidTargetEntityId", "O identificador da entidade alvo da ação é obrigatório."));
        }

        var executionTime = DateTime.UtcNow;

        var domainAction = new CopilotRecommendationAction(
            actionId: command.ActionId == Guid.Empty ? Guid.NewGuid() : command.ActionId,
            actionType: command.ActionType,
            targetEntityId: command.TargetEntityId,
            targetEntityName: command.TargetEntityName,
            platform: command.Platform,
            title: command.Title,
            description: command.Description,
            parameters: command.Parameters,
            isApplied: true,
            appliedAtUtc: executionTime
        );

        var executionResult = await _dataProvider.ExecuteRecommendationActionAsync(
            command.WorkspaceId,
            domainAction,
            cancellationToken);

        if (executionResult.IsFailure)
        {
            return Result<ExecuteCopilotActionResultDto>.Failure(executionResult.Error);
        }

        var responseDto = new ExecuteCopilotActionResultDto(
            ActionId: domainAction.ActionId,
            Success: true,
            Message: $"Ação '{domainAction.Title}' executada com sucesso em 1 clique.",
            ExecutedAtUtc: executionTime
        );

        return Result<ExecuteCopilotActionResultDto>.Success(responseDto);
    }
}
