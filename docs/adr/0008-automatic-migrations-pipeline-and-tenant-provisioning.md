# ADR 0008: Pipeline de Migrações Automáticas do Catálogo Master e Provisionamento de Bancos de Tenant

## Status

Aceito

## Contexto

A plataforma adota arquitetura **Monólito Modular** em **.NET 10** com isolamento físico **Database-per-Tenant** em SQL Server. Nessa topologia:
1. O banco central de catálogo (`MasterDb`) armazena os registros cadastrais dos tenants, assinaturas, subdomínios e credenciais cifradas.
2. Cada inquilino ativo possui uma base de dados SQL Server dedicada contendo tabelas de negócio e metadados de schema.

Durante o ciclo de vida operacional:
- Na inicialização do host da aplicação (Startup / Boot), o `MasterDb` deve aplicar automaticamente quaisquer migrações pendentes do catálogo sem exigir scripts manuais ou pipelines externos fora de banda, assegurando idempotência e tolerância a reinicializações.
- No momento do provisionamento de um novo inquilino (`Tenant`), a criação do banco físico deve ser sucedida imediatamente pelo disparo do comando `tenantContext.Database.MigrateAsync()`, garantindo que a base de dados nasça com todas as migrações operacionais aplicadas e devidamente registradas na tabela `__EFMigrationsHistory` antes de liberar o acesso.

O uso de `EnsureCreatedAsync` foi identificado como inadequado, pois ele ignora o pipeline de versionamento do Entity Framework Core e não gera o histórico em `__EFMigrationsHistory`, provocando falhas em migrações incrementais futuras.

## Decisão

Adotamos a seguinte arquitetura de migrações automáticas:

1. **Separação Física de Migrações por Bounded Context:**
   - As migrações do catálogo master residem em `Master.Infrastructure/Persistence/Migrations/MasterCatalog/`.
   - As migrações operacionais de inquilinos residem em `Master.Infrastructure/Persistence/Migrations/TenantOperational/`.
   - Cada contexto mantém seu próprio `ModelSnapshot` e histórico formal.

2. **Runner de Migração do Master (`IMasterDatabaseMigrationRunner`):**
   - Contrato na camada de aplicação: `IMasterDatabaseMigrationRunner.ApplyMigrationsAsync(CancellationToken)`.
   - Implementado em `MasterDatabaseMigrationRunner` injetando `MasterDbContext` e `ILogger`.
   - Aplica `await _masterDbContext.Database.MigrateAsync(cancellationToken)`.
   - Não propaga exceções não tratadas para controle de fluxo: falhas técnicas (falha de conexão, timeout, cancelamento) são mapeadas no envelope padrão `Result.Failure(Error.Unexpected("MasterMigration.ExecutionFailed", ...))`.
   - Disponibilizado via métodos de extensão para registro de DI (`AddMasterDatabaseMigration`) e execução programática no startup (`host.ApplyMasterDatabaseMigrationsAsync()`) e via `MasterDatabaseMigrationHostedService`.

3. **Provisionamento de Tenant com `MigrateAsync` Obrigatório:**
   - Em `TenantProvisioningService`, a etapa `ApplyTenantSchemaAsync` executa exclusivamente `await tenantContext.Database.MigrateAsync(cancellationToken)`.
   - A chamada a `EnsureCreatedAsync` foi terminantemente removida.
   - O banco físico dedicado é instanciado previamente via `CREATE DATABASE` no SQL Server caso não exista, e em seguida `MigrateAsync()` cria o schema e registra o estado na tabela `__EFMigrationsHistory`.
   - Qualquer falha no pipeline de migração do tenant retorna `Result.Failure(Error.Unexpected("Tenant.MigrationFailed", ...))`, impedindo que tenants inconsistentes sejam liberados.

## Consequências

### Positivas:
- **Idempotência e Rastreabilidade:** Tanto o `MasterDb` quanto as bases dedicadas dos tenants possuem auditoria formal de migrações gerenciada pelo EF Core via `__EFMigrationsHistory`.
- **Conformidade Estrita com `Result<T>`:** Falhas no pipeline de migração não derrubam silenciosamente o processo nem utilizam exceções não tratadas como regra de fluxo de negócio.
- **Isolamento Modular:** O módulo `Master` mantém o isolamento das credenciais e a responsabilidade exclusiva pelo provisionamento do catálogo e dos bancos de clientes.
- **Preparado para Alta Escala:** Novos assinantes podem ser criados de forma dinâmica sob demanda com garantia de que seu banco já entra em produção com schema 100% atualizado.

### Negativas / Mitigações:
- **Tempo de Provisionamento:** O processo de criação de banco e aplicação de migrations via DDL no SQL Server leva centenas de milissegundos a poucos segundos. Esse tempo é inerente ao provisionamento isolado e aceitável para fluxos assíncronos de cadastro/onboarding de tenants.
