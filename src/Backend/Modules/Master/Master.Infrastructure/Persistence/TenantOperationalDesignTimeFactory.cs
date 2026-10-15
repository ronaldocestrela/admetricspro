using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Master.Infrastructure.Persistence;

/// <summary>Permite gerar migrações do schema operacional sem provisionar um tenant ou iniciar jobs.</summary>
public sealed class TenantOperationalDesignTimeFactory : IDesignTimeDbContextFactory<TenantOperationalDbContext>
{
    /// <summary>Cria somente metadados de modelo para as ferramentas EF.</summary>
    /// <param name="args">Argumentos da ferramenta.</param>
    /// <returns>Contexto de design sem executar migrações ou conexões.</returns>
    public TenantOperationalDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<TenantOperationalDbContext>()
            .UseSqlServer("Server=localhost;Database=AdMetricsPro_DesignOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);
}
