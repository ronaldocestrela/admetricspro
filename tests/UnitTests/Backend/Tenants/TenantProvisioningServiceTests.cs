using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Security;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Application.Services;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Master.Infrastructure.Repositories;
using Master.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="TenantProvisioningService"/> and <see cref="ProvisionTenantCommand"/>.
/// </summary>
public sealed class TenantProvisioningServiceTests : IDisposable
{
    private static readonly string EncryptionKey = Convert.ToBase64String(new byte[32]
    {
        1, 2, 3, 4, 5, 6, 7, 8,
        9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31, 32
    });

    private readonly SqliteConnection _connection;
    private readonly MasterDbContext _masterDbContext;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TenantProvisioningService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProvisioningServiceTests"/> class.
    /// </summary>
    public TenantProvisioningServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlite(_connection)
            .Options;

        _masterDbContext = new MasterDbContext(options);
        _masterDbContext.Database.EnsureCreated();

        _tenantRepository = new TenantRepository(_masterDbContext);
        _unitOfWork = new UnitOfWork(_masterDbContext);
        _encryptionService = new AesEncryptionService(EncryptionKey);
        _passwordHasher = new PasswordHasher();
        _sut = new TenantProvisioningService(_masterDbContext, _tenantRepository, _unitOfWork, _encryptionService, _passwordHasher);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _masterDbContext.Dispose();
        _connection.Dispose();
    }

    /// <summary>
    /// Verifies that provisioning fails when company name is invalid.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ProvisionTenantDatabaseAsync_ShouldReturnValidationFailure_WhenCompanyNameIsInvalid(string? companyName)
    {
        // Arrange
        var command = new ProvisionTenantCommand(companyName!, "12345678000199", "valid-subdomain");

        // Act
        var result = await _sut.ProvisionTenantDatabaseAsync(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.CompanyNameRequired");
    }

    /// <summary>
    /// Verifies that provisioning fails when CNPJ format is invalid.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("1234567800019A")]
    [InlineData("12.345.678/0001-90")]
    public async Task ProvisionTenantDatabaseAsync_ShouldReturnValidationFailure_WhenCnpjIsInvalid(string invalidCnpj)
    {
        // Arrange
        var command = new ProvisionTenantCommand("Acme Corp", invalidCnpj, "acme");

        // Act
        var result = await _sut.ProvisionTenantDatabaseAsync(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.InvalidCnpj");
    }

    /// <summary>
    /// Verifies that provisioning fails when subdomain contains invalid characters or whitespace.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid subdomain")]
    public async Task ProvisionTenantDatabaseAsync_ShouldReturnValidationFailure_WhenSubdomainIsInvalid(string invalidSubdomain)
    {
        // Arrange
        var command = new ProvisionTenantCommand("Acme Corp", "12345678000199", invalidSubdomain);

        // Act
        var result = await _sut.ProvisionTenantDatabaseAsync(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.InvalidSubdomain");
    }

    /// <summary>
    /// Verifies that provisioning returns conflict when the subdomain is already registered.
    /// </summary>
    [Fact]
    public async Task ProvisionTenantDatabaseAsync_ShouldReturnConflict_WhenSubdomainAlreadyExists()
    {
        // Arrange
        var existingTenant = Tenant.Create("Existing Corp", "98765432000188", "duplicate-subdomain").Value;
        existingTenant.SetEncryptedConnectionString(_encryptionService.Encrypt("DummyConnection"));
        await _tenantRepository.AddAsync(existingTenant);
        await _unitOfWork.CommitAsync();

        var command = new ProvisionTenantCommand("New Corp", "11111111000111", "duplicate-subdomain");

        // Act
        var result = await _sut.ProvisionTenantDatabaseAsync(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.SubdomainAlreadyExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    /// <summary>
    /// Verifies that provisioning returns conflict when the CNPJ is already registered.
    /// </summary>
    [Fact]
    public async Task ProvisionTenantDatabaseAsync_ShouldReturnConflict_WhenCnpjAlreadyExists()
    {
        // Arrange
        const string sharedCnpj = "55555555000155";
        var existingTenant = Tenant.Create("Existing Corp", sharedCnpj, "existing-subdomain").Value;
        existingTenant.SetEncryptedConnectionString(_encryptionService.Encrypt("DummyConnection"));
        await _tenantRepository.AddAsync(existingTenant);
        await _unitOfWork.CommitAsync();

        var command = new ProvisionTenantCommand("New Corp", sharedCnpj, "unique-subdomain");

        // Act
        var result = await _sut.ProvisionTenantDatabaseAsync(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.CnpjAlreadyExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    /// <summary>
    /// Verifies that provisioning returns validation failure when command is null.
    /// </summary>
    [Fact]
    public async Task ProvisionTenantDatabaseAsync_ShouldReturnValidationFailure_WhenCommandIsNull()
    {
        // Act
        var result = await _sut.ProvisionTenantDatabaseAsync((ProvisionTenantCommand)null!, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.CommandRequired");
    }

    /// <summary>
    /// Verifies that legacy overload delegates correctly and validates company name.
    /// </summary>
    [Fact]
    public async Task ProvisionTenantDatabaseAsync_LegacyOverload_ShouldReturnValidationFailure_WhenCompanyNameIsEmpty()
    {
        // Act
        var result = await _sut.ProvisionTenantDatabaseAsync(string.Empty, "12345678000199", "legacy-subdomain", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.CompanyNameRequired");
    }

    /// <summary>
    /// Verifies that ProvisionTenantCommand defaults to SubscriptionTier.Trial.
    /// </summary>
    [Fact]
    public void ProvisionTenantCommand_ShouldDefaultToTrialTier()
    {
        // Arrange & Act
        var command = new ProvisionTenantCommand("Agencia Nova", "12345678000199", "agencia-nova");

        // Assert
        command.CompanyName.Should().Be("Agencia Nova");
        command.Cnpj.Should().Be("12345678000199");
        command.Subdomain.Should().Be("agencia-nova");
        command.Tier.Should().Be(SubscriptionTier.Trial);
    }

    /// <summary>
    /// Verifies that ProvisionTenantCommand properly initializes onboarding profile and branding properties.
    /// </summary>
    [Fact]
    public void ProvisionTenantCommand_WithOnboardingProfile_ShouldInitializeAllProperties()
    {
        // Arrange & Act
        var command = new ProvisionTenantCommand(
            "Agencia Nova",
            "12345678000199",
            "agencia-nova",
            SubscriptionTier.Pro,
            Segment: "Agência de Performance",
            MonthlyAdSpendRange: "R$ 20k a R$ 100k",
            BillingCycle: "Monthly",
            CustomDomain: "ads.agencianova.com.br",
            PrimaryColor: "#4f46e5",
            SecondaryColor: "#0f172a");

        // Assert
        command.CompanyName.Should().Be("Agencia Nova");
        command.Cnpj.Should().Be("12345678000199");
        command.Subdomain.Should().Be("agencia-nova");
        command.Tier.Should().Be(SubscriptionTier.Pro);
        command.Segment.Should().Be("Agência de Performance");
        command.MonthlyAdSpendRange.Should().Be("R$ 20k a R$ 100k");
        command.BillingCycle.Should().Be("Monthly");
        command.CustomDomain.Should().Be("ads.agencianova.com.br");
        command.PrimaryColor.Should().Be("#4f46e5");
        command.SecondaryColor.Should().Be("#0f172a");
    }

    /// <summary>
    /// Verifies that ProvisionTenantCommand properly initializes admin credential properties.
    /// </summary>
    [Fact]
    public void ProvisionTenantCommand_WithAdminCredentials_ShouldInitializeAllProperties()
    {
        // Arrange & Act
        var command = new ProvisionTenantCommand(
            "Agencia Nova",
            "12345678000199",
            "agencia-nova",
            SubscriptionTier.Pro,
            AdminFullName: "Carlos Mendes",
            AdminEmail: "carlos@agencianova.com.br",
            AdminPhone: "11987654321",
            AdminPassword: "StrongPassword@2026!");

        // Assert
        command.AdminFullName.Should().Be("Carlos Mendes");
        command.AdminEmail.Should().Be("carlos@agencianova.com.br");
        command.AdminPhone.Should().Be("11987654321");
        command.AdminPassword.Should().Be("StrongPassword@2026!");
    }

    /// <summary>
    /// Verifies that SeedTenantInitialAdminAsync creates both Owner TenantUser and initial TenantBranding.
    /// </summary>
    [Fact]
    public async Task SeedTenantInitialAdminAsync_ShouldCreateOwnerUserAndBranding_WhenAdminCredentialsProvided()
    {
        // Arrange
        using var tenantConnection = new SqliteConnection("DataSource=:memory:");
        tenantConnection.Open();

        var options = new DbContextOptionsBuilder<TenantOperationalDbContext>()
            .UseSqlite(tenantConnection)
            .Options;

        await using var tenantContext = new TenantOperationalDbContext(options);
        await tenantContext.Database.EnsureCreatedAsync();

        var command = new ProvisionTenantCommand(
            "Agencia Prime",
            "12345678000199",
            "agencia-prime",
            SubscriptionTier.Pro,
            PrimaryColor: "#6366F1",
            SecondaryColor: "#1E293B",
            AdminFullName: "Mariana Silva",
            AdminEmail: "mariana@agenciaprime.com.br",
            AdminPhone: "11999998888",
            AdminPassword: "SuperSecurePassword#123");

        // Act
        var result = await _sut.SeedTenantInitialAdminAsync(tenantContext, command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var seededUser = await tenantContext.TenantUsers.SingleOrDefaultAsync(u => u.Email == "mariana@agenciaprime.com.br");
        seededUser.Should().NotBeNull();
        seededUser!.FullName.Should().Be("Mariana Silva");
        seededUser.Role.Should().Be(TenantRole.Owner);
        seededUser.IsActive.Should().BeTrue();
        seededUser.PhoneNumber.Should().Be("11999998888");
        _passwordHasher.VerifyPassword(seededUser.PasswordHash, "SuperSecurePassword#123").Should().BeTrue();

        var seededBranding = await tenantContext.TenantBranding.SingleOrDefaultAsync();
        seededBranding.Should().NotBeNull();
        seededBranding!.PrimaryColor.Should().Be("#6366F1");
        seededBranding.SecondaryColor.Should().Be("#1E293B");
    }

    /// <summary>
    /// Verifies that SeedTenantInitialAdminAsync falls back to corporate defaults when colors are omitted.
    /// </summary>
    [Fact]
    public async Task SeedTenantInitialAdminAsync_ShouldUseDefaultColors_WhenBrandingColorsNotProvided()
    {
        // Arrange
        using var tenantConnection = new SqliteConnection("DataSource=:memory:");
        tenantConnection.Open();

        var options = new DbContextOptionsBuilder<TenantOperationalDbContext>()
            .UseSqlite(tenantConnection)
            .Options;

        await using var tenantContext = new TenantOperationalDbContext(options);
        await tenantContext.Database.EnsureCreatedAsync();

        var command = new ProvisionTenantCommand(
            "Agencia Sem Cor",
            "12345678000199",
            "agencia-sem-cor",
            AdminEmail: "owner@semcor.com",
            AdminPassword: "Password123!");

        // Act
        var result = await _sut.SeedTenantInitialAdminAsync(tenantContext, command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var seededBranding = await tenantContext.TenantBranding.SingleOrDefaultAsync();
        seededBranding.Should().NotBeNull();
        seededBranding!.PrimaryColor.Should().Be("#4F46E5");
        seededBranding.SecondaryColor.Should().Be("#0F172A");
    }

    /// <summary>
    /// Verifies that SeedTenantInitialAdminAsync is idempotent and does not duplicate records.
    /// </summary>
    [Fact]
    public async Task SeedTenantInitialAdminAsync_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        // Arrange
        using var tenantConnection = new SqliteConnection("DataSource=:memory:");
        tenantConnection.Open();

        var options = new DbContextOptionsBuilder<TenantOperationalDbContext>()
            .UseSqlite(tenantConnection)
            .Options;

        await using var tenantContext = new TenantOperationalDbContext(options);
        await tenantContext.Database.EnsureCreatedAsync();

        var command = new ProvisionTenantCommand(
            "Agencia Idempotente",
            "12345678000199",
            "agencia-idem",
            AdminEmail: "owner@idem.com",
            AdminPassword: "Password123!");

        // Act
        var firstResult = await _sut.SeedTenantInitialAdminAsync(tenantContext, command, CancellationToken.None);
        var secondResult = await _sut.SeedTenantInitialAdminAsync(tenantContext, command, CancellationToken.None);

        // Assert
        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeTrue();

        var userCount = await tenantContext.TenantUsers.CountAsync();
        userCount.Should().Be(1);

        var brandingCount = await tenantContext.TenantBranding.CountAsync();
        brandingCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that SeedTenantInitialAdminAsync succeeds and only seeds branding when admin credentials are not provided.
    /// </summary>
    [Fact]
    public async Task SeedTenantInitialAdminAsync_ShouldSucceedWithoutUser_WhenAdminCredentialsOmitted()
    {
        // Arrange
        using var tenantConnection = new SqliteConnection("DataSource=:memory:");
        tenantConnection.Open();

        var options = new DbContextOptionsBuilder<TenantOperationalDbContext>()
            .UseSqlite(tenantConnection)
            .Options;

        await using var tenantContext = new TenantOperationalDbContext(options);
        await tenantContext.Database.EnsureCreatedAsync();

        var command = new ProvisionTenantCommand(
            "Agencia Sem Admin",
            "12345678000199",
            "agencia-sem-admin");

        // Act
        var result = await _sut.SeedTenantInitialAdminAsync(tenantContext, command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var userCount = await tenantContext.TenantUsers.CountAsync();
        userCount.Should().Be(0);

        var seededBranding = await tenantContext.TenantBranding.SingleOrDefaultAsync();
        seededBranding.Should().NotBeNull();
    }
}
