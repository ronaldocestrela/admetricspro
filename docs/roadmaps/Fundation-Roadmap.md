# Roadmap de Fundação do Projeto: Setup & Infraestrutura Core

Este documento detalha o passo a passo técnico da **Fase 0 (Fundação)** para a criação da plataforma SaaS de Gestão Unificada de Tráfego Pago, atendendo a todas as diretrizes do `AGENTS.md` (.NET 10, Blazor Server, Monólito Modular, SQL Server com isolamento database-per-tenant, Repository Pattern, `Result<T>`, TDD estrito e Documentação Viva obrigatória).

---

## Tabela Resumo das Fases de Fundação

| Fase | Objetivo Central | Projetos / Diretórios Impactados | Entregável Principal |
| :--- | :--- | :--- | :--- |
| **Fundação 1** | Setup Estrutural da Solução e Governança | `Root`, `docs/`, `Directory.Build.props`, `.editorconfig` | Estrutura monólito modular em pastas separadas e validação estrita de compilação. |
| **Fundação 2** | BuildingBlocks (Kernel Compartilhado) | `BuildingBlocks.Domain`, `BuildingBlocks.Application` | Pattern `Result<T>`, tipos base DDD e contratos globais via TDD estrito. |
| **Fundação 3** | Isolamento Multi-Tenant & SQL Server | `BuildingBlocks.Infrastructure`, `Master.Infrastructure` | Engine *Database-per-Tenant* com resolução dinâmica de contexto e migrações. |
| **Fundação 4** | Setup do Host WebApi & Documentação Viva | `WebApi`, `docs/` | ASP.NET Core 10 com OpenAPI + Scalar em `/scalar/v1` e comentários XML obrigatórios. |
| **Fundação 5** | Setup do Frontend Blazor Server (.NET 10) | `Frontend.WebApp`, `UnitTests.Frontend` | Layout base corporativo com bUnit configurado e injeção do contexto do tenant. |

---

## Fase 1: Setup Estrutural da Solução, Governança e Tooling [Concluída]

### Subfase 1.1: Criação da Solução e Diretórios Físicos [Concluída]
* **1.1.1:** Inicializar a solução principal em .NET 10 LTS (`AdMetricsPro.sln`).
* **1.1.2:** Criar as separações físicas de diretórios:
  * `src/Backend/`
  * `src/Frontend/`
  * `tests/`
  * `docs/` (com subdiretórios `adr/` e `modules/`)
* **1.1.3:** Configurar o arquivo `Directory.Build.props` ativando **Central Package Management (CPM)** para centralização estrita de versões de dependências NuGet em toda a solução.
* **1.1.4:** Adicionar travas de compilação no `Directory.Build.props`:
```xml
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>

```

*(Isso obriga que qualquer aviso de falta de documentação XML `<summary>` quebre o build).*

* **1.1.5:** Configurar `.editorconfig` padronizando regras de formatação C#, regras de indentação e padrões semânticos de nomenclatura.

---

## Fase 2: BuildingBlocks (Kernel Compartilhado) via TDD [Concluída]

Construção das abstrações essenciais e tipos utilitários sem acoplamento externo.

### Subfase 2.1: Implementação do Padrão `Result<T>` e Erros Fortemente Tipados [Concluída]

* **2.1.1 (TDD - Red):** Escrever testes unitários em `tests/UnitTests/Backend/ResultTests.cs` cobrindo:
* Criação de instâncias de sucesso contendo payload.
* Criação de instâncias de falha contendo tipo semântico de erro.
* Invariante de que um resultado com erro não pode expor valor (deve lançar exceção ao tentar acessar valor nulo/inválido).
* **2.1.2 (TDD - Green):** Implementar as estruturas `Result` e `Result<T>` no namespace `BuildingBlocks.Domain.Primitives`.
* **2.1.3:** Implementar a estrutura `Error` com métodos de fábrica semânticos:
* `Error.Validation(code, description)`
* `Error.NotFound(code, description)`
* `Error.Conflict(code, description)`
* `Error.Unauthorized(code, description)`
* **2.1.4 (Documentação Viva):** Adicionar comentários XML com `<summary>`, `<param>` e `<returns>` em todas as classes de resultado e erros.

### Subfase 2.2: Tipos Base de Domínio (DDD) e Contratos [Concluída]

* **2.2.1 (TDD - Concluído):** Implementar classes abstratas `Entity<TId>`, `AggregateRoot<TId>` e manipulação desacoplada de `IDomainEvent` com metadados `EventId` e `OccurredOnUtc`.
* **2.2.2 (TDD - Concluído):** Implementar classe base `ValueObject` com `IEquatable<ValueObject>`, sobrecarga de comparadores (`==`, `!=`) e suporte a componentes nulos.
* **2.2.3 (TDD - Concluído):** Definir os contratos genéricos de persistência:
  * `IRepository<TEntity, TId>` (com métodos `AddAsync`, `GetByIdAsync`, `Update` e `Remove`, vinculados a agregados e com suporte a `CancellationToken`).
  * `IUnitOfWork` (com método `CommitAsync(CancellationToken cancellationToken = default)`).
* **2.2.4 (Documentação Viva - Concluído):** Atualização de `docs/modules/building-blocks.md` e criação do ADR `docs/adr/0005-ddd-base-abstractions-and-persistence-contracts.md`.

---

## Fase 3: Isolamento Multi-Tenant & SQL Server (Database-per-Tenant) [Concluída]

### Subfase 3.1: Resolução de Tenant Dinâmica [Concluída]

* **3.1.1 (TDD - Concluído):** Escrever testes para o middleware de tenant validando os canais de extração de identidade:
  * Leitura via CNAME/Subdomínio (ex.: `agencia-alfa.app.com` e hosts de desenvolvimento).
  * Leitura via Header HTTP `X-Tenant-Id` (com suporte a GUID e slug).
  * Leitura via Claim em Token JWT (`tenant_id` e standard Microsoft).
