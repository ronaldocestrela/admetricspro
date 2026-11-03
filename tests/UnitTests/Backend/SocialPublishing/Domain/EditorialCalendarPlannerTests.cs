using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Planning;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Domain;

/// <summary>
/// Testes unitários do planner editorial determinístico (Subfase 3.2).
/// </summary>
public sealed class EditorialCalendarPlannerTests
{
    private const string SaoPaulo = "America/Sao_Paulo";

    /// <summary>
    /// Plano semanal com 5 posts e pesos 40/20/25/15 deve respeitar maior resto e total exato.
    /// </summary>
    [Fact]
    public void Plan_ComCincoPostsEPesos40202515_DeveDistribuirPilaresPorMaiorResto()
    {
        var profile = CreateProfile(40, 20, 25, 15);
        var start = new DateOnly(2026, 11, 2);
        var end = new DateOnly(2026, 11, 6);

        var result = EditorialCalendarPlanner.Plan(profile, start, end, SaoPaulo, postsPerWeek: 5, seed: 0);

        result.IsSuccess.Should().BeTrue();
        var slots = result.Value;
        slots.Should().HaveCount(5);
        CountPillar(slots, ContentPillar.Authority).Should().Be(2);
        CountPillar(slots, ContentPillar.Sale).Should().Be(1);
        CountPillar(slots, ContentPillar.Connection).Should().Be(1);
        CountPillar(slots, ContentPillar.SocialProof).Should().Be(1);
    }

    /// <summary>
    /// Dois posts Sale não devem cair em dias locais consecutivos quando o espaçamento é possível.
    /// </summary>
    [Fact]
    public void Plan_ComDoisSale_DeveEvitarDiasConsecutivosQuandoPossivel()
    {
        var profile = CreateProfile(20, 40, 20, 20);
        var start = new DateOnly(2026, 11, 2);
        var end = new DateOnly(2026, 11, 8);

        var result = EditorialCalendarPlanner.Plan(profile, start, end, SaoPaulo, postsPerWeek: 5, seed: 0);

        result.IsSuccess.Should().BeTrue();
        var saleDates = result.Value
            .Where(s => s.Pillar == ContentPillar.Sale)
            .Select(s => s.LocalDate)
            .OrderBy(d => d)
            .ToList();

        saleDates.Should().HaveCount(2);
        var gap = saleDates[1].DayNumber - saleDates[0].DayNumber;
        gap.Should().BeGreaterThan(1);
    }

