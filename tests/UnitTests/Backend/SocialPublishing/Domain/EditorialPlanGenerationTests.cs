using FluentAssertions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Planning;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Domain;

/// <summary>
/// Testes de <see cref="EditorialPlanGeneration"/>.
/// </summary>
public sealed class EditorialPlanGenerationTests
{
    /// <summary>
    /// Conclusão sem posts e só falhas retentáveis permite reabrir.
    /// </summary>
    [Fact]
    public void IsRetriableEmptyCompletion_ComFalhasDeProvedor_DeveSerVerdadeiro()
    {
        var generation = CompletedGenerationWithFailedSlots(SocialErrors.AiConfigurationMissing.Code);

        generation.IsRetriableEmptyCompletion().Should().BeTrue();
    }

    /// <summary>
    /// Falha total por ausência de conexão também pode ser reaberta.
    /// </summary>
    [Fact]
    public void IsRetriableEmptyCompletion_ComConnectionRequired_DeveSerVerdadeiro()
    {
        var generation = CompletedGenerationWithFailedSlots(SocialErrors.ConnectionRequired.Code);

        generation.IsRetriableEmptyCompletion().Should().BeTrue();
    }

    /// <summary>
    /// Conclusão com posts criados não é reabertura retentável.
    /// </summary>
    [Fact]
    public void IsRetriableEmptyCompletion_ComPostsCriados_DeveSerFalso()
    {
        var generation = EditorialPlanGeneration.Start(Guid.NewGuid(), new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6), 0).Value;
        generation.RecordCreatedPost(Guid.NewGuid());
        generation.Complete();

        generation.IsRetriableEmptyCompletion().Should().BeFalse();
    }

    /// <summary>
    /// Reabrir limpa falhas e volta para em andamento.
    /// </summary>
    [Fact]
    public void ReopenForRetriableRetry_ComFalhaTotalDeConfig_DeveLimparFalhas()
    {
        var generation = CompletedGenerationWithFailedSlots(SocialErrors.CopyGenerationFailed.Code);

        var result = generation.ReopenForRetriableRetry();

        result.IsSuccess.Should().BeTrue();
        generation.Status.Should().Be(EditorialPlanGenerationStatus.InProgress);
        generation.FailedSlots.Should().BeEmpty();
        generation.CreatedPostIds.Should().BeEmpty();
    }

    private static EditorialPlanGeneration CompletedGenerationWithFailedSlots(string errorCode)
    {
        var generation = EditorialPlanGeneration.Start(Guid.NewGuid(), new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6), 0).Value;
        generation.RecordFailedSlot(new EditorialPlanFailedSlot(
            new DateOnly(2026, 11, 2),
            new DateTime(2026, 11, 2, 13, 0, 0, DateTimeKind.Utc),
            ContentPillar.Authority,
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            errorCode,
            "erro"));
        generation.Complete();
        return generation;
    }
}