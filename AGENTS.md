# AGENTS.md — Regras de Desenvolvimento & Arquitetura de Referência

> **Aviso para Agentes LLM e Desenvolvedores Autônomos:**  
> Este documento contém diretrizes operacionais, arquiteturais e de engenharia mandatárias. Qualquer desvio das regras aqui estabelecidas será rejeitado na revisão de código.

---

## 1. Visão Geral e Princípios Fundamentais

O sistema é um **SaaS de Gestão Unificada de Tráfego Pago** (Meta Ads, Google Ads, Bing Ads e TikTok Ads), multitenant com isolamento por banco de dados, desenhado sob a abordagem de **Monólito Modular** em **.NET 10**.

### Princípios Inegociáveis
1. **.NET 10 em Toda a Solução:** Backend (ASP.NET Core Web API) e Frontend (Blazor Web App Interativo no modo Server).
2. **Monólito Modular:** Módulos autônomos com limites de contexto bem definidos (Bounded Contexts), sem acoplamento direto de persistência ou referências circulares.
3. **Isolamento de Tenant por Banco de Dados (Database-per-Tenant):** Cada tenant possui sua própria instância de banco SQL Server. Um banco central/catálogo (`MasterDb`) armazena tenants, assinaturas e credenciais de conexão seguras.
4. **Persistência com EF Core & Pattern Repository:** Todo acesso aos dados transacionais é encapsulado em Repositórios e coordenado por `UnitOfWork`. Migrações automáticas são aplicadas na inicialização e no provisionamento de tenants.
5. **Pattern `Result<T>` Estrito:** Proibido o uso de exceções (`throw new Exception`) para controle de fluxo de negócio. Toda operação de comando/consulta deve retornar `Result` ou `Result<T>`.
6. **Comunicação Inter-Módulos In-Memory:** Troca de comandos e eventos exclusivamente via contratos e mediador in-memory (`MediatR` / Domain Events desacoplados).
7. **TDD (Test-Driven Development) Global:** Todo código de produção deve nascer a partir de um teste que falha (Ciclo Red-Green-Refactor). Cobertura unitária e de integração em todos os módulos e componentes de tela críticos.
8. **Documentação Viva Mandatória:**
   - Pasta `/docs` com especificação exaustiva de cada funcionalidade, fluxos, payloads de entrada e estruturas de retorno.
   - Comentários XML `<summary>`, `<param>`, `<returns>` em **todas** as classes, interfaces, métodos e records públicos.
   - Registros de Decisão Arquitetural (ADRs) versionados em `/docs/adr/`.
   - Documentação de APIs viva e interativa via **OpenAPI + Scalar UI** versionada por endpoint.

---

## 2. Estrutura da Solução e Organização de Pastas

A solução deve conter backend e frontend no mesmo repositório, mantendo separação física estrita de diretórios:

```text
├── docs/
│   ├── adr/                       # Architecture Decision Records (ex: 0001-modular-monolith.md)
│   ├── modules/                   # Especificações de entrada/retorno por módulo funcional
│   │   ├── backoffice.md
│   │   ├── tenants.md
│   │   ├── analytics.md
│   │   └── automations.md
│   └── architecture.md
├── src/
│   ├── Backend/
│   │   ├── BuildingBlocks/        # Kernel compartilhado (Result, Entity base, Interfaces globais)
│   │   │   ├── Domain/
│   │   │   ├── Application/
│   │   │   └── Infrastructure/
│   │   ├── Modules/
│   │   │   ├── Master/            # Catálogo central de Tenants, Planos e Super Admin
│   │   │   │   ├── Domain/
│   │   │   │   ├── Application/
│   │   │   │   ├── Infrastructure/
│   │   │   │   └── Presentation/
│   │   │   ├── Tenants/           # Gestão de Squads, Workspaces, White-Label e RBAC
│   │   │   ├── Integrations/      # Adaptadores de APIs (Meta, Google, Bing, TikTok)
│   │   │   ├── Analytics/         # Métricas Cross-Platform, MER, Atribuição
│   │   │   └── Automations/       # Motor de Regras, Pacing, Travas de Overspending
│   │   └── WebApi/                # Host ASP.NET Core .NET 10, OpenAPI + Scalar, Middlewares
│   └── Frontend/
│       └── WebApp/                # Blazor Server .NET 10 (Components, Pages, State, Services)
└── tests/
    ├── UnitTests/
    │   ├── Backend/
    │   └── Frontend/
    ├── IntegrationTests/
    │   ├── MultiTenancy/
    │   └── Repositories/
    └── AcceptanceTests/           # Testes de fluxos e contratos de endpoints

```

---

## 3. Padrões de Código e Regras Arquiteturais

### 3.1 Tratamento de Fluxo com `Result<T>`

Nunca retorne exceções para cenários de regra de negócio (ex: "Saldo insuficiente", "Workspace não encontrado", "Token revogado"). Utilize o padrão `Result`:

```csharp
/// <summary>
/// Executa a criação de uma regra de automação com travas de segurança.
/// </summary>
/// <param name="command">Dados de entrada validados para a regra.</param>
/// <param name="cancellationToken">Token de cancelamento da requisição.</param>
/// <returns>Retorna o identificador da regra criada ou falha de negócio.</returns>
public async Task<Result<Guid>> Handle(CreateRuleCommand command, CancellationToken cancellationToken)
{
    if (command.MaxBudgetLimit <= 0)
        return Result<Guid>.Failure(Error.Validation("Rule.InvalidLimit", "O orçamento limite deve ser positivo."));

    var workspace = await _workspaceRepository.GetByIdAsync(command.WorkspaceId, cancellationToken);
    if (workspace is null)
        return Result<Guid>.Failure(Error.NotFound("Workspace.NotFound", "Workspace não localizado para este Tenant."));

    var rule = AutomationRule.Create(command.Name, command.Condition, command.WorkspaceId);
    await _ruleRepository.AddAsync(rule, cancellationToken);
    await _unitOfWork.CommitAsync(cancellationToken);

    return Result<Guid>.Success(rule.Id);
}

```

