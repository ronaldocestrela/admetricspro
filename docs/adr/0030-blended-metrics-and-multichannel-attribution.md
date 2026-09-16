# ADR 0030: Módulo Analytics — Métricas Blended, MER e Atribuição Multicanal

## Status
Aceito

## Data
2026-09-15

## Contexto
Na gestão moderna de tráfego pago multi-plataforma (Meta Ads, Google Ads, TikTok Ads, Bing Ads), gestores enfrentam dois problemas analíticos fundamentais:
1. **Silos e Métricas Isoladas:** Cada rede de anúncios declara um ROAS próprio com critérios de janelas de atribuição distintos (ex: 7 dias clique/1 dia visualização no Meta vs 30 dias clique no Google). A soma direta das conversões declaradas frequentemente excede o total real de pedidos do e-commerce (dupla contagem). É indispensável unificar métricas em **MER (*Marketing Efficiency Ratio*)**, **Blended ROAS** e **Blended CAC**.
2. **Subestimação do Tráfego de Apoio:** Modelos baseados exclusivamente no último clique (*Last-Click*) punem desproporcionalmente redes de descoberta e prospecção (como TikTok Ads e formatos de vídeo no Meta), inflando o valor de canais de busca transacional (Google Ads Search). Faz-se mandatório disponibilizar modelos comparativos de **Primeiro Clique (*First-Touch*)**, **Último Clique (*Last-Touch*)** e **Linear ($1/N$)**, acompanhados de contagem de **Conversões Assistidas (*Assisted Conversions*)**.

Em conformidade com [AGENTS.md](file:///home/rony/LPR/AdMetricsPro/AGENTS.md):
- .NET 10 em toda a arquitetura.
- Monólito Modular sem acoplamento direto ou dependência circular.
- Padrão `Result<T>` estrito sem lançamento de exceções em regras de negócio.
- TDD Red-Green-Refactor como metodologia obrigatória.
- Documentação viva e OpenAPI + Scalar UI nos endpoints RESTful.

## Decisão

### 1. Extensão do Módulo `Analytics`
- **Camada Domain (`Analytics.Domain`):**
  - Definição dos records imutáveis `BlendedMetricInputItem`, `BlendedMetricsResult`, `BlendedChannelBreakdown`, `AttributionTouchpoint`, `ConversionJourney`, `ChannelAttributionResult` e `AttributionComparisonResult`.
  - Interfaces de domínio `IBlendedMetricsCalculator` e `IAttributionCalculator`.
- **Camada Infrastructure (`Analytics.Infrastructure`):**
  - Implementação `BlendedMetricsCalculator`: orquestra a conversão monetária multi-moeda através de `ICurrencyConverter`, calcula as fórmulas financeiras agregadas (MER, Blended ROAS, Blended CAC, CPA, CPC, CPM, CTR) com proteção contra divisão por zero e gera o detalhamento por canal com percentual de participação de investimento (`SpendSharePercentage`).
  - Implementação `AttributionCalculator`: processa coleções de jornadas, ignora toques posteriores à conversão, ordena cronologicamente os touchpoints e calcula pontuações exatas de conversão e receita para First-Touch, Last-Touch e Linear, além de computar conversões assistidas para pontos de contato intermediários.
- **Camada Application (`Analytics.Application`):**
  - Consultas CQRS `CalculateBlendedMetricsQuery` e `CalculateAttributionQuery` com seus respectivos handlers delegando aos calculadores e mapeando envelopes `Result<T>`.

### 2. Endpoints RESTful Web API e OpenAPI + Scalar UI
- `POST /api/v1/analytics/blended-metrics`: recebe métricas heterogêneas, consolida na moeda alvo e retorna todos os indicadores agregados e quebra por plataforma.
- `POST /api/v1/analytics/attribution`: processa jornadas de conversão com touchpoints e retorna a comparação side-by-side dos 3 modelos de atribuição e tráfego assistido.
- Decorados com `[EndpointSummary]`, `[ProducesResponseType]` (200 OK, 400 BadRequest) e XML Docs `<summary>`.

## Consequências

### Positivas
- **Fidelidade Financeira:** Permite que clientes e agências compreendam a saúde real das campanhas via MER e Blended ROAS, eliminando duplicações e distorções causadas por moedas diferentes.
- **Tomada de Decisão Baseada em Dados:** Com a atribuição multicanal e conversões assistidas, times de marketing conseguem justificar verbas em canais de topo de funil (TikTok/Meta) sem depender da atribuição míope de último clique.
- **Robustez e Performance:** Algoritmos determinísticos in-memory com zero chamadas externas desnecessárias, protegidos contra divisão por zero e com 100% de cobertura de testes unitários TDD (17 novos testes adicionados, elevando a suíte para 1.226 testes verdes).
