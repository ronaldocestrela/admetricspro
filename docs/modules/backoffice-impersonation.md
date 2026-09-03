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

## 5. Casos de Borda e Erros Mapeados

- **Tenant Suspenso:** Se o tenant estiver inativo ou bloqueado por inadimplência, a emissão retorna `Impersonation.TenantInactive`.
- **Token Expirado:** Chamadas que utilizem o token após a expiração são rejeitadas com erro tipado `ImpersonationToken.Expired`.
- **Token Adulterado:** Se a assinatura ou claims forem modificadas, o serviço de validação rejeita com `ImpersonationToken.Invalid`.
- **Revogação de Sessão:** A entidade `ImpersonationSession` permite o encerramento imediato do acesso pelo administrador via comando de revogação.
