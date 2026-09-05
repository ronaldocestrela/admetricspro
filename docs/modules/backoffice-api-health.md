# Módulo Backoffice — Hub de Monitoramento de APIs e Rate Limits

## 1. Visão Geral e Objetivos

O **Hub de Monitoramento de APIs e Rate Limits** fornece visibilidade centralizada, em tempo real e preventiva sobre a saúde operacional das integrações da plataforma AdMetricsPro com as quatro principais redes de anúncios:
- **Meta Graph API** (Facebook & Instagram Ads)
- **Google Ads API**
- **TikTok Marketing API**
- **Bing Ads API** (Microsoft Advertising)

### 1.1 O Princípio da Trava Preventiva de 80%
As APIs de mídia paga impõem severos limites de taxa (*rate limits*) por aplicativo e por conta de anúncios. Ultrapassar tais limites acarreta bloqueios de requisições (*throttling*), suspensão de tokens corporativos de desenvolvedor e interrupção das automações e regras de corte de gastos dos inquilinos.

Para evitar indisponibilidades proativamente, o agregador `ApiQuotaTracker`:
- Rastreia o consumo acumulado de requisições e operações na janela temporal corrente.
- Calcula continuamente o percentual de utilização (`UsagePercentage = (CurrentConsumption / MaxLimit) * 100`).
- Ao atingir **80% do teto**, transiciona o estado para `Warning` e emite o evento de domínio `ApiQuotaThresholdWarningEvent`, alertando a equipe de engenharia e permitindo o escalonamento ou throttling preventivo de sincronizações de baixa prioridade.
- Ao atingir **95% do teto**, transiciona o estado para `Critical`.
- Ao atingir **100% do teto**, sinaliza `Exceeded`.

### 1.2 Monitoramento da Saúde dos Tokens dos Inquilinos
Além do consumo global das cotas de desenvolvedor, o módulo monitora os tokens de autorização OAuth dos tenants:
- **Conectado (`Connected`):** Token ativo e com validade superior a 7 dias.
- **Vencendo em Breve (`ExpiringSoon`):** Token com expiração prevista para os próximos 7 dias (D-7), exigindo notificação automática ao inquilino para renovação proativa.
- **Expirado (`Expired`):** Token cuja data de expiração foi ultrapassada.
- **Revogado (`Revoked`):** Acesso cancelado pelo usuário na plataforma de mídia.
- **Desconectado (`Disconnected`):** Falha permanente de autenticação ou credenciais corrompidas.

---

## 2. Contratos de API e Endpoints

### 2.1 Visão Geral Consolidada da Saúde das APIs
- **Rota:** `GET /api/v1/admin/api-health`
- **Sumário OpenAPI:** `Obtém o resumo operacional consolidado de cotas de APIs e saúde de conexões de inquilinos`
- **Autenticação:** SuperAdmin / Operador Corporativo.

