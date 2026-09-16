# 32. Construtor de Regras Cross-Platform (DSL / Condições If-Then) e Despachador de Ações

## Contexto

A entrega da **Subfase 4.1** inicia a **Fase 4 (Automações Cross-Platform, Lances & Travas de Segurança)** do AdMetricsPro. O objetivo central é fornecer aos gestores de tráfego um motor de regras de negócio programadas capaz de avaliar condições combinadas entre múltiplas redes de anúncios (Meta Ads, Google Ads, TikTok Ads e Bing Ads) e disparar ações automáticas de mutação (pausar anúncios/campanhas, aumentar/reduzir verba percentual ou fixa, e realocar saldo orçamentário entre plataformas concorrentes).

Requisitos fundamentais de engenharia:
1. **DSL Expressiva e Componível:** Suporte ao padrão Composite (*árvore de predicados*), combinando nós lógicos (`AND`, `OR`, `NOT`) com folhas de métricas quantitativas (CPA, ROAS, CPC, CPM, CTR, Spend, Conversions, ConversionValue) e janelas temporais configuráveis (24h, 48h, 72h, 168h).
2. **Avaliação 100% Desacoplada de I/O de Rede:** O motor avaliador de condições (`RuleConditionEvaluator`) deve ser um serviço de domínio puro operando exclusivamente sobre estruturas em memória (`RuleEvaluationContext`), garantindo testabilidade unitária determinística e isolamento absoluto de latências de rede ou falhas externas.
3. **Monólito Modular & Comunicação Inter-Módulos via MediatR:** O módulo `Automations` não deve acessar repositórios nem instâncias de DbContext de `Integrations` (conforme Seção 3.3 do `AGENTS.md`). A execução de alterações em campanhas deve ocorrer por meio de comandos in-memory desacoplados (`PauseCampaignCommand`, `PauseAdCommand`, `AdjustCampaignBudgetCommand`, `ReallocateBudgetCommand`).
4. **Isolamento por Banco de Dados (Database-per-Tenant):** As regras configuradas e seu histórico de execuções devem ser armazenados de forma estritamente isolada no `TenantDbContext`.
5. **Pattern Result<T> Estrito:** Proibido lançamento de exceções para regras de negócio ou validações de predicados inválidos.

## Decisão

1. **Criação do Módulo `Automations`:**
   - Adicionados os projetos `Automations.Domain`, `Automations.Application` e `Automations.Infrastructure` registrados na solução `AdMetricsPro.sln`, `WebApi.csproj` e `UnitTests.Backend.csproj`.
2. **Modelo de Árvore de Predicados (Composite Pattern):**
   - Definida a interface `IRuleCondition` com suporte a serialização polimórfica nativa no `System.Text.Json` (`$type = metric` para `MetricPredicate` e `$type = group` para `RuleConditionGroup`).
   - Implementado o agregado raiz `AutomationRule` com invariantes de validação de domínio.
3. **Motor Avaliador em Memória (`RuleConditionEvaluator`):**
   - Avalia recursivamente nós compostos com curto-circuito booleano e operadores relacionais (`>`, `>=`, `<`, `<=`, `==`, `!=`).
   - Mapeia as entidades correspondidas (`MatchedEntityIds`) e compila mensagens de diagnóstico legíveis.
4. **Despachador de Ações (`RuleActionDispatcher`):**
   - Traduz `RuleAction` em comandos tipados emitidos através do `IMediator` para handlers no módulo `Integrations`.
5. **Manipuladores de Mutação em `Integrations`:**
   - Implementados `PauseCampaignCommandHandler`, `PauseAdCommandHandler`, `AdjustCampaignBudgetCommandHandler` e `ReallocateBudgetCommandHandler`, operando sobre o repositório de hierarquia e persistindo atomicamente via `IIntegrationsUnitOfWork`.
6. **Web API RESTful & OpenAPI + Scalar:**
   - Expostos endpoints versionados em `AutomationsController` (`/api/v1/automations/rules`) documentados semanticamente para o Scalar UI.

## Consequências

### Positivas
- **Desacoplamento e Resiliência:** Avaliação de regras ultrarrápida em memória sem bloqueios de rede.
- **Interoperabilidade Cross-Platform:** Permite correlações avançadas entre canais (ex.: *Se TikTok CPA[48h] > 50 E Google ROAS[48h] > 4.5 -> Reduz TikTok e realoca no Google*).
- **Conformidade Arquitetural Total:** Respeito integral às fronteiras de contexto e ausência de acoplamento direto de persistência entre módulos.
- **Cobertura TDD Global:** 100% dos fluxos e casos de borda (divisão por zero, métricas nulas, janelas divergentes) cobertos por testes unitários que garantem regressão zero.

### Neutras / Compensações
- O armazenamento da árvore de predicados e lista de ações em colunas JSON no `TenantDbContext` exige suporte a conversão de tipo via EF Core `HasConversion`, que foi solucionado com serialização JSON polimórfica nativa.
