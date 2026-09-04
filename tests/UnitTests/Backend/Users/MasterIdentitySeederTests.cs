using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Auditing;
using Master.Infrastructure.Configuration;
using Master.Infrastructure.Identity;
using Master.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Users;

/// <summary>
/// Testes unitários para a rotina de provisionamento e seed do Super Administrador (MasterIdentitySeeder).
/// </summary>
public sealed class MasterIdentitySeederTests
{
    private readonly UserManager<MasterUser> _userManager;
    private readonly RoleManager<MasterRole> _roleManager;
    private readonly IMasterAuditService _auditService = Substitute.For<IMasterAuditService>();
    private readonly ILogger<MasterIdentitySeeder> _logger = Substitute.For<ILogger<MasterIdentitySeeder>>();

    /// <summary>
    /// Inicializa a suíte com mocks de UserManager e RoleManager.
    /// </summary>
    public MasterIdentitySeederTests()
    {
        var userStore = Substitute.For<IUserStore<MasterUser>>();
        _userManager = Substitute.For<UserManager<MasterUser>>(
            userStore, null, null, null, null, null, null, null, null);

        var roleStore = Substitute.For<IRoleStore<MasterRole>>();
        _roleManager = Substitute.For<RoleManager<MasterRole>>(
            roleStore, null, null, null, null);
    }

    /// <summary>
    /// Valida que quando o usuário SuperAdmin não existe, as roles e o usuário são criados e auditados com sucesso.
    /// </summary>
    [Fact]
    public async Task SeedSuperAdminAsync_WhenUserDoesNotExist_ShouldCreateRolesAndUser()
    {
        // Arrange
        var options = new SuperAdminSeedOptions
        {
            Email = "admin@admetricspro.internal",
            Password = "SuperAdmin@Secure2026!",
            FullName = "Admin Global",
            Role = "SuperAdmin"
        };
        var optionsWrapper = Options.Create(options);

        _roleManager.RoleExistsAsync(MasterRole.SuperAdmin).Returns(false);
        _roleManager.RoleExistsAsync(MasterRole.SupportTechnician).Returns(false);
        _roleManager.CreateAsync(Arg.Any<MasterRole>()).Returns(IdentityResult.Success);

        _userManager.FindByEmailAsync(options.Email).Returns((MasterUser?)null);
        _userManager.CreateAsync(Arg.Any<MasterUser>(), options.Password).Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<MasterUser>(), MasterRole.SuperAdmin).Returns(IdentityResult.Success);

        var seeder = new MasterIdentitySeeder(_userManager, _roleManager, optionsWrapper, _auditService, _logger);

        // Act
        var result = await seeder.SeedSuperAdminAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _roleManager.Received(2).CreateAsync(Arg.Any<MasterRole>());
        await _userManager.Received(1).CreateAsync(Arg.Any<MasterUser>(), options.Password);
        await _auditService.Received(1).RecordAsync(
            action: "SuperAdminUserSeeded",
            resource: "Users",
            resourceId: Arg.Any<string>(),
            details: Arg.Any<string>(),
            tenantId: null,
            ipAddress: "127.0.0.1",
            additionalTags: Arg.Any<IEnumerable<string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que quando o usuário já existe, apenas garante associação de papel sem criar novamente.
    /// </summary>
    [Fact]
    public async Task SeedSuperAdminAsync_WhenUserAlreadyExists_ShouldEnsureRoleAssociation()
    {
        // Arrange
        var options = new SuperAdminSeedOptions
        {
            Email = "admin@admetricspro.internal",
            Password = "SuperAdmin@Secure2026!",
            FullName = "Admin Global",
            Role = "SuperAdmin"
        };
        var optionsWrapper = Options.Create(options);

        _roleManager.RoleExistsAsync(MasterRole.SuperAdmin).Returns(true);
        _roleManager.RoleExistsAsync(MasterRole.SupportTechnician).Returns(true);

        var existingUser = new MasterUser(options.Email, options.FullName);
        _userManager.FindByEmailAsync(options.Email).Returns(existingUser);
        _userManager.IsInRoleAsync(existingUser, MasterRole.SuperAdmin).Returns(false);
        _userManager.AddToRoleAsync(existingUser, MasterRole.SuperAdmin).Returns(IdentityResult.Success);

        var seeder = new MasterIdentitySeeder(_userManager, _roleManager, optionsWrapper, _auditService, _logger);

        // Act
        var result = await seeder.SeedSuperAdminAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<MasterUser>(), Arg.Any<string>());
        await _userManager.Received(1).AddToRoleAsync(existingUser, MasterRole.SuperAdmin);
    }
}
