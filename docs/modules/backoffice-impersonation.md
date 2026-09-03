# Módulo Backoffice — Mecanismo de Impersonation Seguro ("Shadow Mode")

## 1. Visão Geral e Objetivos

O recurso de **Impersonation Seguro ("Shadow Mode")** permite que operadores de suporte técnico e Super Admins acessem a interface e as operações de um Tenant específico em nome da empresa atendida, para diagnóstico de erros, reprodução de comportamentos inesperados em campanhas e auditoria de integrações.

### Princípios de Segurança
- **Não requer senha:** O operador nunca solicita ou redefine a senha do cliente final.
- **Auditoria obrigatória:** O chamado de suporte (`SupportTicketId`) e a justificativa técnica (`Reason`) com no mínimo 10 caracteres são exigidos em tempo de emissão.
- **Validade temporal restrita:** Duração parametrizada entre 5 e 120 minutos (padrão 30 minutos).
- **Proteção de dados bancários/faturamento:** O modo Shadow Mode ativa a política de mascaramento em tempo real, ocultando números de cartão de crédito e documentos confidenciais.

---

## 2. Contratos de API e Endpoints

### 2.1 Emissão de Token de Impersonation

- **Rota:** `POST /api/v1/tenants/{tenantId}/impersonate`
- **Sumário OpenAPI:** `Emite token JWT contextual de impersonação (Shadow Mode) para suporte técnico auditado`
- **Autenticação:** Requer credenciais de operador corporativo (SuperAdmin / Suporte Nível 1).

#### Exemplo de Payload de Entrada (`ImpersonateTenantApiRequest`):
```json
{
  "superAdminId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "supportTicketId": "INC-84920",
  "reason": "Investigação de divergência de sincronização de métricas do TikTok Ads",
  "durationMinutes": 45
}
```

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
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresInSeconds": 2700,
    "sessionId": "4f5e6d7c-8b9a-0123-4567-89abcdef0123",
    "tenantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "tenantName": "Agência Growth Marketing Ltda",
    "superAdminId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "supportTicketId": "INC-84920",
    "expiresAtUtc": "2026-09-03T16:45:00Z"
  }
}
```

#### Exemplo de Resposta de Falha de Validação (HTTP 422 UnprocessableEntity):
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Validation.General",
    "description": "Validation failed: 'Support ticket identifier is required.', 'Impersonation reason must contain at least 10 characters.'",
    "type": 1
  },
  "value": null
}
```

#### Exemplo de Resposta de Tenant Não Encontrado (HTTP 404 NotFound):
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Tenant.NotFound",
    "description": "Tenant not found for the specified identifier.",
    "type": 2
  },
  "value": null
}
```

### 2.2 Encerramento de Sessão de Impersonation
- **Rota:** `POST /api/v1/tenants/{tenantId}/impersonate/{sessionId}/terminate`
- **Sumário OpenAPI:** `Encerra imediatamente uma sessão de impersonation (Shadow Mode) ativa`
- **Autenticação:** Requer credenciais de operador corporativo (SuperAdmin / Suporte Nível 1).

#### Exemplo de Payload de Entrada (`TerminateImpersonationApiRequest`):
```json
{
  "reason": "Atendimento técnico concluído e reprodução de erro finalizada com sucesso."
}
```

#### Exemplo de Resposta de Sucesso (HTTP 200 OK):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": "",
    "description": "",
    "type": 0
  }
}
```

