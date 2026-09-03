# Documentação Técnica: Catálogo Central MasterDb e Migrações Globais (Subfase 1.1)

## 1. Visão Geral

O **MasterDb** é o banco de dados relacional central do AdMetricsPro responsável pela governança de tenants, assinaturas, isolamento de bancos operacionais e auditoria corporativa. Ele é completamente desacoplado dos bancos de dados individuais de cada tenant e gerencia o catálogo através do contexto `MasterDbContext` (.NET 10 / EF Core 10).

---

## 2. Esquema Relacional e Mapeamento (`MasterDbContext`)

A tabela principal é `Tenants`, com chave primária baseada em `TenantId` (GUID fortemente tipado) e índices únicos estritos para prevenção de duplicidade cadastral e colisões de subdomínio.

### Tabela: `Tenants`

| Coluna | Tipo SQL Server | Nulável | Descrição & Constraints |
| :--- | :--- | :--- | :--- |
| `Id` | `UNIQUEIDENTIFIER` | Não | Chave Primária (PK). Mapeada a partir de `TenantId`. |
| `CompanyName` | `NVARCHAR(200)` | Não | Razão Social ou Nome Fantasia da empresa. Obrigatório. |
| `Cnpj` | `NVARCHAR(14)` | Não | CNPJ sanitizado (14 dígitos numéricos). Índice Único (`IX_Tenants_Cnpj`). |
| `Subdomain` | `NVARCHAR(80)` | Não | Subdomínio normalizado (lowercase, sem espaços). Índice Único (`IX_Tenants_Subdomain`). |
| `EncryptedConnectionString` | `NVARCHAR(2000)` | Não | Connection string do banco dedicado criptografada com algoritmo simétrico AES-256. |
| `Status` | `NVARCHAR(30)` | Não | Enum serializado como string: `Trial`, `Active`, `Suspended`, `Cancelled`. |
| `Tier` | `NVARCHAR(30)` | Não | Enum serializado como string: `Trial`, `Starter`, `Pro`, `Enterprise`. |
| `SubscriptionExpiresAtUtc` | `DATETIME2` | Sim | Data e hora UTC de expiração do trial ou ciclo de cobrança. Nulo para planos contínuos. |
| `CreatedAtUtc` | `DATETIME2` | Não | Carimbo de data/hora UTC de criação do registro no catálogo. |

### Índices e Restrições de Integridade

- `PK_Tenants`: Primary key em `Id`.
- `IX_Tenants_Cnpj`: Unique Index em `Cnpj` (`CREATE UNIQUE INDEX IX_Tenants_Cnpj ON Tenants (Cnpj)`).
- `IX_Tenants_Subdomain`: Unique Index em `Subdomain` (`CREATE UNIQUE INDEX IX_Tenants_Subdomain ON Tenants (Subdomain)`).

---

## 3. Modelo de Domínio (`Tenant`) e Encapsulamento

A classe `Tenant` no namespace `Master.Domain.Tenants` é uma raiz de agregação (`AggregateRoot<TenantId>`) que encapsula o ciclo de vida e garante invariantes:

### Construtores e Fábricas Estáticas
- `private Tenant(...)`: Construtor parametrizado privado para criação via fábrica de domínio.
- `private Tenant()`: Construtor protegido/privado sem parâmetros para materialização do EF Core.
- `public static Result<Tenant> Create(...)`: Método de fábrica estático que valida obrigatoriedade de campos, comprimento de CNPJ (14 dígitos numéricos) e formato do subdomínio antes da instanciação.

### Métodos de Mutação de Estado (Padrão `Result`)
- `UpgradeSubscription(SubscriptionTier newTier, DateTime? expiresAtUtc)`: Atualiza o plano e a nova data de expiração.
- `ExtendTrial(DateTime newExpirationUtc)`: Estende o período de avaliação, exigindo data futura em UTC.
- `Suspend(string reason)`: Suspende o acesso do tenant exigindo justificativa formal.
- `Reactivate()`: Restaura o status do tenant para `Active`.
- `SetEncryptedConnectionString(string encryptedConnectionString)`: Associa a credencial cifrada do banco operacional provisionado.

---

## 4. Pipeline de Migrações Automáticas (`IMasterDatabaseMigrationRunner`)

O catálogo central possui um executor de migrações dedicado que encapsula a execução do `Database.MigrateAsync()` do EF Core 10 com observabilidade e tratamento seguro de erros via `Result`.

### Ciclo de Execução e Idempotência
1. O runner obtém uma instância de `MasterDbContext`.
2. Invoca `Database.MigrateAsync(cancellationToken)`.
3. O EF Core adquire trava de sessão de migração (`sp_getapplock`) e verifica a tabela `__EFMigrationsHistory`.
4. Aplica apenas migrações pendentes de forma sequencial e idempotente.
5. Em caso de cancelamento ou falha de rede/credenciais com o SQL Server, a exceção é interceptada e convertida para `Result.Failure(Error.Failure("MasterMigration.ExecutionFailed", ...))` com log estruturado, prevenindo falhas silenciosas ou crashes não controlados da aplicação.

### Injeção de Dependência e Startup Hooks

No assembly `Master.Infrastructure`:
```csharp
// Registro do DbContext, repositório, Unit of Work e runner de migração
services.AddMasterCatalog(connectionString);

// Execução no startup do host da WebApi (Program.cs)
if (app.Configuration.GetValue<bool>("DatabaseMigrations:ApplyMasterMigrationsOnStartup", false))
{
    var migrationResult = await app.ApplyMasterDatabaseMigrationsAsync();
    if (migrationResult.IsFailure)
    {
        // Tratamento e log estruturado
    }
}
```

---

## 5. Exemplo de Estrutura de Retorno do Runner de Migração

### Sucesso
```json
{
  "isSuccess": true,
  "error": {
    "code": "",
    "description": ""
  }
}
```

### Falha Controlada
```json
{
  "isSuccess": false,
  "error": {
    "code": "MasterMigration.ExecutionFailed",
    "description": "Failed to apply master database migrations: A network-related or instance-specific error occurred while establishing a connection to SQL Server."
  }
}
```

---

## 6. Erros de Domínio e Validação Mapeados

- `Tenant.CompanyNameRequired`: Razão Social não preenchida ou vazia.
- `Tenant.InvalidCnpj`: CNPJ diferente de 14 dígitos numéricos.
- `Tenant.InvalidSubdomain`: Subdomínio vazio ou contendo caracteres de espaço em branco.
- `Tenant.EncryptedConnectionStringRequired`: Tentativa de persistir connection string vazia.
- `Tenant.InvalidExpirationDate`: Data de extensão de trial informada no passado.
- `Tenant.SuspensionReasonRequired`: Tentativa de suspensão de tenant sem fornecer justificativa.
- `MasterMigration.ExecutionFailed`: Falha de conexão, deadlock ou erro de sintaxe durante o processo de migração do EF Core.
