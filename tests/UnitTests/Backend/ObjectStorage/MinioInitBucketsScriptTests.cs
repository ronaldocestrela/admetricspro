using FluentAssertions;

namespace UnitTests.Backend.ObjectStorage;

/// <summary>
/// Confirma que o init do MinIO aplica ILM de staging orgânico.
/// </summary>
public sealed class MinioInitBucketsScriptTests
{
    /// <summary>
    /// O script de init declara expiração em 1 dia no prefixo de staging.
    /// </summary>
    [Fact]
    public void InitBucketsScript_ShouldDeclareStagingIlmRule()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "deploy", "minio", "init-buckets.sh"));

        if (!File.Exists(path))
        {
            path = Path.GetFullPath("/home/rony/admetricspro/deploy/minio/init-buckets.sh");
        }

        var script = File.ReadAllText(path);
        script.Should().Contain("organic-media/staging/");
        script.Should().Contain("--expire-days");
    }
}