using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using MediatR;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Planning.Commands.GenerateEditorialPlan;
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
/// Testes do comando <see cref="GenerateEditorialPlanCommandHandler"/> (Subfase 3.4).
/// </summary>
public sealed class GenerateEditorialPlanCommandTests
{
    private const string SaoPaulo = "America/Sao_Paulo";

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly FakeBrandVoiceRepository _brandVoiceRepository = new();
    private readonly FakeConnectionRepository _connectionRepository = new();
    private readonly InMemoryPostRepository _postRepository = new();
    private readonly InMemoryPlanGenerationRepository _planRepository = new();
    private readonly ISocialPublishingUnitOfWork _unitOfWork = Substitute.For<ISocialPublishingUnitOfWork>();
    private readonly SequentialCreativeGenerator _generator = new();

    /// <summary>
    /// Plano válido cria posts Draft com metadados do slot e legenda composta.
    /// </summary>
    [Fact]
    public async Task Handle_ComPlanoValido_DeveCriarPostsDraftComCopia()
    {
        SeedBrandVoice();
        SeedAllPlatformConnections();
        _generator.SetDefaultSuccess("Legenda base", ["marca"], ["nicho"], ["alcance"]);

        var command = DefaultCommand();
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedPostIds.Should().HaveCount(5);
        result.Value.FailedSlots.Should().BeEmpty();

        var posts = _postRepository.AllPosts;
        posts.Should().HaveCount(5);
        posts.Should().OnlyContain(p => p.Status == OrganicPostStatus.Draft);
        posts.Should().OnlyContain(p => p.WorkspaceId == _workspaceId);
        posts[0].Caption.Should().Contain("Legenda base");
        posts[0].Caption.Should().Contain("#marca");
        posts[0].ScheduledAtUtc.Should().Be(new DateTime(2026, 11, 2, 13, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Falha de cópia em um slot preserva posts anteriores e registra failedSlots.
    /// </summary>
    [Fact]
    public async Task Handle_ComFalhaDeCopiaNoMeio_DevePreservarPostsAnteriores()
    {
        SeedBrandVoice();
        SeedAllPlatformConnections();
        _generator.SetDefaultSuccess("Ok", ["a"], [], []);
        _generator.FailOnCall(3, SocialErrors.CopyGenerationFailed);

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedPostIds.Should().HaveCount(4);
        result.Value.FailedSlots.Should().ContainSingle();
        result.Value.FailedSlots[0].ErrorCode.Should().Be(SocialErrors.CopyGenerationFailed.Code);
        _postRepository.AllPosts.Should().HaveCount(4);
        await _unitOfWork.Received(6).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Reexecução com mesma chave não duplica posts nem chama o gerador.
    /// </summary>
    [Fact]
    public async Task Handle_ComMesmaChaveIdempotente_NaoDuplicaPosts()
    {
        SeedBrandVoice();
        SeedAllPlatformConnections();
        _generator.SetDefaultSuccess("Legenda", ["tag"], [], []);

        var handler = CreateHandler();
        var command = DefaultCommand();

        var first = await handler.Handle(command, CancellationToken.None);
        var callsAfterFirst = _generator.CallCount;

        var second = await handler.Handle(command, CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        second.Value.CreatedPostIds.Should().BeEquivalentTo(first.Value.CreatedPostIds);
        second.Value.FailedSlots.Should().BeEquivalentTo(first.Value.FailedSlots);
        _generator.CallCount.Should().Be(callsAfterFirst);
        _postRepository.AllPosts.Should().HaveCount(5);
    }

    /// <summary>
    /// Brand Voice ausente falha antes de qualquer commit.
    /// </summary>
    [Fact]
    public async Task Handle_SemBrandVoice_NaoCommita()
    {
        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.BrandVoiceNotFound.Code);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Intervalo inválido falha sem commit.
    /// </summary>
    [Fact]
    public async Task Handle_ComIntervaloInvalido_NaoCommita()
    {
        SeedBrandVoice();
        var command = DefaultCommand() with { RangeStart = new DateOnly(2026, 11, 10), RangeEnd = new DateOnly(2026, 11, 1) };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PlanRangeInvalid.Code);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Posts por semana inválidos falham sem commit.
    /// </summary>
    [Fact]
    public async Task Handle_ComPostsPorSemanaInvalido_NaoCommita()
    {
        SeedBrandVoice();
        var command = DefaultCommand() with { PostsPerWeek = 0 };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PostsPerWeekInvalid.Code);
        await _unitOfWork.DidNotReceive().CommitAsync(CancellationToken.None);
    }

    /// <summary>
    /// Slot sem conexão ativa para a rede entra em failedSlots sem chamar o gerador para esse slot.
    /// </summary>
    [Fact]
    public async Task Handle_SemConexaoParaRede_DeveRegistrarConnectionRequired()
    {
        SeedBrandVoice();
        _connectionRepository.Seed(
            CreateConnection(OrganicPlatform.Instagram),
            CreateConnection(OrganicPlatform.FacebookPage));
        _generator.SetDefaultSuccess("Legenda", ["a"], [], []);

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FailedSlots.Should().Contain(f =>
            f.Platform == OrganicPlatform.TikTok &&
            f.ErrorCode == SocialErrors.ConnectionRequired.Code);
        _generator.CallCount.Should().BeLessThan(5);
    }

    private GenerateEditorialPlanCommand DefaultCommand() =>
        new(
            _workspaceId,
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 6),
            SaoPaulo,
            PostsPerWeek: 5,
            Seed: 0);

    private GenerateEditorialPlanCommandHandler CreateHandler()
    {
        var tenantAccessor = new TenantContextAccessor();
        tenantAccessor.TenantContext = TenantContext.Create(
            Guid.NewGuid(),
            "slug",
            TenantResolutionSource.Header);

        var slotCopyHandler = new GenerateSlotCopyCommandHandler(
            _brandVoiceRepository,
            _generator,
            tenantAccessor);

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GenerateSlotCopyCommand>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => slotCopyHandler.Handle(
                callInfo.Arg<GenerateSlotCopyCommand>(),
                callInfo.Arg<CancellationToken>()));

        return new GenerateEditorialPlanCommandHandler(
            _brandVoiceRepository,
            _connectionRepository,
            _postRepository,
            _planRepository,
            _unitOfWork,
            sender,
            tenantAccessor);
    }

    private void SeedBrandVoice() =>
        _brandVoiceRepository.Seed(
            BrandVoiceProfile.Create(
                _workspaceId,
                "Tom",
                "Persona",
                ["https://example.com"],
                [],
                EmojiPolicy.Sparingly,
                15,
                40,
                20,
                25,
                15).Value);

    private void SeedAllPlatformConnections() =>
        _connectionRepository.Seed(
            CreateConnection(OrganicPlatform.Instagram),
            CreateConnection(OrganicPlatform.FacebookPage),
            CreateConnection(OrganicPlatform.TikTok));

    private OrganicConnection CreateConnection(OrganicPlatform platform) =>
        OrganicConnection.Create(
            _workspaceId,
            platform,
            $"{platform}-ext",
            $"{platform} display",
            "scope",
            "cipher").Value;

    private sealed class FakeBrandVoiceRepository : IBrandVoiceRepository
    {
        private BrandVoiceProfile? _profile;

        public void Seed(BrandVoiceProfile profile) => _profile = profile;

        public Task<BrandVoiceProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_profile?.Id == id ? _profile : null);

        public Task<BrandVoiceProfile?> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_profile?.WorkspaceId == workspaceId ? _profile : null);

        public Task<IReadOnlyList<BrandVoiceProfile>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BrandVoiceProfile>>(
                _profile?.WorkspaceId == workspaceId ? [_profile] : Array.Empty<BrandVoiceProfile>());

        public Task AddAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeConnectionRepository : IOrganicConnectionRepository
    {
        private readonly List<OrganicConnection> _connections = new();

        public void Seed(params OrganicConnection[] connections) => _connections.AddRange(connections);

        public Task<OrganicConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_connections.FirstOrDefault(c => c.Id == id));

        public Task<OrganicConnection?> GetByWorkspacePlatformAndExternalAccountAsync(
            Guid workspaceId,
            OrganicPlatform platform,
            string externalAccountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_connections.FirstOrDefault(c =>
                c.WorkspaceId == workspaceId &&
                c.Platform == platform &&
                c.ExternalAccountId == externalAccountId));

        public Task<IReadOnlyList<OrganicConnection>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicConnection>>(_connections.Where(c => c.WorkspaceId == workspaceId).ToList());

        public Task AddAsync(OrganicConnection connection, CancellationToken cancellationToken = default)
        {
            _connections.Add(connection);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganicConnection connection, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> CountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_connections.Count(c => c.WorkspaceId == workspaceId));
    }

