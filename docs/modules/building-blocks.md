# Especificação de Módulo: BuildingBlocks — Primitives (`Result<T>` & `Error`)

Este documento detalha o funcionamento, convenções, casos de uso e estruturas do kernel compartilhado para retorno de operações e manipulação de erros.

---

## 1. Visão Geral e Propósito

O pacote `BuildingBlocks.Domain.Primitives` provê tipos fundamentais para substituir o uso de exceções como controle de fluxo por retornos tipados explícitos, garantindo que o chamador trate cenários de sucesso e erro sem ambiguidades.

---

## 2. Estrutura de Tipos

### 2.1 `Result` & `Result<TValue>`
- `IsSuccess` (`bool`): Indica se a operação foi executada com êxito.
- `IsFailure` (`bool`): Inverso de `IsSuccess`.
- `Error` (`Error`): Detalhes do erro em caso de falha (`Error.None` se bem-sucedido).
- `Value` (`TValue`): Carga útil resultante da operação. **Lança `InvalidOperationException` se acessado quando `IsFailure == true`.**

### 2.2 `Error` & `ErrorType`
- `Code` (`string`): Código estável, único e legível por máquina (ex.: `Tenant.SubdomainAlreadyExists`).
- `Description` (`string`): Mensagem legível para operadores/usuários.
- `Type` (`ErrorType`): Enum semântico que determina a categoria do erro:
  - `Failure` (0): Falha inesperada ou erro genérico (HTTP 500).
  - `Validation` (1): Violação de regra de formato ou validação de entrada (HTTP 400).
  - `NotFound` (2): Entidade ou recurso não localizado (HTTP 404).
  - `Conflict` (3): Conflito com o estado atual do recurso (HTTP 409).
  - `Unauthorized` (4): Ausência ou invalidade de credenciais de autenticação (HTTP 401).
  - `Forbidden` (5): Usuário autenticado sem privilégios suficientes (HTTP 403).

---

## 3. Exemplos de Uso

### 3.1 Retornando Sucesso e Falha em Handlers / Services
```csharp
public async Task<Result<Guid>> Handle(CreateTenantCommand command, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(command.CompanyName))
        return Result<Guid>.Failure(Error.Validation("Tenant.CompanyNameRequired", "Company name is required."));

    var existing = await _tenantRepository.GetBySubdomainAsync(command.Subdomain, ct);
    if (existing is not null)
        return Result<Guid>.Failure(Error.Conflict("Tenant.SubdomainAlreadyExists", "Subdomain already in use."));

    var tenant = Tenant.Create(command.CompanyName, command.Subdomain);
    await _tenantRepository.AddAsync(tenant, ct);
    await _unitOfWork.CommitAsync(ct);

    return Result<Guid>.Success(tenant.Id);
}
```

### 3.2 Consumindo via Pattern Matching Funcional (`Match`)
```csharp
var response = result.Match(
    onSuccess: tenantId => Results.Ok(new { Id = tenantId }),
    onFailure: error => error.Type switch
    {
        ErrorType.Validation => Results.BadRequest(new { error.Code, error.Description }),
        ErrorType.NotFound => Results.NotFound(new { error.Code, error.Description }),
        ErrorType.Conflict => Results.Conflict(new { error.Code, error.Description }),
        ErrorType.Unauthorized => Results.Unauthorized(),
        ErrorType.Forbidden => Results.Forbid(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    }
);
```

---

## 4. Representação e Payloads JSON

### 4.1 Retorno de Sucesso (`Result<TValue>`)
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": "",
    "description": "",
    "type": 0
  },
  "value": {
    "tenantId": "c39a8e9e-a89e-4e3a-9659-33b63261a8ef",
    "subdomain": "agencia-digital",
    "status": "Active"
  }
}
```

### 4.2 Retorno de Erro (`Result<TValue>`)
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Tenant.SubdomainAlreadyExists",
    "description": "Subdomain already exists in master catalog.",
    "type": 3
  }
}
```

---

## 5. Casos de Borda e Invariantes Testadas

