using System.Text.Json;
using Analytics.Application.Jobs;
using Analytics.Application.Reports.Commands.DispatchReportSchedule;
using Analytics.Application.Reports.Commands.GenerateReport;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Messaging.Contracts;
using MassTransit;
using MediatR;

namespace WebApi.Messaging;

/// <summary>
/// Consumidor de jobs de relatório (geração e disparo).
/// </summary>
public sealed class AnalyticsReportJobConsumer(IServiceScopeFactory scopeFactory) : IConsumer<IntegrationEventTransportMessage>
{
    /// <summary>
    /// Nome do consumidor compartilhado para relatórios.
    /// </summary>
    public const string ConsumerName = "admetricspro.analytics.report-job";

    private readonly UserInitiatedJobConsumerSupport _support = new(scopeFactory, ConsumerName);

    /// <inheritdoc />
    public Task Consume(ConsumeContext<IntegrationEventTransportMessage> context)
    {
        var messageType = context.Message.MessageType;
        if (string.Equals(messageType, UserInitiatedJobIntegrationMessageTypes.ReportGenerate, StringComparison.Ordinal))
        {
            return _support.ConsumeAsync(
                context,
                UserInitiatedJobIntegrationMessageTypes.ReportGenerate,
                ExecuteGenerateAsync,
                context.CancellationToken);
        }

        if (string.Equals(messageType, UserInitiatedJobIntegrationMessageTypes.ReportDispatch, StringComparison.Ordinal))
        {
            return _support.ConsumeAsync(
                context,
                UserInitiatedJobIntegrationMessageTypes.ReportDispatch,
                ExecuteDispatchAsync,
                context.CancellationToken);
        }

        return Task.CompletedTask;
    }

    private static async Task<(BuildingBlocks.Domain.Primitives.Result Result, string? ResultJson)> ExecuteGenerateAsync(
        IServiceProvider services,
        BuildingBlocks.Domain.Messaging.UserInitiatedJob job,
        CancellationToken cancellationToken)
    {
        var parameters = JsonSerializer.Deserialize<ReportGenerateJobParameters>(job.ParametersJson)!;
        var sender = services.GetRequiredService<ISender>();
        var result = await sender.Send(
            new GenerateReportCommand(
                parameters.WorkspaceId,
                parameters.Title,
                parameters.StartDateUtc,
                parameters.EndDateUtc,
                parameters.CustomNotes,
                parameters.IncludeCopilotInsights,
                parameters.IncludeTopCreatives,
                parameters.IncludeChannelBreakdown,
                parameters.IncludePacingSummary,
                parameters.ExpirationDays),
            cancellationToken);

        if (result.IsFailure)
        {
            return (BuildingBlocks.Domain.Primitives.Result.Failure(result.Error), null);
        }

        return (BuildingBlocks.Domain.Primitives.Result.Success(), JsonSerializer.Serialize(new { reportId = result.Value }));
    }

    private static async Task<(BuildingBlocks.Domain.Primitives.Result Result, string? ResultJson)> ExecuteDispatchAsync(
        IServiceProvider services,
        BuildingBlocks.Domain.Messaging.UserInitiatedJob job,
        CancellationToken cancellationToken)
    {
        var scheduleId = JsonDocument.Parse(job.ParametersJson).RootElement.GetProperty("scheduleId").GetGuid();
        var sender = services.GetRequiredService<ISender>();
        var result = await sender.Send(new DispatchReportScheduleCommand(scheduleId), cancellationToken);

        if (result.IsFailure)
        {
            return (BuildingBlocks.Domain.Primitives.Result.Failure(result.Error), null);
        }

        return (BuildingBlocks.Domain.Primitives.Result.Success(), JsonSerializer.Serialize(new { scheduleId = result.Value }));
    }
}