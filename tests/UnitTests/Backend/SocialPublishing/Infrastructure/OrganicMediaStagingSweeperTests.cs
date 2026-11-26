using FluentAssertions;
using SocialPublishing.Application.Options;
using SocialPublishing.Infrastructure.Media;

namespace UnitTests.Backend.SocialPublishing.Infrastructure;

/// <summary>
/// Testes de <see cref="OrganicMediaStagingSweeper"/>.
/// </summary>
public sealed class OrganicMediaStagingSweeperTests
{
    /// <summary>
    /// Remove apenas staging com mais de 24 horas; durable permanece.
    /// </summary>
    [Fact]
    public async Task SweepAsync_ShouldDeleteOnlyOldStagingFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"staging-sweep-{Guid.NewGuid():N}");
        var staging = Path.Combine(root, "organic-media", "staging", Guid.NewGuid().ToString("N"));
        var durable = Path.Combine(root, "organic-media", "durable", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        Directory.CreateDirectory(durable);

        var oldStaging = Path.Combine(staging, "old.bin");
        var newStaging = Path.Combine(staging, "new.bin");
        var durableFile = Path.Combine(durable, "keep.bin");
        await File.WriteAllBytesAsync(oldStaging, [1]);
        await File.WriteAllBytesAsync(newStaging, [2]);
        await File.WriteAllBytesAsync(durableFile, [3]);

        File.SetLastWriteTimeUtc(oldStaging, DateTime.UtcNow.AddHours(-30));
        File.SetLastWriteTimeUtc(newStaging, DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(durableFile, DateTime.UtcNow.AddHours(-30));

        var fixedNow = DateTimeOffset.UtcNow;
        var time = new FakeTimeProvider(fixedNow);
        var sweeper = new OrganicMediaStagingSweeper(
            new OrganicMediaOptions { LocalRoot = root },
            time);

        var deleted = await sweeper.SweepAsync();

        deleted.Should().Be(1);
        File.Exists(oldStaging).Should().BeFalse();
        File.Exists(newStaging).Should().BeTrue();
        File.Exists(durableFile).Should().BeTrue();
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}