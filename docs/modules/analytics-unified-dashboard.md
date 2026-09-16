# Especificação de Módulo: Dashboard Executivo Unificado no Blazor Server

Este documento especifica a arquitetura técnica, os contratos da Web API, os componentes de apresentação Blazor Server e as regras de negócio do **Dashboard Executivo Unificado** no **AdMetricsPro**, desenvolvido na **Subfase 3.3** do roadmap conforme os princípios inegociáveis do [AGENTS.md](file:///home/rony/LPR/AdMetricsPro/AGENTS.md).

---

## 1. Visão Geral e Princípios Arquiteturais

O Dashboard Executivo Unificado fornece visão consolidada e em tempo real do desempenho de mídia paga através das 4 redes nativas suportadas (**Meta Ads, Google Ads, TikTok Ads e Bing Ads**).

### Princípios Chave:
1. **Frontend Estritamente de Apresentação (Zero Acesso a Banco):** A camada Blazor (`WebApp`) nunca referencia `Infrastructure`, `DbContext` ou repositórios. Todo o consumo analítico é efetuado via cliente HTTP tipado (`IAnalyticsDashboardClientService`) contra o endpoint versionado `/api/v1/analytics/dashboard/executive`, tratando envelopes `Result<T>`.
2. **Cálculo Automático de Período Anterior:** O backend determina a duração do período selecionado ($D = \text{End} - \text{Start}$) e projeta a janela comparativa anterior equivalente ($\text{PreviousEnd} = \text{Start} - 1$, $\text{PreviousStart} = \text{PreviousEnd} - D$).
3. **Polaridade Invertida de Custos:** Para métricas de custo (CPC e CPA), redução é classificada como melhoria (`IsPositiveImprovement = true`), renderizando badges verdes. Para métricas de retorno e volume (Spend, CTR, CPM, ROAS), crescimento é tratado como positivo.
4. **Gráficos SVG Vetoriais Nativos:** Construídos inteiramente em vetores SVG responsivos dentro do Blazor, sem dependência de pacotes npm pesados ou bibliotecas JavaScript instáveis, garantindo fidelidade visual e alta performance sobre conexões SignalR.
5. **Exportação Multi-Formato Rápida:** Geração de planilha CSV estruturada com codificação UTF-8 com BOM e captura gráfica/vetorial para relatórios.

---

## 2. Contratos da Web API

### 2.1 Endpoint: `GET /api/v1/analytics/dashboard/executive`

#### Parâmetros de Query:
| Parâmetro | Tipo | Obrigatório | Descrição |
| :--- | :--- | :--- | :--- |
| `workspaceId` | `Guid?` | Não | Filtra métricas de um cliente/workspace específico. |
| `startDateUtc` | `DateTime?` | Não | Data inicial UTC (padrão: D-6). |
| `endDateUtc` | `DateTime?` | Não | Data final UTC (padrão: hoje). |
| `platform` | `string?` | Não | Canal específico (Meta, Google, TikTok, Bing) ou "All". |
| `device` | `string?` | Não | Dispositivo (Mobile, Desktop, Tablet) ou "All". |
| `currency` | `string?` | Não | Moeda de consolidação (padrão: BRL). |

#### Headers Requeridos:
- `X-Tenant-Id`: Identificador do Tenant ativo (resolvido pelo middleware multitenant).

---

### 2.2 Exemplo de Resposta de Sucesso (`Result<ExecutiveDashboardDto>`)

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": null,
    "description": null,
    "type": 0
  },
  "value": {
    "startDateUtc": "2026-09-08T00:00:00Z",
    "endDateUtc": "2026-09-14T00:00:00Z",
    "previousStartDateUtc": "2026-09-01T00:00:00Z",
    "previousEndDateUtc": "2026-09-07T00:00:00Z",
    "currency": "BRL",
    "metrics": [
      {
        "metricKey": "Spend",
        "label": "Investimento Total",
        "currentValue": 5420.00,
        "previousValue": 4850.00,
        "percentageChange": 11.75,
        "isPositiveImprovement": true,
        "unitFormat": "Currency"
      },
      {
        "metricKey": "Cpc",
        "label": "Custo por Clique (CPC)",
        "currentValue": 1.35,
        "previousValue": 1.62,
        "percentageChange": -16.67,
        "isPositiveImprovement": true,
        "unitFormat": "Currency"
      },
      {
        "metricKey": "Cpm",
        "label": "Custo por Mil Impressões (CPM)",
        "currentValue": 14.50,
        "previousValue": 16.20,
        "percentageChange": -10.49,
        "isPositiveImprovement": true,
        "unitFormat": "Currency"
      },
      {
        "metricKey": "Ctr",
        "label": "Taxa de Cliques (CTR)",
        "currentValue": 3.85,
        "previousValue": 3.20,
        "percentageChange": 20.31,
        "isPositiveImprovement": true,
        "unitFormat": "Percentage"
      },
      {
        "metricKey": "Cpa",
        "label": "Custo por Aquisição (CPA)",
        "currentValue": 28.50,
        "previousValue": 34.00,
        "percentageChange": -16.18,
        "isPositiveImprovement": true,
        "unitFormat": "Currency"
      },
      {
        "metricKey": "Roas",
        "label": "Retorno sobre Ad Spend (ROAS)",
        "currentValue": 4.65,
        "previousValue": 3.90,
        "percentageChange": 19.23,
        "isPositiveImprovement": true,
        "unitFormat": "Multiplier"
      }
    ],
    "timeSeries": [
      {
        "date": "2026-09-08T00:00:00Z",
        "spend": 750.00,
        "revenue": 3450.00,
        "roas": 4.60,
        "clicks": 550,
        "impressions": 52000,
        "conversions": 26
      }
    ],
    "platformBreakdown": [
      {
        "platform": "Meta",
        "spend": 2710.00,
        "revenue": 12600.00,
        "roas": 4.65,
        "shareOfSpendPercentage": 50.00,
        "clicks": 2000,
        "conversions": 95
      },
      {
        "platform": "Google",
        "spend": 1897.00,
        "revenue": 9105.00,
        "roas": 4.80,
        "shareOfSpendPercentage": 35.00,
        "clicks": 1400,
        "conversions": 66
      },
      {
        "platform": "TikTok",
        "spend": 542.00,
        "revenue": 2168.00,
        "roas": 4.00,
        "shareOfSpendPercentage": 10.00,
        "clicks": 450,
        "conversions": 19
      },
      {
        "platform": "Bing",
        "spend": 271.00,
        "revenue": 1084.00,
        "roas": 4.00,
        "shareOfSpendPercentage": 5.00,
        "clicks": 200,
        "conversions": 10
      }
    ],
    "deviceBreakdown": [
      {
        "device": "Mobile",
        "spend": 3794.00,
        "clicks": 2800,
        "conversions": 133,
        "roas": 4.70,
        "shareOfSpendPercentage": 70.00
      },
      {
        "device": "Desktop",
        "spend": 1355.00,
        "clicks": 1050,
        "conversions": 48,
        "roas": 4.55,
        "shareOfSpendPercentage": 25.00
      },
      {
        "device": "Tablet",
        "spend": 271.00,
        "clicks": 200,
        "conversions": 9,
        "roas": 4.30,
        "shareOfSpendPercentage": 5.00
      }
    ]
  }
}
```

---

## 3. Componentes Blazor Server Desenvolvidos

| Componente | Responsabilidade |
| :--- | :--- |
| `ExecutiveDashboardView.razor` | Contêiner mestre que orquestra filtros, grids de métricas, gráficos e exportação. |
| `DashboardFiltersBar.razor` | Barra de controles com seletores de Workspace, Canal (Meta, Google, TikTok, Bing), Período e Dispositivo. |
| `MetricCardsGrid.razor` | Grade responsiva com os 6 cartões de KPIs principais e esqueleto animado (skeleton loader). |
| `MetricCard.razor` | Cartão individual com valor formatado, delta percentual, indicação de polaridade e valor anterior. |
| `PerformanceTrendChart.razor` | Gráfico vetorial SVG de evolução diária (Spend vs Receita / ROAS) com tooltips nos nós de dados. |
| `PlatformShareDonutChart.razor` | Gráfico Donut em SVG com cálculo dinâmico de arcos para divisão de verba entre as 4 plataformas. |
| `DevicePerformanceBarChart.razor` | Gráfico de barras de progresso comparativo de desempenho por hardware (Mobile, Desktop, Tablet). |
| `DashboardExportActions.razor` | Botões de exportação rápida para planilha CSV formatada (UTF-8 BOM) e captura gráfica/vetorial. |

---

## 4. Exportação Rápida (Subfase 3.3.3)

1. **CSV Estruturado:**
   - Delimitador `;` configurado para compatibilidade nativa com Excel PT-BR sem necessidade de assistente de importação.
   - Cabeçalho com metadados do período corrente e comparativo.
   - 4 seções ordenadas: Métricas Principais, Evolução Diária, Participação por Canal e Participação por Dispositivo.
   - Download disparado via blob em `dashboard-export.js`.
2. **Exportação de Imagem / Relatório:**
   - Disparo do modo de impressão otimizado e vetorial via `window.print()` e captura de snapshot em alta resolução.

---

## 5. Cobertura de Testes Automatizados

- **Backend:**
  - `ExecutiveDashboardCalculatorTests.cs`: 4 testes unitários cobrindo invariantes matemáticas, divisão por zero e polaridade invertida de custos.
  - `GetExecutiveDashboardQueryTests.cs`: 2 testes unitários cobrindo cálculo da duração do período comparativo e orquestração do handler.
  - `AnalyticsExecutiveDashboardControllerTests.cs`: 2 testes de aceitação no controller Web API validando 200 OK e 400 BadRequest.
- **Frontend (bUnit):**
  - `MetricCardsGridTests.cs`: 4 testes cobrindo renderização dos 6 cartões, valores formatados, polaridade semântica de CPA/CPC e skeleton loader.
  - `DashboardExportActionsTests.cs`: 5 testes cobrindo botões habilitados/desabilitados, estrutura do CSV e invocação dos métodos JSInterop.
  - `ExecutiveDashboardViewTests.cs`: 3 testes cobrindo renderização completa de componentes, reatividade ao alterar filtros e exibição de alertas de erro.
  - `TenantDashboardPageTests.cs`: 7 testes de integração do painel geral cobrindo cabeçalho personalizado, FTUX, canais conectados e a visão executiva integrada.
