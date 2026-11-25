using Analytics.Application.Currencies.Queries.ConvertCurrencyBatch;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.Queries.GetCampaignMetrics;
using MediatR;

namespace Analytics.Application.Ingestion.Commands.ProcessCampaignMetricsSynced;

/// <summary>
/// Comando de reação analítica após ingestão durável de métricas (normalização cambial em memória).
/// </summary>
/// <param name="WorkspaceId">Workspace sincronizado.</param>
/// <param name="ConnectedAdAccountId">Conta conectada.</param>
/// <param name="Granularity">Granularidade temporal.</param>
/// <param name="WindowStartUtc">Início da janela analítica.</param>
/// <param name="WindowEndUtc">Fim da janela analítica.</param>
public sealed record ProcessCampaignMetricsSyncedCommand(
    Guid WorkspaceId,
    Guid ConnectedAdAccountId,
    MetricGranularity Granularity,
    DateTime WindowStartUtc,
    DateTime WindowEndUtc) : ICommand;

/// <summary>
/// Relê métricas persistidas e aplica conversão cambial em lote para BRL (sem persistência).
/// </summary>
public sealed class ProcessCampaignMetricsSyncedCommandHandler : ICommandHandler<ProcessCampaignMetricsSyncedCommand>
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ProcessCampaignMetricsSyncedCommandHandler"/>.
    /// </summary>
    /// <param name="sender">Mediador para consultas cross-módulo via contratos.</param>
    public ProcessCampaignMetricsSyncedCommandHandler(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ProcessCampaignMetricsSyncedCommand command, CancellationToken cancellationToken)
    {
        var metrics = await _sender.Send(
            new GetCampaignMetricsQuery(
                command.WorkspaceId,
                command.WindowStartUtc,
                command.WindowEndUtc,
                command.Granularity,
                ConnectedAdAccountId: command.ConnectedAdAccountId),
            cancellationToken);

        if (metrics.IsFailure)
        {
            return Result.Failure(metrics.Error);
        }

        if (metrics.Value.Count == 0)
        {
            return Result.Success();
        }

        var items = metrics.Value
            .Select(m => new CurrencyBatchItemInput(m.Spend, m.Currency, m.Date))
            .ToList();

        var convert = await _sender.Send(
            new ConvertCurrencyBatchQuery(items, "BRL"),
            cancellationToken);

        return convert.IsFailure ? Result.Failure(convert.Error) : Result.Success();
    }
}