| Cenário | Comportamento Esperado |
| :--- | :--- |
| Instanciar `Result` com sucesso e erro não-nulo | Lança `ArgumentException` ("Successful result cannot contain an error.") |
| Instanciar `Result` com falha e `Error.None` | Lança `ArgumentException` ("Failed result must contain an error.") |
| Acessar `Value` em um `Result<T>` com falha | Lança `InvalidOperationException` ("Cannot access Value when result is a failure.") |
| Executar `Result.Create<T>(null)` | Retorna resultado com falha e `Error.NullValue` |
| Comparar duas instâncias de `Error` com mesmos atributos | Retorna `true` (igualdade por valor via record semantics) |

---

## 6. Tipos Base de Domínio (DDD) — `BuildingBlocks.Domain.Abstractions`

### 6.1 `Entity<TId>`
- **Conceito**: Objeto de domínio cuja identidade é determinada exclusivamente por seu identificador tipado (`TId`).
- **Igualdade**: Duas instâncias de entidade são consideradas iguais se compartilharem o mesmo tipo exato de runtime (ou compatibilidade direta) e o mesmo `Id`.
- **Implementação**: Implementa `IEquatable<Entity<TId>>` e sobrecarrega `Equals`, `GetHashCode`, `==` e `!=`.

```csharp
public sealed class Workspace : Entity<Guid>
{
    public Workspace(Guid id, string name) : base(id)
    {
        Name = name;
    }

    public string Name { get; private set; }
}
```

### 6.2 `AggregateRoot<TId>` & `IDomainEvent`
- **Conceito**: Raiz de agregado que encapsula a consistência transacional de um grupo de entidades e gerencia emissão de eventos de domínio.
- **Buffer de Eventos**: Métodos de negócio invocam `RaiseDomainEvent(IDomainEvent domainEvent)`. Os eventos são acumulados em memória e expostos via `IReadOnlyCollection<IDomainEvent> DomainEvents`.
- **Limpeza**: Após despacho (ex.: em interceptor do EF Core ou UnitOfWork), o pipeline chama `ClearDomainEvents()`.
- **`IDomainEvent`**: Marcador semântico puro com propriedades padrão `EventId` (`Guid`) e `OccurredOnUtc` (`DateTimeOffset`).

```csharp
public sealed class Campaign : AggregateRoot<Guid>
{
    public Campaign(Guid id, string name) : base(id)
    {
        Name = name;
    }

    public void Pause()
    {
        IsPaused = true;
        RaiseDomainEvent(new CampaignPausedDomainEvent(Id, DateTimeOffset.UtcNow));
    }
}
```

### 6.3 `ValueObject`
- **Conceito**: Objeto imutável sem identidade própria, definido integralmente pelo valor de seus atributos.
- **Igualdade Estrutural**: Deriva de `ValueObject` e implementa `protected abstract IEnumerable<object?> GetEqualityComponents()`.
- **Invariantes**: Suporta componentes nulos, tipos numéricos, enumerações e coleções. Implementa `IEquatable<ValueObject>` com operadores `==` e `!=`.

```csharp
public sealed class BudgetLimit : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public BudgetLimit(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

---

## 7. Contratos de Persistência — `BuildingBlocks.Application.Persistence`

### 7.1 `IRepository<TEntity, TId>`
- Contrato genérico estrito para agregados: `where TEntity : AggregateRoot<TId> where TId : notnull`.
- Métodos disponíveis:
  - `Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);`
  - `Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);`
  - `void Update(TEntity entity);` (marca agregação para atualização no rastreador)
  - `void Remove(TEntity entity);` (marca agregação para remoção)

### 7.2 `IUnitOfWork`
- Fronteira atômica de persistência. Coordena a gravação de todas as operações pendentes em uma transação única:
  - `Task<int> CommitAsync(CancellationToken cancellationToken = default);`
- Retorna o número de registros afetados na base de dados.

---

## 8. Subsistema de MultiTenancy — Resolução Dinâmica de Contexto

Este subsistema gerencia a extração, identificação e propagação do inquilino (Tenant) ativo por requisição HTTP.

### 8.1 Contratos (`BuildingBlocks.Application.MultiTenancy`)
- **`ITenantContext`**: Interface somente leitura injetada por escopo (`Scoped`) nos serviços e handlers de negócio:
  - `Guid? TenantId`: Identificador único do tenant (quando fornecido via GUID).
  - `string? Subdomain`: Subdomínio ou slug alfanumérico do tenant.
  - `string? RawIdentifier`: Valor bruto extraído do canal de entrada.
  - `TenantResolutionSource Source`: Canal de origem (`None`, `Header`, `JwtClaim`, `Subdomain`).
  - `bool IsResolved`: Indica se o tenant foi validado e está presente no contexto atual.
- **`ITenantContextAccessor`**: Provedor do contexto no escopo da requisição.

### 8.2 Canais de Extração (`BuildingBlocks.Infrastructure.MultiTenancy.Strategies`)
1. **Header HTTP (`HeaderTenantIdentificationStrategy`)**:
   - Header configurável (`X-Tenant-Id`). Suporta tanto GUID quanto slug/subdomínio.
2. **Claim JWT (`JwtClaimTenantIdentificationStrategy`)**:
   - Extrai do usuário autenticado a claim `tenant_id` ou o schema padrão Microsoft (`http://schemas.microsoft.com/identity/claims/tenantid`).
