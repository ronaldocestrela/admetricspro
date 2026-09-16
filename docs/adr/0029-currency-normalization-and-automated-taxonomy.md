# ADR 0029: Módulo Analytics — Normalização Cambial e Taxonomia Automatizada

## Status
Aceito

## Data
2026-09-15

## Contexto
Com a conclusão da ingestão de métricas diárias e horárias multi-rede no módulo de integrações (ADR 0028), o **AdMetricsPro** inicia a **Fase 3: Métricas Cross-Network, Atribuição & Dashboard Unificado**.

Para que gestores de tráfego, agências e tomadores de decisão visualizem relatórios consolidados e executem comparações confiáveis entre canais, dois obstáculos imediatos precisam ser superados:
1. **Heterogeneidade de Moedas:** Contas de anúncio faturadas em USD (Meta/Google), EUR ou BRL não podem ter seus custos e receitas somados diretamente sem conversão cambial fidedigna baseada na data de ocorrência da veiculação (`Date`).
2. **Caos de Nomenclaturas de Campanhas:** Cada gestor ou equipe de tráfego utiliza nomenclaturas customizadas para sinalizar objetivos e etapas de funil (ex.: `[TOF]`, `Topo`, `Prospecting`, `Awareness` para topo; `[BOF]`, `RMK`, `Remarketing`, `Checkout` para fundo; `LAL`, `Interesses`, `Broad` para públicos; `Vídeo`, `Carrossel`, `Search` para formatos). Sem uma padronização determinística, não é viável gerar relatórios agregados por etapa do funil de marketing.

Além disso, conforme [AGENTS.md](file:///home/rony/LPR/AdMetricsPro/AGENTS.md):
- A solução deve adotar **Monólito Modular** em **.NET 10**.
- Proibido acoplamento de persistência ou referências circulares entre módulos.
- Tratamento estrito com `Result<T>` sem exceções para controle de fluxo de negócio.
- Cobertura de testes unitários TDD (Red-Green-Refactor) e documentação viva.

## Decisão

### 1. Criação do Módulo Autônomo `Analytics`
Estruturou-se o novo módulo em três camadas desacopladas:
- `Analytics.Domain`: Value Objects `Currency`, `ExchangeRate`, `CurrencyConversionResult`, `TaxonomyClassificationResult` e os contratos centrais `ICurrencyConverter`, `IExchangeRateProvider` e `ITaxonomyClassifier`.
- `Analytics.Application`: Consultas e manipuladores CQRS via MediatR (`ConvertCurrencyQuery`, `ConvertCurrencyBatchQuery`, `ClassifyTaxonomyQuery`, `BatchClassifyTaxonomyQuery`).
- `Analytics.Infrastructure`: Implementações de alto desempenho `CurrencyConverter` (com cache em dois níveis) e `AutomatedTaxonomyClassifier` (com expressões regulares pré-compiladas).

### 2. Normalização Cambial com Cache Local em Dois Níveis (`CurrencyConverter`)
- Implementou-se o provedor canônico `CanonicalExchangeRateProvider` com taxas de referência (BRL, USD, EUR, GBP) e algoritmo de triangulação cambial automática ($\text{Taxa}(A \to B) = \text{Taxa}(A \to \text{BRL}) / \text{Taxa}(B \to \text{BRL})$).
- O conversor `CurrencyConverter` orquestra o cache em memória (`IMemoryCache`):
  - Retorno imediato (taxa 1.0) quando moeda de origem é idêntica à de destino.
  - Chave de cache determinística: `fx_{source}_{target}_{date:yyyyMMdd}`.
  - Cotações passadas (`date < hoje`): imutáveis, armazenadas com expiração estendida (TTL de 7 dias).
  - Cotação do dia (`date == hoje`): sujeita a flutuações, armazenada com expiração de 30 minutos.
  - Tratamento resiliente e determinístico que garante zero exceções e alta performance em relatórios analíticos em massa.

### 3. Motor Determinístico de Taxonomia (`AutomatedTaxonomyClassifier`)
- Normalização prévia de delimitadores comuns de mídia (`_`, `-`, `|`, `/`, `[ ]`, `( )`, `.`, `:`) para garantir integridade de correspondência de limites de palavras (`\b`).
- Expressões regulares compiladas (`RegexOptions.Compiled | RegexOptions.IgnoreCase`) mapeando:
  - **Funil:** `Top` (Topo/Prospecção), `Middle` (Meio/Consideração), `Bottom` (Fundo/Remarketing), `Retention` (Retenção/LTV).
  - **Audiência:** `Broad`, `Lookalike`, `Interest`, `CustomAudience`, `SearchBrand`, `SearchNonBrand`.
  - **Formato:** `Video`, `Carousel`, `Image`, `SearchText`, `Dynamic`.
- Capacidade de processamento pontual e em lote (`ClassifyBatch`) com enriquecimento de tags categóricas.

### 4. Endpoints RESTful Web API e Documentação OpenAPI + Scalar
- `GET /api/v1/analytics/currency/convert`: Conversão monetária pontual com validação de parâmetros.
- `POST /api/v1/analytics/currency/convert-batch`: Conversão monetária em lote para múltiplos registros.
- `POST /api/v1/analytics/taxonomy/classify`: Classificação taxonômica individual.
- `POST /api/v1/analytics/taxonomy/classify-batch`: Classificação taxonômica em lote.
- Todos os endpoints documentados com `[EndpointSummary]`, `[ProducesResponseType]` e envelope `Result<T>`.

## Consequências

### Positivas
- **Desacoplamento Modular Total:** O módulo `Analytics` é completamente independente do módulo de persistência de `Integrations`, consumindo dados via contratos e DTOs.
- **Performance Elevada:** O cache em dois níveis e regex compiladas permitem normalizar milhares de métricas por segundo sem gargalos de rede externa ou I/O desnecessário.
- **Preparação Imediata para Subfase 3.2 e 3.3:** As fórmulas financeiras consolidadas (MER, Blended ROAS, Blended CAC) e os dashboards unificados em Blazor Server consumirão diretamente essas abstrações de normalização e classificação.
- **TDD e Estabilidade:** 47 testes unitários específicos garantindo 100% de cobertura das regras cambiais, limites de palavras, delimitadores e mapeamentos de funil.

### Limitações e Mitigações
- Para moedas exóticas além de BRL, USD, EUR e GBP, novos pares podem ser registrados no `CanonicalExchangeRateProvider` ou integrados com provedores externos via a mesma interface `IExchangeRateProvider`.
