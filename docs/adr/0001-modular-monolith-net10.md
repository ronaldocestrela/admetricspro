# ADR 0001: Arquitetura em Monólito Modular com .NET 10 e Blazor Server Interativo

## Status
Aceito

## Contexto
O AdMetricsPro é um SaaS multitenant voltado para a **Gestão Unificada de Tráfego Pago**, integrando canais como Meta Ads, Google Ads, Bing Ads e TikTok Ads. O sistema requer:
1. **Isolamento e Segurança Estrita:** Cada cliente (tenant) deve possuir isolamento de dados a nível de banco de dados (`Database-per-Tenant`) para garantir segurança, compliance e governança.
2. **Alta Coesão e Baixo Acoplamento:** Múltiplos módulos funcionais com responsabilidades distintas (`Master`, `Tenants`, `Integrations`, `Analytics`, `Automations`).
3. **Prevenção de Complexidade Prematura:** Uma abordagem distribuída baseada em microsserviços acarretaria sobrecarga operacional desnecessária nesta fase (latência de rede, orquestração de containers, transações distribuídas).
4. **Produtividade e Tipagem Ponta a Ponta:** Necessidade de utilizar uma única stack moderna e de alta performance tanto no backend quanto no frontend administrativo.

## Decisão
1. **Adoção do Monólito Modular em .NET 10:**
   - Todo o sistema é estruturado em uma única solução `.sln` composta por limites de contexto bem definidos (Bounded Contexts).
   - Backend hospedado através de um único host ASP.NET Core Web API (`src/Backend/WebApi`).
   - Frontend administrativo desenvolvido em **Blazor Server Interativo** (`src/Frontend/WebApp`), garantindo estado em tempo real via conexões SignalR estáveis e reaproveitamento integral dos modelos e regras em C#.

2. **Isolamento Modular Inegociável:**
   - **Persistência Exclusiva:** Cada módulo possui seus próprios agregados e seu próprio `DbContext`. Módulos **não compartilham** instâncias de `DbContext` e não referenciam repositórios de outros módulos.
   - **Comunicação In-Memory Desacoplada:** A interação inter-módulos para comandos, consultas e propagação de eventos de domínio é mediada exclusivamente por contratos tipados em memória via `MediatR` (ADR 0009).
   - **Kernel Compartilhado:** Abstrações transversais (`Result<T>`, entidades base, `UnitOfWork`, criptografia e resolução de tenancy) residem no namespace `BuildingBlocks`.

3. **Exposição de Contratos e Documentação Viva:**
   - Geração de esquemas OpenAPI nativa do ASP.NET Core 10 (`AddOpenApi()` / `MapOpenApi()`).
   - Interface interativa moderna via **Scalar UI** exposta na rota padronizada `/scalar/v1`.
   - Comunicação estrita baseada no padrão `Result<T>`, sem exceções para controle de fluxo.
   - Comentários XML `<summary>` universais com compilação sob `TreatWarningsAsErrors`.

4. **Desenvolvimento Guiado por Testes (TDD):**
   - Ciclo obrigatório Red-Green-Refactor suportado por suítes isoladas de testes unitários (`tests/UnitTests`), integração com contêineres SQL Server reais (`tests/IntegrationTests`) e testes de aceitação de contratos de API via `WebApplicationFactory` (`tests/AcceptanceTests`).

## Consequências

### Positivas
- **Manutenibilidade e Evolução:** Fronteiras modulares nítidas permitem refatoração segura e até extração futura de módulos específicos para microsserviços independentes, caso a escala justifique.
- **Performance e Baixa Latência:** Chamadas inter-módulos ocorrem em memória sem overhead de serialização de rede.
- **Segurança de Tipos Unificada:** O ecossistema C# .NET 10 em toda a stack (Web API, Kernel, Módulos e Blazor) minimiza erros de desserialização e duplicação de modelos.
- **Rigor de Qualidade:** A governança por TDD e compilação sem avisos de documentação garante contratos estáveis e documentação viva sempre sincronizada.

### Negativas / Mitigações
- **Risco de Vazamento de Dependências:** Facilidade acidental de desenvolvedores referenciarem classes internas de outros módulos.  
  *Mitigação:* Regras de arquitetura enforced por testes de arquitetura e convenções estritas descritas em `AGENTS.md`.
- **Governança de Migrações em Múltiplos Bancos:** Necessidade de pipeline automatizado para aplicar migrações em dezenas ou centenas de bancos dedicados de inquilinos.  
  *Mitigação:* Pipeline dinâmico de migrações automáticas e provisionamento seguro estruturado na Fase 3 (ADRs 0007 e 0008).