### 3.2 Isolamento Multitenant (Database-per-Tenant)

* **Identificação do Tenant:** O `TenantId` deve ser resolvido no pipeline HTTP via header `X-Tenant-Id`, subdomínio (CNAME) ou claim do token JWT.
* **Resolução de Conexão Dinâmica:** O serviço `ITenantConnectionResolver` busca a Connection String correspondente no banco `MasterDb` (com cache seguro).
* **Provedor de DbContext por Tenant:** O `TenantDbContext` deve ser resolvido por escopo com a string de conexão do tenant contextual.
* **Migrações Automáticas:**
* O `MasterDbContext` aplica migrações no startup via `masterContext.Database.MigrateAsync()`.
* Ao provisionar um novo Tenant, o serviço `ITenantProvisioningService` cria o banco dedicado no SQL Server e dispara `tenantContext.Database.MigrateAsync()` automaticamente antes de liberar o acesso.



### 3.3 Comunicação Entre Módulos

* Módulos **não devem** compartilhar instâncias de `DbContext`.
* Módulos **não devem** referenciar repositórios de outros módulos.
* Se o módulo de **Automations** precisar pausar uma campanha que pertence ao módulo de **Integrations**, ele emite um comando/evento via `MediatR` com tipos definidos no Kernel compartilhado ou no contrato exportado pelo módulo.

### 3.4 Padrão Repository e Unit of Work

* Cada agregado de domínio possui sua interface de repositório explícita (ex.: `IWorkspaceRepository`, `ICampaignMetricRepository`).
* Métodos devem suportar `CancellationToken` obrigatoriamente.
* Operações de escrita devem ser consolidadas através do `IUnitOfWork.CommitAsync()`.

---

## 4. Frontend em Blazor Server (.NET 10)

1. **Modo Interativo Server:** Toda a interface administrativa (painel do cliente e backoffice) opera em modo Blazor Server com conexões SignalR estáveis.
2. **Consumo de Dados:** O frontend consome os serviços da aplicação via clientes HTTP fortemente tipados que tratam `Result<T>` ou serviços locais injetados por escopo.
3. **Isolamento de Marca (White-Label):**
* O layout raiz deve renderizar dinamicamente o tema (cores primárias, secundárias, favicon e logo) a partir do estado do Tenant carregado na sessão.
* Resolução de rotas respeitando domínios CNAME personalizados cadastrados no Tenant.


4. **Componentização e TDD:**
* Componentes visuais atômicos devem residir em `/Shared/Components/`.
* Testes de componentes devem ser escritos utilizando **bUnit** com ciclo Red-Green-Refactor.



---

## 5. Documentação Viva & Padrão OpenAPI + Scalar

1. **Tag `<summary>` Universal:** Toda classe, interface, record, DTO, método e propriedade pública deve conter comentário de documentação XML legível.
2. **Pasta `/docs/modules`:** Para cada funcionalidade desenvolvida, adicione ou atualize o arquivo `.md` correspondente contendo:
* Descrição do caso de uso e regras de validação.
* Exemplo JSON do payload de entrada.
* Exemplo JSON da estrutura de retorno (comunicação do padrão `Result`).
* Casos de borda e erros de negócio mapeados.


3. **ADRs em `/docs/adr`:** Qualquer nova adição arquitetural (ex: introdução de fila de background jobs, estratégia de cache distribuído) deve ter seu arquivo `.md` registrado no formato Nygard (Título, Contexto, Decisão, Consequências).
4. **Configuração OpenAPI & Scalar:**
* A API deve configurar o gerador OpenAPI nativo do .NET 10.
* Expor o endpoint do Scalar interativo em `/scalar/v1` em ambientes de desenvolvimento e homologação.
* Todos os endpoints devem declarar atributos `[EndpointSummary]`, `[ProducesResponseType]` com códigos de status HTTP semânticos (200, 400, 404, 422).



---

## 6. Fluxo de Trabalho do Agente LLM (TDD Obrigatório)

Antes de gerar qualquer linha de código de implementação, siga estritamente o pipeline:

```text
[1. Escrever Teste com Falha (Red)]
       │
       ▼
[2. Implementar Contrato Mínimo e Executar Teste]
       │
       ▼
[3. Teste Passa (Green)]
       │
       ▼
[4. Refatorar Mantendo o Teste Verde (Refactor)]
       │
       ▼
[5. Escrever XML Docs (<summary>) & Atualizar /docs]

```

### Critérios de Aceite para Cada Tarefa Executada por IA

* [ ] O teste unitário/integração correspondente foi criado antes da implementação.
* [ ] A implementação não lança exceções para falhas de validação ou de negócio (usa `Result<T>`).
* [ ] Todas as novas entidades e métodos possuem tags XML `<summary>`.
* [ ] A rota foi versionada e descrita para o OpenAPI/Scalar.
* [ ] A operação respeita o contexto do Tenant atual (`TenantDbContext`).
* [ ] Os documentos em `/docs` e eventuais ADRs foram devidamente atualizados.