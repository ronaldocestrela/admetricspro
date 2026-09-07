# ADR 0020: Desacoplamento Total do Frontend e Eliminação de Acesso Direto a Banco de Dados

## Status
Aceito (Accepted) — 2026-09-07

## Contexto
Durante as fases iniciais do projeto, os projetos de frontend Blazor Server (`WebApp` e `BackofficeApp`) referenciavam o módulo `Master.Infrastructure` e registravam o `MasterDbContext` com conexão direta ao SQL Server. Além disso, o `BackofficeApp` executava comandos de migração estrutural (`ApplyMasterDatabaseMigrationsAsync`) no seu startup, e os serviços de tela injetavam repositórios EF Core ou mediadores in-process (`MediatR`) que disparavam transações diretas no banco de dados.

Embora o Blazor Server execute no servidor (.NET Runtime), esse acoplamento gerava:
1. **Bypass da Web API:** Controladores REST documentados com OpenAPI/Scalar eram ignorados pelo próprio frontend do ecossistema.
2. **Duplicação de infraestrutura e concorrência:** Múltiplos processos disputavam pools de conexão do SQL Server e execução de migrações.
3. **Risco de Segurança e Barreira para Deploy Distribuído:** Se os frontends forem hospedados em DMZ ou contêineres públicos, necessitariam de portas abertas e credenciais diretas do SQL Server.

## Decisão
1. **Frontend Estritamente de Apresentação:**
   - Nenhum frontend (`WebApp`, `BackofficeApp`) pode referenciar projetos de `Infrastructure`, instanciar `DbContext`, executar migrações de banco ou utilizar repositórios EF Core.
2. **Consumo Exclusivo via Web API:**
   - Todos os serviços de frontend (`TenantOnboardingClientService`, `PlanManagementService`, `TenantDirectoryService`, `FeatureFlagClientService`, `ApiHealthClientService` e `ImpersonationClientService`) passam a se comunicar com a aplicação exclusivamente via chamadas HTTP (`HttpClient`) direcionadas aos endpoints versionados da `WebApi` (`/api/v1/...`), tratando respostas envelopadas no padrão `Result<T>`.
3. **Migrações e Persistência Centralizadas:**
   - A execução de migrações automáticas de banco de dados (`MasterDb` e provisionamento de inquilinos) e seeds iniciais de dados pertencem unicamente ao host `WebApi` e seus eventuais workers de background.
4. **Atualização das Regras no `AGENTS.md`:**
   - A proibição de acesso direto a bancos de dados pelo frontend foi inserida como Princípio Inegociável (Seção 1, item 9) e detalhada na Seção 4.2 do documento de referência.

## Consequências
### Positivas
- **Arquitetura Limpa e Desacoplada:** O frontend torna-se puramente consumidor da API, alinhado aos padrões universais de segurança para aplicações web modernas.
- **Ponto Único de Entrada:** Toda regra de negócio, autorização, validação e auditoria é garantida pela Web API.
- **Preparação para Nuvem e DMZ:** Os frontends podem ser distribuídos e escalados independentemente sem necessidade de conexão de rede direta com o banco de dados.

### Negativas / Mitigações
- Necessidade de manter a Web API em execução para que o frontend opere localmente (mitigado por perfis orquestrados no Visual Studio/VSCode e scripts de inicialização).
