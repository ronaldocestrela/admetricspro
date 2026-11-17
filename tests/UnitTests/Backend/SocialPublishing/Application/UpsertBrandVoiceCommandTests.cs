using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Brand.Commands.UpsertBrandVoice;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do comando <see cref="UpsertBrandVoiceCommandHandler"/>.
/// </summary>
public sealed class UpsertBrandVoiceCommandTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly FakeBrandVoiceRepository _repository = new();
    private readonly ISocialPublishingUnitOfWork _unitOfWork = Substitute.For<ISocialPublishingUnitOfWork>();

    /// <summary>
    /// Primeira gravação persiste todos os campos e chama Add e Commit.
    /// </summary>
    [Fact]
    public async Task Handle_PrimeiraGravacao_DevePersistirCamposEChamarAdd()
    {
        var handler = CreateHandler();
        var command = ValidCommand();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Tone.Should().Be("Tom novo");
        result.Value.Persona.Should().Be("Persona nova");
        result.Value.MandatoryLinks.Should().Contain("https://example.com/link");
        result.Value.ForbiddenTerms.Should().Contain("proibido");
        result.Value.EmojiPolicy.Should().Be(EmojiPolicy.Sparingly);
        result.Value.HashtagLimit.Should().Be(15);
        result.Value.AuthorityWeight.Should().Be(40);
        result.Value.SaleWeight.Should().Be(20);
        result.Value.ConnectionWeight.Should().Be(25);
        result.Value.SocialProofWeight.Should().Be(15);
        result.Value.LanguageCode.Should().Be("pt-BR");

        _repository.AddCalls.Should().Be(1);
        _repository.UpdateCalls.Should().Be(0);
        _repository.GetCalls.Should().Be(1);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Segunda gravação no mesmo workspace atualiza a mesma linha.
    /// </summary>
    [Fact]
    public async Task Handle_SegundaGravacaoNoMesmoWorkspace_DeveAtualizarMesmoId()
    {
        var existing = BrandVoiceProfile.Create(
            _workspaceId,
            "Tom antigo",
            "Persona antiga",
            ["https://example.com/old"],
            ["velho"],
            EmojiPolicy.Allowed,
            10,
            25,
            25,
            25,
            25).Value;

        _repository.Seed(existing);

        var handler = CreateHandler();
        var command = ValidCommand() with
        {
            Tone = "Tom revisado",
            Persona = "Persona revisada",
            MandatoryLinks = ["https://example.com/new"],
            ForbiddenTerms = ["novo"],
            EmojiPolicy = EmojiPolicy.Forbidden,
            HashtagLimit = 20,
            AuthorityWeight = 50,
            SaleWeight = 10,
            ConnectionWeight = 20,
            SocialProofWeight = 20,
            LanguageCode = "en-US"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(existing.Id);
        result.Value.Tone.Should().Be("Tom revisado");
        result.Value.Persona.Should().Be("Persona revisada");

        _repository.AddCalls.Should().Be(0);
        _repository.UpdateCalls.Should().Be(1);
        _repository.GetCalls.Should().Be(1);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Pesos inválidos não chamam o repositório.
    /// </summary>
    [Fact]
    public async Task Handle_ComPesosInvalidos_NaoDeveChamarRepositorio()
    {
        var handler = CreateHandler();
        var command = ValidCommand() with { AuthorityWeight = 30, SaleWeight = 30, ConnectionWeight = 30, SocialProofWeight = 30 };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PillarWeightsInvalid.Code);
        _repository.AddCalls.Should().Be(0);
        _repository.UpdateCalls.Should().Be(0);
        _repository.GetCalls.Should().Be(0);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Peso negativo não chama o repositório.
    /// </summary>
    [Fact]
    public async Task Handle_ComPesoNegativo_NaoDeveChamarRepositorio()
    {
        var handler = CreateHandler();
        var command = ValidCommand() with { SaleWeight = -5 };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PillarWeightsInvalid.Code);
        _repository.GetCalls.Should().Be(0);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private UpsertBrandVoiceCommand ValidCommand() =>
        new(
            _workspaceId,
            "Tom novo",
            "Persona nova",
            ["https://example.com/link"],
            ["proibido"],
            EmojiPolicy.Sparingly,
            15,
            40,
            20,
            25,
            15,
            "pt-BR");

    private UpsertBrandVoiceCommandHandler CreateHandler()
    {
        var tenantAccessor = new TenantContextAccessor();
        tenantAccessor.TenantContext = TenantContext.Create(
            Guid.NewGuid(),
            "slug",
            TenantResolutionSource.Header);

        return new UpsertBrandVoiceCommandHandler(_repository, _unitOfWork, tenantAccessor);
    }

    private sealed class FakeBrandVoiceRepository : IBrandVoiceRepository
    {
        private BrandVoiceProfile? _profile;

        public int GetCalls { get; private set; }

        public int AddCalls { get; private set; }

        public int UpdateCalls { get; private set; }

        public void Seed(BrandVoiceProfile profile) => _profile = profile;

        public Task<BrandVoiceProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_profile?.Id == id ? _profile : null);

        public Task<BrandVoiceProfile?> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            GetCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_profile?.WorkspaceId == workspaceId ? _profile : null);
        }

        public Task<IReadOnlyList<BrandVoiceProfile>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BrandVoiceProfile>>(
                _profile?.WorkspaceId == workspaceId ? [_profile] : Array.Empty<BrandVoiceProfile>());

        public Task AddAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default)
        {
            AddCalls++;
            _profile = profile;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            _profile = profile;
            return Task.CompletedTask;
        }
    }
}
