using System.Text.RegularExpressions;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
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
    {
        _masterDbContext = masterDbContext;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
    }

    /// <inheritdoc />
    public async Task<Result<TenantId>> ProvisionTenantDatabaseAsync(
        string companyName,
        string cnpj,
        string subdomain,
        CancellationToken cancellationToken)
    {
        var normalizedSubdomain = subdomain.Trim().ToLowerInvariant();

        var subdomainInUse = await _masterDbContext.Tenants
            .AnyAsync(tenant => tenant.Subdomain == normalizedSubdomain, cancellationToken);
        if (subdomainInUse)
        {
            return Result<TenantId>.Failure(
                Error.Conflict("Tenant.SubdomainAlreadyExists", "Subdomain already exists in master catalog."));
        }

        var cnpjInUse = await _masterDbContext.Tenants
            .AnyAsync(tenant => tenant.Cnpj == cnpj, cancellationToken);
        if (cnpjInUse)
        {
            return Result<TenantId>.Failure(
                Error.Conflict("Tenant.CnpjAlreadyExists", "CNPJ already exists in master catalog."));
        }

        var tenantCreationResult = Tenant.Create(companyName, cnpj, subdomain);
        if (tenantCreationResult.IsFailure)
        {
            return Result<TenantId>.Failure(tenantCreationResult.Error);
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

    private static async Task<Result> ApplyTenantSchemaAsync(string tenantConnectionString, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<TenantOperationalDbContext>()
            .UseSqlServer(tenantConnectionString)
            .Options;

        await using var tenantContext = new TenantOperationalDbContext(options);
        await tenantContext.Database.EnsureCreatedAsync(cancellationToken);
        await tenantContext.Database.MigrateAsync(cancellationToken);
        return Result.Success();
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