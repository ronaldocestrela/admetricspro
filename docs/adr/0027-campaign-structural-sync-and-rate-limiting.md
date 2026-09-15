# ADR 0027: Sincronização Estrutural de Campanhas e Estratégia de Rate Limiting

## Status
Aceito

## Data
2026-09-15

## Contexto
O SaaS **AdMetricsPro** necessita ingerir e normalizar periodicamente a estrutura publicitária de campanhas provenientes de 4 grandes gerenciadores de anúncios (**Meta Ads**, **Google Ads**, **Bing Ads** e **TikTok Ads**).
Cada rede externa possui nomenclatura, níveis e formatos próprios:
- **Meta e TikTok:** utilizam *Campaign &rarr; AdSet / AdGroup &rarr; Ad (Creative)* com orçamentos em centavos ou valores decimais.
- **Google Ads e Bing Ads:** utilizam *Campaign &rarr; AdGroup &rarr; Ad* com consultas analíticas (GAQL) e orçamentos em micros ($10^{-6}$).
- Todas as redes externas impõem severos limites de taxa de requisições (*Rate Limiting* / *Throttling* / HTTP 429 / Quotas de API).
- Para manter a arquitetura de **Monólito Modular** e isolamento **Database-per-Tenant** definida em [AGENTS.md](file:///home/rony/LPR/AdMetricsPro/AGENTS.md), o pipeline de sincronização deve operar no contexto do inquilino (`TenantDbContext`), sem acoplamento direto com outros módulos (`Analytics` e `Automations`), que deverão ser notificados através de eventos in-memory desacoplados.

## Decisão

### 1. Modelo Universal de 4 Camadas
Adotou-se o modelo de domínio canônico padronizado em:
$$\text{ConnectedAdAccount} \longrightarrow \text{Campaign} \longrightarrow \text{AdSet} \longrightarrow \text{Ad}$$
- Entidades ricas de domínio (`Campaign`, `AdSet`, `Ad`) modeladas com identificadores globais (`Guid`), chaves externas estáveis (`ExternalCampaignId`, `ExternalAdSetId`, `ExternalAdId`), enums universais fortemente tipados (`CampaignStatus`, `AdSetStatus`, `AdStatus`, `AdCreativeType`) e métodos de atualização sem lançamento de exceções (`Result`).
- A entidade `Ad` preserva a `DestinationUrl` (Landing Page URL) para atender diretamente a auditoria contínua de integridade de links (Health Check) prevista na Fase 4.

### 2. Conversão Desacoplada com Mappers Especializados
Implementou-se a camada de tradutores:
- `MetaAdsHierarchyMapper`: conversão de centavos para moeda, normalização de status (`ACTIVE`, `PAUSED`, `ARCHIVED`) e extração de criativos de link do `object_story_spec`.
- `GoogleAdsHierarchyMapper`: conversão de micros (`amount_micros / 1.000.000m`), unificação de linhas do `searchStream` e extração de Responsive Search Ads (RSA) com múltiplos títulos e descrições.
- `TikTokAdsHierarchyMapper`: conversão de respostas de campanhas, grupos e anúncios da TikTok Marketing API v1.3.
- `BingAdsHierarchyMapper`: conversão de entidades da Microsoft Advertising API v13.

### 3. Resiliência com Backoff Exponencial e Jitter
Criou-se a política `HierarchyRateLimitPolicy` baseada na fórmula:
$$T = 2^{\text{attempt}} \times \text{baseDelayMs} + \text{jitter}$$
- Identificação de erros de limite de requisição (`RateLimit`, `TooManyRequests`, HTTP 429 e `QuotaExhausted`).
- Teto de retentativas configurável (padrão 3) com atraso máximo de 10 segundos.
- Falhas definitivas são encapsuladas em `Result.Failure` sem propagar exceções de fluxo.

### 4. Sincronização Paginada e Despacho Dinâmico
- O orquestrador `CampaignHierarchySyncDispatcher` roteia chamadas dinamicamente:
  - Contas reais: utilizam adaptadores com clientes HTTP tipados (`MetaAdsHierarchySyncAdapter`, `GoogleAdsHierarchySyncAdapter`, `TikTokAdsHierarchySyncAdapter`, `BingAdsHierarchySyncAdapter`) munidos de credenciais descriptografadas do `OAuthTokenVault`.
  - Contas demonstrativas (`IsDemo == true`): utilizam `DemoHierarchySyncAdapter`, gerando estruturas sintéticas de alta fidelidade para aceleração do FTUX.

### 5. Persistência Atômica no TenantDbContext e Evento In-Memory
- Repositório `CampaignHierarchyRepository` executa operações de upsert em lote atômico (`UpsertHierarchyBatchAsync`) com índices únicos compostos, prevenindo duplicações em sincronizações repetidas.
- Após o commit transacional via `IIntegrationsUnitOfWork`, o manipulador despacha o evento in-memory `CampaignHierarchySyncedEvent` via `MediatR` / `IDomainEvent`.

## Consequências

### Positivas
- Total conformidade com as regras arquiteturais e princípios de TDD do `AGENTS.md`.
- Persistência 100% isolada no banco de dados do inquilino corrente (`TenantDbContext`).
- Base estrutural pronta para suportar a ingestão de métricas diárias e horárias (Subfase 2.3) e os motores de inteligência analítica e automação.
- 100% de cobertura de testes unitários sem chamadas de rede externas.

### Negativas / Mitigações
- Volume de dados potencialmente elevado em contas com milhares de criativos: mitigado pelo processamento em lote com mapeamento por dicionários em memória e transações consolidadas.
