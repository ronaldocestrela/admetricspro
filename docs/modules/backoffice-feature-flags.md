# Módulo: Backoffice — Feature Flags & Kill Switches Operacionais

## 1. Visão Geral e Responsabilidades

O módulo de **Feature Flags & Kill Switches Operacionais** é responsável pela governança de lançamentos de recursos e proteção emergencial contra falhas catastróficas ou instabilidades em integrações externas no ecossistema AdMetricsPro.

### Principais Capacidades:
1. **Disjuntores Operacionais de Emergência (Kill Switches):**
   - Circuit-breakers operacionais capazes de interromper instantaneamente a execução do motor de automação cross-network e ingestão de dados em segundo plano.
   - Suporte a corte seletivo por rede de anúncios: **Global**, **Meta Ads**, **Google Ads**, **TikTok Ads** e **Bing Ads**.
   - Congelamento atômico e determinístico: se o disjuntor global ou o disjuntor da rede estiver engatado, qualquer execução de regra de automação naquela rede é suspensa com erro semântico de negócio.
   - Trilha de auditoria imutável via `IMasterAuditService`: armar ou desarmar um Kill Switch exige justificativa técnica obrigatória e registra o operador responsável no catálogo central `MasterDb`.

2. **Lançamentos Progressivos (Staged Feature Flags):**
   - **Rollout Percentual Determinístico:** Distribuição estocástica consistente baseada em hash SHA-256 da chave composta `"{flagKey}:{tenantId}"`. O inquilino permanece de forma estável no mesmo bucket percentual (0 a 99) sem variações espúrias.
   - **Segmentação por Inquilinos (Allowlist):** Liberação antecipada ou direcionada para inquilinos beta e parceiros estratégicos (`TargetingType.TenantList`).
   - **Chaveamento Global:** Habilitação ou desligamento geral para 100% da base.

3. **Performance e Cache In-Memory:**
   - Avaliação ultrarrápida via `IFeatureFlagService` com cache in-memory thread-safe (`IMemoryCache`).
   - Invalidação atômica e imediata de chaves e prefixos sob qualquer mutação, ativação ou desativação de Kill Switch.

---

## 2. Padrões de Segurança e Contratos de Dados

### 2.1 Enum: Estratégias de Segmentação (`FeatureFlagTargetingType`)

| Valor | Nome | Descrição |
|---|---|---|
| `0` | `Global` | Liberação para toda a base de inquilinos (100%). |
| `1` | `PercentageRollout` | Liberação gradual (0% a 100%) calculada via bucket hash do TenantId. |
| `2` | `TenantList` | Liberação restrita aos inquilinos explicitamente listados em `TargetTenantIds`. |

---

## 3. Especificação dos Endpoints REST

Base URL: `/api/v1/admin/feature-flags`

### 3.1 Listar Todas as Feature Flags e Kill Switches

- **Método:** `GET`
- **Rota:** `/api/v1/admin/feature-flags`
- **Sumário:** Obtém a lista completa de feature flags e disjuntores operacionais.
- **Códigos HTTP:** `200 OK`.

#### Payload de Retorno (Exemplo):
```json
{
  "isSuccess": true,
  "value": [
    {
      "id": "b0000001-0000-0000-0000-000000000001",
      "key": "killswitch.automation.global",
      "name": "Kill Switch Global de Automações Cross-Network",
      "description": "Disjuntor operacional que congela instantaneamente a execução de todas as regras de automação em todas as redes de anúncios.",
      "isEnabled": false,
      "isKillSwitch": true,
      "targetingType": "Global",
      "rolloutPercentage": 100,
      "targetTenantIds": [],
      "killSwitchActivatedAtUtc": null,
      "killSwitchReason": null,
      "killSwitchTriggeredBy": null,
      "createdBy": "system@admetricspro.com",
      "createdAtUtc": "2026-09-03T19:00:00Z",
      "updatedAtUtc": "2026-09-03T19:00:00Z",
      "updatedBy": null
    },
    {
      "id": "b0000001-0000-0000-0000-000000000007",
      "key": "feature.analytics.mer-v2",
      "name": "Motor de Atribuição e MER v2",
      "description": "Novo algoritmo avançado de Marketing Efficiency Ratio com deduplicação de conversões cross-channel.",
      "isEnabled": true,
      "isKillSwitch": false,
      "targetingType": "PercentageRollout",
      "rolloutPercentage": 20,
      "targetTenantIds": [],
      "killSwitchActivatedAtUtc": null,
      "killSwitchReason": null,
      "killSwitchTriggeredBy": null,
      "createdBy": "system@admetricspro.com",
      "createdAtUtc": "2026-09-03T19:00:00Z",
      "updatedAtUtc": "2026-09-03T19:00:00Z",
      "updatedBy": null
    }
  ],
  "error": null
}
```

---

### 3.2 Verificar Status Geral do Motor de Automações

- **Método:** `GET`
- **Rota:** `/api/v1/admin/feature-flags/automation-status?platform={platform}`
- **Sumário:** Retorna o status de congelamento do motor de automação (global ou por rede).
- **Parâmetros:**
  - `platform` (opcional, query): `Meta`, `Google`, `TikTok`, `Bing`.
- **Códigos HTTP:** `200 OK`.

