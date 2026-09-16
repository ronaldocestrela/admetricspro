using Analytics.Application.Dashboard.DTOs;
using Analytics.Domain.Dashboard;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Application.Dashboard.Queries.GetExecutiveDashboard;

/// <summary>
/// Manipulador da consulta <see cref="GetExecutiveDashboardQuery"/>.
/// Determina as janelas temporais corrente e anterior, obtém os dados brutos e invoca o motor analítico de consolidação.
/// </summary>
public sealed class GetExecutiveDashboardQueryHandler : IQueryHandler<GetExecutiveDashboardQuery, ExecutiveDashboardDto>
{
    private readonly IExecutiveDashboardCalculator _calculator;
    private readonly IExecutiveDashboardDataProvider _dataProvider;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetExecutiveDashboardQueryHandler"/>.
    /// </summary>
    /// <param name="calculator">Calculador de métricas analíticas e comparativo temporal.</param>
    /// <param name="dataProvider">Provedor de métricas normalizadas brutas.</param>
    public GetExecutiveDashboardQueryHandler(
        IExecutiveDashboardCalculator calculator,
        IExecutiveDashboardDataProvider dataProvider)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    /// <inheritdoc />
    public async Task<Result<ExecutiveDashboardDto>> Handle(
        GetExecutiveDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var currentEnd = request.EndDateUtc ?? DateTime.UtcNow.Date;
        var currentStart = request.StartDateUtc ?? currentEnd.AddDays(-6);

        if (currentStart > currentEnd)
        {
            return Result<ExecutiveDashboardDto>.Failure(
                Error.Validation("GetExecutiveDashboard.InvalidDateRange", "A data inicial não pode ser superior à data final."));
        }

        // Calcula período anterior com duração idêntica
        var daysSpan = (int)(currentEnd.Date - currentStart.Date).TotalDays;
        var previousEnd = currentStart.AddDays(-1);
        var previousStart = previousEnd.AddDays(-daysSpan);

        var currentPoints = await _dataProvider.GetMetricsAsync(
            request.WorkspaceId,
            currentStart,
            currentEnd,
            cancellationToken);

        var previousPoints = await _dataProvider.GetMetricsAsync(
            request.WorkspaceId,
            previousStart,
            previousEnd,
            cancellationToken);

        var currency = !string.IsNullOrWhiteSpace(request.Currency) ? request.Currency : "BRL";

        var calcResult = _calculator.Calculate(
            currentPoints,
            previousPoints,
            currentStart,
            currentEnd,
            previousStart,
            previousEnd,
            currency,
            request.Platform,
            request.Device);

        if (calcResult.IsFailure)
        {
            return Result<ExecutiveDashboardDto>.Failure(calcResult.Error);
        }

        var domain = calcResult.Value;

        var dto = new ExecutiveDashboardDto(
            domain.StartDateUtc,
            domain.EndDateUtc,
            domain.PreviousStartDateUtc,
            domain.PreviousEndDateUtc,
            domain.Currency,
            domain.Metrics.Select(m => new ExecutiveMetricItemDto(
                m.MetricKey,
                m.Label,
                m.CurrentValue,
                m.PreviousValue,
                m.PercentageChange,
                m.IsPositiveImprovement,
                m.UnitFormat)).ToList(),
            domain.TimeSeries.Select(t => new ExecutiveTimeSeriesPointDto(
                t.Date,
                t.Spend,
                t.Revenue,
                t.Roas,
                t.Clicks,
                t.Impressions,
                t.Conversions)).ToList(),
            domain.PlatformBreakdown.Select(p => new PlatformShareDto(
                p.Platform,
                p.Spend,
                p.Revenue,
                p.Roas,
                p.ShareOfSpendPercentage,
                p.Clicks,
                p.Conversions)).ToList(),
            domain.DeviceBreakdown.Select(d => new DeviceShareDto(
                d.Device,
                d.Spend,
                d.Clicks,
                d.Conversions,
                d.Roas,
                d.ShareOfSpendPercentage)).ToList());

        return Result<ExecutiveDashboardDto>.Success(dto);
    }
}
