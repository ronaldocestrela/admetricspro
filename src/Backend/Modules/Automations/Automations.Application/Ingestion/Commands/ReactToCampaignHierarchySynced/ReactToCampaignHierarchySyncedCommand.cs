using Automations.Application.SafetyGuards.Commands.CheckLandingPages;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Automations.Application.Ingestion.Commands.ReactToCampaignHierarchySynced;

/// <summary>
/// Comando disparado após sincronização durável da hierarquia para verificar landing pages.
/// </summary>
/// <param name="WorkspaceId">Workspace afetado pela sincronização.</param>
public sealed record ReactToCampaignHierarchySyncedCommand(Guid WorkspaceId) : ICommand;

/// <summary>
/// Executa o detector de landing pages após atualização estrutural de anúncios.
/// </summary>
public sealed class ReactToCampaignHierarchySyncedCommandHandler : ICommandHandler<ReactToCampaignHierarchySyncedCommand>
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ReactToCampaignHierarchySyncedCommandHandler"/>.
    /// </summary>
    /// <param name="sender">Mediador in-process.</param>
    public ReactToCampaignHierarchySyncedCommandHandler(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ReactToCampaignHierarchySyncedCommand command, CancellationToken cancellationToken)
    {
        if (command.WorkspaceId == Guid.Empty)
        {
            return Result.Failure(
                Error.Validation("IngestionReaction.EmptyWorkspaceId", "O workspace é obrigatório para reação pós-hierarquia."));
        }

        var check = await _sender.Send(new CheckLandingPagesCommand(command.WorkspaceId), cancellationToken);
        return check.IsFailure ? Result.Failure(check.Error) : Result.Success();
    }
}