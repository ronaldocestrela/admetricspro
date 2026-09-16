# ADR 0028: Pipeline de Ingestão de Métricas Diárias e Horárias com Idempotência Estrita

## Status
Aceito

## Data
2026-09-15

## Contexto
Após a sincronização da hierarquia estrutural de campanhas (ADR 0027), o **AdMetricsPro** requer a ingestão periódica, contínua e confiável das métricas de performance publicitária provenientes dos 4 canais de anúncios (**Meta Ads**, **Google Ads**, **TikTok Ads** e **Bing Ads**).

Desafios e Requisitos Centrais:
1. **Granularidade Temporal Dupla:** Suporte simultâneo a granularidade diária (`Daily`) para consolidação histórica e granularidade horária (`Hourly`) para monitoramento intraday e automações em tempo real (ex.: regras de pacing e travas de overspending).
2. **Garantia Absoluta de Idempotência:** Falhas de rede, re-execuções manuais ou re-tentativas de rotinas agendadas (cron/background jobs) não podem, sob hipótese alguma, duplicar ou inflar os valores consolidados de gastos (*Spend*), impressões, cliques ou conversões para uma mesma data/hora.
3. **Cálculo Automático e Preciso de KPIs Derivados:** Indicadores como CTR, CPC, CPM, CPA e ROAS devem ser calculados de forma segura e consistente, evitando divisões por zero ou estados inválidos.
4. **Isolamento Multitenant (Database-per-Tenant):** As métricas de cada inquilino devem ser persistidas estritamente no seu banco SQL Server dedicado (`TenantDbContext`), sem interferência entre clientes.
5. **Consumo Seguro no Frontend Blazor:** O painel do cliente (`WebApp`) precisa apresentar o status de saúde e expiração das conexões com opção de renovação imediata, operando estritamente via clientes HTTP fortemente tipados consumindo a Web API, respeitando a Regra 9 de `AGENTS.md` (Zero Acesso Direto a Banco).

## Decisão

### 1. Entidade de Domínio Rica `CampaignMetric` com Chave Única Composta
Criou-se a entidade `CampaignMetric` no Kernel Compartilhado (`BuildingBlocks.Domain.Campaigns`), vinculada por chave estrangeira à campanha local e com a chave única composta de idempotência:
$$\text{UniqueKey} = (\text{ConnectedAdAccountId}, \text{ExternalCampaignId}, \text{ExternalAdSetId}, \text{ExternalAdId}, \text{Date}, \text{Hour}, \text{Granularity})$$
- Configurado índice único no EF Core (`CampaignMetricEntityTypeConfiguration`) garantindo idempotência tanto no nível de aplicação quanto no motor relacional de persistência.
- Métricas base: `Spend` (decimal), `Impressions` (long), `Clicks` (long), `Conversions` (decimal), `ConversionValue` (decimal).
- KPIs derivados calculados em propriedades e no método `UpdateMetrics`:
  - $\text{CTR} = \frac{\text{Clicks}}{\text{Impressions}} \times 100$
  - $\text{CPC} = \frac{\text{Spend}}{\text{Clicks}}$
  - $\text{CPM} = \frac{\text{Spend}}{\text{Impressions}} \times 1000$
  - $\text{CPA} = \frac{\text{Spend}}{\text{Conversions}}$
  - $\text{ROAS} = \frac{\text{ConversionValue}}{\text{Spend}}$

### 2. Repositório com Upsert Atômico em Lote (`CampaignMetricsRepository`)
Implementou-se a interface `ICampaignMetricsRepository` com o método `UpsertBatchAsync`:
- O método carrega em memória as métricas existentes no intervalo (`StartDate` a `EndDate`) através de dicionário indexado pela chave única.
- Se o registro já existe, atualiza valores via `metric.UpdateMetrics(...)` (preservando o `Id` original).
- Se não existe, adiciona nova entidade via `AddAsync`.
- Todas as operações são consolidadas atomicamente via `IIntegrationsUnitOfWork.CommitAsync()`, impedindo duplicações ou estados inconsistentes.

### 3. Pipeline de Adaptadores e Despachante (`CampaignMetricsSyncDispatcher`)
- Interface `ICampaignMetricsSyncAdapter` implementada para cada plataforma:
  - `MetaAdsMetricsSyncAdapter`: mapeia insights de campanhas com tratamento de resiliência.
  - `GoogleAdsMetricsSyncAdapter`: converte métricas de GAQL (`metrics.cost_micros / 1.000.000m`).
  - `TikTokAdsMetricsSyncAdapter`: consome relatórios analíticos integrados.
  - `BingAdsMetricsSyncAdapter`: processa métricas de relatórios da Microsoft Ads.
  - `DemoMetricsSyncAdapter`: gera dados sintéticos consistentes e determinísticos baseados em seed da data e da campanha, garantindo idempotência em contas de demonstração.
- Orquestrador `CampaignMetricsSyncDispatcher` despacha chamadas com a política de resiliência exponencial `HierarchyRateLimitPolicy`.
- Disparo do evento in-memory `CampaignMetricsSyncedEvent` via `MediatR` para alertar outros módulos (Analytics, Automations) de forma desacoplada.

### 4. Endpoints REST Web API Documentados
- `POST /api/v1/integrations/campaigns/metrics/sync`: inicia sincronização de métricas por período, com parâmetros validados (`WorkspaceId`, `ConnectedAdAccountId`, `StartDate`, `EndDate`, `Granularity`).
- `GET /api/v1/integrations/campaigns/metrics`: consulta métricas agregadas ou detalhadas com filtros e suporte a envelope `Result<T>`.
- Totalmente integrados ao OpenAPI e Scalar UI com anotações semânticas.

### 5. Frontend Blazor Server com Cliente HTTP Tipado
- Criado o serviço `OAuthIntegrationsClientService` (`IOAuthIntegrationsClientService`), consumindo exclusivamente a Web API via `HttpClient`.
- Criado o componente `ConnectionStatusList.razor` e estilos isolados `ConnectionStatusList.razor.css` para visualização das integrações conectadas, status do token (Ativo, Expirando, Revogado) e botão de ação para renovação imediata (`Reconectar / Renovar Token`).
- Zero dependência de `DbContext`, `Infrastructure` ou comandos diretos de banco no frontend (100% compliant com Regra 9 do `AGENTS.md`).

## Consequências

### Positivas
- Idempotência absoluta comprovada por testes unitários e de integração (re-execução de mesma data mantém número idêntico de registros).
- Suporte imediato aos módulos futuros de Analytics (Atribuição, MER) e Automations (Pacing intraday e travas de orçamento).
- Arquitetura 100% alinhada com os princípios inegociáveis de `AGENTS.md` (TDD, `Result<T>`, XML Docs, separação de camadas e isolamento de tenant).
- 100% de cobertura nos novos componentes e regras de negócio com todos os testes da solução passando.

### Negativas / Mitigações
- Volume de métricas horárias em contas de alta escala pode crescer substancialmente: mitigado por agregação prévia nas rotinas de consulta e estratégia de índices específicos na tabela `CampaignMetrics`.
