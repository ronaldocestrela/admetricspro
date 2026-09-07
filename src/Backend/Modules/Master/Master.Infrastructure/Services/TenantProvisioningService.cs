using System.Text.RegularExpressions;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Security;
using Master.Application.Repositories;
using Master.Application.Services;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Services;

/// <summary>
/// Provisions dedicated SQL Server databases for tenants and stores encrypted connection metadata.
/// </summary>
public sealed partial class TenantProvisioningService : ITenantProvisioningService
{
    private readonly MasterDbContext _masterDbContext;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProvisioningService"/> class.
    /// </summary>
    /// <param name="masterDbContext">Master catalog context.</param>
    /// <param name="tenantRepository">Tenant repository abstraction.</param>
    /// <param name="unitOfWork">Unit of work for commit coordination.</param>
    /// <param name="encryptionService">Encryption service for connection string storage.</param>
    public TenantProvisioningService(
        MasterDbContext masterDbContext,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IEncryptionService encryptionService)
        : this(masterDbContext, tenantRepository, unitOfWork, encryptionService, new PasswordHasher())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProvisioningService"/> class with cryptographic password hasher.
    /// </summary>
    /// <param name="masterDbContext">Master catalog context.</param>
    /// <param name="tenantRepository">Tenant repository abstraction.</param>
    /// <param name="unitOfWork">Unit of work for commit coordination.</param>
    /// <param name="encryptionService">Encryption service for connection string storage.</param>
    /// <param name="passwordHasher">Cryptographic password hasher for operational tenant user accounts.</param>
    public TenantProvisioningService(
        MasterDbContext masterDbContext,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IEncryptionService encryptionService,
        IPasswordHasher passwordHasher)
    {
        _masterDbContext = masterDbContext;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    /// <inheritdoc />
    public Task<Result<TenantId>> ProvisionTenantDatabaseAsync(
        string companyName,
        string cnpj,
        string subdomain,
        CancellationToken cancellationToken)
    {
        return ProvisionTenantDatabaseAsync(
            new ProvisionTenantCommand(companyName, cnpj, subdomain),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<TenantId>> ProvisionTenantDatabaseAsync(
        ProvisionTenantCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null)
        {
            return Result<TenantId>.Failure(
                Error.Validation("Tenant.CommandRequired", "Provisioning command cannot be null."));
        }

        var tenantCreationResult = Tenant.Create(
            command.CompanyName,
            command.Cnpj,
            command.Subdomain,
            command.Tier,
            subscriptionExpiresAtUtc: null,
            segment: command.Segment,
            monthlyAdSpendRange: command.MonthlyAdSpendRange,
            billingCycle: command.BillingCycle,
            customDomain: command.CustomDomain,
            primaryColor: command.PrimaryColor,
            secondaryColor: command.SecondaryColor);

        if (tenantCreationResult.IsFailure)
        {
            return Result<TenantId>.Failure(tenantCreationResult.Error);
        }

        var normalizedSubdomain = (command.Subdomain ?? string.Empty).Trim().ToLowerInvariant();

        var subdomainInUse = await _masterDbContext.Tenants
            .AnyAsync(tenant => tenant.Subdomain == normalizedSubdomain, cancellationToken);
        if (subdomainInUse)
        {
            return Result<TenantId>.Failure(
                Error.Conflict("Tenant.SubdomainAlreadyExists", "Subdomain already exists in master catalog."));
        }

        var cnpjInUse = await _masterDbContext.Tenants
            .AnyAsync(tenant => tenant.Cnpj == command.Cnpj, cancellationToken);
        if (cnpjInUse)
        {
            return Result<TenantId>.Failure(
                Error.Conflict("Tenant.CnpjAlreadyExists", "CNPJ already exists in master catalog."));
        }

        var tenant = tenantCreationResult.Value;
        var sanitizedDatabaseName = BuildTenantDatabaseName(normalizedSubdomain);

        var tenantDbConnectionString = BuildTenantConnectionString(_masterDbContext.Database.GetConnectionString(), sanitizedDatabaseName);
        if (string.IsNullOrWhiteSpace(tenantDbConnectionString))
        {
            return Result<TenantId>.Failure(
                Error.Validation("Tenant.ConnectionStringUnavailable", "Master connection string must be configured."));
        }

        var createDbResult = await CreateDatabaseIfNotExistsAsync(tenantDbConnectionString, sanitizedDatabaseName, cancellationToken);
        if (createDbResult.IsFailure)
        {
            return Result<TenantId>.Failure(createDbResult.Error);
        }

        var applySchemaResult = await ApplyTenantSchemaAsync(tenantDbConnectionString, cancellationToken);
        if (applySchemaResult.IsFailure)
        {
            return Result<TenantId>.Failure(applySchemaResult.Error);
        }

        var seedResult = await SeedTenantInitialAdminAsync(tenantDbConnectionString, command, cancellationToken);
        if (seedResult.IsFailure)
        {
            return Result<TenantId>.Failure(seedResult.Error);
        }

        var encryptedConnectionString = _encryptionService.Encrypt(tenantDbConnectionString);
        var setConnectionStringResult = tenant.SetEncryptedConnectionString(encryptedConnectionString);
        if (setConnectionStringResult.IsFailure)
        {
            return Result<TenantId>.Failure(setConnectionStringResult.Error);
        }

        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<TenantId>.Success(tenant.Id);
    }

    private static async Task<Result> CreateDatabaseIfNotExistsAsync(
        string tenantDbConnectionString,
        string databaseName,
        CancellationToken cancellationToken)
    {
        var connectionBuilder = new SqlConnectionStringBuilder(tenantDbConnectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(connectionBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT COUNT(1) FROM sys.databases WHERE name = @databaseName";
        existsCommand.Parameters.AddWithValue("@databaseName", databaseName);

        var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken), null) > 0;
        if (exists)
        {
            return Result.Failure(Error.Conflict("Tenant.DatabaseAlreadyExists", "A database already exists for the requested tenant."));
        }

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE [{databaseName}]";
        await createCommand.ExecuteNonQueryAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Semeia o usuário administrador inicial (Owner) e a identidade visual (TenantBranding) no banco de dados operacional dedicado do tenant.
    /// </summary>
    /// <param name="tenantConnectionString">String de conexão com o banco de dados dedicado do tenant.</param>
    /// <param name="command">Comando estruturado de provisionamento contendo credenciais e branding.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Retorna <see cref="Result.Success()"/> em caso de sucesso ou falha semântica tipada.</returns>
    public async Task<Result> SeedTenantInitialAdminAsync(
        string tenantConnectionString,
        ProvisionTenantCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var options = new DbContextOptionsBuilder<TenantOperationalDbContext>()
                .UseSqlServer(tenantConnectionString)
                .Options;

            await using var tenantContext = new TenantOperationalDbContext(options);
            return await SeedTenantInitialAdminAsync(tenantContext, command, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(Error.Failure("Tenant.SeedCancelled", "Tenant database seeding was cancelled."));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Tenant.SeedFailed", $"Failed to seed tenant initial admin or branding: {ex.Message}"));
        }
    }

    /// <summary>
    /// Executa o semeamento de registros operacionais iniciais no contexto dedicado do tenant de forma idempotente.
    /// </summary>
    /// <param name="tenantContext">Contexto operacional do tenant.</param>
    /// <param name="command">Comando estruturado com dados do inquilino.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado semântico da operação.</returns>
    public async Task<Result> SeedTenantInitialAdminAsync(
        TenantOperationalDbContext tenantContext,
        ProvisionTenantCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Seed TenantBranding se ainda não existir
            var brandingExists = await tenantContext.TenantBranding.AnyAsync(cancellationToken);
            if (!brandingExists)
            {
                var primaryColor = string.IsNullOrWhiteSpace(command.PrimaryColor) ? "#4F46E5" : command.PrimaryColor.Trim();
                var secondaryColor = string.IsNullOrWhiteSpace(command.SecondaryColor) ? "#0F172A" : command.SecondaryColor.Trim();

                var brandingResult = TenantBranding.Create(
                    Guid.NewGuid(),
                    primaryColor,
                    secondaryColor);

                if (brandingResult.IsFailure)
                {
                    return Result.Failure(brandingResult.Error);
                }

                await tenantContext.TenantBranding.AddAsync(brandingResult.Value, cancellationToken);
            }

            // 2. Seed TenantUser (Owner) se email e senha foram fornecidos
            if (!string.IsNullOrWhiteSpace(command.AdminEmail) && !string.IsNullOrWhiteSpace(command.AdminPassword))
            {
                var normalizedEmail = command.AdminEmail.Trim().ToLowerInvariant();
                var userExists = await tenantContext.TenantUsers.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

                if (!userExists)
                {
                    var passwordHash = _passwordHasher.HashPassword(command.AdminPassword);
                    var fullName = string.IsNullOrWhiteSpace(command.AdminFullName)
                        ? command.CompanyName.Trim()
                        : command.AdminFullName.Trim();
                    var phone = string.IsNullOrWhiteSpace(command.AdminPhone)
                        ? null
                        : command.AdminPhone.Trim();

                    var userResult = TenantUser.Create(
                        Guid.NewGuid(),
                        fullName,
                        normalizedEmail,
                        phone,
                        passwordHash,
                        TenantRole.Owner,
                        createdAtUtc: DateTime.UtcNow);

                    if (userResult.IsFailure)
                    {
                        return Result.Failure(userResult.Error);
                    }

                    await tenantContext.TenantUsers.AddAsync(userResult.Value, cancellationToken);
                }
            }

            await tenantContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(Error.Failure("Tenant.SeedCancelled", "Tenant database seeding was cancelled."));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Tenant.SeedFailed", $"Failed to seed initial tenant data: {ex.Message}"));
        }
    }

    private static async Task<Result> ApplyTenantSchemaAsync(string tenantConnectionString, CancellationToken cancellationToken)
    {
        try
        {
            var options = new DbContextOptionsBuilder<TenantOperationalDbContext>()
                .UseSqlServer(tenantConnectionString)
                .Options;

            await using var tenantContext = new TenantOperationalDbContext(options);
            await tenantContext.Database.MigrateAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(Error.Failure("Tenant.MigrationFailed", "Tenant database migration was cancelled."));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Tenant.MigrationFailed", $"Failed to apply tenant database schema migration: {ex.Message}"));
        }
    }

    private static string BuildTenantDatabaseName(string subdomain)
    {
        var rawName = string.IsNullOrWhiteSpace(subdomain) ? "tenant" : subdomain;
        var sanitized = NonWordCharsRegex().Replace(rawName, string.Empty);
        sanitized = string.IsNullOrWhiteSpace(sanitized) ? "tenant" : sanitized;
        return $"Tenant_{sanitized}";
    }

    private static string BuildTenantConnectionString(string? masterConnectionString, string databaseName)
    {
        if (string.IsNullOrWhiteSpace(masterConnectionString))
        {
            return string.Empty;
        }

        var builder = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = databaseName
        };

        return builder.ConnectionString;
    }

    [GeneratedRegex("[^a-zA-Z0-9_]+", RegexOptions.Compiled)]
    private static partial Regex NonWordCharsRegex();
}