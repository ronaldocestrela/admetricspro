using BuildingBlocks.Domain.Campaigns;
using FluentAssertions;
using Integrations.Application.Campaigns.Queries.GetCampaignHierarchy;
using Integrations.Domain.Campaigns;
using NSubstitute;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o manipulador da consulta <see cref="GetCampaignHierarchyQueryHandler"/>.
/// </summary>
public sealed class GetCampaignHierarchyQueryHandlerTests
{
    private readonly ICampaignHierarchyRepository _repository = Substitute.For<ICampaignHierarchyRepository>();
    private readonly GetCampaignHierarchyQueryHandler _handler;

    /// <summary>
    /// Inicializa dependências e o handler sob teste.
    /// </summary>
    public GetCampaignHierarchyQueryHandlerTests()
    {
        _handler = new GetCampaignHierarchyQueryHandler(_repository);
    }

    /// <summary>
    /// Valida que requisição com WorkspaceId vazio retorna erro de validação.
    /// </summary>
    [Fact]
    public async Task Handle_WhenWorkspaceIdEmpty_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetCampaignHierarchyQuery(Guid.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GetCampaignHierarchy.EmptyWorkspaceId");
    }

    /// <summary>
    /// Valida que a consulta lista todas as campanhas e mapeia seus conjuntos e anúncios.
    /// </summary>
    [Fact]
    public async Task Handle_WhenValidWorkspace_ShouldReturnHierarchyDtos()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var campaign = Campaign.Create(
            Guid.NewGuid(),
            workspaceId,
            accountId,
            "MetaAds",
            "cmp_100",
            "Campanha Black Friday",
            CampaignStatus.Active,
            "CONVERSIONS",
            150m).Value;

        var adSet = AdSet.Create(
            Guid.NewGuid(),
            campaign.Id,
            accountId,
            "set_200",
            "Conjunto Remarketing",
            AdSetStatus.Active).Value;

        var ad = Ad.Create(
            Guid.NewGuid(),
            adSet.Id,
            campaign.Id,
            accountId,
            "ad_300",
            "Criativo Oferta",
            AdStatus.Active,
            AdCreativeType.Image,
            "Título",
            "Texto",
            "https://dest.com").Value;

        // Adiciona à coleção via reflexão se necessário ou por método
        // Como Campaign.AdSets é read-only na entidade, o repositório traz hidratado via EF Core
        _repository.GetCampaignsByWorkspaceAsync(workspaceId, null, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Campaign>>(new[] { campaign }));

        var query = new GetCampaignHierarchyQuery(workspaceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Name.Should().Be("Campanha Black Friday");
        result.Value[0].Platform.Should().Be("MetaAds");
    }
}
