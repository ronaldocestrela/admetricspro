using FluentAssertions;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Brand.Queries.GetBrandVoice;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes da consulta <see cref="GetBrandVoiceQueryHandler"/>.
/// </summary>
public sealed class GetBrandVoiceQueryTests
{
    /// <summary>
    /// Perfil existente devolve DTO mapeado.
    /// </summary>
    [Fact]
    public async Task Handle_ComPerfilExistente_DeveRetornarDto()
    {
        var workspaceId = Guid.NewGuid();
        var profile = BrandVoiceProfile.Create(
            workspaceId,
            "Tom",
            "Persona",
            ["https://example.com"],
            ["termo"],
            EmojiPolicy.Allowed,
            12,
            40,
            20,
            25,
            15).Value;

        var handler = new GetBrandVoiceQueryHandler(new FakeRepository(profile));
        var result = await handler.Handle(new GetBrandVoiceQuery(workspaceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(profile.Id);
        result.Value.WorkspaceId.Should().Be(workspaceId);
        result.Value.Tone.Should().Be("Tom");
    }

    /// <summary>
    /// Ausência de perfil retorna BrandVoiceNotFound.
    /// </summary>
    [Fact]
    public async Task Handle_SemPerfil_DeveRetornarBrandVoiceNotFound()
    {
        var handler = new GetBrandVoiceQueryHandler(new FakeRepository(null));
        var result = await handler.Handle(new GetBrandVoiceQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.BrandVoiceNotFound.Code);
    }

    /// <summary>
    /// Workspace vazio não consulta repositório.
    /// </summary>
    [Fact]
    public async Task Handle_ComWorkspaceVazio_DeveRetornarWorkspaceRequired()
    {
        var repository = new FakeRepository(null);
        var handler = new GetBrandVoiceQueryHandler(repository);
        var result = await handler.Handle(new GetBrandVoiceQuery(Guid.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.WorkspaceRequired.Code);
        repository.GetCalls.Should().Be(0);
    }

    private sealed class FakeRepository(BrandVoiceProfile? profile) : IBrandVoiceRepository
    {
        public int GetCalls { get; private set; }

        public Task<BrandVoiceProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile?.Id == id ? profile : null);

        public Task<BrandVoiceProfile?> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            GetCalls++;
            return Task.FromResult(profile?.WorkspaceId == workspaceId ? profile : null);
        }

        public Task<IReadOnlyList<BrandVoiceProfile>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task UpdateAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