#### Payload de Retorno (Exemplo):
```json
{
  "isSuccess": true,
  "value": {
    "isFrozen": true,
    "platform": "Meta",
    "globalKillSwitchActive": false,
    "platformKillSwitchActive": true,
    "statusMessage": "O subsistema de automações para a plataforma 'Meta' está congelado por intervenção operacional emergencial.",
    "checkedAtUtc": "2026-09-03T20:15:30Z"
  },
  "error": null
}
```

---

### 3.3 Acionar Disjuntor de Emergência (Ativar Kill Switch)

- **Método:** `POST`
- **Rota:** `/api/v1/admin/feature-flags/{key}/kill-switch/activate`
- **Sumário:** Congela imediatamente o subsistema operacional protegido pela chave.
- **Códigos HTTP:** `200 OK`, `404 Not Found`, `422 Unprocessable Entity`.

#### Payload de Entrada:
```json
{
  "reason": "Instabilidade de latência crítica e rate limit excessivo na API da Meta Ads (Graph API v20).",
  "triggeredBy": "ops-lead@admetricspro.com"
}
```

#### Payload de Retorno Sucesso (`200 OK`):
```json
{
  "isSuccess": true,
  "error": null
}
```

#### Payload de Retorno Falha (`422 Unprocessable Entity`):
```json
{
  "isSuccess": false,
  "error": {
    "code": "KillSwitch.ReasonRequired",
    "description": "Uma justificativa operacional clara (mínimo de 5 caracteres) é obrigatória para acionar um Kill Switch.",
    "type": 1
  }
}
```

---

### 3.4 Desativar Disjuntor de Emergência (Restauração Operacional)

- **Método:** `POST`
- **Rota:** `/api/v1/admin/feature-flags/{key}/kill-switch/deactivate`
- **Sumário:** Desativa o disjuntor e restaura o funcionamento normal do subsistema.
- **Códigos HTTP:** `200 OK`, `404 Not Found`, `422 Unprocessable Entity`.

#### Payload de Entrada:
```json
{
  "reason": "Normalização comprovada dos endpoints da Meta após mitigação do incidente INC-4829.",
  "triggeredBy": "ops-lead@admetricspro.com"
}
```

---

### 3.5 Avaliar Ativação de Feature Flag para Tenant

- **Método:** `GET`
- **Rota:** `/api/v1/admin/feature-flags/{key}/evaluate?tenantId={tenantId}`
- **Sumário:** Avalia deterministicamente se a feature flag está habilitada para o inquilino informado.
- **Códigos HTTP:** `200 OK`.

#### Payload de Retorno:
```json
{
  "isSuccess": true,
  "value": true,
  "error": null
}
```

---

## 4. Casos de Borda e Erros de Negócio

| Código do Erro | Descrição |
|---|---|
| `FeatureFlag.NotFound` | A flag ou disjuntor com a chave fornecida não foi localizado no catálogo. |
| `KillSwitch.NotAKillSwitch` | A chave informada pertence a uma feature flag funcional, não a um Kill Switch. |
| `KillSwitch.ReasonRequired` | O operador não informou justificativa válida ou forneceu menos de 5 caracteres. |
| `FeatureFlag.InvalidRollout` | O percentual de rollout deve estar estritamente contido no intervalo de 0 a 100. |
| `FeatureFlag.KeyRequired` | A chave identificadora da flag é obrigatória. |
| `FeatureFlag.DuplicateKey` | Já existe uma flag cadastrada com a chave informada no MasterDb. |
| `FeatureFlag.TenantListRequired` | Para estratégias `TenantList`, ao menos um inquilino deve ser configurado. |

---

## 5. Interface Visual do Backoffice: `FeatureFlagsPage.razor` e `FeatureFlagsDashboard.razor`

A página é disponibilizada na rota `/feature-flags` (e `/admin/feature-flags` em WebApp), incorporando o **Design System Executivo do Backoffice** (`backoffice.css`):

1. **Topologia de Cabeçalho Executivo (`.page-header`):**
   - `.page-category`: `"Backoffice Global · Governança Operacional"`.
   - `.page-title`: `"Feature Flags & Kill Switches Operacionais"`.
   - `.page-description`: Controle centralizado de disjuntores de segurança e rollouts progressivos.
   - `.header-actions`: Botão de ação primária com ícone SVG (`.btn-primary`) para atualização do catálogo.

2. **Banner de Congelamento Operacional de Emergência (`.freeze-alert-banner`):**
   - Exibido dinamicamente quando um ou mais Kill Switches estão armados.
   - Ícone vetorial SVG pulsante (`.alert-icon-pulse`), contorno vermelho de advertência com sombra luminosa e listagem de switches ativos com autor e motivo.

3. **Grade de Disjuntores de Emergência (`.kill-switches-grid`):**
   - Cartões estruturados (`.kill-switch-card`) com indicador de estado (`.badge-safe` ou `.badge-danger`).
   - Identificador técnico em monospace (`.ks-key`), nome, descrição e detalhes do disparo (quando ativo).
   - Botões de corte emergencial (`.btn-ks-freeze`) e restauração (`.btn-ks-restore`) acoplados ao diálogo modal de confirmação com dupla checagem (`ConfirmActionDialog`).

4. **Tabela de Feature Flags Funcionais (`.flags-table`):**
   - Barra de pesquisa integrada com input estilizado (`.search-input`).
   - Badges de segmentação (`.targeting-badge` para Global, Rollout % e Allowlist).
   - Toggle switch visual com transição suave e slider de ajuste fino de percentual de rollout (`.rollout-slider`).