#### Exemplo de Resposta de Sessão Não Encontrada (HTTP 404 NotFound):
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Impersonation.SessionNotFound",
    "description": "The requested impersonation session was not found.",
    "type": 2
  }
}
```

---

## 3. Especificação das Claims do Token JWT

O token contextual emitido contém as seguintes claims estruturadas:

| Claim | Nome Técnico (`ImpersonationClaims`) | Tipo / Formato | Descrição |
| :--- | :--- | :--- | :--- |
| `sub` | `JwtRegisteredClaimNames.Sub` | `Guid (string)` | Identificador do SuperAdmin solicitante. |
| `is_impersonated` | `ImpersonationClaims.IsImpersonated` | `"true"` | Flag indicando sessão ativa de Shadow Mode. |
| `original_superadmin_id` | `ImpersonationClaims.OriginalSuperAdminId` | `Guid (string)` | Identificador imutável do autor da personificação. |
| `tenant_id` | `ImpersonationClaims.TenantId` | `Guid (string)` | Identificador do Tenant alvo da sessão. |
| `support_ticket` | `ImpersonationClaims.SupportTicketId` | `string` | Número do ticket de suporte referenciado. |
| `impersonation_session_id` | `ImpersonationClaims.SessionId` | `Guid (string)` | Identificador da sessão persistida no banco central. |
| `name` | `ClaimTypes.Name` | `string` | Razão social do Tenant personificado. |

---

## 4. Política de Mascaramento de Dados Sensíveis (Shadow Mode)

Quando a requisição contém `is_impersonated=true`, o serviço `IBillingDataMasker` intercepta dados confidenciais:

| Tipo de Dado | Valor Original | Valor Mascarado (Shadow Mode) | Regra de Anonimização |
| :--- | :--- | :--- | :--- |
| **Cartão de Crédito** | `4111 2222 3333 4444` | `**** **** **** 4444` | Preserva apenas os 4 últimos dígitos. |
| **CPF** | `123.456.789-00` | `***.***.789-**` | Preserva o bloco central e oculta início e dígito verificador. |
| **CNPJ** | `11.222.333/0001-81` | `**.***.333/****-81` | Preserva radical intermediário e sufixo de controle. |
| **Dados Bancários / PIX** | `Agência 1234 / CC 987654-3` | `Agência **** / CC *****4-3` | Ofusca sequências numéricas superiores a 3 dígitos. |

---

## 5. Auditoria Master Imutável e Tagging de Shadow Mode

Todas as operações e comandos executados sob o contexto de Shadow Mode são automaticamente persistidos de forma indelével na tabela `MasterAuditLogs` do Catálogo Master:

### 5.1 Esquema da Entidade `MasterAuditEntry`
- **Tabela:** `MasterAuditLogs`
- **Imutabilidade:** Estritamente *append-only* (sem suporte a comandos de `UPDATE` ou `DELETE` no domínio).
- **Campos Principais:**
  - `Id` (Guid PK): Identificador único da linha de auditoria.
  - `TenantId` (Guid?): Identificador do tenant sob operação.
  - `Action` (string, max 150): Nome da ação executada (ex.: `"Workspace.UpdateBudgetLimit"`, `"Impersonation.Terminated"`).
  - `Resource` (string, max 100): Tipo do recurso afetado (ex.: `"Workspace"`, `"Campaign"`, `"ImpersonationSession"`).
  - `ResourceId` (string?, max 200): Identificador do recurso manipulado.
  - `Details` (string?, max 4000): Justificativa técnica, sumário ou payload informativo.
  - `IsImpersonated` (bool): Flag booleana indicando intervenção sob Shadow Mode.
  - `SuperAdminId` (Guid?): Identificador do SuperAdmin executor da intervenção.
  - `SupportTicketId` (string?, max 50): Código do ticket do chamado de suporte associado.
  - `ImpersonationSessionId` (Guid?): Identificador da sessão em `ImpersonationSessions`.
  - `IpAddress` (string?, max 45): IP de origem da requisição.
  - `CreatedAtUtc` (datetime2): Timestamp UTC de auditoria.
  - `Tags` (string JSON array, max 2000): Tags associadas ao evento.

### 5.2 Tag Obrigatória: `performed_by_superadmin`
Sempre que `IsImpersonated == true`:
- A validação de domínio em `MasterAuditEntry.Record(...)` exige obrigatoriamente `SuperAdminId`, `SupportTicketId` e `ImpersonationSessionId`.
- A tag `performed_by_superadmin` (`MasterAuditTags.PerformedBySuperadmin`) é inserida na coleção `Tags`.
- O interceptador `AuditImpersonationBehavior<TRequest, TResponse>` no pipeline do MediatR e o serviço `IMasterAuditService` garantem a captura automática e transparente para todos os módulos sem acoplamento.

---

## 6. Sinalização Visual no Frontend com `ImpersonationBanner.razor`

Para prevenir erros de intervenção inadvertida em contas reais de clientes e assegurar total transparência durante atendimentos:

### 6.1 Componente e Comportamento
- **Localização:** `src/Frontend/WebApp/Components/Shared/ImpersonationBanner.razor`.
- **Posicionamento:** Fixado de forma proeminente no topo do shell principal (`MainLayout.razor`), acima do cabeçalho institucional (`AppHeader`).
- **Design de Segurança:**
  - Fundo contrastante escuro/âmbar (`#1c1917` a `#291807`) com borda inferior em ouro/âmbar (`#f59e0b`).
  - Indicador pulsante em vermelho (`pulse-ring`) sinalizando auditoria ativa em tempo real.
  - Badge em destaque: `"🛡️ MODO SHADOW ATIVO (ACESSO AUDITADO)"`.
  - Exibição do número do chamado (`SupportTicketId`), ID do operador e tempo restante até a expiração automática.
- **Botão de Encerramento Imediato:**
  - Botão `"Encerrar Sessão"` com indicador de progresso (spinner) e prevenção de múltiplos cliques (`disabled`).
  - Dispara a chamada para `IImpersonationClientService.TerminateSessionAsync` que aciona a revogação no backend.
  - Notifica `IImpersonationStateProvider` limpando a sessão e retornando o frontend ao estado corporativo normal.

---

## 7. Casos de Borda e Erros Mapeados

- **Tenant Suspenso:** Se o tenant estiver inativo ou bloqueado por inadimplência, a emissão retorna `Impersonation.TenantInactive`.
- **Token Expirado:** Chamadas que utilizem o token após a expiração são rejeitadas com erro tipado `ImpersonationToken.Expired`.
- **Token Adulterado:** Se a assinatura ou claims forem modificadas, o serviço de validação rejeita com `ImpersonationToken.Invalid`.
- **Revogação de Sessão:** A entidade `ImpersonationSession` permite o encerramento imediato do acesso via comando `TerminateImpersonationSessionCommand`.
- **Tentativa de Encerramento Repetido:** Se a sessão já estiver revogada, a API retorna erro tipado `ImpersonationErrors.SessionRevoked`.
