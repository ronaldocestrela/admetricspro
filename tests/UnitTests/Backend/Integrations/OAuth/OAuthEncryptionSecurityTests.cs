using System.Security.Cryptography;
using BuildingBlocks.Infrastructure.Security;
using FluentAssertions;
using Integrations.Application.Options;
using Integrations.Infrastructure.OAuth;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Protege o cofre com criptografia autenticada e chave externa.</summary>
public sealed class OAuthEncryptionSecurityTests
{
    /// <summary>Mesmo com IDs locais iguais, ciphertexts não podem ser transferidos entre tenants.</summary>
    [Fact]
    public void Decrypt_WithDifferentTenant_RejectsTokenSwap()
    {
        var tenants = Substitute.For<BuildingBlocks.Application.MultiTenancy.ITenantContextAccessor>();
        var tenantA = Substitute.For<BuildingBlocks.Application.MultiTenancy.ITenantContext>(); tenantA.TenantId.Returns(Guid.NewGuid());
        var tenantB = Substitute.For<BuildingBlocks.Application.MultiTenancy.ITenantContext>(); tenantB.TenantId.Returns(Guid.NewGuid());
        tenants.TenantContext.Returns(tenantA);
        var service = new OAuthEncryptionService(Substitute.For<IEncryptionService>(), Options.Create(new OAuthNetworkOptions
        { TokenEncryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }), tenants);
        var encrypted = service.Encrypt("token-A", "same-local-context");
        tenants.TenantContext.Returns(tenantB);
        var swap = () => service.Decrypt(encrypted, "same-local-context");
        swap.Should().Throw<CryptographicException>();
    }
    /// <summary>Um ciphertext copiado para outra conexão não pode ser descriptografado.</summary>
    [Fact]
    public void Decrypt_WithDifferentConnectionPurpose_RejectsTokenSwap()
    {
        var service = new OAuthEncryptionService(Substitute.For<IEncryptionService>(), Options.Create(new OAuthNetworkOptions
        { TokenEncryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }));
        var encrypted = service.Encrypt("token-A", "MetaAds:workspace-A:connection-A");
        var swap = () => service.Decrypt(encrypted, "MetaAds:workspace-B:connection-B");
        swap.Should().Throw<CryptographicException>();
    }
    /// <summary>Ciphertext adulterado deve falhar na autenticação antes de liberar qualquer token.</summary>
    [Fact]
    public void Encrypt_RoundTripsAndRejectsTampering()
    {
        var service = new OAuthEncryptionService(Substitute.For<IEncryptionService>(), Options.Create(new OAuthNetworkOptions
        { TokenEncryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }));
        var encrypted = service.Encrypt("token-private");
        encrypted.Should().NotContain("token-private");
        service.Decrypt(encrypted).Should().Be("token-private");
        var bytes = Convert.FromBase64String(encrypted[5..]);
        bytes[^1] ^= 1;
        var act = () => service.Decrypt("gcm1." + Convert.ToBase64String(bytes));
        act.Should().Throw<CryptographicException>();
    }
}