    /// <summary>
    /// Conversão para UTC deve respeitar regras históricas de horário de verão do fuso.
    /// </summary>
    [Fact]
    public void Plan_ComFusoSaoPaulo_DeveConverterSegunda10hParaUtcCorreto()
    {
        var profile = CreateProfile(25, 25, 25, 25);
        var start = new DateOnly(2026, 11, 2);
        var end = new DateOnly(2026, 11, 2);

        var resultNov2026 = EditorialCalendarPlanner.Plan(profile, start, end, SaoPaulo, postsPerWeek: 1, seed: 0);
        resultNov2026.IsSuccess.Should().BeTrue();
        resultNov2026.Value[0].UtcInstant.Should().Be(new DateTime(2026, 11, 2, 13, 0, 0, DateTimeKind.Utc));

        var startDst = new DateOnly(2018, 10, 29);
        var endDst = new DateOnly(2018, 11, 5);

        var resultDst = EditorialCalendarPlanner.Plan(profile, startDst, endDst, SaoPaulo, postsPerWeek: 1, seed: 0);
        resultDst.IsSuccess.Should().BeTrue();
        var mondayOct29 = resultDst.Value.First(s => s.LocalDate == new DateOnly(2018, 10, 29));
        var mondayNov5 = resultDst.Value.First(s => s.LocalDate == new DateOnly(2018, 11, 5));
        mondayOct29.UtcInstant.Should().Be(new DateTime(2018, 10, 29, 13, 0, 0, DateTimeKind.Utc));
        mondayNov5.UtcInstant.Should().Be(new DateTime(2018, 11, 5, 12, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Semente explícita torna a ordem estável entre execuções; sementes distintas alteram a ordem mantendo contagens.
    /// </summary>
    [Fact]
    public void Plan_ComMesmaSemente_DeveProduzirSequenciaIdentica()
    {
        var profile = CreateProfile(40, 20, 25, 15);
        var start = new DateOnly(2026, 11, 2);
        var end = new DateOnly(2026, 11, 6);

        var first = EditorialCalendarPlanner.Plan(profile, start, end, SaoPaulo, 5, seed: 42);
        var second = EditorialCalendarPlanner.Plan(profile, start, end, SaoPaulo, 5, seed: 42);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        SequenceKey(first.Value).Should().Equal(SequenceKey(second.Value));

        var otherSeed = EditorialCalendarPlanner.Plan(profile, start, end, SaoPaulo, 5, seed: 43);
        otherSeed.IsSuccess.Should().BeTrue();
        CountPillar(otherSeed.Value, ContentPillar.Authority).Should().Be(2);
        SequenceKey(first.Value).Should().NotEqual(SequenceKey(otherSeed.Value));
    }

    /// <summary>
    /// Perfil ausente deve retornar BrandVoiceNotFound.
    /// </summary>
    [Fact]
    public void Plan_SemBrandVoice_DeveRetornarBrandVoiceNotFound()
    {
        var result = EditorialCalendarPlanner.Plan(
            null,
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 6),
            SaoPaulo,
            postsPerWeek: 5,
            seed: 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.BrandVoiceNotFound.Code);
    }

    /// <summary>
    /// Data final anterior à inicial deve falhar com validação.
    /// </summary>
    [Fact]
    public void Plan_ComRangeInvertido_DeveRetornarPlanRangeInvalid()
    {
        var profile = CreateProfile(25, 25, 25, 25);

        var result = EditorialCalendarPlanner.Plan(
            profile,
            new DateOnly(2026, 11, 10),
            new DateOnly(2026, 11, 1),
            SaoPaulo,
            postsPerWeek: 3,
            seed: 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PlanRangeInvalid.Code);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    /// <summary>
    /// Posts por semana fora de 1–7 deve falhar.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void Plan_ComPostsPorSemanaInvalido_DeveRetornarPostsPerWeekInvalid(int postsPerWeek)
    {
        var profile = CreateProfile(25, 25, 25, 25);

        var result = EditorialCalendarPlanner.Plan(
            profile,
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 6),
            SaoPaulo,
            postsPerWeek,
            seed: 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PostsPerWeekInvalid.Code);
    }

    /// <summary>
    /// Fuso IANA inválido deve falhar.
    /// </summary>
    [Fact]
    public void Plan_ComFusoInvalido_DeveRetornarTimeZoneInvalid()
    {
        var profile = CreateProfile(25, 25, 25, 25);

        var result = EditorialCalendarPlanner.Plan(
            profile,
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 6),
            "Invalid/Zone",
            postsPerWeek: 3,
            seed: 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.TimeZoneInvalid.Code);
    }

    private static BrandVoiceProfile CreateProfile(int authority, int sale, int connection, int socialProof)
    {
        return BrandVoiceProfile.Create(
            Guid.NewGuid(),
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Allowed,
            20,
            authority,
            sale,
            connection,
            socialProof).Value;
    }

    private static int CountPillar(IReadOnlyList<EditorialSlot> slots, ContentPillar pillar) =>
        slots.Count(s => s.Pillar == pillar);

    private static IEnumerable<string> SequenceKey(IReadOnlyList<EditorialSlot> slots) =>
        slots.Select(s =>
            $"{s.LocalDate:yyyy-MM-dd}|{s.UtcInstant:O}|{s.Pillar}|{s.Platform}|{s.Format}");
}
