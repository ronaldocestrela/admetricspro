using System.Reflection;
using System.Security.Cryptography.Pkcs;
using FluentAssertions;

namespace UnitTests.Backend.Compliance;

/// <summary>Verifica a versão efetiva da dependência criptográfica utilizada pela aplicação.</summary>
public sealed class PkcsSecurityDependencyTests
{
    /// <summary>Impede regressão para versões anteriores à correção dos avisos de segurança PKCS de outubro de 2026.</summary>
    [Fact]
    public void ResolvedPkcsAssembly_IncludesOctober2026SecurityFixes()
    {
        var version = typeof(SignedCms).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        var release = Version.Parse(version.Split('+')[0]);

        release.Should().BeGreaterThanOrEqualTo(new Version(10, 0, 13));
    }
}
