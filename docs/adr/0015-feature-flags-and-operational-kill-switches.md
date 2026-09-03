# ADR 0015: Sistema de Feature Flags e Kill Switches Operacionais

## Status
Aceito

## Contexto
O AdMetricsPro executa rotinas críticas em segundo plano e em tempo real, tais como corte preventivo de gastos excessivos (*overspending*), sincronização massiva de conversões e otimizações automáticas de campanhas em quatro grandes redes de anúncios (Meta, Google, TikTok, Bing).

Em ambientes de produção com dezenas ou centenas de inquilinos, podem ocorrer cenários de emergência operacional:
1. Uma API de rede de anúncios sofre degradação severa ou bug de retorno (ex: Meta Graph API retornando status 500 ou dados corrompidos de faturamento).
2. Um bug na lógica de automação pode pausar indevidamente campanhas que geram receita substancial para clientes.
3. Necessidade de lançar novos recursos (como o novo motor de atribuição e MER v2) gradualmente (*canary release*), permitindo avaliar impacto técnico antes do rollout amplo.

Sem um mecanismo centralizado e instantâneo de contenção e chaveamento:
- A equipe de engenharia dependeria de *hotfixes* e novos *deployments* emergenciais para desligar funcionalidades problemáticas, levando dezenas de minutos ou horas.
- Não haveria rastreamento auditável sobre quem desligou o motor, por qual motivo e quando o serviço foi restabelecido.
- A experiência de inquilinos em testes beta ficaria comprometida sem isolamento determinístico.

## Decisão

1. **Agregado de Domínio `FeatureFlag` (`Master.Domain.FeatureFlags`):**
   - Criação da entidade agregada raiz suportando tanto **Feature Flags funcionais** quanto **Kill Switches operacionais**.
   - Modelagem de estratégias de segmentação (`FeatureFlagTargetingType`): `Global`, `PercentageRollout` e `TenantList`.
   - Implementação de algoritmo determinístico de *bucket hashing* via SHA-256 (`(BitConverter.ToUInt32(hash) % 100) < RolloutPercentage`). O inquilino tem estabilidade matemática na atribuição ao longo do ciclo de vida da flag.
   - Circuit-breakers operacionais imutáveis: métodos explícitos `ActivateKillSwitch` e `DeactivateKillSwitch` exigindo obrigatoriamente justificativa com no mínimo 5 caracteres e identificação do operador.

2. **Isolamento de Congelamento por Rede de Anúncios:**
   - Padronização de chaves de Kill Switches:
     - Global: `killswitch.automation.global`
     - Plataformas específicas: `killswitch.automation.meta`, `killswitch.automation.google`, `killswitch.automation.tiktok`, `killswitch.automation.bing`
     - Background jobs: `killswitch.data-sync.global`
   - O método `IsAutomationFrozenAsync(platform)` do `IFeatureFlagService` avalia se o disjuntor global OU o disjuntor específico da plataforma está armado, garantindo isolamento sem impactar redes saudáveis.

3. **Auditoria Imutável Obrigatória via `IMasterAuditService`:**
   - Qualquer engate ou desengate de Kill Switch gera um registro imutável no log de auditoria do `MasterDb` com tags `["kill_switch", "operational_emergency"]`, preservando a rastreabilidade exigida pela governança corporativa.

4. **Cache In-Memory Thread-Safe (`IMemoryCache`):**
   - Para que as consultas a flags nos loops de automação não saturem o banco de dados SQL Server, as avaliações são mantidas em cache em memória com TTL dinâmico.
   - Qualquer mutação de estado (ativação/desativação/atualização de rollout) invalida imediatamente e atomicamente as entradas de cache correspondentes.

5. **Interface Administrativa Blazor Server (`FeatureFlagsDashboard.razor`):**
   - Banner de alerta de alta prioridade pulsante quando qualquer disjuntor estiver armado.
   - Cartões operacionais para os disjuntores de rede com botões de congelamento e restauração integrados ao diálogo modal `ConfirmActionDialog`.
   - Tabela de gerenciamento de flags com controle de rollout slider em tempo real e toggles rápidos.

## Consequências

### Positivas
- **Contenção Imediata de Danos (MTTR Próximo de Zero):** Em caso de falha externa de uma rede (ex.: instabilidade Meta), o operador desliga apenas o disjuntor Meta em segundos pela interface ou API, sem interromper as automações saudáveis do Google ou TikTok e sem novo deploy.
- **Rastreabilidade e Compliance:** Toda intervenção humana emergencial fica gravada com motivo, timestamp UTC e operador no log de auditoria.
- **Lançamentos Seguros e Graduais:** Permite validar algoritmos complexos (ex.: MER v2) com 10% a 20% da base antes da liberação total, com distribuição uniforme e estável.
- **Alta Performance:** Avaliações de flags em memória com custo computacional insignificante em rotinas de alta concorrência.

### Negativas / Mitigações
- Múltiplas instâncias de API distribuídas precisam invalidar seus respectivos caches locais.
  - *Mitigação Futura:* O evento de domínio desacoplado `FeatureFlagUpdatedDomainEvent` poderá ser publicado no barramento de eventos (Redis / Azure Service Bus) para invalidar nós distribuídos simultaneamente quando a arquitetura migrar para múltiplos pods no Kubernetes.
