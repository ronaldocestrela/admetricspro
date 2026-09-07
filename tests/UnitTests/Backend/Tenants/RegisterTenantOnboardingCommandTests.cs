using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Services;
using Master.Application.Tenants.Commands.RegisterTenantOnboarding;
using Master.Domain.Tenants;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para validação e execução do comando de onboarding de novo inquilino (<see cref="RegisterTenantOnboardingCommand"/>).
/// </summary>
public sealed class RegisterTenantOnboardingCommandTests
{
    private readonly ITenantProvisioningService _provisioningService = Substitute.For<ITenantProvisioningService>();

    /// <summary>
    /// Valida que o validador rejeita comandos com dados incompletos ou formatos fiscais inválidos.
    /// </summary>
    [Theory]
    [InlineData("", "12345678000195", "vanguarda", "carlos@empresa.com", "Senha@123")] // Nome em branco
    [InlineData("Empresa", "123", "vanguarda", "carlos@empresa.com", "Senha@123")] // CNPJ inválido
    [InlineData("Empresa", "12345678000195", "adm in", "carlos@empresa.com", "Senha@123")] // Subdomínio com espaços
    [InlineData("Empresa", "12345678000195", "vanguarda", "email-invalido", "Senha@123")] // E-mail inválido
    [InlineData("Empresa", "12345678000195", "vanguarda", "carlos@empresa.com", "123")] // Senha fraca
    public void Validator_WhenInputsAreInvalid_ShouldHaveValidationErrors(
        string companyName, string cnpj, string subdomain, string email, string password)
    {
        // Arrange
        var validator = new RegisterTenantOnboardingCommandValidator();
        var command = new RegisterTenantOnboardingCommand(
            companyName,
            cnpj,
            subdomain,
            "Agência de Performance",
            "Até R$ 50k",
            SubscriptionTier.Pro,
            "Monthly",
            "Carlos Mendes",
            email,
            "11987654321",
            password);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// Valida que o validador aceita comando com CPF de 11 dígitos numéricos válidos.
    /// </summary>
    [Fact]
    public void Validator_WhenCpfIsValid_ShouldPassValidation()
    {
        // Arrange
        var validator = new RegisterTenantOnboardingCommandValidator();
        var command = new RegisterTenantOnboardingCommand(
            "Consultoria de Tráfego",
            "52998224725",
            "consultoria-trafego",
            "Agência de Performance",
            "Até R$ 50k",
            SubscriptionTier.Pro,
            "Monthly",
            "Carlos Mendes",
            "carlos@consultoria.com",
            "11987654321",
            "Senha@123");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Valida que o handler orquestra o provisionamento com sucesso e retorna a URL e dados do inquilino.
    /// </summary>
    [Fact]
    public async Task Handle_WhenValidInput_ShouldProvisionTenantAndReturnAccessDetails()
    {
        // Arrange
        var expectedTenantId = TenantId.New();
        _provisioningService.ProvisionTenantDatabaseAsync(
            Arg.Any<ProvisionTenantCommand>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<TenantId>.Success(expectedTenantId));

        var handler = new RegisterTenantOnboardingCommandHandler(_provisioningService);
        var command = new RegisterTenantOnboardingCommand(
            "Vanguarda Digital",
            "12345678000195",
            "vanguarda",
            "Agência",
            "R$ 20k a R$ 100k",
            SubscriptionTier.Pro,
            "Monthly",
            "Carlos Mendes",
            "carlos@vanguarda.com",
            "11987654321",
            "Forte#2026!Key");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(expectedTenantId.Value);
        result.Value.Subdomain.Should().Be("vanguarda");
        result.Value.AccessUrl.Should().Contain("vanguarda");
        result.Value.AdminEmail.Should().Be("carlos@vanguarda.com");
    }

    /// <summary>
    /// Valida que falhas da engine de provisionamento são propagadas como Result.Failure.
    /// </summary>
    [Fact]
    public async Task Handle_WhenProvisioningFails_ShouldPropagateFailure()
    {
        // Arrange
        var domainError = Error.Conflict("Tenant.SubdomainAlreadyExists", "Subdomínio já em uso.");
        _provisioningService.ProvisionTenantDatabaseAsync(
            Arg.Any<ProvisionTenantCommand>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<TenantId>.Failure(domainError));

        var handler = new RegisterTenantOnboardingCommandHandler(_provisioningService);
        var command = new RegisterTenantOnboardingCommand(
            "Vanguarda Digital",
            "12345678000195",
            "vanguarda",
            "Agência",
            "R$ 20k",
            SubscriptionTier.Starter,
            "Monthly",
            "Carlos Mendes",
            "carlos@vanguarda.com",
            "11987654321",
            "Forte#2026!Key");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.SubdomainAlreadyExists");
    }

    /// <summary>
    /// Valida que o handler repassa os dados de perfil e identidade visual para o ProvisionTenantCommand.
    /// </summary>
    [Fact]
    public async Task Handle_WhenBrandingAndProfileProvided_ShouldForwardAllFieldsToProvisioningService()
    {
        // Arrange
        ProvisionTenantCommand? capturedCommand = null;
        _provisioningService.ProvisionTenantDatabaseAsync(
            Arg.Do<ProvisionTenantCommand>(cmd => capturedCommand = cmd),
            Arg.Any<CancellationToken>())
            .Returns(Result<TenantId>.Success(TenantId.New()));

        var handler = new RegisterTenantOnboardingCommandHandler(_provisioningService);
        var command = new RegisterTenantOnboardingCommand(
            "Vanguarda Digital",
            "12345678000195",
            "vanguarda",
            "Agência de Performance",
            "R$ 20k a R$ 100k",
            SubscriptionTier.Pro,
            "Monthly",
            "Carlos Mendes",
            "carlos@vanguarda.com",
            "11987654321",
            "Forte#2026!Key",
            CustomDomain: "ads.vanguarda.com.br",
            PrimaryColor: "#4f46e5",
            SecondaryColor: "#0f172a");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Segment.Should().Be("Agência de Performance");
        capturedCommand.MonthlyAdSpendRange.Should().Be("R$ 20k a R$ 100k");
        capturedCommand.BillingCycle.Should().Be("Monthly");
        capturedCommand.CustomDomain.Should().Be("ads.vanguarda.com.br");
        capturedCommand.PrimaryColor.Should().Be("#4f46e5");
        capturedCommand.SecondaryColor.Should().Be("#0f172a");
        capturedCommand.AdminFullName.Should().Be("Carlos Mendes");
        capturedCommand.AdminEmail.Should().Be("carlos@vanguarda.com");
        capturedCommand.AdminPhone.Should().Be("11987654321");
        capturedCommand.AdminPassword.Should().Be("Forte#2026!Key");
    }
}