3. **Subdomínio / CNAME (`SubdomainTenantIdentificationStrategy`)**:
   - Extrai o subdomínio da URL (ex.: `agencia-alfa.admetricspro.com` ou `cliente.localhost:5000`) baseado em lista configurável de `BaseDomains`.
   - Ignora automaticamente subdomínios reservados (`www`, `api`, `admin`, `app`, `mail`).

### 8.3 Middleware e Registro
- **`TenantIdentificationMiddleware`**: Executa a cadeia de estratégias em ordem de precedência configurável (`Header` > `JwtClaim` > `Subdomain`) e popula o `ITenantContextAccessor`.
- **Extensões de DI**:
  - `services.AddMultiTenancy(options => ...)`: Registra os serviços como `Scoped` e estratégias como `Transient`.
  - `app.UseTenantResolution()`: Adiciona o middleware ao pipeline ASP.NET Core.

---

## 9. Persistência Multi-Tenant Dinâmica (Database-per-Tenant)

### 9.1 `ITenantConnectionResolver` (`BuildingBlocks.Application.MultiTenancy`)
- Contrato central para obter a string de conexão decifrada do banco de dados operacional de cada tenant.
- Métodos disponíveis:
  - `Task<Result<string>> ResolveConnectionStringAsync(Guid tenantId, CancellationToken cancellationToken = default);`
  - `Task<Result<string>> ResolveConnectionStringBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);`
  - `Task<Result<string>> ResolveCurrentTenantConnectionStringAsync(CancellationToken cancellationToken = default);`
  - `void InvalidateCache(Guid tenantId);`
  - `void InvalidateCacheBySubdomain(string subdomain);`

### 9.2 `CachedTenantConnectionResolver` (`Master.Infrastructure.Services`)
- Implementa `ITenantConnectionResolver` com cache em memória seguro (`IMemoryCache`), desacoplando as requisições recorrentes do `MasterDb`.
- Decodifica o payload `EncryptedConnectionString` em repouso com `IEncryptionService` (AES-256-CBC com IV randômico).
- Retém a string decifrada por até 30 minutos de inatividade (Sliding) e 4 horas absolutas.

### 9.3 `TenantDbContext` & `ITenantDbContextFactory<TContext>` (`BuildingBlocks.Infrastructure.Persistence`)
- **`TenantDbContext`**: Contexto base para bancos de dados isolados por inquilino, contendo a tabela de verificação `TenantSchemaMarkers`.
- **`ITenantDbContextFactory<TContext>`**: Fábrica dinâmica que instancia DbContexts operacionais conectando ao banco correto com 100% de suporte assíncrono:
  - `Task<Result<TContext>> CreateDbContextAsync(CancellationToken cancellationToken = default);`
  - `Task<Result<TContext>> CreateDbContextAsync(Guid tenantId, CancellationToken cancellationToken = default);`
- **`ITenantConnectionHolder`**: Armazenamento com escopo de requisição (`Scoped`) para reaproveitamento imediato da connection string no mesmo ciclo HTTP.