#### Exemplo de Resposta de Sucesso (HTTP 200 OK):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": "",
    "description": "",
    "type": 0
  },
  "value": {
    "platformQuotas": [
      {
        "platform": 1,
        "platformName": "Meta Graph API",
        "maxLimit": 100000,
        "currentConsumption": 82450,
        "usagePercentage": 82.45,
        "alertLevel": 1,
        "isWarning": true,
        "windowDuration": "01:00:00",
        "windowStartUtc": "2026-09-03T16:00:00Z",
        "lastUpdatedUtc": "2026-09-03T16:54:12Z"
      },
      {
        "platform": 2,
        "platformName": "Google Ads API",
        "maxLimit": 500000,
        "currentConsumption": 142300,
        "usagePercentage": 28.46,
        "alertLevel": 0,
        "isWarning": false,
        "windowDuration": "24:00:00",
        "windowStartUtc": "2026-09-03T00:00:00Z",
        "lastUpdatedUtc": "2026-09-03T16:53:45Z"
      },
      {
        "platform": 3,
        "platformName": "TikTok Marketing API",
        "maxLimit": 60000,
        "currentConsumption": 12100,
        "usagePercentage": 20.17,
        "alertLevel": 0,
        "isWarning": false,
        "windowDuration": "01:00:00",
        "windowStartUtc": "2026-09-03T16:00:00Z",
        "lastUpdatedUtc": "2026-09-03T16:52:10Z"
      },
      {
        "platform": 4,
        "platformName": "Bing Ads API",
        "maxLimit": 30000,
        "currentConsumption": 29100,
        "usagePercentage": 97.00,
        "alertLevel": 2,
        "isWarning": true,
        "windowDuration": "01:00:00",
        "windowStartUtc": "2026-09-03T16:00:00Z",
        "lastUpdatedUtc": "2026-09-03T16:54:30Z"
      }
    ],
    "totalConnections": 184,
    "connectedCount": 162,
    "expiringSoonCount": 11,
    "expiredCount": 7,
    "revokedOrDisconnectedCount": 4,
    "timestampUtc": "2026-09-03T16:55:00Z"
  }
}
```

---

### 2.2 Listagem Filtrada de Conexões dos Inquilinos
- **Rota:** `GET /api/v1/admin/api-health/connections`
- **Query Params:**
  - `platform` (opcional: `1` = Meta, `2` = Google, `3` = TikTok, `4` = Bing)
  - `status` (opcional: `0` = Connected, `1` = ExpiringSoon, `2` = Expired, `3` = Revoked, `4` = Disconnected)
  - `pageNumber` (int, default `1`)
  - `pageSize` (int, default `20`)
- **Sumário OpenAPI:** `Lista conexões de APIs de inquilinos com suporte a filtros de plataforma e saúde do token`

#### Exemplo de Resposta de Sucesso (HTTP 200 OK):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": "",
    "description": "",
    "type": 0
  },
  "value": [
    {
      "id": "e3a89bc2-4217-48f6-b184-c8c360b91501",
      "tenantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "tenantName": "Agência Growth Marketing Ltda",
      "platform": 1,
      "platformName": "Meta Graph API",
      "accountIdentifier": "act_849201938",
      "accountName": "E-Commerce Black Friday 2026",
      "status": 1,
      "tokenExpiresAtUtc": "2026-09-06T14:30:00Z",
      "lastSyncAtUtc": "2026-09-03T16:40:00Z",
      "errorMessage": "O token de autenticação expira em breve (3 dias).",
      "updatedAtUtc": "2026-09-03T16:40:00Z"
    },
    {
      "id": "7b8a1c3d-9e0f-4123-8abc-9876543210ab",
      "tenantId": "c9a646d3-9c61-4cb7-bfcd-ee2522c8f633",
      "tenantName": "Varejo Express S.A.",
      "platform": 4,
      "platformName": "Bing Ads API",
      "accountIdentifier": "bing_992019",
      "accountName": "Campanhas Institucionais",
      "status": 3,
      "tokenExpiresAtUtc": null,
      "lastSyncAtUtc": "2026-09-01T10:15:00Z",
      "errorMessage": "OAuth authorization revoked by user on Microsoft Advertising account.",
      "updatedAtUtc": "2026-09-02T08:00:00Z"
    }
  ]
}
```

---

### 2.3 Registro de Consumo de Operações
- **Rota:** `POST /api/v1/admin/api-health/usage`
- **Sumário OpenAPI:** `Registra unidades consumidas de API de uma rede de anúncios`

#### Exemplo de Payload de Entrada (`RecordUsageApiRequest`):
```json
{
  "platform": 1,
  "units": 150,
  "timestampUtc": "2026-09-03T16:55:00Z"
}
```

