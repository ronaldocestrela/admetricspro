# ADR 0017: Testes Automatizados de Conformidade Arquitetural e Guardrails do AGENTS.md

## Status
Aceito

## Contexto
O projeto **AdMetricsPro** estabelece um conjunto rigoroso e inegociável de diretrizes arquiteturais e operacionais no documento [AGENTS.md](file:///home/rony/LPR/AdMetricsPro/AGENTS.md), incluindo:
1. **.NET 10 Unificado:** Backend ASP.NET Core Web API e Frontend Blazor Server Interativo.
2. **Monólito Modular e Limites de Contexto:** Camadas de Domínio e Aplicação puras, sem referências acopladas a detalhes de infraestrutura ou persistência de outros módulos.
3. **Padrão `Result<T>` Estrito:** Eliminação do uso de exceções como mecanismo de fluxo de negócio; todo handler MediatR deve produzir envelopes tipados `Result` ou `Result<T>`.
4. **Isolamento de Persistência e Multitenancy:** Separação estrita entre `MasterDbContext` (catálogo central) e `TenantDbContext` (bancos dedicados por tenant), orquestrados por `UnitOfWork` e com suporte a cancelamento assíncrono (`CancellationToken`).
5. **Documentação Viva Mandatória:** Presença de comentários XML (`<summary>`, `<param>`, `<returns>`) em todas as classes, interfaces, records e propriedades/métodos públicos, além de OpenAPI nativo e Scalar UI.

Conforme a base de código e os times evoluem (humanos ou agentes autônomos de IA), existe o risco constante de regressão ou desvio dessas regras caso a conformidade dependa exclusivamente de auditoria manual ou revisões pontuais.

## Decisão

1. **Instituição de Guardrails Automatizados de CI/CD (Testes de Conformidade):**
   - Criamos duas suítes automatizadas de testes de conformidade executadas a cada ciclo de compilação e validação:
     - `XmlDocumentationComplianceTests`: Realiza varredura reflexiva em todos os assemblies do backend (`BuildingBlocks.Domain`, `BuildingBlocks.Application`, `BuildingBlocks.Infrastructure`, `Master.Domain`, `Master.Application`, `Master.Infrastructure`, `WebApi`), comparando contra os arquivos de documentação XML gerados pelo compilador (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`). Falha de forma determinística caso qualquer tipo ou membro público careça de `<summary>` ou `<inheritdoc/>`.
     - `FrontendDocumentationComplianceTests`: Aplica auditoria equivalente sobre todos os serviços, modelos e provedores de estado públicos do Blazor Server (`WebApp.Services`, `WebApp.Models`, `WebApp.State`).
     - `AgentsArchitectureComplianceTests`: Audita a adesão estrita às regras estruturais do monólito:
       - 100% dos handlers MediatR implementam `IRequestHandler<TRequest, Result<TResponse>>`.
       - Camadas de Domínio não possuem referências diretas ao EF Core, ASP.NET Core ou Infraestrutura.
       - A camada de Aplicação não referencia a Infraestrutura ou WebApi diretamente.
       - Todas as interfaces de repositório expõem métodos assíncronos que aceitam `CancellationToken`.
       - Persistência transacional consolidada por `IUnitOfWork`.

2. **Garantia de Zero Tolerância a Testes Pulados ou Flaky:**
   - A suíte completa é executada via `dotnet test` consolidando 438 testes (Unitários Backend, Unitários Frontend com bUnit, Integração com Testcontainers SQL Server e Aceitação com `WebApplicationFactory`).
   - Todos os 438 testes executam com 100% de sucesso (zero falhas e zero pulados).

3. **Governança de ADRs:**
   - Todos os 17 ADRs são versionados em `docs/adr/` no formato Nygard e indexados em `docs/adr/README.md`.

## Consequências

### Positivas
- **Prevenção Ativa de Débito Técnico:** Qualquer commit ou PR gerado por agentes ou desenvolvedores que viole as regras do AGENTS.md é rejeitado imediatamente em tempo de teste unitário.
- **Documentação XML Garantida:** 100% dos tipos e membros públicos permanecem documentados, alimentando IntelliSense, geradores de documentação e contratos de API.
- **Arquitetura Limpa e Auditável:** O acoplamento indevido entre camadas ou desvios do padrão `Result<T>` são capturados instantaneamente antes de chegarem à integração contínua ou homologação.

### Negativas / Mitigações
- Testes baseados em reflexão exigem manutenção caso novas convenções de nomenclatura ou novos assemblies sejam adicionados.
  - *Mitigação:* As listas de assemblies e padrões são centralizadas e extensíveis nos testes de conformidade.
