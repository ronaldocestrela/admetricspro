using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Planning.Commands.GenerateSlotCopy;
using SocialPublishing.Application.Planning.Copy;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Planning;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do comando <see cref="GenerateSlotCopyCommandHandler"/> (Subfase 3.3).
/// </summary>
public sealed class GenerateSlotCopyCommandTests
{
    private const string OAuthSentinel = "EAAB-test-oauth-token";

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly FakeBrandVoiceRepository _repository = new();
    private readonly RecordingCreativeGenerator _generator = new();

    /// <summary>
    /// O brief montado contém persona, tom, termos proibidos, pilar e rede, sem token OAuth.
    /// </summary>
    [Fact]
    public async Task Handle_DeveMontarBriefComDnaDeMarca_SemTokenOAuth()
    {
        var profile = CreateProfile(
            forbiddenTerms: ["proibido", "spam"],
            mandatoryLinks: ["https://example.com/link"]);
        _repository.Seed(profile);

        _generator.SetSuccess(
            "Legenda limpa com https://example.com/link",
            ["marca"],
            ["nicho"],
            ["alcance"]);

        var handler = CreateHandler();
        var command = DefaultCommand();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _generator.LastBrief.Should().NotBeNull();
        var prompt = _generator.LastBrief!.ToPromptText();
        prompt.Should().Contain("Persona teste");
        prompt.Should().Contain("Tom teste");
        prompt.Should().Contain("proibido");
        prompt.Should().Contain("spam");
        prompt.Should().Contain(nameof(ContentPillar.Authority));
        prompt.Should().Contain(nameof(OrganicPlatform.Instagram));
        prompt.Should().NotContain(OAuthSentinel);
    }

