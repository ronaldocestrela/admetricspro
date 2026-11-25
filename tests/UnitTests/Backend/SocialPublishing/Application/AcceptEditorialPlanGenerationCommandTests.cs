using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Jobs.Commands.AcceptEditorialPlanGeneration;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Planning;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes de aceite assíncrono de plano editorial (Fase 8).
/// </summary>
public sealed class AcceptEditorialPlanGenerationCommandTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly ISocialUserInitiatedJobCommitter _committer = Substitute.For<ISocialUserInitiatedJobCommitter>();
    private readonly FakeBrandVoiceRepository _brandVoice = new();
    private readonly InMemoryPlanGenerationRepository _planRepo = new();

    /// <summary>
    /// Deve gravar job Accepted e outbox sem consumir gerador de cópia.
    /// </summary>
    [Fact]
    public async Task Handle_ComMensageria_DeveGravarJobSemChamarIa()
    {
        _brandVoice.Set(CreateBrandVoice());
        _committer.CommitAcceptedJobAsync(
                Arg.Any<UserInitiatedJob>(),
                Arg.Any<IntegrationOutboxMessage>(),
                Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result.Success());

        var handler = new AcceptEditorialPlanGenerationCommandHandler(
            _brandVoice,
            _planRepo,
            _committer,
            TenantAccessor(),
            TimeProvider.System);

        var result = await handler.Handle(
            new AcceptEditorialPlanGenerationCommand(
                _workspaceId,
                new DateOnly(2026, 11, 1),
                new DateOnly(2026, 11, 30),
                "America/Sao_Paulo",
                5,
                42),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(UserInitiatedJobStatus.Accepted);
        result.Value.Kind.Should().Be(UserInitiatedJobKind.EditorialPlanGenerate);
        await _committer.Received(1).CommitAcceptedJobAsync(
            Arg.Is<UserInitiatedJob>(j => j.Status == UserInitiatedJobStatus.Accepted),
            Arg.Is<IntegrationOutboxMessage>(m =>
                m.MessageType == BuildingBlocks.Application.Messaging.UserInitiatedJobIntegrationMessageTypes.EditorialPlanGenerate),
            Arg.Any<CancellationToken>());
    }

    private ITenantContextAccessor TenantAccessor()
    {
        var accessor = Substitute.For<ITenantContextAccessor>();
        accessor.TenantContext = TenantContext.Create(_tenantId, "ag", TenantResolutionSource.Header);
        return accessor;
    }

    private BrandVoiceProfile CreateBrandVoice() =>
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
            15).Value;

    private sealed class FakeBrandVoiceRepository : IBrandVoiceRepository
    {
        private BrandVoiceProfile? _profile;

        public void Set(BrandVoiceProfile profile) => _profile = profile;

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

    private sealed class InMemoryPlanGenerationRepository : IEditorialPlanGenerationRepository
    {
        public Task<EditorialPlanGeneration?> GetByKeyAsync(
            Guid workspaceId,
            DateOnly rangeStart,
            DateOnly rangeEnd,
            int seed,
            CancellationToken cancellationToken = default) => Task.FromResult<EditorialPlanGeneration?>(null);

        public Task AddAsync(EditorialPlanGeneration generation, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(EditorialPlanGeneration generation, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}