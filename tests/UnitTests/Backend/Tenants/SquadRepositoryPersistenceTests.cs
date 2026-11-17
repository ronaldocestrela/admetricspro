using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tenants.Infrastructure.Persistence;
using Tenants.Infrastructure.Squads;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes de persistência do agregado <see cref="Squad"/> via <see cref="SquadRepository"/> e EF Core,
/// reproduzindo o fluxo real de vincular membros e workspaces (inclusão em coleção + <c>Update</c> + <c>SaveChanges</c>).
/// </summary>
public sealed class SquadRepositoryPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TenantDbContext _dbContext;
    private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly SquadRepository _repository;
    private readonly TenantUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa SQLite em memória com o esquema completo do <see cref="TenantDbContext"/>.
    /// </summary>
    public SquadRepositoryPersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TenantDbContext(options);
        _dbContext.Database.EnsureCreated();

        _contextAccessor = Substitute.For<ITenantDbContextAccessor>();
        _contextAccessor.GetDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<TenantDbContext>.Success(_dbContext)));

        _repository = new SquadRepository(_contextAccessor);
        _unitOfWork = new TenantUnitOfWork(_contextAccessor);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    /// <summary>
    /// Valida que adicionar membro a um squad rastreado persiste em <c>SquadMembers</c> sem exceção de concorrência.
    /// </summary>
    [Fact]
    public async Task Update_AfterAddMember_OnTrackedSquad_ShouldInsertSquadMemberRow()
    {
        var squadId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Growth", null).Value;

        await _repository.AddAsync(squad);
        await _unitOfWork.CommitAsync();

        var loaded = await _repository.GetByIdAsync(squadId);
        loaded.Should().NotBeNull();

        var addResult = loaded!.AddMember(userId);
        addResult.IsSuccess.Should().BeTrue();

        _repository.Update(loaded);
        var act = async () => await _unitOfWork.CommitAsync();
        await act.Should().NotThrowAsync();

        var memberCount = await _dbContext.SquadMembers.CountAsync(m => m.SquadId == squadId && m.UserId == userId);
        memberCount.Should().Be(1);
    }

    /// <summary>
    /// Valida que alocar workspace a um squad rastreado persiste em <c>SquadWorkspaces</c> sem exceção de concorrência.
    /// </summary>
    [Fact]
    public async Task Update_AfterAssignWorkspace_OnTrackedSquad_ShouldInsertSquadWorkspaceRow()
    {
        var squadId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Ops", null).Value;
        var workspace = Workspace.Create(workspaceId, "Cliente Alpha", "123.456.789-09", 5000m, "E-commerce").Value;

        await _repository.AddAsync(squad);
        await _dbContext.Workspaces.AddAsync(workspace);
        await _unitOfWork.CommitAsync();

        var loaded = await _repository.GetByIdAsync(squadId);
        loaded.Should().NotBeNull();

        var assignResult = loaded!.AssignWorkspace(workspaceId);
        assignResult.IsSuccess.Should().BeTrue();

        _repository.Update(loaded);
        var act = async () => await _unitOfWork.CommitAsync();
        await act.Should().NotThrowAsync();

        var linkCount = await _dbContext.SquadWorkspaces.CountAsync(w => w.SquadId == squadId && w.WorkspaceId == workspaceId);
        linkCount.Should().Be(1);
    }

    /// <summary>
    /// Garante que alterações escalares e remoção de membro continuam persistindo após a correção de tracking.
    /// </summary>
    [Fact]
    public async Task Update_AfterRemoveMemberAndRename_ShouldPersistScalarAndDeleteLink()
    {
        var squadId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Original Name", null).Value;
        squad.AddMember(userId);

        await _repository.AddAsync(squad);
        await _unitOfWork.CommitAsync();

        var loaded = await _repository.GetByIdAsync(squadId);
        loaded.Should().NotBeNull();
        loaded!.RemoveMember(userId).IsSuccess.Should().BeTrue();
        loaded.UpdateDetails("Renamed Squad", "Nova descrição").IsSuccess.Should().BeTrue();

        _repository.Update(loaded);
        await _unitOfWork.CommitAsync();

        var persisted = await _repository.GetByIdAsync(squadId);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("Renamed Squad");
        persisted.Members.Should().BeEmpty();
    }
}