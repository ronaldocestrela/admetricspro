using Analytics.Domain.Dashboard;
using Analytics.Infrastructure.Dashboard;
using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Application.Campaigns.Queries;
using Integrations.Domain.Campaigns;
using MediatR;
using NSubstitute;

namespace UnitTests.Backend.Analytics;

/// <summary>Regressões do consumo de métricas persistidas através do contrato inter-módulos.</summary>
public sealed class PersistedDashboardTests
{
    /// <summary>Workspace sem métricas não pode receber números fabricados.</summary>
    [Fact]
    public async Task Provider_WithoutMetrics_ReturnsEmpty()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetDashboardMetricPointsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<DashboardMetricPoint>>.Success([]));
        var provider = new ExecutiveDashboardDataProvider(sender);
        var result = await provider.GetMetricsAsync(Guid.NewGuid(), DateTime.UtcNow.Date, DateTime.UtcNow.Date, default, "BRL");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    /// <summary>Preserva números importados e mapeia nomes de plataforma sem inventar dispositivos.</summary>
    [Fact]
    public async Task Provider_UsesStoredNumbersAndPropagatesFailures()
    {
        var sender = Substitute.For<ISender>();
        var date = DateTime.UtcNow.Date;
        sender.Send(Arg.Any<GetDashboardMetricPointsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<DashboardMetricPoint>>.Success([new(date, "MetaAds", "BRL", 123.45m, 321m, 1000, 20, 2)]));
        var provider = new ExecutiveDashboardDataProvider(sender);
        var result = await provider.GetMetricsAsync(null, date, date, default, "BRL");
        result.Value.Should().Equal(new RawMetricPoint(date, "Meta", "Unknown", 123.45m, 321m, 1000, 20, 2));
        sender.Send(Arg.Any<GetDashboardMetricPointsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<DashboardMetricPoint>>.Failure(Error.Forbidden("Metrics.Forbidden", "Sem acesso.")));
        (await provider.GetMetricsAsync(null, date, date, default, "BRL")).Error.Code.Should().Be("Metrics.Forbidden");
    }

    /// <summary>Não rotula valores em dólares como reais nem soma moedas distintas.</summary>
    [Fact]
    public async Task Provider_RejectsCurrencyMismatch()
    {
        var sender = Substitute.For<ISender>();
        var date = DateTime.UtcNow.Date;
        sender.Send(Arg.Any<GetDashboardMetricPointsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<DashboardMetricPoint>>.Success([new(date, "MetaAds", "USD", 10m, 0m, 100, 2, 0)]));
        (await new ExecutiveDashboardDataProvider(sender).GetMetricsAsync(null, date, date, default, "BRL"))
            .Error.Code.Should().Be("Dashboard.CurrencyMismatch");
    }

    /// <summary>Falha de autorização não deve consultar repositórios nem gerar métricas.</summary>
    [Fact]
    public async Task MetricsQuery_WhenWorkspaceDenied_DoesNotReadMetrics()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetAnalyticsWorkspaceIdsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Guid>>.Failure(Error.Forbidden("Workspace.Forbidden", "Sem acesso.")));
        var repository = Substitute.For<ICampaignMetricsRepository>();
        var handler = new GetDashboardMetricPointsQueryHandler(sender, repository);
        var result = await handler.Handle(new(Guid.NewGuid(), DateTime.UtcNow.Date, DateTime.UtcNow.Date), default);
        result.Error.Code.Should().Be("Workspace.Forbidden");
        repository.ReceivedCalls().Should().BeEmpty();
    }
}
