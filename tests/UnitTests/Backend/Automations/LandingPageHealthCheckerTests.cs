using Automations.Domain.SafetyGuards;
using Automations.Infrastructure.SafetyGuards;
using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Automations.SafetyGuards;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Automations;

/// <summary>
/// Testes unitários para o detector de links quebrados LandingPageHealthChecker (Subfase 4.2.3 - TDD).
/// Valida a trava que pausa anúncios cujas páginas de destino retornem erros HTTP 4xx, 5xx ou inacessibilidade.
/// </summary>
public sealed class LandingPageHealthCheckerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ISecurityAlertNotifier _alertNotifier = Substitute.For<ISecurityAlertNotifier>();
    private readonly IHttpLandingPageVerifier _httpVerifier = Substitute.For<IHttpLandingPageVerifier>();
    private readonly ITenantDbContextAccessor _contextAccessor = Substitute.For<ITenantDbContextAccessor>();
    private readonly TenantDbContext _dbContext;
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _connectedAccountId = Guid.NewGuid();
    private readonly Guid _campaignId = Guid.NewGuid();
    private readonly Guid _adSetId = Guid.NewGuid();

    /// <summary>
    /// Inicializa a base SQLite em memória e mocks dos testes.
    /// </summary>
    public LandingPageHealthCheckerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TenantDbContext(options);
        _dbContext.Database.EnsureCreated();

        // Inserir Campaign e AdSet pai para integridade referencial de chave estrangeira
        var campaign = Campaign.Create(
            _campaignId,
            _workspaceId,
            _connectedAccountId,
            "GoogleAds",
            "ext_camp_1",
            "Campanha Pai",
            CampaignStatus.Active,
            "Conversions",
            100m,
            null,
            "BRL",
            DateTime.UtcNow.AddDays(-10),
            null).Value;

        var adSet = AdSet.Create(
            _adSetId,
            _campaignId,
            _connectedAccountId,
            "ext_set_1",
            "Conjunto Pai",
            AdSetStatus.Active,
            "LOWEST_COST",
            "CONVERSIONS",
            100m,
            null,
            null,
            DateTime.UtcNow.AddDays(-10),
            null).Value;

        _dbContext.Campaigns.Add(campaign);
        _dbContext.AdSets.Add(adSet);
        _dbContext.SaveChanges();

        _contextAccessor.GetDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<TenantDbContext>.Success(_dbContext)));

        _sender.Send(Arg.Any<PauseAdCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _alertNotifier.DispatchAlertAsync(Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyDictionary<SafetyAlertChannel, bool>>.Success(
                new Dictionary<SafetyAlertChannel, bool>
                {
                    { SafetyAlertChannel.Slack, true },
                    { SafetyAlertChannel.Email, true }
                })));
    }

    /// <summary>
    /// Libera conexões e recursos.
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private Ad CreateAd(Guid adId, string name, AdStatus status, string? destinationUrl, string platform = "GoogleAds")
    {
        var adResult = Ad.Create(
            adId,
            _adSetId,
            _campaignId,
            _connectedAccountId,
            $"ext_ad_{adId}",
            name,
            status,
            AdCreativeType.ResponsiveSearch,
            "Título do Anúncio",
            "Texto do Anúncio",
            destinationUrl,
            null,
            "Compre Agora",
            DateTime.UtcNow);

        return adResult.Value;
    }

    /// <summary>
    /// Anúncio com URL saudável (200 OK) não deve ser pausado e não gera incidentes.
    /// </summary>
    [Fact]
    public async Task CheckAndMitigateLandingPagesAsync_ShouldNotPause_WhenLandingPageIsHealthy()
    {
        // Arrange
        var adId = Guid.NewGuid();
        var url = "https://minhaloja.com.br/produto-top";
        var ad = CreateAd(adId, "Anúncio Saudável", AdStatus.Active, url);
        _dbContext.Ads.Add(ad);
        await _dbContext.SaveChangesAsync();

        _httpVerifier.ProbeUrlAsync(url, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LandingPageProbeResult(IsHealthy: true, StatusCode: 200, ErrorMessage: "OK")));

        var checker = new LandingPageHealthChecker(_contextAccessor, _sender, _alertNotifier, _httpVerifier);

        // Act
        var result = await checker.CheckAndMitigateLandingPagesAsync(_workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EvaluatedAdsCount.Should().Be(1);
        result.Value.HealthyAdsCount.Should().Be(1);
        result.Value.BrokenAdsCount.Should().Be(0);
        result.Value.PausedAdsCount.Should().Be(0);
        result.Value.Incidents.Should().BeEmpty();

        await _sender.DidNotReceive().Send(Arg.Any<PauseAdCommand>(), Arg.Any<CancellationToken>());
        await _alertNotifier.DidNotReceive().DispatchAlertAsync(Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Anúncio com URL retornando 404 Not Found deve ser pausado imediatamente com alarme de emergência.
    /// </summary>
    [Fact]
    public async Task CheckAndMitigateLandingPagesAsync_ShouldPauseAndAlert_WhenLandingPageReturns404()
    {
        // Arrange
        var adId = Guid.NewGuid();
        var url = "https://minhaloja.com.br/pagina-deletada";
        var ad = CreateAd(adId, "Anúncio Quebrado 404", AdStatus.Active, url);
        _dbContext.Ads.Add(ad);
        await _dbContext.SaveChangesAsync();

        _httpVerifier.ProbeUrlAsync(url, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LandingPageProbeResult(IsHealthy: false, StatusCode: 404, ErrorMessage: "Not Found")));

        var checker = new LandingPageHealthChecker(_contextAccessor, _sender, _alertNotifier, _httpVerifier);

        // Act
        var result = await checker.CheckAndMitigateLandingPagesAsync(_workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EvaluatedAdsCount.Should().Be(1);
        result.Value.HealthyAdsCount.Should().Be(0);
        result.Value.BrokenAdsCount.Should().Be(1);
        result.Value.PausedAdsCount.Should().Be(1);
        result.Value.Incidents.Should().HaveCount(1);

        var incident = result.Value.Incidents[0];
        incident.GuardType.Should().Be(SafetyGuardType.BrokenLandingPage);
        incident.TargetEntityId.Should().Be(adId);
        incident.HttpStatusCode.Should().Be(404);
        incident.TargetUrl.Should().Be(url);

        // Verifica despacho do comando PauseAdCommand
        await _sender.Received(1).Send(
            Arg.Is<PauseAdCommand>(cmd =>
                cmd.WorkspaceId == _workspaceId &&
                cmd.AdId == adId &&
                cmd.Reason!.Contains("404")),
            Arg.Any<CancellationToken>());

        // Verifica envio do alarme multi-canal
        await _alertNotifier.Received(1).DispatchAlertAsync(
            Arg.Is<SafetyAlertPayload>(p =>
                p.WorkspaceId == _workspaceId &&
                p.GuardType == SafetyGuardType.BrokenLandingPage &&
                p.TargetEntityId == adId &&
                p.Severity == SafetyAlertSeverity.Emergency),
            Arg.Any<CancellationToken>());

        // Verifica persistência do incidente
        _dbContext.SafetyGuardIncidents.Should().ContainSingle(i => i.TargetEntityId == adId);
    }

    /// <summary>
    /// Anúncio com URL retornando 500 Internal Server Error deve ser pausado imediatamente.
    /// </summary>
    [Fact]
    public async Task CheckAndMitigateLandingPagesAsync_ShouldPauseAndAlert_WhenLandingPageReturns500()
    {
        // Arrange
        var adId = Guid.NewGuid();
        var url = "https://minhaloja.com.br/erro-servidor";
        var ad = CreateAd(adId, "Anúncio Quebrado 500", AdStatus.Active, url);
        _dbContext.Ads.Add(ad);
        await _dbContext.SaveChangesAsync();

        _httpVerifier.ProbeUrlAsync(url, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LandingPageProbeResult(IsHealthy: false, StatusCode: 500, ErrorMessage: "Internal Server Error")));

        var checker = new LandingPageHealthChecker(_contextAccessor, _sender, _alertNotifier, _httpVerifier);

        // Act
        var result = await checker.CheckAndMitigateLandingPagesAsync(_workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BrokenAdsCount.Should().Be(1);
        result.Value.PausedAdsCount.Should().Be(1);

        await _sender.Received(1).Send(
            Arg.Is<PauseAdCommand>(cmd => cmd.AdId == adId && cmd.Reason!.Contains("500")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Anúncio com falha de conexão / timeout (sem status code) deve ser pausado com mensagem de inacessibilidade.
    /// </summary>
    [Fact]
    public async Task CheckAndMitigateLandingPagesAsync_ShouldPauseAndAlert_WhenLandingPageTimesOut()
    {
        // Arrange
        var adId = Guid.NewGuid();
        var url = "https://servidor-desligado.com.br";
        var ad = CreateAd(adId, "Anúncio Timeout", AdStatus.Active, url);
        _dbContext.Ads.Add(ad);
        await _dbContext.SaveChangesAsync();

        _httpVerifier.ProbeUrlAsync(url, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LandingPageProbeResult(IsHealthy: false, StatusCode: null, ErrorMessage: "Connection timed out após 5s")));

        var checker = new LandingPageHealthChecker(_contextAccessor, _sender, _alertNotifier, _httpVerifier);

        // Act
        var result = await checker.CheckAndMitigateLandingPagesAsync(_workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BrokenAdsCount.Should().Be(1);
        result.Value.PausedAdsCount.Should().Be(1);

        await _sender.Received(1).Send(
            Arg.Is<PauseAdCommand>(cmd => cmd.AdId == adId && cmd.Reason!.Contains("Connection timed out")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Anúncios sem DestinationUrl configurada ou já pausados devem ser ignorados.
    /// </summary>
    [Fact]
    public async Task CheckAndMitigateLandingPagesAsync_ShouldIgnoreAdsWithoutUrlOrAlreadyPaused()
    {
        // Arrange
        var ad1 = CreateAd(Guid.NewGuid(), "Sem URL", AdStatus.Active, null);
        var ad2 = CreateAd(Guid.NewGuid(), "URL Vazia", AdStatus.Active, "   ");
        var ad3 = CreateAd(Guid.NewGuid(), "Já Pausado", AdStatus.Paused, "https://minhaloja.com.br");

        _dbContext.Ads.AddRange(ad1, ad2, ad3);
        await _dbContext.SaveChangesAsync();

        var checker = new LandingPageHealthChecker(_contextAccessor, _sender, _alertNotifier, _httpVerifier);

        // Act
        var result = await checker.CheckAndMitigateLandingPagesAsync(_workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EvaluatedAdsCount.Should().Be(0);
        await _httpVerifier.DidNotReceive().ProbeUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _sender.DidNotReceive().Send(Arg.Any<PauseAdCommand>(), Arg.Any<CancellationToken>());
    }
}