#### Resposta de Sucesso (HTTP 200 OK):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": "",
    "description": "",
    "type": 0
  },
  "value": {
    "platform": 1,
    "platformName": "Meta Graph API",
    "maxLimit": 100000,
    "currentConsumption": 82600,
    "usagePercentage": 82.60,
    "alertLevel": 1,
    "isWarning": true,
    "windowDuration": "01:00:00",
    "windowStartUtc": "2026-09-03T16:00:00Z",
    "lastUpdatedUtc": "2026-09-03T16:55:00Z"
  }
}
```

#### Resposta de Erro de Validação (HTTP 422 UnprocessableEntity):
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "ApiQuota.InvalidUnits",
    "description": "O consumo registrado deve ser maior que zero.",
    "type": 1
  },
  "value": null
}
```

---

## 3. Matriz de Erros de Negócio

| Código do Erro | Tipo | Descrição |
| :--- | :--- | :--- |
| `ApiQuota.InvalidMaxLimit` | Validação | O teto máximo de cota configurado deve ser estritamente superior a zero. |
| `ApiQuota.InvalidThreshold` | Validação | O limiar de alerta preventivo deve estar compreendido no intervalo (0%, 100%]. |
| `ApiQuota.InvalidUnits` | Validação | O volume de unidades consumidas registrado deve ser maior que zero. |
| `ApiQuota.InvalidWindowDuration` | Validação | A duração da janela de cota temporal deve ser superior a zero. |
| `ApiConnection.InvalidParameters` | Validação | O `TenantId`, nome da organização e identificador da conta de anúncios são obrigatórios. |
| `ApiConnection.NotFound` | NotFound | Conexão de integração não localizada para o identificador informado. |

---

## 4. Componente Visual Blazor: `ApiHealthPage.razor` e `ApiHealthDashboard.razor`

A funcionalidade é disponibilizada no Backoffice Executivo na rota `/api-health` (e `/admin/api-health` em WebApp), seguindo o **Design System Executivo do Backoffice** (`backoffice.css`):

1. **Topologia de Cabeçalho Executivo (`.page-header`):**
   - `.page-category`: `"Backoffice Global · Operação & Conectividade"`.
   - `.page-title`: `"Monitor de Integrações & Rate Limits"`.
   - `.page-description`: Resumo em tempo real do monitoramento de limites de taxa.
   - `.header-actions`: Botão primário com ícone SVG (`.btn-primary`) para atualização e recarregamento da telemetria sob demanda.

2. **Quotas Cards Grid (`.quotas-grid`):** Quatro cards com fundo `--bg-card` e elevação (Meta, Google, TikTok, Bing). Cada card renderiza:
   - Ícone estilizado com gradiente oficial da plataforma de anúncios.
   - Medidor de consumo numérico e barra de progresso horizontal (`.progress-track`, `.progress-bar`) com marcador explícito de 80% (`.threshold-marker.threshold-80`).
   - Indicador visual contextual:
     - **Verde (< 80%):** Operação regular (`.bar-normal`, `.status-pill.quota-normal`).
     - **Âmbar / Amarelo (80% a 94.9%):** Badge pulsante `"⚠️ ALERTA PREVENTIVO (80%+)"` (`.badge-quota-warning`) com borda iluminada.
     - **Vermelho (>= 95%):** Badge `"🚨 CRÍTICO (95%+)"` (`.badge-quota-warning.badge-critical`) com borda avermelhada pulsante.

3. **KPIs de Saúde dos Tokens (`.kpi-grid`, `.kpi-card`):**
   - Substituição de emojis por ícones vetoriais SVG padronizados em `.kpi-icon-wrapper`:
     - **Total de Integrações:** Wrapper azul com globo vetorial.
     - **Tokens Ativos & Saudáveis:** Wrapper verde com check circunscrito.
     - **Vencendo em até 7 Dias (D-7):** Wrapper âmbar com relógio de expiração.
     - **Expirados ou Revogados:** Wrapper vermelho com triângulo de atenção.

4. **Tabela de Credenciais de Inquilinos (`.connections-table`):**
   - Container em `--bg-card` com borda sutil, filtros com selects padronizados (`.filter-select`), linhas com hover executivo, tags de plataforma (`.platform-tag`) e badges pill de status (`.badge-status`).