    /// <summary>
    /// Brand Voice ausente não chama o gerador.
    /// </summary>
    [Fact]
    public async Task Handle_SemBrandVoice_NaoChamaGerador()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(DefaultCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.BrandVoiceNotFound.Code);
        _generator.CallCount.Should().Be(0);
    }

    /// <summary>
    /// Workspace vazio retorna erro de validação.
    /// </summary>
    [Fact]
    public async Task Handle_ComWorkspaceVazio_RetornaWorkspaceRequired()
    {
        var handler = CreateHandler();
        var command = DefaultCommand() with { WorkspaceId = Guid.Empty };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.WorkspaceRequired.Code);
        _generator.CallCount.Should().Be(0);
    }

    /// <summary>
    /// Legenda com termo proibido é recusada.
    /// </summary>
    [Fact]
    public async Task Handle_ComTermoProibidoNaLegenda_RetornaCopyContainsForbiddenTerm()
    {
        _repository.Seed(CreateProfile(forbiddenTerms: ["proibido"]));
        _generator.SetSuccess("Texto com PROIBIDO no meio", [], [], []);

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.CopyContainsForbiddenTerm.Code);
    }

    /// <summary>
    /// Legenda acima do teto da rede é recusada.
    /// </summary>
    [Fact]
    public async Task Handle_ComLegendaAcimaDoTeto_RetornaCaptionTooLong()
    {
        _repository.Seed(CreateProfile());
        var over = new string('a', CaptionLimits.Instagram + 1);
        _generator.SetSuccess(over, ["tag"], [], []);

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.CaptionTooLong.Code);
    }

    /// <summary>
    /// Hashtags válidas em três grupos são aceitas.
    /// </summary>
    [Fact]
    public async Task Handle_ComHashtagsValidas_RetornaTresGrupos()
    {
        _repository.Seed(CreateProfile(hashtagLimit: 10));
        _generator.SetSuccess(
            "Legenda ok",
            ["marca1"],
            ["nicho1"],
            ["alcance1"]);

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HashtagGroups.Brand.Should().Contain("marca1");
        result.Value.HashtagGroups.Niche.Should().Contain("nicho1");
        result.Value.HashtagGroups.Reach.Should().Contain("alcance1");
    }

    /// <summary>
    /// Duplicata entre grupos viola política de hashtags.
    /// </summary>
    [Fact]
    public async Task Handle_ComHashtagDuplicadaEntreGrupos_RetornaHashtagPolicyViolated()
    {
        _repository.Seed(CreateProfile(hashtagLimit: 10));
        _generator.SetSuccess("Legenda ok", ["dup"], ["dup"], []);

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.HashtagPolicyViolated.Code);
    }

    /// <summary>
    /// Falha do provedor propaga CopyGenerationFailed.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoGeradorFalha_RetornaCopyGenerationFailed()
    {
        _repository.Seed(CreateProfile());
        _generator.SetFailure(SocialErrors.CopyGenerationFailed);

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.CopyGenerationFailed.Code);
    }

    /// <summary>
    /// Carrossel Instagram inclui instrução de sequência de cards.
    /// </summary>
    [Fact]
    public async Task Handle_CarrosselInstagram_InstrucaoSequenciaDeCards()
    {
        _repository.Seed(CreateProfile());
        _generator.SetSuccess("Legenda", ["a"], ["b"], ["c"]);

        var command = DefaultCommand() with
        {
            Platform = OrganicPlatform.Instagram,
            Format = OrganicPostFormat.FeedCarousel
        };

        await CreateHandler().Handle(command, CancellationToken.None);

        _generator.LastBrief!.FormatInstruction.Should().ContainEquivalentOf("sequência de cards");
    }

    /// <summary>
    /// Reel inclui instrução de gancho falado.
    /// </summary>
    [Fact]
    public async Task Handle_Reel_InstrucaoGanchoFalado()
    {
        _repository.Seed(CreateProfile());
        _generator.SetSuccess("Legenda", ["a"], ["b"], ["c"]);

        var command = DefaultCommand() with { Format = OrganicPostFormat.Reel };

        await CreateHandler().Handle(command, CancellationToken.None);

        _generator.LastBrief!.FormatInstruction.Should().ContainEquivalentOf("gancho falado");
    }

    /// <summary>
    /// TikTok inclui instrução de gancho falado.
    /// </summary>
    [Fact]
    public async Task Handle_TikTok_InstrucaoGanchoFalado()
    {
        _repository.Seed(CreateProfile());
        _generator.SetSuccess("Legenda", ["a"], ["b"], ["c"]);

        var command = DefaultCommand() with
        {
            Platform = OrganicPlatform.TikTok,
            Format = OrganicPostFormat.TikTokVideo
        };

        await CreateHandler().Handle(command, CancellationToken.None);

        _generator.LastBrief!.FormatInstruction.Should().ContainEquivalentOf("gancho falado");
    }

    /// <summary>
    /// Feed Instagram e Facebook Page recebem instruções distintas.
    /// </summary>
    [Fact]
    public async Task Handle_FeedImage_InstagramEFacebookPage_InstrucoesDistintas()
    {
        _repository.Seed(CreateProfile());
        _generator.SetSuccess("Legenda", ["a"], ["b"], ["c"]);

        var handler = CreateHandler();

        await handler.Handle(DefaultCommand() with { Platform = OrganicPlatform.Instagram, Format = OrganicPostFormat.FeedImage }, CancellationToken.None);
        var instagramInstruction = _generator.LastBrief!.FormatInstruction;

        await handler.Handle(DefaultCommand() with { Platform = OrganicPlatform.FacebookPage, Format = OrganicPostFormat.FeedImage }, CancellationToken.None);
        var facebookInstruction = _generator.LastBrief!.FormatInstruction;

        instagramInstruction.Should().NotBe(facebookInstruction);
    }

    /// <summary>
    /// Link obrigatório ausente gera warning sem reprovar a cópia.
    /// </summary>
    [Fact]
    public async Task Handle_ComLinkObrigatorioAusente_RetornaWarning()
    {
        _repository.Seed(CreateProfile(mandatoryLinks: ["https://example.com/obrigatorio"]));
        _generator.SetSuccess("Legenda sem link", ["a"], ["b"], ["c"]);

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Warnings.Should().Contain(w => w.Contains("https://example.com/obrigatorio", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Link obrigatório presente não gera warning.
    /// </summary>
    [Fact]
    public async Task Handle_ComLinkObrigatorioPresente_SemWarnings()
    {
        _repository.Seed(CreateProfile(mandatoryLinks: ["https://example.com/obrigatorio"]));
        _generator.SetSuccess("Visite https://example.com/obrigatorio hoje", ["a"], ["b"], ["c"]);

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Warnings.Should().BeEmpty();
    }

    private GenerateSlotCopyCommand DefaultCommand() =>
        new(
            _workspaceId,
            new DateOnly(2026, 11, 2),
            new DateTime(2026, 11, 2, 13, 0, 0, DateTimeKind.Utc),
            ContentPillar.Authority,
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage);

    private GenerateSlotCopyCommandHandler CreateHandler()
    {
        var tenantAccessor = new TenantContextAccessor();
        tenantAccessor.TenantContext = TenantContext.Create(
            Guid.NewGuid(),
            "slug",
            TenantResolutionSource.Header);

        return new GenerateSlotCopyCommandHandler(_repository, _generator, tenantAccessor);
    }

    private BrandVoiceProfile CreateProfile(
        IReadOnlyList<string>? forbiddenTerms = null,
        IReadOnlyList<string>? mandatoryLinks = null,
        int hashtagLimit = 15)
    {
        return BrandVoiceProfile.Create(
            _workspaceId,
            "Tom teste",
            "Persona teste",
            mandatoryLinks ?? ["https://example.com/link"],
            forbiddenTerms ?? [],
            EmojiPolicy.Sparingly,
            hashtagLimit,
            40,
            20,
            25,
            15).Value;
    }

    private sealed class FakeBrandVoiceRepository : IBrandVoiceRepository
    {
        private BrandVoiceProfile? _profile;

        public void Seed(BrandVoiceProfile profile) => _profile = profile;

        public Task<BrandVoiceProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_profile?.Id == id ? _profile : null);

        public Task<BrandVoiceProfile?> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_profile?.WorkspaceId == workspaceId ? _profile : null);
        }

        public Task<IReadOnlyList<BrandVoiceProfile>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BrandVoiceProfile>>(
                _profile?.WorkspaceId == workspaceId ? [_profile] : Array.Empty<BrandVoiceProfile>());

        public Task AddAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingCreativeGenerator : ISocialCreativeGenerator
    {
        private Result<GeneratedCopyDraft> _next = Result<GeneratedCopyDraft>.Failure(SocialErrors.CopyGenerationFailed);

        public CopyBrief? LastBrief { get; private set; }

        public int CallCount { get; private set; }

        public void SetSuccess(string caption, IReadOnlyList<string> brand, IReadOnlyList<string> niche, IReadOnlyList<string> reach)
        {
            _next = Result<GeneratedCopyDraft>.Success(new GeneratedCopyDraft(caption, brand, niche, reach));
        }

        public void SetFailure(Error error)
        {
            _next = Result<GeneratedCopyDraft>.Failure(error);
        }

        public Task<Result<GeneratedCopyDraft>> GenerateAsync(CopyBrief brief, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastBrief = brief;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_next);
        }
    }
}
