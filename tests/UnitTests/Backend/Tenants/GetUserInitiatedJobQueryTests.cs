using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Jobs.Queries.GetUserInitiatedJob;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes de consulta de job de usuário.
/// </summary>
public sealed class GetUserInitiatedJobQueryTests
{
    private static readonly DateTime FixedUtc = new(2026, 11, 25, 18, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Deve retornar DTO quando o job existe no workspace.
    /// </summary>
    [Fact]
    public async Task Handle_JobExistente_DeveRetornarDto()
    {
        var workspaceId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var job = UserInitiatedJob.CreateAccepted(
            jobId,
            Guid.NewGuid(),
            workspaceId,
            UserInitiatedJobKind.ReportGenerate,
            Guid.NewGuid(),
            """{"scheduleId":"11111111-1111-1111-1111-111111111111"}""",
            FixedUtc).Value;

        var store = Substitute.For<IUserInitiatedJobStore>();
        store.GetByIdAsync(workspaceId, jobId, Arg.Any<CancellationToken>()).Returns(job);

        var handler = new GetUserInitiatedJobQueryHandler(store);
        var result = await handler.Handle(new GetUserInitiatedJobQuery(workspaceId, jobId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.JobId.Should().Be(jobId);
        result.Value.Status.Should().Be(UserInitiatedJobStatus.Accepted);
    }
}