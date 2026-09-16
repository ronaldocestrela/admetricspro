# 0036. Copiloto de Otimização via IA (Auditor de Tráfego) e Detecção de Anomalias Cross-Channel

## Status
Aceito

## Contexto
Gestores de tráfego pago enfrentam diariamente dois desafios analíticos críticos de alta complexidade:
1. **Sobreposição de Públicos (*Audience Overlap*) no Meta Ads:** Quando múltiplos conjuntos de anúncios (`AdSet`) de uma mesma conta competem pelas mesmas segmentações de interesses e comportamentos, o algoritmo da Meta causa auto-concorrência no leilão interno, elevando o CPM artificialmente e dispersando o orçamento sem ganhos incrementais de alcance.
2. **Disputa e Canibalização de Termos de Busca entre Google Ads e Bing Ads:** Anunciantes frequentemente ativam as mesmas palavras-chave institucionais ou de alta intenção em ambos os motores de busca, sem monitorar disparidades de CPA. Casos em que um canal custa 2x a 3x mais que o outro para o mesmo termo acarretam desperdício orçamentário considerável.

Era imperativo disponibilizar um assistente autônomo (Copiloto de IA / Auditor de Tráfego) capaz de:
- Identificar deterministicamente essas duas categorias de anomalias com fórmulas matemáticas transparentes (Jaccard Index e razão de disparidade de CPA).
- Sintetizar briefings diários em texto natural compreensível pelo gestor (destacando Vitórias, Riscos e Oportunidades com estimativa monetária de economia).
- Disponibilizar um mecanismo de **Execução em 1 Clique** para acionar remediações diretas na API (pausa de conjunto redundante, negativação de palavras-chave, etc.) com registro na auditoria imutável do Tenant.

## Decisão
Decidimos implementar o subsistema de Copiloto de IA e Auditor de Tráfego estruturado da seguinte forma:
1. **Módulo Analytics:**
   - Detectores desacoplados no domínio: `MetaAudienceOverlapDetector` (analisa tags via Índice de Jaccard com limiares de 30% para alerta High e 50% para Critical) e `SearchTermCannibalizationDetector` (normaliza termos de busca e calcula disparidades de CPA $\ge 1.75x$ e $\ge 2.0x$).
   - Sintetizador analítico: `TrafficAuditorSynthesizer` compondo diagnósticos diários estruturados em linguagem natural e consolidando economias projetadas.
   - Provedor de dados: `CopilotDataProvider` consultando o banco dedicado do tenant através de `ITenantDbContextAccessor`.
2. **Camada de Aplicação & Web API:**
   - Consulta `GetDailyDiagnosticQuery` expondo `/api/v1/analytics/copilot/diagnostic`.
   - Comando `ExecuteCopilotRecommendationCommand` expondo `/api/v1/analytics/copilot/actions/execute`, executando mutações no banco dedicado e registrando em `TenantAuditLogs`.
   - Todas as operações seguem o padrão `Result<T>` sem lançar exceções de negócio.
3. **Frontend Blazor Server:**
   - Consumo exclusivo via cliente HTTP fortemente tipado `ITrafficCopilotClientService` com injeção automática do cabeçalho de isolamento `X-Tenant-Id`.
   - Componentes visuais atômicos (`CopilotDailySummaryCard`, `AudienceOverlapVisualizer`, `SearchCannibalizationCompare` e `CopilotAnomalyCard`) permitindo visualização de métricas e disparo da remediação em 1 clique com feedback reativo.

## Consequências

### Positivas
- **Eficiência Financeira Imediata:** Permite que agências estanquem auto-concorrência no Meta Ads e desperdício de termos concorrentes entre Google e Bing com um único clique.
- **Transparência Algorítmica:** Diagnósticos auditáveis baseados em métricas claras e fórmulas matemáticas reproduzíveis.
- **Rastreabilidade e Segurança:** Toda ação executada pelo Copiloto registra um log de auditoria imutável no tenant, garantindo governança total.
- **Zero Acesso Direto a Banco no Frontend:** A interface consome exclusivamente a Web API, respeitando as regras estritas do [AGENTS.md](file:///home/rony/LPR/AdMetricsPro/AGENTS.md).

### Negativas / Mitigações
- **Volume de Dados para Análise de Jaccard:** A comparação combinatória entre $N$ conjuntos de anúncios tem complexidade $O(N^2)$.
  - *Mitigação:* O filtro restringe a análise apenas a conjuntos ativos do workspace contextual com segmentações registradas, mantendo tempos de resposta na ordem de milissegundos.
