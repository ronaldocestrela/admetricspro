# Especificação de Módulo: Autenticação Corporativa & Backoffice Dedicado

Este documento descreve a arquitetura, segurança e fluxos do subsistema de autenticação do console executivo **Backoffice**, isolado como aplicação autônoma (`src/Frontend/BackofficeApp`) e integrado ao **ASP.NET Core Identity Framework** persistido no banco central `MasterDb`, em conformidade com as diretrizes do [AGENTS.md](file:///home/rony/LPR/AdMetricsPro/AGENTS.md).

---

## 1. Visão Geral e Princípios de Isolamento

O Backoffice opera exclusivamente sobre o banco de catálogo central (`MasterDb`), sendo física e logicamente desacoplado dos bancos de dados individuais de cada tenant.

1. **Aplicação Dedicada (`BackofficeApp`):** Porta própria (HTTPS 7002 / HTTP 5002), layout executivo corporativo dark mode e sessão isolada com cookies `SameSite=Lax`, `HttpOnly=true` e `SecurePolicy=SameAsRequest`.
2. **Identity Framework no MasterDb:** Tabelas gerenciadas pelo EF Core (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.) com extensão customizada para os operadores do catálogo (`MasterUser` e `MasterRole`).
3. **Seed Idempotente via .env:** O provisionamento das credenciais do primeiro Super Administrador é executado no startup a partir das variáveis `SuperAdmin__Email`, `SuperAdmin__Password`, `SuperAdmin__FullName` e `SuperAdmin__Role`.
4. **Pattern Result<T> Estrito:** Todas as validações e comandos de login retornam tipos `Result<T>`, sem controle de fluxo baseado em exceptions.
5. **Auditoria Imutável:** Todas as tentativas de login (sucesso ou falha) e seeds de sistema são gravados na tabela `MasterAuditLogs` através de `IMasterAuditService.RecordAsync`.

---

## 2. Papéis e Controle de Acesso (RBAC)

| Papel (Role) | Descrição | Permissões no Backoffice |
| :--- | :--- | :--- |
| `SuperAdmin` | Administrador Global | Acesso irrestrito: Diretório 360 de Tenants, Planos, Saúde de APIs, Feature Flags/Kill Switches e Shadow Mode. |
| `SupportTechnician` | Técnico de Suporte | Monitor de Saúde de APIs, Rate Limits e Impersonation com justificativa obrigatória. Sem permissão de mutação de planos ou desligamento de kill switches globais. |

---

## 3. Endpoints e Contratos de Comunicação

### 3.1 Autenticação de Operador (`POST /api/auth/login`)

#### Payload de Entrada (JSON ou Form-Data)
```json
{
  "email": "admin@admetricspro.internal",
  "password": "SuperAdmin@Secure2026!",
  "rememberMe": true
}
```

#### Resposta de Sucesso (HTTP 200)
```json
{
  "success": true,
  "user": {
    "id": "c1f7a2d8-4b92-4f3e-97bb-23a5e18ef901",
    "email": "admin@admetricspro.internal",
    "fullName": "Administrador Global AdMetricsPro",
    "roles": [
      "SuperAdmin"
    ],
    "lastLoginAtUtc": "2026-09-04T01:10:00.0000000Z"
  },
  "redirectUrl": "/tenants"
}
```

#### Resposta de Falha de Negócio (HTTP 400 - Result Pattern)
```json
{
  "code": "Auth.InvalidCredentials",
  "error": "E-mail ou senha incorretos."
}
```

---

## 4. Variáveis de Ambiente (.env)

```env
# ------------------------------------------------------------------------------
# 5. CREDENCIAIS DO SUPER ADMIN DO BACKOFFICE (IDENTITY SEED)
# ------------------------------------------------------------------------------
SuperAdmin__Email="admin@admetricspro.internal"
SuperAdmin__Password="SuperAdmin@Secure2026!"
SuperAdmin__FullName="Administrador Global AdMetricsPro"
SuperAdmin__Role="SuperAdmin"
```

---

## 5. Casos de Borda e Segurança

1. **Tentativas de Força Bruta (Brute-Force Lockout):** Após 5 tentativas consecutivas com senha inválida, a conta do operador é temporariamente bloqueada por 15 minutos via `LockoutOptions` nativo do Identity.
2. **Conta Desativada:** Operadores com flag `IsActive = false` recebem erro `Auth.AccountInactive` e são bloqueados imediatamente, mesmo se a senha for válida.
3. **Expiração Deslizante (Sliding Expiration):** Sessões ativas são renovadas automaticamente a cada interação, expirando após 8 horas de inatividade total.
