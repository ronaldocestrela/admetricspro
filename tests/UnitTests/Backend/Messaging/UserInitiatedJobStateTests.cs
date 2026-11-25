using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes de transição de <see cref="UserInitiatedJob"/>.
/// </summary>
public sealed class UserInitiatedJobStateTests
{
    private static readonly DateTime FixedUtc = new(2026, 11, 25, 15, 0, 0, DateTimeKind.Utc);

    private static UserInitiatedJob CreateAccepted() =>
        UserInitiatedJob.CreateAccepted(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            UserInitiatedJobKind.PostImageGenerate,
            Guid.NewGuid(),
            """{"postId":"11111111-1111-1111-1111-111111111111"}""",
            FixedUtc).Value;

    /// <summary>
    /// CreateAccepted deve iniciar em Accepted.
    /// </summary>
    [Fact]
    public void CreateAccepted_DeveIniciarEmAccepted()
    {
        var job = CreateAccepted();

        job.Status.Should().Be(UserInitiatedJobStatus.Accepted);
        job.IsTerminal.Should().BeFalse();
    }

    /// <summary>
    /// MarkRunning transiciona de Accepted para Running.
    /// </summary>
    [Fact]
    public void MarkRunning_DeAccepted_DeveTransicionar()
    {
        var job = CreateAccepted();

        job.MarkRunning(FixedUtc).IsSuccess.Should().BeTrue();
        job.Status.Should().Be(UserInitiatedJobStatus.Running);
        job.StartedAtUtc.Should().Be(FixedUtc);
    }

    /// <summary>
    /// MarkSucceeded persiste resultado.
    /// </summary>
    [Fact]
    public void MarkSucceeded_DeRunning_DeveTransicionar()
    {
        var job = CreateAccepted();
        job.MarkRunning(FixedUtc);

        job.MarkSucceeded("""{"postId":"x"}""", FixedUtc.AddMinutes(1)).IsSuccess.Should().BeTrue();
        job.Status.Should().Be(UserInitiatedJobStatus.Succeeded);
        job.ResultJson.Should().Be("""{"postId":"x"}""");
        job.IsTerminal.Should().BeTrue();
    }

    /// <summary>
    /// MarkFailed registra código semântico.
    /// </summary>
    [Fact]
    public void MarkFailed_DeRunning_DeveTransicionar()
    {
        var job = CreateAccepted();
        job.MarkRunning(FixedUtc);

        var error = Error.Validation("Social.CopyGenerationFailed", "Falha na IA.");
        job.MarkFailed(error, FixedUtc.AddMinutes(1)).IsSuccess.Should().BeTrue();

        job.Status.Should().Be(UserInitiatedJobStatus.Failed);
        job.ErrorCode.Should().Be("Social.CopyGenerationFailed");
        job.IsTerminal.Should().BeTrue();
    }

    /// <summary>
    /// EventId vazio falha na criação.
    /// </summary>
    [Fact]
    public void CreateAccepted_ComEventIdVazio_DeveFalhar()
    {
        var result = UserInitiatedJob.CreateAccepted(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            UserInitiatedJobKind.ReportGenerate,
            Guid.Empty,
            "{}",
            FixedUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserInitiatedJobErrors.InvalidEventId.Code);
    }
}