    private sealed class InMemoryPostRepository : IOrganicPostRepository
    {
        public List<OrganicPost> AllPosts { get; } = new();

        public Task<OrganicPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(AllPosts.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(AllPosts.Where(p => p.WorkspaceId == workspaceId).ToList());

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceScheduledRangeAsync(
            Guid workspaceId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(
                AllPosts
                    .Where(p => p.WorkspaceId == workspaceId &&
                                p.ScheduledAtUtc >= fromUtc &&
                                p.ScheduledAtUtc <= toUtc)
                    .OrderBy(p => p.ScheduledAtUtc)
                    .ToList());

        public Task AddAsync(OrganicPost post, CancellationToken cancellationToken = default)
        {
            AllPosts.Add(post);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganicPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> CountByWorkspaceAndStatusAsync(
            Guid workspaceId,
            OrganicPostStatus status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(AllPosts.Count(p => p.WorkspaceId == workspaceId && p.Status == status));
    }

    private sealed class InMemoryPlanGenerationRepository : IEditorialPlanGenerationRepository
    {
        private readonly List<EditorialPlanGeneration> _items = new();

        public Task<EditorialPlanGeneration?> GetByKeyAsync(
            Guid workspaceId,
            DateOnly rangeStart,
            DateOnly rangeEnd,
            int seed,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(g =>
                g.WorkspaceId == workspaceId &&
                g.RangeStart == rangeStart &&
                g.RangeEnd == rangeEnd &&
                g.Seed == seed));

        public Task AddAsync(EditorialPlanGeneration generation, CancellationToken cancellationToken = default)
        {
            _items.Add(generation);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(EditorialPlanGeneration generation, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SequentialCreativeGenerator : ISocialCreativeGenerator
    {
        private readonly List<(int CallIndex, Error Error)> _failures = new();
        private string _caption = "Legenda";
        private IReadOnlyList<string> _brand = ["marca"];
        private IReadOnlyList<string> _niche = [];
        private IReadOnlyList<string> _reach = [];

        public int CallCount { get; private set; }

        public void SetDefaultSuccess(string caption, IReadOnlyList<string> brand, IReadOnlyList<string> niche, IReadOnlyList<string> reach)
        {
            _caption = caption;
            _brand = brand;
            _niche = niche;
            _reach = reach;
        }

        public void FailOnCall(int callIndex, Error error) => _failures.Add((callIndex, error));

        public Task<Result<GeneratedCopyDraft>> GenerateAsync(CopyBrief brief, CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (_failures.Any(f => f.CallIndex == CallCount))
            {
                var error = _failures.First(f => f.CallIndex == CallCount).Error;
                return Task.FromResult(Result<GeneratedCopyDraft>.Failure(error));
            }

            return Task.FromResult(Result<GeneratedCopyDraft>.Success(
                new GeneratedCopyDraft(_caption, _brand, _niche, _reach)));
        }
    }
}