* **3.1.2 (TDD - Concluído):** Implementar o serviço `TenantContextAccessor` com interface `ITenantContext` injetada por escopo (`Scoped`) e propagação assíncrona segura.
* **3.1.3 (Documentação Viva - Concluído):** Criação do ADR `docs/adr/0006-dynamic-tenant-resolution-pipeline.md` e atualização de `docs/modules/building-blocks.md`.

### Subfase 3.2: Persistência com EF Core 10 e Catálogo Master [Concluída]

* **3.2.1 (TDD - Concluído):** Configurar `MasterDbContext` com a entidade `Tenant` mapeada no SQL Server, contendo chaves, dados de assinatura (`SubscriptionTier`, `SubscriptionExpiresAtUtc`) e credencial de conexão criptografada.
* **3.2.2 (TDD - Concluído):** Implementar serviço de criptografia simétrica AES-256 (`AesEncryptionService`) para gravação segura das Connection Strings dos bancos de inquilino com cobertura unitária exaustiva.
* **3.2.3 (TDD - Concluído):** Implementar contrato `ITenantConnectionResolver`, implementação com cache seguro `CachedTenantConnectionResolver` e fábrica dinâmica `ITenantDbContextFactory<TenantDbContext>` para instanciação assíncrona do `TenantDbContext` contextual.
* **3.2.4 (Documentação Viva - Concluído):** Criação do ADR `docs/adr/0007-tenant-connection-resolver-and-dynamic-dbcontext.md` e atualização de `docs/modules/backoffice-master-db.md` e `docs/modules/building-blocks.md`.

### Subfase 3.3: Pipeline de Migrações Automáticas [Concluída]

* **3.3.1 (TDD - Concluído):** Configurar hook de inicialização do MasterDb via `IMasterDatabaseMigrationRunner`, extensões `ApplyMasterDatabaseMigrationsAsync()` e hosted service `MasterDatabaseMigrationHostedService` aplicando `masterContext.Database.MigrateAsync()` com pattern `Result`.
* **3.3.2 (TDD - Concluído):** Implementar/refatorar o serviço `ITenantProvisioningService` criando dinamicamente o banco dedicado no SQL Server e disparando estritamente `tenantContext.Database.MigrateAsync()` (gerando `__EFMigrationsHistory` nativo) no provisionamento de novos inquilinos.
* **3.3.3 (Documentação Viva - Concluído):** Criação do ADR `docs/adr/0008-automatic-migrations-pipeline-and-tenant-provisioning.md` e atualização da especificação em `docs/modules/backoffice-master-db.md`.

---

## Fase 4: Setup da WebApi, In-Memory Mediator & OpenAPI/Scalar

### Subfase 4.1: Mensageria In-Memory (MediatR) [Concluída]

* **4.1.1 (Concluído):** Configurar contratos CQRS (`ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>`, `IDomainEventHandler<TEvent>`) e injeção de dependência do `MediatR` com logging e descoberta automática via extensão fluente `AddMessaging()`.
* **4.1.2 (TDD - Concluído):** Implementar pipeline behavior genérico `ValidationBehavior<TRequest, TResponse>` para validação de entrada usando `FluentValidation`, convertendo automaticamente quebras de regras em envelopes `Result<T>.Failure(ValidationError)` sem lançar exceções.
* **4.1.3 (Documentação Viva - Concluído):** Registro do ADR `docs/adr/0009-in-memory-messaging-and-validation-pipeline.md` e atualização de `docs/modules/building-blocks.md`.

### Subfase 4.2: Exposição de Contratos com OpenAPI e Scalar UI [Concluída]

* **4.2.1 (Concluído):** Ativar o gerador OpenAPI nativo do ASP.NET Core 10 (`AddOpenApi()`).
* **4.2.2 (Concluído):** Adicionar pacote do Scalar e registrar a rota `/scalar/v1` em `Program.cs`.
* **4.2.3 (TDD - Concluído):** Criar um endpoint de health check `GET /api/v1/health` retornando envelope `Result<HealthStatusResponse>` documentado via atributos `[EndpointSummary]` e `[ProducesResponseType]`.
* **4.2.4 (Documentação Viva - Concluído):** Criar `docs/adr/0001-modular-monolith-net10.md` e validar se o build conclui com zero alertas de XML documentation.

---

## Fase 5: Setup do Frontend Blazor Server (.NET 10) & Tooling de Testes

### Subfase 5.1: Estruturação do Projeto Blazor Server [Concluída]

* **5.1.1 (Concluído):** Criar projeto Blazor Server interativo na pasta `src/Frontend/WebApp/` com modo `InteractiveServer` em .NET 10 integrado à solution.
* **5.1.2 (Concluído):** Configurar layout corporativo responsivo padrão (`MainLayout`, `AppHeader`, `AppSidebar`, `AppFooter`) com slots dinâmicos para customização White-Label (logo, cores institucionais via CSS Custom Properties).
* **5.1.3 (TDD - Concluído):** Implementar o `TenantStateProvider` com interface `ITenantStateProvider` injetado com ciclo de vida Scoped por circuito SignalR para propagar a identidade do tenant ativo na sessão Blazor.

### Subfase 5.2: Suíte de Testes Frontend com bUnit

* **5.2.1:** Configurar projeto `tests/UnitTests/Frontend/` com suporte a **bUnit** e biblioteca de asserções.
* **5.2.2 (TDD):** Escrever o primeiro teste de componente em bUnit para validar que o layout renderiza corretamente a identidade visual e o nome do tenant injetado no estado.
